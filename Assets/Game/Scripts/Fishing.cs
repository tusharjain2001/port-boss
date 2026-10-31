using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    // Fishing, F1–F6 of gdd/fishing §8, as one state machine on the player. Every timing is a number below (fishing §3).
    //   Stowed ─(rod slot)→ Drawing → Ready ─hold LMB→ Charging ─release→ Flight → Settle → Waiting (nibbles)
    //   → Bite (1.2 s strike window) ─strike→ [Heavy? ask] → Minigame (3 stops) → Reel → Reveal → Ready
    //   strike early = "Too soon!", late = "It swam off." (bait kept, Recover → Ready)
    public class Fishing : MonoBehaviour
    {
        public enum St { Stowed, Drawing, Ready, Charging, Flight, Settle, Waiting, Bite, Strike, Heavy, Minigame, Reel, Reveal, Recover }
        public St State = St.Stowed;
        public float StateT;

        // --- tunables (fishing §3) ---
        public const float DrawT = 0.35f, StowT = 0.30f, ChargePeriod = 1.10f, Sweet = 0.92f, SettleT = 0.6f, StrikeWindow = 1.20f,
            Freeze = 0.45f, RevealMin = 0.5f, RevealAuto = 6f, CancelHold = 0.3f, WalkAway = 2f, DragSpeed = 2f, DragT = 1.5f;

        // --- gear (debug keys F5 / F6; M1 has no shop) ---
        public int RodTier = 1;
        public Rod Rod => FishDb.Rods[RodTier - 1];
        public int BaitI;
        public readonly int[] BaitCount = { 99, 99, 99, 99, 99 };
        public string BaitId => BaitCount[BaitI] > 0 ? FishDb.Baits[BaitI].Id : "None";

        // --- live state the HUD reads ---
        public float Power; public bool InSweet; public Vector3 Marker; public bool MarkerSnapped; public float MarkerDist;
        public Vector3 CastFrom;
        public FishSpecies Hooked; public Fish Catch; public float BiteSeenAt, BiteAt, BiteWait, WaitStart; public int Nibbles, NibblesDone;
        readonly List<float> nibbleAt = new();
        public int Stop, Hits, Misses; public readonly int[] Result = new int[3]; // 0 pending, 1 hit, -1 miss
        public readonly float[] ErrMs = new float[3];
        public float ZoneC, ZoneW, PtrSpeed; double sweepT0, freezeUntil; public float PointerShown; public bool Frozen;
        public bool HeavyFight; public bool Snapped;
        public bool IsNew, IsBest; public int TargetSlot;
        public float ReelLen;
        public string LastOutcome;
        public bool Locked => State >= St.Charging && State != St.Recover;      // no slot switching
        public bool Holding => State != St.Stowed;
        public bool BiteIcon;  // accessibility "!" (off by default)

        readonly HashSet<string> seen = new(); readonly Dictionary<string, float> best = new();
        public static System.Random Rng = new();

        Angler me; RodView rod; LineView line; BobberView bob; GameObject fishGo; Transform markerT; Renderer markerR;
        float elev = 35, elevV, yawOff, rodK = 1; Vector3 butt, buttV; float creakVol; AudioSource creak, reelSnd;
        float cancelHeld, dragT; Vector3 fishUnder; float fightPh;
        public System.Action<string> OnLog; // test hooks

        void Start()
        {
            me = GetComponent<Angler>();
            var toon = Game.ToonMat;
            rod = RodView.Make(toon, RodTier); line = LineView.Make(); line.A = rod.Tip; bob = BobberView.Make(toon);
            markerT = Hud.MakeMarker(); markerR = markerT.GetComponent<Renderer>(); markerT.gameObject.SetActive(false);
            creak = gameObject.AddComponent<AudioSource>(); creak.clip = Synth.Creak; creak.loop = true; creak.volume = 0; creak.Play();
            reelSnd = gameObject.AddComponent<AudioSource>(); reelSnd.clip = Synth.Reel; reelSnd.loop = true; reelSnd.volume = 0; reelSnd.Play();
            butt = RestButt; PlaceRod(0, true);
        }

        void Go(St s) { State = s; StateT = 0; OnLog?.Invoke("state " + s); }

        public void SetRod(int tier)
        {
            RodTier = Mathf.Clamp(tier, 1, 5);
            Destroy(rod.gameObject); rod = RodView.Make(Game.ToonMat, RodTier); line.A = rod.Tip;
            Alerts.Push("Debug", $"Rod: {Rod.Name} (bites {Rod.BiteLo:0}–{Rod.BiteHi:0} s, green ×{Rod.GreenMul:0.00}, line {Rod.LineKg:0} kg)", Alerts.Kind.Info);
        }

        public void Stow() { if (State == St.Ready || State == St.Drawing) Go(St.Stowed); }

        // ---------------------------------------------------------------------------------------------------------
        void Update()
        {
            float dt = Time.deltaTime; StateT += dt;
            bool rodSel = me.Bar.Sel.Rod && !me.Swimming && !me.AtHelm && !me.Climbing;
            DebugKeys();

            switch (State)
            {
                case St.Stowed:
                    if (rodSel && !Ctl.Sprint) { Go(St.Drawing); Snd.Play(Synth.Whoosh, transform.position, 0.25f, 1.6f); Snd.Play(Synth.Tik, transform.position, 0.4f, 0.8f); }
                    break;
                case St.Drawing:
                    if (!rodSel) { Go(St.Stowed); break; }
                    if (StateT >= DrawT) Go(St.Ready);
                    break;
                case St.Ready:
                    if (!rodSel) { Go(St.Stowed); break; }
                    if (Ctl.UsePressed && !me.Busy) { Go(St.Charging); Power = 0; }
                    break;
                case St.Charging: Charging(dt); break;
                case St.Flight: Flight(); break;
                case St.Settle: Settle(); break;
                case St.Waiting: Waiting(dt); break;
                case St.Bite: Bite(); break;
                case St.Strike: if (StateT >= 0.25f) BeginFight(); break;
                case St.Heavy: HeavyAsk(); break;
                case St.Minigame: Minigame(); break;
                case St.Reel: Reel(); break;
                case St.Reveal: Reveal(); break;
                case St.Recover: if (StateT >= 0.6f) { bob.Floating = false; bob.gameObject.SetActive(false); line.Live = false; Go(St.Ready); } else ReelInVisual(StateT / 0.6f); break;
            }

            // cancel / quick reel-in (hold RMB 0.3 s), keeps the bait
            if (State == St.Charging && Ctl.CancelPressed) { Go(St.Ready); }
            if (State is St.Waiting or St.Settle or St.Flight)
            {
                cancelHeld = Ctl.CancelHeld ? cancelHeld + dt : 0;
                if (cancelHeld >= CancelHold) { ReelIn("Reeled in."); cancelHeld = 0; }
                // walked away from the cast, or the boat drags the line
                var from = CastFrom; var d = transform.position - from; d.y = 0;
                if (!me.OnBoat && d.magnitude > WalkAway) ReelIn("Reeled in.");
                var s = Skiff.I;
                if (me.OnBoat && s != null && s.Velocity.magnitude > DragSpeed) { dragT += dt; if (dragT > DragT) { ReelIn("Line dragging: reeled in."); Alerts.Push("Line", "Line dragging: reeled in. Bait's still on.", Alerts.Kind.Info); } }
                else dragT = 0;
            }

            Pose(dt);
            Cam();
            me.Busy = State is St.Strike or St.Heavy or St.Minigame or St.Reel or St.Reveal;
            Snd.TickRumble();
        }

        void DebugKeys()
        {
            if (Ctl.Key_(UnityEngine.InputSystem.Key.F5) && !Locked) SetRod(RodTier % 5 + 1);
            if (Ctl.Key_(UnityEngine.InputSystem.Key.F6) && !Locked) { BaitI = (BaitI + 1) % FishDb.Baits.Length; Alerts.Push("Debug", "Bait: " + FishDb.Baits[BaitI].Name, Alerts.Kind.Info); }
        }

        // ---------------- cast ----------------
        void Charging(float dt)
        {
            float u = StateT / ChargePeriod;
            Power = Mathf.PingPong(u, 1f);
            bool sweet = Power >= Sweet;
            if (sweet && !InSweet) Snd.Play2D(Synth.Ting, 0.35f);
            InSweet = sweet;
            creakVol = 0.35f; creak.pitch = Mathf.Pow(2, 6 * Power / 12f) * 0.9f;
            var cam = CameraRig.I;
            float yaw = cam != null ? cam.Yaw : transform.eulerAngles.y;
            me.FaceYaw = yaw;
            var dir = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            float dist = Rod.RangeMin + (Rod.RangeMax - Rod.RangeMin) * Mathf.Pow(Power, 0.9f);
            Marker = ValidPoint(transform.position, dir, dist, out MarkerSnapped);
            MarkerDist = dist;
            markerT.gameObject.SetActive(true);
            markerT.position = new Vector3(Marker.x, Sea.Height(Marker) + 0.04f, Marker.z);
            markerT.rotation = Quaternion.Euler(90, Time.time * 20f, 0);
            markerR.material.SetColor("_BaseColor", MarkerSnapped ? new Color(0.89f, 0.33f, 0.25f, 0.95f) : sweet ? new Color(0.97f, 0.77f, 0.28f, 1) : new Color(1, 1, 1, 0.9f));
            if (!Ctl.Use)
            {
                // release
                creakVol = 0;
                float sc = sweet ? 0.4f : 1.2f;
                var scatter = new Vector3((float)(Rng.NextDouble() * 2 - 1), 0, (float)(Rng.NextDouble() * 2 - 1)) * sc;
                Marker = ValidPoint(transform.position, dir, dist + scatter.z, out _) + Vector3.Cross(Vector3.up, dir) * scatter.x;
                if (!IsWater(Marker)) Marker = ValidPoint(transform.position, dir, dist, out _);
                markerT.gameObject.SetActive(false);
                CastFrom = transform.position;
                flightT0 = Time.time; flightLen = 0.55f + 0.03f * dist; flightFrom = rod.Tip.position;
                bob.gameObject.SetActive(true); bob.Floating = false; bob.Dip = bob.Yank = 0;
                line.Live = true; line.Slack = 0.02f; line.Arc = true; line.Reset(rod.Tip.position, rod.Tip.position);
                Snd.Play(Synth.Whoosh, transform.position, 0.3f + 0.5f * Power, 1.1f);
                Snd.Play(Synth.Zing, transform.position, 0.35f, Mathf.Lerp(1.2f, 0.8f, Power));
                if (sweet) Fx.Splash(rod.Tip.position, 0.1f, 4); // a little sparkle on a clean cast
                OnLog?.Invoke($"cast power={Power:F2} dist={Vector3.Distance(Flat(transform.position), Flat(Marker)):F1} sweet={sweet}");
                Go(St.Flight);
            }
        }
        float flightT0, flightLen; Vector3 flightFrom;
        static Vector3 Flat(Vector3 v) => new(v.x, 0, v.z);

        // Water point at `dist` along `dir`; land, the boat or water shallower than 0.5 m snap to the nearest valid point
        // (never a dead cast, fishing §3.2/§3.12).
        Vector3 ValidPoint(Vector3 from, Vector3 dir, float dist, out bool snapped)
        {
            var p = from + dir * dist; snapped = false;
            if (IsWater(p)) return p;
            snapped = true;
            for (float k = 0.5f; k < 40f; k += 0.5f)
            {
                var a = from + dir * (dist + k); if (IsWater(a)) return a;
                var b = from + dir * Mathf.Max(1.5f, dist - k); if (IsWater(b) && dist - k > 1.5f) return b;
                foreach (float ang in new[] { 25f, -25f, 50f, -50f })
                {
                    var c = from + Quaternion.Euler(0, ang, 0) * dir * (dist + k * 0.5f); if (IsWater(c)) return c;
                }
            }
            return p;
        }

        static bool IsWater(Vector3 p)
        {
            var s = Skiff.I;
            if (s != null && s.Inside(p, 0.6f)) return false;
            float h = Sea.Height(p);
            if (Physics.Raycast(new Vector3(p.x, 60, p.z), Vector3.down, out var hit, 200, ~0, QueryTriggerInteraction.Ignore)
                && (s == null || !hit.transform.IsChildOf(s.transform)) && hit.collider.GetComponentInParent<Angler>() == null)
                if (hit.point.y > h - 0.5f) return false;
            return true;
        }

        void Flight()
        {
            float u = Mathf.Clamp01((Time.time - flightT0) / flightLen);
            float d = Vector3.Distance(Flat(flightFrom), Flat(Marker));
            float apex = Mathf.Min(6f, 0.25f * d);
            var target = new Vector3(Marker.x, Sea.Height(Marker), Marker.z);
            var p = Vector3.Lerp(flightFrom, target, u) + Vector3.up * apex * 4 * u * (1 - u);
            bob.transform.position = p; bob.transform.rotation = Quaternion.identity; bob.transform.localScale = Vector3.one;
            line.B = p; line.Slack = 0.0f;
            if (u >= 1)
            {
                bob.Pos = target; bob.Floating = true; bob.Dip = 0.08f;
                Fx.Splash(target, 0.6f, 12);
                Snd.Play(d < 10 ? Synth.Plip : Synth.Plunk, target, 0.8f);
                Go(St.Settle);
            }
        }

        void Settle()
        {
            // dunk 8 cm, pop up, settle in 0.6 s (2 bounces)
            float u = StateT / SettleT;
            bob.Dip = 0.08f * Mathf.Exp(-u * 3.5f) * Mathf.Cos(u * Mathf.PI * 4f);
            line.B = bob.transform.position + Vector3.up * 0.08f; line.Slack = Mathf.Lerp(0, 0.05f, u);
            if (u >= 1) { bob.Dip = 0; line.Arc = false; StartWait(); }
        }

        // ---------------- wait, nibbles, bite ----------------
        void StartWait()
        {
            // the rod's bite range (MUST 4–10 s; better rods narrow it to 4–6 s); species rolled now, revealed at the bite
            // (20 ms kept back from the top: the bite shows on the next frame, so it never lands after the rod's top)
            BiteWait = Rod.BiteLo + (Rod.BiteHi - Rod.BiteLo - 0.02f) * (float)Rng.NextDouble();
            WaitStart = Time.time; BiteAt = Time.time + BiteWait;
            Hooked = FishDb.Roll(Rng, RodTier, BaitId);
            int c = (int)Hooked.Class;
            Nibbles = Rng.Next(FishDb.NibbleLo[c], FishDb.NibbleHi[c] + 1); NibblesDone = 0;
            nibbleAt.Clear();
            // ≥ 1.5 s after landing, ≥ 0.6 s before the bite, ≥ 0.5 s apart
            float lo = 1.5f, hi = BiteWait - 0.6f;
            for (int i = 0; i < Nibbles && hi - lo > 0.05f; i++)
            {
                float t = lo + (hi - lo) * (float)Rng.NextDouble(), tries = 0;
                while (nibbleAt.Exists(x => Mathf.Abs(x - t) < 0.5f) && tries++ < 20) t = lo + (hi - lo) * (float)Rng.NextDouble();
                if (!nibbleAt.Exists(x => Mathf.Abs(x - t) < 0.5f)) nibbleAt.Add(t);
            }
            nibbleAt.Sort(); Nibbles = nibbleAt.Count;
            OnLog?.Invoke($"wait {BiteWait:F2} s, {Nibbles} nibbles, species {Hooked.Id}");
            Go(St.Waiting);
        }

        float nibT = -1;
        void Waiting(float dt)
        {
            float t = Time.time - WaitStart;
            line.B = bob.transform.position + Vector3.up * 0.06f; line.Slack = 0.05f;
            bob.Pos += new Vector3(0.06f, 0, -0.04f) * dt; // drift
            if (NibblesDone < nibbleAt.Count && t >= nibbleAt[NibblesDone])
            {
                NibblesDone++; nibT = 0;
                Snd.Play(Synth.Tik, bob.transform.position, 0.5f, Random.Range(0.95f, 1.1f)); Snd.Rumble(0.15f, 0, 0.08f);
                Fx.Ring(bob.transform.position, 0.5f, 0.6f, 0.6f);
                OnLog?.Invoke($"nibble at {t:F2}");
            }
            if (nibT >= 0) { nibT += dt; bob.Dip = Mathf.Sin(Mathf.Clamp01(nibT / 0.15f) * Mathf.PI) * 0.045f; if (nibT > 0.15f) { nibT = -1; bob.Dip = 0; } }
            if (Ctl.Strike) { LastOutcome = "early"; Alerts.Push("Too soon!", "Nothing on the hook yet. The bait's still on.", Alerts.Kind.Info); OnLog?.Invoke("strike early"); Recover(); return; }
            if (Time.time >= BiteAt)
            {
                // the bite: yanked fully under, dragged sideways, BLOOP + 3 ratchet clicks, punch-in
                Go(St.Bite); BiteSeenAt = Time.time;
                Snd.Play(Synth.Bloop, bob.transform.position, 1f); Snd.Play(Synth.Ratchet, rod.Tip.position, 0.7f);
                Fx.Splash(bob.transform.position, 1.1f, 18); Snd.Rumble(0.8f, 0.8f, 0.3f);
                if (CameraRig.I != null) CameraRig.I.FovPunch = 3f;
                OnLog?.Invoke($"bite after {t:F2} s (planned {BiteWait:F2})");
            }
        }

        Vector3 yankDir;
        void Bite()
        {
            if (StateT < 0.05f) yankDir = Quaternion.Euler(0, Random.Range(0, 360f), 0) * Vector3.forward;
            float u = Mathf.Clamp01(StateT / 0.25f);
            bob.Dip = 0.25f * u; bob.Pos += yankDir * 0.4f * Time.deltaTime / 0.25f * (u < 1 ? 1 : 0);
            line.B = bob.transform.position; line.Slack = 0;
            if (Ctl.Strike)
            {
                OnLog?.Invoke($"strike ok {StateT:F2} s into the window");
                Snd.Play(Synth.Zing, rod.Tip.position, 0.5f, 1.3f); Snd.Play(Synth.Whoosh, transform.position, 0.5f, 1.4f);
                Go(St.Strike); return;
            }
            if (StateT > StrikeWindow)
            {
                LastOutcome = "late"; Alerts.Push("It swam off.", "Too slow on the strike. The bait's still on.", Alerts.Kind.Info);
                OnLog?.Invoke("strike late"); bob.Spin = 0; Recover(); return;
            }
        }

        void Recover() { Go(St.Recover); bob.Dip = 0; line.Arc = false; }

        void ReelIn(string why) { LastOutcome = "reeled"; Recover(); }

        void ReelInVisual(float u)
        {
            // the bobber skips back toward the rod tip
            var from = new Vector3(bob.Pos.x, Sea.Height(bob.Pos), bob.Pos.z);
            var p = Vector3.Lerp(from, rod.Tip.position, u * u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.5f;
            bob.Floating = false; bob.transform.position = p; bob.Spin += 900 * Time.deltaTime;
            bob.transform.rotation = Quaternion.Euler(0, bob.Spin, 0);
            line.B = p; line.Slack = 0.01f;
            reelSnd.volume = 0.3f;
        }

        // ---------------- the fight ----------------
        void BeginFight()
        {
            var f = new Fish { Species = Hooked, CaughtAt = Time.time };
            FishDb.RollSize(Rng, Hooked, out f.Cm, out f.Kg);
            Catch = f;
            Hits = Misses = 0; Stop = 0; Snapped = false; HeavyFight = false;
            for (int i = 0; i < 3; i++) { Result[i] = 0; ErrMs[i] = 0; }
            fishUnder = bob.Pos; fightPh = Random.value * 10;
            bob.gameObject.SetActive(false); bob.Floating = false;
            if (f.Kg > Rod.LineKg) { Go(St.Heavy); OnLog?.Invoke($"heavy {f.Kg:F1} kg > line {Rod.LineKg} kg"); return; }
            StartMinigame();
        }

        void HeavyAsk()
        {
            FightLine();
            if (Ctl.Keep || Ctl.Bot && Bot.FightHeavy) { HeavyFight = true; StartMinigame(); return; }
            if (Ctl.LetGo || StateT > 4f || Ctl.Bot && !Bot.FightHeavy && StateT > 0.5f)
            {
                LastOutcome = "letgo"; Alerts.Push("Let it go", "A stronger line would hold it (June's shop, later). Bait's still on.", Alerts.Kind.Info);
                OnLog?.Invoke("heavy: let go"); Recover(); bob.gameObject.SetActive(true); bob.Pos = fishUnder;
            }
        }

        void StartMinigame()
        {
            Go(St.Minigame);
            NextStop(first: true);
        }

        void NextStop(bool first)
        {
            int c = (int)Hooked.Class;
            ZoneW = Mathf.Min(0.6f, FishDb.Green[c] * Rod.GreenMul);
            PtrSpeed = FishDb.Speed[c] * Rod.SpeedMul * new[] { 1.00f, 1.08f, 1.16f }[Mathf.Min(Stop, 2)];
            // stop 1: the green column in the middle (Manan); stops 2–3: 50% ± 8–18% on a random side
            ZoneC = first ? 0.5f : 0.5f + (Rng.Next(2) == 0 ? -1 : 1) * (0.08f + 0.10f * (float)Rng.NextDouble());
            ZoneC = Mathf.Clamp(ZoneC, ZoneW / 2 + 0.02f, 1 - ZoneW / 2 - 0.02f);
            sweepT0 = Time.timeAsDouble; Frozen = false;
        }

        // pointer position (0..1) at game time t (Legend surges; plain ping-pong otherwise)
        public float PointerAt(double t)
        {
            float x = (float)(t - sweepT0);
            float travel = Hooked.Class == FishClass.Legend ? PtrSpeed * (x + 0.25f * 1.3f / (2 * Mathf.PI) * (1 - Mathf.Cos(2 * Mathf.PI * x / 1.3f))) : PtrSpeed * x;
            return Mathf.PingPong(travel, 1f);
        }
        public float ZoneAt(double t)
        {
            if (Hooked.Class != FishClass.Trophy) return ZoneC;
            float x = (float)(t - sweepT0);
            float lo = ZoneW / 2, hi = 1 - ZoneW / 2;
            return lo + Mathf.PingPong(ZoneC - lo + 0.08f * x, hi - lo); // drifts 0.08 bar/s (Dredge-style)
        }
        public float Passes => PtrSpeed * (float)(Time.timeAsDouble - sweepT0);

        void Minigame()
        {
            FightLine();
            double now = Time.timeAsDouble;
            if (Frozen) { if (now >= freezeUntil) { if (Stop >= 3 || Snapped) EndFight(); else NextStop(false); } return; }
            PointerShown = PointerAt(now);
            bool press = Ctl.Strike;
            bool timeout = PtrSpeed * (now - sweepT0) >= 3.0; // 3 full passes, no press = a miss
            if (!press && !timeout) return;
            // judged at the input's own timestamp (+ the calibration offset), not at this frame
            double tg = press ? now - (Time.realtimeSinceStartupAsDouble - Ctl.StrikeTime) * Time.timeScale + Settings.TimingOffsetMs / 1000.0 : now;
            if (Ctl.Bot) tg = PortBoss.M1.Bot.PressAt; // the bot's press time is its "event timestamp"
            float p = timeout ? -1 : PointerAt(tg), z = ZoneAt(tg);
            bool hit = !timeout && Mathf.Abs(p - z) <= ZoneW / 2;
            ErrMs[Stop] = timeout ? 999 : (Mathf.Abs(p - z) - ZoneW / 2) / PtrSpeed * 1000f;
            PointerShown = timeout ? PointerShown : p;
            Result[Stop] = hit ? 1 : -1;
            if (hit)
            {
                Hits++;
                Snd.Play2D(Synth.Ding[Mathf.Clamp(Hits - 1, 0, 2)], 0.7f); Snd.Rumble(0.5f, 0.5f, 0.06f);
                Fx.Splash(fishUnder, 0.4f, 6);
            }
            else
            {
                Misses++;
                Snd.Play(Synth.Miss, fishUnder, 0.6f); Snd.Rumble(0.2f, 0, 0.08f);
                Fx.Splash(fishUnder + Vector3.up * 0.1f, 0.7f, 10); // the fish splashes once; nothing else happens
                if (HeavyFight && Misses >= 2)
                {
                    Snapped = true; Snd.Play2D(Synth.Snap, 0.8f);
                    Alerts.Push("Snap!", $"The line broke. The {Hooked.Name} is gone, and the bait with it.", Alerts.Kind.Warning);
                    BaitCount[BaitI] = Mathf.Max(0, BaitCount[BaitI] - 1);
                }
            }
            OnLog?.Invoke($"stop {Stop + 1}: {(hit ? "hit" : timeout ? "timeout" : "miss")} err {ErrMs[Stop]:F0} ms zone {ZoneW:F2} speed {PtrSpeed:F2}");
            Stop++;
            Frozen = true; freezeUntil = now + Freeze;
            if (Stop >= 3 && Hits == 3) Snd.Play2D(Synth.Fanfare, 0.6f);
        }

        void FightLine()
        {
            // the fish swims under the surface in slow figure-eights; the line goes taut to it
            fightPh += Time.deltaTime;
            var p = bob.Pos + new Vector3(Mathf.Sin(fightPh * 1.3f) * 0.8f, 0, Mathf.Sin(fightPh * 2.1f) * 0.4f);
            fishUnder = new Vector3(p.x, Sea.Height(p) - 0.3f, p.z);
            line.B = fishUnder; line.Slack = 0;
            if (Random.value < Time.deltaTime * 1.5f) Fx.Ring(new Vector3(p.x, Sea.Height(p), p.z), 0.8f, 0.7f, 0.5f);
        }

        void EndFight()
        {
            if (Snapped) { line.Live = false; LastOutcome = "snap"; OnLog?.Invoke("snap"); Go(St.Ready); return; }
            Catch.Grade = Hits;
            BaitCount[BaitI] = Mathf.Max(0, BaitCount[BaitI] - (BaitCount[BaitI] > 0 ? 1 : 0)); // 1 bait per landed fish
            ReelLen = Mathf.Clamp(1.0f + 0.25f * Mathf.Sqrt(Catch.Kg), 1.0f, 3.5f);
            fishGo = FishView.Make(Catch, null);
            fishGo.transform.position = fishUnder;
            FishView.Swim(fishGo, 0.16f, 3f);
            Go(St.Reel);
            OnLog?.Invoke($"landed {Catch.Species.Id} {Catch.Cm:F0} cm {Catch.Kg:F2} kg grade {Ponyo.GradeName[Catch.Grade]}");
        }

        bool jumped;
        void Reel()
        {
            float u = Mathf.Clamp01(StateT / ReelLen);
            reelSnd.volume = 0.35f; reelSnd.pitch = 0.8f + u * 0.8f;
            var hand = HoldPoint();
            var start = fishUnder;
            Vector3 p;
            if (u < 0.75f)
            {
                float k = u / 0.75f;
                var surf = Vector3.Lerp(start, new Vector3(hand.x, 0, hand.z) + (start - hand).normalized * 1.2f, k);
                surf.y = Sea.Height(surf) - 0.25f * (1 - k);
                p = surf;
                // Scarce+ breaks the surface once at 60%
                if (Hooked.Class >= FishClass.Scarce && k > 0.55f && k < 0.75f) { p.y += Mathf.Sin((k - 0.55f) / 0.2f * Mathf.PI) * 0.9f; if (!jumped) { jumped = true; Fx.Splash(p, 0.9f, 14); Snd.Play(Synth.Splash, p, 0.6f); } }
            }
            else
            {
                float k = (u - 0.75f) / 0.25f;
                var from = new Vector3(hand.x, 0, hand.z) + (start - hand).normalized * 1.2f; from.y = Sea.Height(from);
                p = Vector3.Lerp(from, hand, Mathf.SmoothStep(0, 1, k)) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.4f;
                if (k > 0.05f && k < 0.12f && Random.value < 0.5f) Fx.Splash(from, 0.4f, 6);
            }
            fishGo.transform.position = p;
            var to = hand - p; to.y = 0;
            if (to.sqrMagnitude > 0.01f) fishGo.transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
            line.B = p + fishGo.transform.forward * Catch.Cm / 200f; line.Slack = 0;
            if (u >= 1)
            {
                jumped = false; reelSnd.volume = 0;
                Snd.Play(Synth.Slap, p, 0.8f); Snd.Play(M1Assets.I.Splash, p, 0.25f, 1.6f);
                line.Live = false;
                FishView.Swim(fishGo, 0.05f, 1.6f);
                IsNew = seen.Add(Catch.Species.Id);
                IsBest = !IsNew && (!best.TryGetValue(Catch.Species.Id, out var b) || Catch.Kg >= b);
                if (!best.TryGetValue(Catch.Species.Id, out var b2) || Catch.Kg > b2) best[Catch.Species.Id] = Catch.Kg;
                TargetSlot = me.Bar.Target(Catch);
                if (IsNew) Alerts.Push("NEW species", Catch.Species.Name + " · in your journal", Alerts.Kind.Crew);
                AudioClip sting = Catch.Grade switch { 0 => Synth.Pluck, 1 => Synth.Chime, 2 => Synth.Shimmer, _ => Synth.Brass };
                Snd.Play2D(sting, 0.6f);
                if (Catch.Grade == 3) Fx.Splash(HoldPoint(), 0.5f, 16);
                if (CameraRig.I != null)
                {
                    // frame the hold-up from whichever 3/4 side has nothing between the camera and the fish
                    float Clear(float side)
                    {
                        var dir = Quaternion.Euler(6, transform.eulerAngles.y + 180 + side * 32, 0) * Vector3.back;
                        var from = HoldPoint(); var hits = Physics.RaycastAll(from, dir, 3.2f, ~0, QueryTriggerInteraction.Ignore); float best = 3.2f;
                        foreach (var h in hits) if (h.collider.GetComponentInParent<Angler>() == null) best = Mathf.Min(best, h.distance);
                        return best;
                    }
                    CameraRig.I.RevealSide = Clear(1) > Clear(-1) + 0.1f ? 1 : -1;
                }
                Go(St.Reveal);
            }
        }

        Vector3 HoldPoint() => transform.TransformPoint(new Vector3(0, 1.32f, 0.45f));

        void Reveal()
        {
            bool big = Catch.Cm > 140;
            fishGo.transform.position = big ? transform.TransformPoint(new Vector3(0, 0.25f, 1.0f + Catch.Cm / 400f)) : HoldPoint();
            fishGo.transform.rotation = Quaternion.LookRotation(transform.right, Vector3.up) * Quaternion.Euler(big ? 0 : -8f + Mathf.Sin(Time.time * 7) * 2f, 0, 0);
            bool full = TargetSlot < 0;
            if (StateT >= RevealMin)
            {
                bool keep = Ctl.Keep || Ctl.UsePressed || (!full && StateT >= RevealAuto);
                bool let = Ctl.LetGo;
                if (full && Ctl.Bot) { let = true; keep = false; }
                if (keep && full) { DropOnDeck(); return; }
                if (keep) { int slot = me.Bar.Add(Catch); Hud.I?.FlyIn(fishGo.transform.position, slot); Snd.Play2D(Synth.Thup, 0.7f); OnLog?.Invoke($"kept slot {slot + 1}"); EndReveal(); return; }
                if (let) { LetGo(); return; }
            }
        }

        void DropOnDeck()
        {
            FishPickup.Drop(Catch, transform.TransformPoint(new Vector3(0, 0.15f, 0.8f)), transform.rotation);
            Alerts.Push("Hotbar full", Catch.Species.Name + " is on the deck. Free a slot and pick it up (E).", Alerts.Kind.Info);
            OnLog?.Invoke("dropped on deck"); EndReveal();
        }

        void LetGo()
        {
            var p = transform.TransformPoint(new Vector3(0, 0, 2.2f)); p.y = Sea.Height(p);
            Fx.Splash(p, 0.7f, 12); Snd.Play(M1Assets.I.Splash, p, 0.5f);
            OnLog?.Invoke("let go"); EndReveal();
        }

        void EndReveal()
        {
            if (fishGo != null) Destroy(fishGo);
            Catch = null; Go(St.Ready);
        }

        // ---------------- body + rod pose ----------------
        static readonly Vector3 RestButt = new(0.2f, 0.98f, 0.22f);
        void PlaceRod(float dt, bool snap = false)
        {
            float tElev = 35, tYaw = 0; Vector3 tButt = RestButt; float tension = 0.03f; bool onBack = false;
            var bobP = bob.transform.position;
            float yawToBob = AngleTo(bobP);
            switch (State)
            {
                case St.Stowed: onBack = true; break;
                case St.Drawing: { float u = StateT / DrawT; tElev = Mathf.Lerp(80, 35, u); tButt = Vector3.Lerp(new Vector3(0.15f, 1.3f, 0), RestButt, u); break; }
                case St.Ready: tElev = 35 + Mathf.Sin(Time.time * 1.1f) * 2f; break;
                case St.Charging:
                    // wind-up: back over the right shoulder (0 → −110°), weight on the back foot, trembling at 100%
                    tElev = 35 + 120 * Power + (Power > 0.98f ? Mathf.Sin(Time.time * 60) * 1.5f : 0);
                    tButt = Vector3.Lerp(RestButt, new Vector3(0.28f, 1.42f, 0.0f), Power); tension = 0.05f + 0.1f * Power; break;
                case St.Flight:
                    { float u = Mathf.Clamp01(StateT / 0.18f); tElev = u < 1 ? Mathf.Lerp(150, 12, u) : 18; tButt = Vector3.Lerp(new Vector3(0.28f, 1.42f, 0.0f), new Vector3(0.18f, 1.15f, 0.35f), u); tension = 0.05f; break; }
                case St.Settle: case St.Waiting:
                    tElev = 28 + (nibT >= 0 ? -4 : 0); tYaw = Mathf.Clamp(yawToBob, -40, 40); tension = 0.05f + (nibT >= 0 ? 0.15f : 0); break;
                case St.Bite: tElev = 20; tYaw = Mathf.Clamp(yawToBob, -40, 40); tension = 0.6f; break;
                case St.Strike: tElev = Mathf.Lerp(20, 70, StateT / 0.2f); tYaw = Mathf.Clamp(yawToBob, -40, 40); tension = 0.6f; tButt = RestButt + new Vector3(0, 0.15f, -0.05f); break;
                case St.Heavy: case St.Minigame:
                    tElev = 60 + Mathf.Sin(Time.time * 3.1f) * 4; tYaw = Mathf.Clamp(AngleTo(fishUnder), -45, 45); tButt = RestButt + new Vector3(0, 0.12f, -0.05f);
                    tension = 0.55f + 0.1f * Hits + (Frozen && Stop > 0 && Result[Stop - 1] < 0 ? 0.25f : 0); break;
                case St.Reel:
                    tElev = 55 + Mathf.Sin(StateT * 6f) * 7; tYaw = Catch != null && fishGo != null ? Mathf.Clamp(AngleTo(fishGo.transform.position), -45, 45) : 0;
                    tension = Mathf.Lerp(0.4f, 0.8f, Mathf.Clamp01(Catch != null ? Catch.Kg / 15f : 0)); tButt = RestButt + new Vector3(0, 0.1f, 0); break;
                case St.Reveal: onBack = true; break;
                case St.Recover: tElev = 70; tension = 0.1f; break;
            }
            if (onBack)
            {
                // on the back strap (systems H7: long items show on your back when not in hand)
                rod.Root.SetParent(me.Model, false);
                // butt at the right hip behind, tip up over the left shoulder
                rod.Root.localRotation = Quaternion.LookRotation(new Vector3(-0.32f, 0.93f, -0.12f), Vector3.back);
                rod.Root.localPosition = new Vector3(0.2f, 0.72f, -0.24f);
                rod.Tension = 0; if (me.Anim.Grip == rod.Root) me.Anim.Grip = null;
                elev = 35; butt = RestButt; return;
            }
            rod.Root.SetParent(transform, false);
            if (snap) { elev = tElev; butt = tButt; }
            // the rod follows its targets on a spring (lags the hands a little, sways with the boat)
            float w = State is St.Flight or St.Strike ? 40f : 16f;
            elevV += (w * w * (tElev - elev) - 2 * 0.7f * w * elevV) * dt; elev += elevV * dt;
            yawOff = Mathf.LerpAngle(yawOff, tYaw, 1 - Mathf.Exp(-8f * dt));
            butt = Vector3.SmoothDamp(butt, tButt, ref buttV, State == St.Flight ? 0.03f : 0.08f);
            var boatRoll = Skiff.I != null && me.OnBoat ? Skiff.I.transform.eulerAngles.z : 0;
            rod.Root.localPosition = butt;
            rod.Root.localRotation = Quaternion.Euler(0, yawOff, 0) * Quaternion.Euler(-elev, 0, -Mathf.DeltaAngle(0, boatRoll) * 0.6f + Mathf.Sin(Time.time * 0.9f) * 1.5f);
            rod.Tension = tension;
            rod.BendDir = line.Live ? (line.B - rod.Tip.position).normalized : Vector3.down;
            me.Anim.Grip = rod.Root; me.Anim.GripL = RodView.GripL; me.Anim.GripR = RodView.GripR;
            me.Anim.GripElbow = new Vector3(0, -0.8f, -0.25f); me.Anim.GripFingers = new Vector3(0, 0.2f, 1);
        }

        float AngleTo(Vector3 p)
        {
            var l = transform.InverseTransformPoint(p); l.y = 0;
            return l.sqrMagnitude < 0.01f ? 0 : Mathf.Atan2(l.x, l.z) * Mathf.Rad2Deg;
        }

        void Pose(float dt)
        {
            PlaceRod(dt);
            creak.volume = Mathf.MoveTowards(creak.volume, State == St.Charging ? creakVol : 0, dt * 4);
            if (State != St.Reel && State != St.Recover) reelSnd.volume = Mathf.MoveTowards(reelSnd.volume, 0, dt * 3);
            // face the aim / the bobber / the fish
            me.FaceYaw = State switch
            {
                St.Charging => CameraRig.I != null ? CameraRig.I.Yaw : float.NaN,
                St.Flight or St.Settle or St.Waiting or St.Bite or St.Strike or St.Heavy or St.Minigame => YawTo(bob.Pos),
                _ => float.NaN,
            };
            if (State is St.Heavy or St.Minigame) me.FaceYaw = YawTo(fishUnder);
            // hands on the fish for the hold-up
            if (State == St.Reveal && fishGo != null && Catch.Cm <= 140)
            {
                me.Anim.Grip = fishGo.transform; me.Anim.GripL = new Vector3(0, -0.06f, -0.28f); me.Anim.GripR = new Vector3(0, -0.06f, 0.22f);
                me.Anim.GripElbow = new Vector3(0, -0.8f, 0); me.Anim.GripFingers = new Vector3(0, 1, 0.3f);
            }
        }

        float YawTo(Vector3 p) { var d = p - transform.position; d.y = 0; return d.sqrMagnitude < 0.01f ? transform.eulerAngles.y : Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; }

        void Cam()
        {
            var c = CameraRig.I; if (c == null || me.AtHelm) return;
            switch (State)
            {
                case St.Charging: c.M = CameraRig.Mode.Shoulder; c.Focus = Marker; break;
                case St.Flight: c.M = CameraRig.Mode.Shoulder; c.Focus = bob.transform.position; break;
                case St.Settle: case St.Waiting: case St.Bite: case St.Strike: c.M = CameraRig.Mode.Wait; c.Focus = bob.transform.position; break;
                case St.Heavy: case St.Minigame: c.M = CameraRig.Mode.Fight; c.Focus = fishUnder; break;
                case St.Reel: c.M = CameraRig.Mode.Fight; c.Focus = fishGo != null ? fishGo.transform.position : fishUnder; break;
                case St.Reveal: c.M = CameraRig.Mode.Reveal; c.Focus = HoldPoint(); break;
                default: c.M = CameraRig.Mode.Follow; break;
            }
        }

        // The test bot's knobs (Bot.cs sets them).
        public static class Bot { public static bool FightHeavy; }
    }
}
