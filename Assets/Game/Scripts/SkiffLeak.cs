using System.Collections;
using UnityEngine;

namespace PortBoss.M1
{
    // Leaks (gdd systems §1.6.4): in a storm Biscuit can spring a leak (seeded, at most once per storm, Sky.LeakAt: the
    // forecast's storm, or one held with F11 / -pb-weather storm, timed from the hold).
    // Water rises inside (visible), she slows by up to 30% and lists 5°. Patch: hold E at the hole (10 s). Bail: hold E
    // anywhere else on deck while there's water. She never sinks: at max water the engine coughs at 30% and Dot offers a tow.
    // The real copy (the driver's, else the host's) owns the water; it rides in the boat's pose; a crewmate's scoop or patch
    // goes to the owner like the anchor does (Coop.Ask).
    // ponytail: no patch-kit item yet (Walt's kit lives aboard, unlimited) and no 6 s two-player patch; both with the shops (M5).
    // ponytail: Dot's tow is only offered (a toast); the tow itself comes with the phone (M4).
    public partial class Skiff
    {
        public const float FillTime = 150, Scoop = 0.05f, ScoopEvery = 1.1f, PatchTime = 10;
        public float Water;                       // 0..1 inside the hull
        public bool Leaking;
        public Vector3 HoleLocal = new(0.35f, 0.1f, 0.5f);   // (midships: clear of the tiller's and the anchor's prompts)
        public float SpeedMul => Water >= 0.999f ? 0.3f : 1 - 0.3f * Water;
        public float List => 5f * Water;

        double leakDone = double.NaN; bool towSaid, patchSent; float holdT; Transform waterQ; Material waterM; bool deckFound;

        public Vector3 Hole => transform.TransformPoint(HoleLocal);

        // the real copy, every frame
        void LeakTick(float dt)
        {
            if (Sky.W != null && !Leaking && !Moored)
            {
                var (d, b) = Weather.BlockAt(Sky.Minutes);
                double at = Sky.LeakAt(d, b);
                if (!double.IsNaN(at) && Sky.Minutes >= at && leakDone != at) { leakDone = at; SpringLeak(); }
            }
            if (Leaking) Water = Mathf.Min(1, Water + dt / FillTime);
            if (Water >= 1 && !towSaid) { towSaid = true; Alerts.Push("Dot", "You're riding low, kid. Bail her out, or I'll tow you in.", Alerts.Kind.Warning, "low"); }
            if (Water < 0.5f && towSaid) { towSaid = false; Alerts.Push("Dot", "That's better. She's riding high again.", Alerts.Kind.Info, ends: "low"); }
        }

        public void SpringLeak()
        {
            if (Leaking) return;
            Leaking = true; FindDeck(); LeakFx.Ensure(this);
            Alerts.Push("Biscuit", "She's sprung a leak! Patch the hole, or bail.", Alerts.Kind.Warning, "leak");
            Snd.Play(M1Assets.I.Thud, Hole, 0.9f); CameraRig.Shake(0.2f);
            Debug.Log($"[PB] leak: sprang at {DayTime.Clock(Sky.Minutes)}");
        }

        public void ScoopOut() { Water = Mathf.Max(0, Water - Scoop); }
        public void Patch() { if (!Leaking) return; Leaking = false; Alerts.Push("Biscuit", "Hole patched. Now bail her dry.", Alerts.Kind.Info, ends: "leak"); Snd.Play(M1Assets.I.Thud, Hole, 0.7f, 1.3f); Debug.Log("[PB] leak: patched"); }

        // the hold-E prompt for the walker on deck (Angler.Interact); null = nothing to do here
        public string LeakPrompt(Angler me)
        {
            if (!Leaking) patchSent = false;
            if (!me.OnBoat || me.AtHelm || (!Leaking && Water < 0.02f)) { holdT = 0; return null; }
            bool atHole = Leaking && Vector3.Distance(me.transform.position, Hole) < 1.1f;
            float need = atHole ? PatchTime : ScoopEvery;
            if (!Ctl.InteractHeld) { holdT = 0; patchSent = false; return atHole ? "Hold to patch the leak" : "Hold to bail"; }
            holdT = Mathf.Min(holdT + Time.deltaTime, need);
            if (atHole) me.RepairT = holdT;                       // M3.1: down at the hole with the hammer (everyone sees it)
            if (holdT >= need)
            {
                // patched: once (a crewmate's ask goes to the boat's owner; the kneel holds till her answer comes back)
                if (atHole) { if (!patchSent) { patchSent = true; if (Remote) Coop.Request(Coop.Ask.Patch); else Patch(); } }
                else { holdT = 0; if (Remote) Coop.Request(Coop.Ask.Scoop); else ScoopOut(); Snd.Play(M1Assets.I.Splash, transform.position, 0.25f); }
            }
            return atHole ? (patchSent ? "Patched!" : $"Patching… {Mathf.CeilToInt(PatchTime - holdT)}") : "Bailing…";
        }

        // the hole sits on the deck: find the deck under it once (the boat's own collider)
        void FindDeck()
        {
            if (deckFound) return; deckFound = true;
            var from = transform.TransformPoint(new Vector3(HoleLocal.x, 1.5f, HoleLocal.z));
            float best = float.MaxValue;
            foreach (var h in Physics.RaycastAll(from, -transform.up, 3f, ~0, QueryTriggerInteraction.Ignore))
                if (h.transform.IsChildOf(transform) && h.distance < best) { best = h.distance; HoleLocal.y = transform.InverseTransformPoint(h.point).y; }
        }

        // ---- rogue waves (systems §1.6.5): Storm only, seeded (Weather.Rogues), so every machine knows when from the clock
        // alone. 3 s ahead: a rising roar and a warning. The hit: the boat lurches (the real copy), and each machine's own
        // player, unless braced (hold C), is knocked down; near a rail, one in four goes over the side.
        // ponytail: no white crest on the sea yet, and loose deck items don't slide; add with the storm art pass.
        public const float RogueWarn = 3;
        double rogueWarned = double.NaN, rogueHit = double.NaN, forcedRogue = double.NaN;
        public static System.Action<bool, bool> OnRogue;          // (knocked, overboard): for the tests
        public static bool TestOver;                               // the tests: near a rail always goes over

        public void RogueNow() { forcedRogue = Sky.Minutes + RogueWarn; }   // the tests: one in 3 s

        void RogueTick()
        {
            if (Sky.W == null || Moored) return;
            var (d, b) = Weather.BlockAt(Sky.Minutes);
            foreach (var at in Sky.Rogues(d, b)) Rogue(at);
            if (!double.IsNaN(forcedRogue)) Rogue(forcedRogue);
        }

        // the telegraph's white crest (systems §1.6.5): a line of foam that rises out of the sea to starboard and runs in
        // over the 3 s warning, breaking on her side as it hits
        IEnumerator Crest(double at)
        {
            var root = new GameObject("rogue.crest").transform; var mat = Util.Soft(new Color(1, 1, 1, 0));
            var qs = new Transform[11];
            for (int i = 0; i < qs.Length; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>());
                var r = q.GetComponent<Renderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                q.transform.SetParent(root, false); qs[i] = q.transform;
            }
            for (double lead = at - Sky.Minutes; lead > -0.5; lead = at - Sky.Minutes)
            {
                float k = Mathf.Clamp01(1 - (float)lead / RogueWarn), fade = Mathf.Clamp01(k * 3) * Mathf.Clamp01(1 + (float)lead / 0.5f);
                var cam = Camera.main; var fwd = transform.forward; fwd.y = 0; fwd.Normalize(); var side = Vector3.Cross(Vector3.up, fwd);
                var c = transform.position + side * Mathf.Lerp(26, 1.2f, k);
                for (int i = 0; i < qs.Length; i++)
                {
                    float o = (i - qs.Length / 2) * 1.3f, w = 1 - Mathf.Abs(o) / 9f;   // (taller in the middle: one wave, not a wall)
                    var p = c + fwd * o - side * (o * o * 0.04f); p.y = Sea.Height(p) + Mathf.Lerp(0.2f, 1.1f, k * k) * w;
                    qs[i].position = p; qs[i].localScale = new Vector3(2.2f, Mathf.Lerp(0.4f, 1.3f, k) * (0.6f + 0.4f * w), 1);
                    if (cam != null) { var d = p - cam.transform.position; d.y = 0; if (d.sqrMagnitude > 0.01f) qs[i].rotation = Quaternion.LookRotation(d); }
                }
                mat.SetColor("_BaseColor", new Color(1, 1, 1, 0.9f * fade));
                yield return null;
            }
            Destroy(root.gameObject); Destroy(mat);
        }

        // near a rail = near the gunwale (the rail objects' origins sit mid-boat): a hand on it (systems §1.6.5)
        public bool NearRail(Vector3 at) { var lp = transform.InverseTransformPoint(at); return Mathf.Abs(lp.x) > Hull.extents.x - 0.5f || Mathf.Abs(lp.z) > Hull.extents.z - 0.6f; }

        // footing (systems §1.6.5): a deck tilted over 14° slides you slowly to the low side, unless you're at a rail or braced
        public Vector3 Slide(Vector3 at)
        {
            float tilt = Vector3.Angle(transform.up, Vector3.up);
            if (tilt <= 14 || NearRail(at)) return Vector3.zero;
            var down = Vector3.ProjectOnPlane(Vector3.down, transform.up); down.y = 0;
            return down.normalized * 0.7f * Mathf.Clamp01((tilt - 14) / 8);
        }
        public static bool Wet => Sky.I != null && Sky.I.Wx_.Rain > 0.3f;   // rain or storm spray: a wet deck (stops +0.15 s)

        void Rogue(double at)
        {
            double lead = at - Sky.Minutes;                          // in-game minutes = real seconds
            if (lead <= RogueWarn && lead > 0 && rogueWarned != at)
            {
                rogueWarned = at;
                var me = Angler.I;
                if (me != null && me.OnBoat) Alerts.Push("Biscuit", "Rogue wave! Hold C to brace!", Alerts.Kind.Warning);
                Snd.Play(Synth.Roar, transform.position + transform.right * 6, 1f);
                StartCoroutine(Crest(at));
            }
            if (lead > 0 || lead < -1 || rogueHit == at) return;
            rogueHit = at; if (at == forcedRogue) forcedRogue = double.NaN;
            if (!Remote) vr += 38f;                                  // the lurch (riders see it through the pose)
            Snd.Play(M1Assets.I.Splash, transform.position + transform.right * 1.5f, 1f, 0.7f); CameraRig.Shake(0.5f);
            PortBoss.M1.Fx.Splash(transform.position + transform.right * 1.1f + Vector3.up * 0.4f, 3.5f, 40); // (the crest breaks over her side)
            var p = Angler.I;
            if (p == null || !p.OnBoat || p.Swimming || p.Down) return;
            if (p.Braced) { Debug.Log("[PB] rogue: braced, safe"); OnRogue?.Invoke(false, false); return; }
            bool rail = NearRail(p.transform.position);
            bool over = rail && (TestOver || Weather.U(Sky.Seed, (int)at, 900 + (int)Coop.Me) < 0.25);
            var side = -transform.right;                               // the wave hits from starboard: you go to port
            p.Knock(over ? side * 4.5f + Vector3.up * 3.5f : side * 1.6f + Vector3.up * 1f);
            Debug.Log($"[PB] rogue: knocked down{(over ? ", over the side" : "")} (near a rail: {rail})");
            OnRogue?.Invoke(true, over);
        }

        // every copy: the water inside, a sheet of sea that rises up to 22 cm over the deck. Shallow, it's a narrow, clear
        // puddle down the keel line; deep, a darker sheet wall to wall; it sloshes against her roll. Under ~1 cm: dry.
        void WaterVisual()
        {
            if (Water < 0.04f) { if (waterQ != null) waterQ.gameObject.SetActive(false); return; }
            if (waterQ == null)
            {
                FindDeck();
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "bilge"; Destroy(q.GetComponent<Collider>());
                q.GetComponent<Renderer>().sharedMaterial = waterM = Util.Soft(new Color(0.36f, 0.8f, 0.82f, 0.8f), Texture2D.whiteTexture);
                q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                waterQ = q.transform; waterQ.SetParent(transform, false);
            }
            waterQ.gameObject.SetActive(true);
            float k = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.04f, 0.6f, Water));
            float roll = Mathf.DeltaAngle(0, transform.localEulerAngles.z);
            waterQ.localRotation = Quaternion.Euler(90, 0, -roll * 0.35f);                 // (it stays a little more level than the hull)
            waterQ.localScale = new Vector3(Mathf.Lerp(0.55f, 1.3f, k), Mathf.Lerp(2.4f, 3.2f, k), 1);
            waterQ.localPosition = new Vector3(-roll * 0.012f * (1 - k), HoleLocal.y + 0.02f + 0.2f * Water, -0.3f); // (a puddle runs to the low side)
            waterM.SetColor("_BaseColor", Color.Lerp(new Color(0.55f, 0.86f, 0.86f, 0.35f), new Color(0.2f, 0.56f, 0.64f, 0.78f), k));
        }
    }
}
