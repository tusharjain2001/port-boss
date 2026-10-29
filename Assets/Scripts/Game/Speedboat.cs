using UnityEngine;

namespace PortBoss
{
    // The rival's speedboat. Every few minutes Marlowe roars over from his port, swings in hard right off the end of
    // our jetty (a wall of spray over anyone standing there), sits there laughing and pointing with a speech bubble
    // and a silly horn tune, then guns it away (another soaking). His path is a pure function of network time, so
    // every peer draws the same boat with no sync; the host applies the wake (docked boat rocks, loose crates slide).
    public class Speedboat : MonoBehaviour
    {
        public const float Period = 170f, Offset = 70f, Active = 32f;
        static readonly Vector3 Home = new(-20, 0, 215), Approach = new(16, 0, 50), Ctrl = new(-12, 0, 54), Stop = new(0, 0, 38.5f);
        const float ArcAt = 9f, StopAt = 12.5f, TurnAt = 21f, LeaveAt = 23f, Gloat0 = 13f, Gloat1 = 20.5f;

        static readonly string[] Taunts =
        {
            "Cute little pier! Shame if something... SPLASHED it!",
            "HA! Still carrying crates by hand? I've got SEVEN cranes!",
            "Your grandpa's pier? More like grandpa's PUDDLE!",
            "Smell that? That's YOUR fish. Smell this? That's MY money!",
            "Sell it to me, boss! I'll give you... a dollar fifty!",
            "Wave to my freighters on the way out! Oh wait, they're too big to see you.",
        };

        public static Speedboat I;
        public static bool IsActive { get; private set; }
        public static Transform Head { get; private set; }  // Marlowe's head (speech bubble anchor)
        public static string Bubble { get; private set; }   // what he's shouting right now (null: nothing)
        static Vector3 pos;
        static float burst, speed;

        ParticleSystem wake;
        AudioSource engine;
        CharAnim marlowe;
        float lastC = -1;
        Vector3 lastPos;

        public static void Create()
        {
            var g = new GameObject("RivalSpeedboat");
            I = g.AddComponent<Speedboat>();
            var t = g.transform;
            float deck = 0.75f, standZ = -0.4f;
            if (Skin.On && Skin.I.Speedboat != null)
            {
                var b = Art.Place(Skin.I.Speedboat, t, new Vector3(0, -0.35f, 0), 0, 0.42f, true);
                // Stand him in the open part of the boat: probe down along its length onto its own collision mesh and take
                // the lowest floor (the top hits over the cabin are its roof), then drop the colliders.
                Physics.SyncTransforms();
                float bestY = float.MaxValue;
                for (float z = -3f; z <= 3f; z += 0.25f)
                    foreach (var h in Physics.RaycastAll(t.position + new Vector3(0, 6, z), Vector3.down, 12f))
                        if (h.collider.transform.IsChildOf(b.transform) && h.normal.y > 0.8f && h.point.y - t.position.y > -0.3f && h.point.y < bestY) { bestY = h.point.y; deck = h.point.y - t.position.y; standZ = z; }
                Debug.Log($"[PB] speedboat: Marlowe stands at deck {deck:F2}, z {standZ:F2}");
                foreach (var c in b.GetComponentsInChildren<Collider>()) DestroyImmediate(c);
            }
            else Hull(t);
            I.AddMarlowe(t, deck, standZ);
            I.AddWake(t);
        }

        // Own-art hull (used when the Toon Harbor skin is off).
        static void Hull(Transform t)
        {
            Art.Box(t, new Vector3(0, 0.25f, 0), new Vector3(1.9f, 0.7f, 5f), Art.Red, false, "hull", 0.45f);
            Art.Box(t, new Vector3(0, 0.62f, 0), new Vector3(1.95f, 0.06f, 5.05f), Art.Cream, false, "stripe");
            var bow = Art.Box(t, new Vector3(0, 0.25f, 2.7f), new Vector3(1.34f, 0.69f, 1.34f), Art.Red, false, "bow", 0.45f);
            bow.transform.localRotation = Quaternion.Euler(0, 45, 0);
            Art.Box(t, new Vector3(0, 0.95f, 0.4f), new Vector3(1.4f, 0.12f, 1.1f), Art.Hex("#24323d"), false, "windscreen", 0.9f).transform.localRotation = Quaternion.Euler(-30, 0, 0);
            Art.Box(t, new Vector3(0, 0.75f, -2.3f), new Vector3(0.5f, 0.7f, 0.4f), Art.Hex("#2b2b2b"), false, "outboard");
        }

        // Marlowe himself: a pack worker in a white captain's hat, standing behind the wheel.
        void AddMarlowe(Transform t, float deck, float z)
        {
            var prefab = Skin.On ? (Skin.I.Rival != null ? Skin.I.Rival : Skin.I.Buyer) : null;
            if (prefab != null)
            {
                var m = Art.Place(prefab, t, new Vector3(0, deck, z), 0, 1f);
                float h = Art.Bounds(m).size.y;
                if (h > 0.5f) m.transform.localScale = Vector3.one * Mathf.Clamp(1.85f / h, 0.8f, 1.3f);
                m.transform.localRotation = Quaternion.Euler(0, 180, 0); // the pack's people face -Z
                var an = m.GetComponentInChildren<Animator>();
                if (an != null)
                {
                    marlowe = an.gameObject.AddComponent<CharAnim>();
                    marlowe.Init(an, Skin.I);
                    marlowe.Facing = t;
                    Head = an.GetBoneTransform(HumanBodyBones.Head);
                    if (Head != null)
                    {
                        // The captain's hat (bone space: up is the head's up).
                        var hat = new GameObject("hat").transform; hat.SetParent(Head, false); hat.localPosition = new Vector3(0, 0.17f, 0.0f);
                        hat.rotation = t.rotation; hat.localScale = Vector3.one / Head.lossyScale.x;
                        Art.Prim(PrimitiveType.Cylinder, hat, new Vector3(0, 0.06f, 0), Quaternion.identity, new Vector3(0.3f, 0.06f, 0.3f), Art.Hex("#f4f4f0"), false, "crown");
                        Art.Prim(PrimitiveType.Cylinder, hat, new Vector3(0, 0.0f, 0.06f), Quaternion.identity, new Vector3(0.3f, 0.01f, 0.32f), Art.Hex("#1d2433"), false, "peak");
                        Art.Box(hat, new Vector3(0, 0.07f, 0.15f), new Vector3(0.09f, 0.05f, 0.01f), Art.Hex("#e8c24a"), false, "badge");
                    }
                }
            }
            else
            {
                Art.Prim(PrimitiveType.Capsule, t, new Vector3(0, deck + 0.45f, -0.6f), Quaternion.identity, new Vector3(0.55f, 0.55f, 0.5f), Art.Cream, false, "marlowe");
                var head = Art.Prim(PrimitiveType.Sphere, t, new Vector3(0, deck + 1.2f, -0.6f), Quaternion.identity, Vector3.one * 0.34f, Art.Hex("#d6a27f"), false, "marlowe_head");
                Art.Box(t, new Vector3(0, deck + 1.38f, -0.6f), new Vector3(0.4f, 0.12f, 0.42f), Art.Hex("#f2f2ee"), false, "marlowe_hat");
                Head = head.transform;
            }
        }

        void AddWake(Transform t)
        {
            var w = new GameObject("wake").transform;
            w.SetParent(t, false); w.localPosition = new Vector3(0, -0.1f, -2.6f);
            wake = w.gameObject.AddComponent<ParticleSystem>();
            wake.Stop();
            var m = wake.main;
            m.startLifetime = 2.5f; m.startSpeed = 0.6f; m.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            m.startColor = new Color(0.96f, 0.97f, 0.95f, 0.85f); m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 400;
            var e = wake.emission; e.rateOverTime = 70;
            var sh = wake.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(1.6f, 0.05f, 0.4f);
            var sz = wake.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.8f));
            var col = wake.colorOverLifetime; col.enabled = true;
            var grad = new Gradient(); grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0, 1) });
            col.color = grad;
            w.gameObject.GetComponent<ParticleSystemRenderer>().sharedMaterial = Art.Particle;

            engine = gameObject.AddComponent<AudioSource>();
            engine.clip = Sfx.Clip("engine"); engine.loop = true; engine.spatialBlend = 1; engine.minDistance = 8; engine.maxDistance = 140;
            engine.rolloffMode = AudioRolloffMode.Linear; engine.volume = 0;
            if (engine.clip != null) engine.Play();
        }

        static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float u) => (1 - u) * (1 - u) * a + 2 * (1 - u) * u * b + u * u * c;

        // Where the boat is at time t (and which way it faces). Inactive = parked out of sight at home.
        static bool Pose(float t, out Vector3 p, out Vector3 fwd)
        {
            float c = Mathf.Repeat(t - Offset, Period);
            fwd = Vector3.back;
            if (t < Offset || c > Active) { p = Home + Vector3.down * 5; return false; }
            var arcEnd = Stop - Ctrl;
            if (c < ArcAt) { float u = Mathf.SmoothStep(0, 1, c / ArcAt); p = Vector3.Lerp(Home, Approach, u); fwd = Approach - Home; }
            else if (c < StopAt)
            {
                // A hard swing in, braking all the way to a stop off the end of the jetty.
                float u = (c - ArcAt) / (StopAt - ArcAt); u = 1 - (1 - u) * (1 - u);
                p = Bezier(Approach, Ctrl, Stop, u);
                fwd = Bezier(Approach, Ctrl, Stop, Mathf.Min(u + 0.02f, 1f)) - Bezier(Approach, Ctrl, Stop, Mathf.Max(u - 0.02f, 0f));
            }
            else if (c < TurnAt) { p = Stop; fwd = Vector3.Slerp(arcEnd.normalized, Vector3.back, Mathf.SmoothStep(0, 1, (c - StopAt) / 1.2f)); }
            else if (c < LeaveAt) { p = Stop; fwd = Quaternion.Euler(0, 180f * Mathf.SmoothStep(0, 1, (c - TurnAt) / (LeaveAt - TurnAt)), 0) * Vector3.back; }
            else { float u = Mathf.SmoothStep(0, 1, (c - LeaveAt) / (Active - LeaveAt)); p = Vector3.Lerp(Stop, Home, u); fwd = Vector3.Lerp(Vector3.forward, Home - Stop, Mathf.Clamp01((c - LeaveAt) * 2f)); }
            return true;
        }

        // 0..1 wake strength felt at a point (host uses it for physics): close to the boat while it's moving fast,
        // and a burst of spray each time he swings in or guns it away.
        public static float WakeAt(Vector3 at)
        {
            if (!IsActive) return 0;
            var d = at - pos; d.y = 0;
            return Mathf.Clamp01(1.2f - d.magnitude / 22f) * Mathf.Max(Mathf.Clamp01(speed / 6f), burst);
        }

        void Update()
        {
            float t = Water.T;
            IsActive = Pose(t, out var p, out var fwd);
            var y = Water.Height(p.x, p.z, t);
            pos = new Vector3(p.x, y, p.z);
            fwd.y = 0;
            var rot = Quaternion.LookRotation(fwd.sqrMagnitude > 1e-4f ? fwd : Vector3.back);
            float c = Mathf.Repeat(t - Offset, Period);
            // Nose up at speed, lean hard into the swing; rock when sitting still.
            float bank = c > ArcAt && c < StopAt ? 22f : 0f, pitch = c < StopAt || c > LeaveAt ? -6f : Mathf.Sin(t * 2.2f) * 2.5f;
            transform.SetPositionAndRotation(pos + Vector3.up * 0.1f, rot * Quaternion.Euler(pitch, 0, bank));
            if (Time.deltaTime > 0) speed = Mathf.Lerp(speed, (transform.position - lastPos).magnitude / Time.deltaTime, 0.2f);
            lastPos = transform.position;
            burst = Mathf.MoveTowards(burst, 0, Time.deltaTime / 1.5f);
            bool moving = IsActive && speed > 2f;
            if (moving && !wake.isPlaying) wake.Play();
            if (!moving && wake.isPlaying) wake.Stop();
            if (engine != null) engine.volume = Mathf.MoveTowards(engine.volume, IsActive ? (moving ? 0.55f : 0.22f) * Settings.Sfx : 0, Time.deltaTime);

            // The show (same moments on every peer).
            bool Crossed(float at) => IsActive && lastC >= 0 && lastC < at && c >= at && c - lastC < 2f;
            if (Crossed(0.05f)) Arrived?.Invoke();
            if (Crossed(StopAt)) Soak(Vector3.back);
            if (Crossed(TurnAt + 1.2f)) Soak(Vector3.back);
            if (Crossed(Gloat0))
            {
                Sfx.Play("tauntHorn", pos + Vector3.up, 1f);
                int cycle = Mathf.FloorToInt((t - Offset) / Period);
                Bubble = Taunts[Mathf.Abs(cycle) % Taunts.Length];
                // Too far away to see him: hear about it on the radio instead.
                var me = Player.Local;
                if (me != null && Vector3.Distance(me.transform.position, Stop) > 50f) GameUI.Shout("RIVAL MARLOWE", Bubble, 4f);
            }
            if (!IsActive || c >= Gloat1) Bubble = null;
            lastC = IsActive ? c : -1;

            // Marlowe: laughing and pointing at whoever is closest while he's stopped.
            if (marlowe != null)
            {
                bool gloat = IsActive && c >= Gloat0 && c < Gloat1;
                marlowe.Laugh = Mathf.MoveTowards(marlowe.Laugh, gloat ? 1f : 0f, Time.deltaTime * 3f);
                Vector3 target = Stop + Vector3.back * 8f + Vector3.up;
                float best = 40f;
                foreach (var pl in Player.All)
                    if (pl != null) { float d = Vector3.Distance(pl.transform.position, Stop); if (d < best) { best = d; target = pl.transform.position + Vector3.up * 1.4f; } }
                marlowe.PointAt = target;
            }
        }

        // A wall of spray thrown towards the jetty: everyone standing near its end gets drenched; loose crates slide.
        void Soak(Vector3 dir)
        {
            burst = 1f;
            Fx.Spray(pos + dir * 1.5f + Vector3.up * 0.3f, dir, 2.2f);
            Sfx.Play("splash", pos, 1f, 0.75f);
            Sfx.Play("splash", pos + dir * 4f, 0.8f, 1.1f);
            var me = Player.Local;
            if (me == null || me.Swimming) return;
            var d = me.transform.position - Stop; d.y = 0;
            if (d.magnitude < 13f && me.transform.position.y > Water.Level + 0.5f)
            {
                GameUI.Soak();
                Stats.Add("soaked");
                GameUI.Toast(PortGame.Pick("SOAKED! Marlowe is laughing his hat off.", "SPLOOSH! Right in the face.", "Drenched by Marlowe. Again."));
            }
        }

        public static event System.Action Arrived;
    }
}
