using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    // M3 test switches (gdd systems §5.2):
    //   -pb-daytest <dir>        Walt's Landing in the 6 lighting states from two sides, a whole-day time-lapse
    //                            (<dir>/lapse/f_00000.jpg...), and the checks: nights never black, lamps only after dusk
    //   -pb-weathertest [n]      the weather Markov over n seeded days (10,000): forecast accuracy, storm calls, budget
    //   -pb-sleeptest solo       too early; the cot at 21:00; tired at 01:30; pass out on the pier, swimming, driving
    //   -pb-sleeptest host|rider (tools/sleeptest.sh) the host sleeps while the rider fishes (a fight holds the skip),
    //                            swims, drives, stands on the deck; then both pass out at 02:00 (one skip)
    public partial class Bot
    {
        IEnumerator DayTests()
        {
            if (Arg("-pb-daytest") != null) { shots = Arg("-pb-daytest"); Directory.CreateDirectory(shots); return DayTest(); }
            if (Arg("-pb-weathertest") != null) return WeatherTest((int)ArgF("-pb-weathertest", 10000));
            if (Arg("-pb-sleeptest") != null) return SleepTest(Arg("-pb-sleeptest"));
            if (Arg("-pb-stormtest") != null) { shots = Arg("-pb-stormtest"); Directory.CreateDirectory(shots); return StormTest(); }
            return null;
        }

        int dayFails;
        void Check(bool ok, string what) { Debug.Log($"[PB] m3: {(ok ? "ok" : "FAIL")} {what}"); if (!ok) dayFails++; }
        static IEnumerator Until(System.Func<bool> f, float secs) { for (float t = 0; !f() && t < secs; t += Time.unscaledDeltaTime) yield return null; }

        IEnumerator WeatherTest(int n)
        {
            yield return null;
            var rules = DayCheck.Run(); bool all = rules.Count == 0;
            foreach (var x in rules) Debug.Log("[PB] m3: FAIL rule " + x);
            Debug.Log($"[PB] m3: rules (clock, bed hours, lighting blend, rot) {(all ? "PASS" : rules.Count + " FAIL")}");
            foreach (int seed in new[] { Sky.Seed, 12345 })
            {
                var r = Weather.SelfTest(seed, n, out bool ok); all &= ok;
                Debug.Log("[PB] weather: " + r + (ok ? " PASS" : " FAIL"));
            }
            // the forecast the phone will show at a few moments of Day 5 (data for M4)
            foreach (var h in new[] { 7.0, 13.0, 19.0 })
            {
                var m = DayTime.At(5, h * 60);
                Debug.Log($"[PB] weather: forecast at Day 5 {DayTime.Clock(m)}: " + string.Join(", ", Sky.W.Forecast(m).ConvertAll(l => $"D{l.Day} {Weather.BlockName[l.Block]} {l.Shown} {l.Dots}/4")));
            }
            Debug.Log($"[PB] RESULT weather: {(all ? "PASS" : "FAIL")}");
            Application.Quit();
        }

        // ---------------------------------------------------------------- the light
        IEnumerator DayTest()
        {
            Ctl.Bot = true; Sleep.Off = true; yield return new WaitForSeconds(0.5f);
            var spawn = Landing.I.Spawn;
            float seaYaw = Quaternion.LookRotation(Flat(Skiff.I.transform.position - spawn.position)).eulerAngles.y;
            me.Teleport(spawn.position + Vector3.up * 0.05f, seaYaw);
            var cases = new (string n, double h, int wx)[] { ("1-dawn", 6.5, 1), ("2-day", 12, 1), ("3-golden", 18.5, 1), ("4-dusk", 20.25, 1), ("5-night", 23.5, 1), ("6-storm", 14, 5) };
            var bright = new Dictionary<string, double>(); var lamp = new Dictionary<string, float>(); var ui = new Dictionary<string, Color>();
            foreach (var c in cases)
            {
                Sky.ForceWx = c.wx; Sky.Jump(DayTime.At(2, c.h * 60));
                for (int side = 0; side < 2; side++)
                {
                    CameraRig.I.Yaw = side == 0 ? seaYaw + 25 : seaYaw + 200; CameraRig.I.Pitch = side == 0 ? 12 : 16;
                    yield return new WaitForSeconds(0.8f);
                    yield return Shot(c.n + (side == 0 ? "-sea" : "-shore"));
                    if (side == 0) { bright[c.n] = LastBright; lamp[c.n] = Sky.I.LampW; ui[c.n] = UiKit.Lit.color; }
                }
                Debug.Log($"[PB] daytest: {c.n}: state {Sky.I.State}, lamps {Sky.I.LampW:F2}, sun {Ponyo.Sun.intensity:F2} ({(Ponyo.Sun.enabled ? "on" : "off")}), brightness {bright[c.n]:F0} · {Sky.DebugLine()}");
            }
            Check(ui["2-day"] == Color.white && ui["5-night"].grayscale < 0.92f && ui["5-night"].b > ui["5-night"].r && ui["4-dusk"].b < ui["4-dusk"].r, $"the HUD follows the light (day {ui["2-day"]}, dusk {ui["4-dusk"]}, night {ui["5-night"]})");
            Check(bright["5-night"] > 30, $"night is never black (brightness {bright["5-night"]:F0} > 30)");
            Check(bright["2-day"] > bright["5-night"] * 1.6, $"day is far brighter than night ({bright["2-day"]:F0} vs {bright["5-night"]:F0}; the paper HUD, a quarter of the frame, stays light)");
            Check(bright["4-dusk"] < bright["3-golden"] && bright["5-night"] < bright["4-dusk"], "golden > dusk > night");
            Check(lamp["2-day"] < 0.01f && lamp["3-golden"] < 0.01f && lamp["5-night"] > 0.99f, "lamps off by day, on at night");
            // a whole day, 04:00 -> 04:00, a frame every 4 in-game minutes (Fair held, the sea side)
            var lapse = Path.Combine(shots, "lapse"); Directory.CreateDirectory(lapse);
            Sky.ForceWx = 1; CameraRig.I.Yaw = seaYaw + 25; CameraRig.I.Pitch = 12;
            Sky.Jump(DayTime.At(3, 240)); yield return new WaitForSeconds(3f); // (the storm before eases out, its rain falls clear)
            int f = 0; double prevB = -1, maxJump = 0;
            for (double m = DayTime.At(3, 240); m <= DayTime.At(4, 240); m += 4)
            {
                Sky.Jump(m); yield return null; yield return new WaitForEndOfFrame();
                Save(Path.Combine(lapse, $"f_{f++:00000}.jpg"), 1280, 720);
                if (prevB >= 0) maxJump = System.Math.Max(maxJump, System.Math.Abs(LastBright - prevB));
                prevB = LastBright;
            }
            Check(maxJump < 12, $"the day blends smoothly: the largest brightness step between frames 4 min apart is {maxJump:F1} (< 12)");
            Debug.Log($"[PB] RESULT daytest: {(dayFails == 0 ? "PASS" : "FAIL")} ({f} lapse frames)");
            Application.Quit();
        }

        // -pb-stormtest <dir>: Biscuit anchored in the open, then a storm blows in (held): the roll and pitch at anchor and
        // under way stay under the storm limits (25° / 20°: heavy, never over), and you stay aboard (systems §5.2)
        IEnumerator StormTest()
        {
            Sleep.Off = true; Sky.ForceWx = 1; Sky.Jump(DayTime.At(2, 14 * 60));
            yield return new WaitForSeconds(0.5f);
            yield return SailOut(false);
            var s = Skiff.I;
            if (shots != null) { CameraRig.I.Pitch = 14; yield return Shot("storm-0-fair"); }
            float dry = 0, wet = 0; yield return StopTime(t => dry = t);
            Sky.ForceWx = 5; Sky.HeldAt = 1e9; yield return new WaitForSeconds(6f); // (the look eases in over ~2 s; the held storm's own leak and waves pushed away: this test springs its own)
            float roll = 0, pitch = 0; bool aboard = true;
            void Measure() { roll = Mathf.Max(roll, Mathf.Abs(Mathf.DeltaAngle(0, s.transform.eulerAngles.z))); pitch = Mathf.Max(pitch, Mathf.Abs(Mathf.DeltaAngle(0, s.transform.eulerAngles.x))); aboard &= me.OnBoat && !me.Swimming; }
            for (float t = 0; t < 30; t += Time.deltaTime) { Measure(); if (shots != null && Mathf.Abs(t - 10) < Time.deltaTime) yield return Shot("storm-1-anchored"); yield return null; }
            Debug.Log($"[PB] storm: anchored 30 s in a storm (wave amp {Sea.Amp:F2} m, len {Sea.Len:F0} m): max roll {roll:F1}°, pitch {pitch:F1}°, aboard {aboard}");
            Check(roll <= 25 && pitch <= 20 && aboard, $"anchored in the storm: roll {roll:F1}° <= 25, pitch {pitch:F1}° <= 20, still aboard");
            Check(roll > 8, $"a storm rolls her harder than a fair day's 6° cap (systems §1.6: up to 22°): {roll:F1}°");
            yield return StopTime(t => wet = t);
            Check(wet - dry > 0.1f && wet - dry < 0.2f, $"a wet deck: stopping from a walk takes {dry:F2} s dry, {wet:F2} s in the storm (+0.15 s, systems §1.6.5)");
            // under way: up anchor, full ahead, a turn
            float r0 = roll, p0 = pitch; roll = pitch = 0;
            s.ToggleAnchor(); yield return Until(() => !s.Anchored, 4);
            me.Teleport(s.Helm.position + Vector3.up * 0.05f, s.transform.eulerAngles.y); yield return null; me.TakeHelm(); yield return new WaitForSeconds(0.4f);
            s.Notch = 3;
            for (float t = 0; t < 15; t += Time.deltaTime) { s.Steer = t > 6 && t < 10 ? 0.6f : 0; Measure(); if (shots != null && Mathf.Abs(t - 8) < Time.deltaTime) yield return Shot("storm-2-driving"); yield return null; }
            Debug.Log($"[PB] storm: under way 15 s: max roll {roll:F1}°, pitch {pitch:F1}°, speed {s.Speed:F1} m/s");
            Check(roll <= 25 && pitch <= 20 && aboard, $"driving in the storm: roll {roll:F1}° <= 25, pitch {pitch:F1}° <= 20, still aboard");

            // the leak (systems §1.6.4): it springs, the water rises, she slows and lists; patch at the hole, bail her dry
            s.SpringLeak(); float w0 = s.Water; yield return new WaitForSeconds(4f);
            Check(s.Leaking && s.Water > w0, $"a leak lets water in ({w0:F2} -> {s.Water:F2})");
            s.Water = 0.6f; yield return new WaitForSeconds(6f);       // (skip the 90 s it takes to get this deep)
            Check(s.Speed <= Skiff.MaxSpeed * 0.85f, $"shipping water she slows: {s.Speed:F2} m/s at full (max {Skiff.MaxSpeed}, x{s.SpeedMul:F2})");
            s.Notch = 0; yield return Until(() => Mathf.Abs(s.Speed) < 0.3f, 10); me.LeaveHelm(); s.AnchorNow();
            float lsum = 0; int ln = 0; for (float t = 0; t < 5; t += Time.deltaTime) { lsum += Mathf.DeltaAngle(0, s.transform.eulerAngles.z); ln++; yield return null; }
            Check(lsum / ln > 1.5f, $"she lists with the water in her: mean roll {lsum / ln:F1}° (wants ~{s.List:F1})");
            me.Teleport(s.Hole + Vector3.up * 0.1f, s.transform.eulerAngles.y + 180); yield return new WaitForSeconds(0.5f);
            if (shots != null) { CameraRig.I.Pitch = 38; CameraRig.I.Yaw = s.transform.eulerAngles.y + 160; yield return new WaitForSeconds(0.3f); yield return Shot("leak-1-water"); }
            Ctl.BotInteractHeld = true; yield return Until(() => !s.Leaking, Skiff.PatchTime + 4); Ctl.BotInteractHeld = false;
            Check(!s.Leaking, $"holding E at the hole patches it (prompt '{me.Prompt}')");
            yield return new WaitForSecondsRealtime(0.5f);                // (toasts run on real time; -pb-fps game time runs faster)
            Check(Hud.I.ToastsShown("She's sprung a leak! Patch the hole, or bail.") == 0 && Hud.I.ToastsShown("Hole patched. Now bail her dry.") == 1, $"'Hole patched' dismisses the leak warning ({Hud.I.ToastsShown("She's sprung a leak! Patch the hole, or bail.")} leak toasts up)");
            me.Teleport(s.transform.TransformPoint(new Vector3(0, s.HoleLocal.y + 0.1f, 0.3f)), s.transform.eulerAngles.y); yield return new WaitForSeconds(0.5f);
            float wb = s.Water; Ctl.BotInteractHeld = true; yield return Until(() => s.Water < 0.02f, 25); Ctl.BotInteractHeld = false;
            Check(s.Water < 0.02f, $"bailing empties her ({wb:F2} -> {s.Water:F2})");
            if (shots != null) yield return Shot("leak-2-dry");

            // rogue waves (systems §1.6.5): braced = safe; unbraced = knocked down; at a rail, over the side
            bool kn = false, ov = false; Skiff.OnRogue = (k, o) => { kn = k; ov = o; };
            Vector3 Deck(float x, float z) => s.transform.TransformPoint(new Vector3(x, s.HoleLocal.y + 0.1f, z));
            me.Teleport(Deck(0, 0.3f), s.transform.eulerAngles.y); yield return new WaitForSeconds(0.5f);
            Ctl.BotBrace = true; s.RogueNow(); yield return new WaitForSeconds(Skiff.RogueWarn + 1.5f); Ctl.BotBrace = false;
            Check(!kn && !me.Down && me.OnBoat, "braced (hold C), the rogue wave can't take you");
            s.RogueNow(); yield return new WaitForSeconds(1.5f);
            Check(Hud.I.ToastsShown("Rogue wave! Hold C to brace!") == 1, $"a second rogue-wave warning refreshes the first toast, no copy ({Hud.I.ToastsShown("Rogue wave! Hold C to brace!")} up)");
            if (shots != null) { CameraRig.I.Pitch = 16; CameraRig.I.Yaw = s.transform.eulerAngles.y + 120; yield return Shot("rogue-1-warning"); }
            yield return Until(() => kn, 4);
            if (shots != null) { yield return new WaitForSeconds(0.3f); yield return Shot("rogue-2-knocked"); }
            Check(kn && me.Down, $"unbraced, the rogue wave knocks you down (over: {ov})");
            yield return Until(() => !me.Down, 8); kn = ov = false;
            me.Teleport(Deck(-0.6f, 0), s.transform.eulerAngles.y); yield return new WaitForSeconds(0.5f);
            Skiff.TestOver = true; s.RogueNow(); yield return Until(() => kn, Skiff.RogueWarn + 2); yield return Until(() => me.Swimming, 5); Skiff.TestOver = false;
            Check(ov && me.Swimming, $"at the rail, unbraced, it can throw you over the side (over {ov}, swimming {me.Swimming})");

            // a held storm (F11, -pb-weather storm) brings its own leak and rogue waves, timed from the hold (M3 lead 1)
            yield return Until(() => !me.Down, 8); me.Teleport(Deck(0, 0.3f), s.transform.eulerAngles.y); Ctl.BotBrace = true;
            int hits = 0; Skiff.OnRogue = (k, o) => hits++;
            Sky.ForceWx = 1; Sky.ForceWx = 5; double held = Sky.HeldAt, first = double.NaN; float ts0 = Time.timeScale; Time.timeScale = 4;
            yield return Until(() => { if (hits > 0 && double.IsNaN(first)) first = Sky.Minutes - held; return s.Leaking && hits > 0; }, 160 / 4f);
            Time.timeScale = ts0; Ctl.BotBrace = false;
            Check(s.Leaking && hits > 0 && first <= 75 + 1, $"a held storm springs its own leak ({(s.Leaking ? "yes" : "no")}, due {Sky.LeakAt(0, 0) - held:F0} min after the hold) and rogue waves (first {first:F0} min after the hold, {hits} braced)");
            Debug.Log($"[PB] RESULT stormtest: {(dayFails == 0 ? "PASS" : "FAIL")}");
            Application.Quit();
        }

        // walk the deck aft to fore 0.7 s, let go: how long until the walk is down to 10% (the stop's t90)
        IEnumerator StopTime(System.Action<float> got)
        {
            var s = Skiff.I; me.Teleport(s.transform.TransformPoint(new Vector3(0, s.HoleLocal.y + 0.1f, -1.3f)), s.transform.eulerAngles.y); CameraRig.I.Yaw = s.transform.eulerAngles.y;
            yield return new WaitForSeconds(0.4f);
            Ctl.BotMove = new Vector2(0, 1); yield return new WaitForSeconds(0.7f); float v0 = me.Vel.magnitude; Ctl.BotMove = Vector2.zero;
            float t = 0; while (me.Vel.magnitude > 0.1f * v0 && t < 2) { yield return null; t += Time.deltaTime; }
            Debug.Log($"[PB] storm: stop from {v0:F2} m/s in {t:F2} s (wet {Skiff.Wet})"); got(t);
        }

        // ---------------------------------------------------------------- sleep
        IEnumerator WaitSkip(float secs = 40)
        {
            yield return Until(() => Sleep.I.Skipping, 12);
            yield return Until(() => !Sleep.I.Skipping, secs);
        }

        void LookAt(Vector3 at) { var d = Flat(at - me.transform.position); if (d.sqrMagnitude > 0.01f) CameraRig.I.Yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; }

        IEnumerator LieDown()
        {
            me.Teleport(Sleep.BedSide, Quaternion.LookRotation(Flat(Sleep.Cot.position - Sleep.BedSide)).eulerAngles.y);
            yield return new WaitForSeconds(0.3f);
            Ctl.BotInteractHeld = true;
            yield return Until(() => Sleep.I.Lying, 3);
            Ctl.BotInteractHeld = false;
        }

        IEnumerator Swim()
        {
            // off the pier's other side from Biscuit's berth (the ladder's side has the boat)
            var l = Landing.I; var out_ = Flat(l.LadderBottom.position - l.LadderTop.position).normalized;
            var at = l.Spawn.position - out_ * 3.2f; at.y = Sea.Height(at) - Angler.SwimDepth;
            me.Teleport(at, 0);
            yield return Until(() => me.Swimming, 3);
        }

        string WakeLine() { var p = me.transform.position; return $"Day {Sky.Day} {DayTime.Clock(Sky.Minutes)}, woke {Sleep.I.LastWake} at {p.x:F1} {p.y:F1} {p.z:F1}"; }

        IEnumerator SleepTest(string role)
        {
            Ctl.Bot = true; Application.targetFrameRate = 60;
            shots = Arg("-pb-sleepshots"); if (shots != null) Directory.CreateDirectory(shots); // (-pb-shots is the M1 tour)
            if (role == "host" || role == "rider") { yield return CoopSleep(role == "host"); yield break; }
            yield return new WaitForSeconds(0.5f);
            var s = Skiff.I; Sky.ForceWx = 1;
            int horns = 0; Sleep.OnLog += l => { if (l.StartsWith("the ferry horn")) horns++; };
            // 1) too early
            Sky.Jump(DayTime.At(1, 15 * 60));
            me.Teleport(Sleep.BedSide, Quaternion.LookRotation(Flat(Sleep.Cot.position - Sleep.BedSide)).eulerAngles.y);
            yield return new WaitForSeconds(0.4f);
            Ctl.BotInteractHeld = true; yield return new WaitForSeconds(1.5f);
            Check(!Sleep.I.Lying && !Sleep.I.Skipping && me.Prompt == "Too early. Back after 8 PM.", $"15:00 at the cot: \"{me.Prompt}\", no sleep");
            Ctl.BotInteractHeld = false;
            if (shots != null) yield return Shot("sleep-1-too-early");
            // 2) the cot at 21:00
            Sky.Jump(DayTime.At(1, 21 * 60)); yield return new WaitForSeconds(0.3f);
            // a mackerel caught at dusk (18:00) rides in the hotbar through the night (rot: fishing §5.6)
            var mack = new Fish { Species = FishDb.All.Find(x => x.Id == "mackerel") ?? FishDb.All[0], Cm = 30, Kg = 0.4f, CaughtAt = (float)DayTime.At(1, 18 * 60) };
            me.Bar.S[2].Fish.Add(mack);
            Check(mack.Fresh == 0, $"the dusk catch at 21:00 is {Rot.Names[mack.Fresh]}");
            LookAt(Sleep.Cot.position);
            yield return LieDown();
            Check(Sleep.I.Lying, "21:00: hold E 1 s on the cot lies down");
            yield return Until(() => Sleep.I.CardUp, 6); yield return new WaitForSeconds(0.2f);
            Check(Sleep.I.CardUp && Hud.I.ToastsShown() == 0, $"the night card shows with the toast stack hidden ({Hud.I.ToastsShown()} toasts up)");
            if (shots != null) yield return Shot("sleep-2-card");
            yield return WaitSkip();
            Check(Sky.Day == 2 && Sky.Hour >= 7 && Sky.Hour < 7.25, "woke Day 2 at 7:00 AM: " + WakeLine());
            Check(Sleep.I.LastWake == "in bed" && Vector3.Distance(me.transform.position, Sleep.BedSide) < 0.6f, "woke beside the cot");
            Check(horns == 1, $"the ferry horn sounds once with the 07:00 fade-in ({horns})");
            yield return new WaitForSeconds(0.3f);
            Check(Hud.I.ToastsShown() > 0, $"toasts show again after the card ({Hud.I.ToastsShown()} up: the morning toast)");
            Check(mack.Fresh == 1, $"caught 18:00, slept 21:00 -> 07:00: the mackerel is {Rot.Names[mack.Fresh]} ({(Sky.Minutes - mack.CaughtAt) / 60:F1} h old; want Good)");
            me.Select(2); // (its name pill shows the freshness in the morning shot)
            Debug.Log($"[PB] m3: Day 2 weather {Sky.W.Block(2, 0)}/{Sky.W.Block(2, 1)}/{Sky.W.Block(2, 2)} (seed {Sky.Seed})");
            if (shots != null) yield return Shot("sleep-3-morning");
            // 3) tired on the pier, pass out at 02:00
            var spot = Landing.I.Spawn.position + Vector3.up * 0.05f;
            me.Teleport(spot, 0); Sky.Jump(DayTime.At(3, 88)); // 01:28
            yield return new WaitForSeconds(4f);
            Check(Mathf.Approximately(Sleep.WalkMul, 0.9f), $"01:32: tired, walking at {Sleep.WalkMul:P0}");
            if (shots != null) yield return Shot("sleep-4-tired-0132");
            Sky.Jump(DayTime.At(3, 119)); var before = me.transform.position;
            yield return WaitSkip();
            Check(Sky.Day == 3 && Sky.Hour < 7.25 && Sleep.I.LastWake == "where I fell" && Vector3.Distance(Flat(me.transform.position), Flat(before)) < 2.5f, "passed out on the pier, woke where I fell: " + WakeLine());
            // 4) swimming at 02:00: woken on a ladder
            yield return Swim(); Check(me.Swimming, "in the sea");
            Sky.Jump(DayTime.At(4, 119));
            yield return WaitSkip();
            Check(Sleep.I.LastWake != null && Sleep.I.LastWake.Contains("ladder") && !me.Swimming, "swimming at 02:00: " + WakeLine());
            // 5) driving at 02:00: the boat anchors itself
            me.Teleport(s.transform.TransformPoint(new Vector3(0, 1.0f, 0.4f)), s.transform.eulerAngles.y); yield return new WaitForSeconds(0.5f);
            me.TakeHelm(); yield return new WaitForSeconds(0.5f); s.CastOff(); s.Notch = 2;
            Sky.Jump(DayTime.At(5, 119)); yield return new WaitForSeconds(3f);
            float sp = s.Speed;
            yield return WaitSkip();
            Check(s.Anchored && !s.Moored && me.OnBoat, $"driving at 02:00 ({sp:F1} m/s): Biscuit {(s.Anchored ? "anchored" : "NOT anchored")}, " + WakeLine());
            Debug.Log($"[PB] RESULT sleeptest solo: {(dayFails == 0 ? "PASS" : "FAIL")}");
            Application.Quit();
        }

        // host + one rider (tools/sleeptest.sh). The host sleeps in the cot at 21:00 on Days 1-4 while the rider is
        // (1) in a fight, (2) swimming, (3) at the tiller under way, (4) on the deck; then (5) both are up at 02:00 and pass
        // out together: one skip; (6) both in the one cot (the latency race's outcome): one skip, the rider steps out the
        // far side, and can't walk off lying down; (7) the rider lies down and the host leaves mid heads-up: the rider
        // still sleeps (alone), never stuck. Each machine checks where it woke and that both clocks agree.
        IEnumerator CoopSleep(bool host)
        {
            yield return Until(() => Coop.On && Coop.I.Roster.Count >= 2, 40);
            if (!Coop.On || Coop.I.Roster.Count < 2) { Debug.Log("[PB] RESULT sleeptest: FAIL (never connected)"); Application.Quit(); yield break; }
            yield return new WaitForSeconds(2f);
            var s = Skiff.I;
            string[] want = { "", "where I stood", "ladder", "at the tiller", "on the boat", "where I fell", "in bed", "in bed" };
            for (int r = 1; r <= 7; r++)
            {
                if (host)
                {
                    Sky.ForceWx = 1;
                    Sky.Jump(r < 5 ? DayTime.At(r, 21 * 60) : r == 5 ? DayTime.At(6, 115) : DayTime.At(r + 1, 21 * 60));
                    Debug.Log($"[PB] m3: round {r} at Day {Sky.Day} {DayTime.Clock(Sky.Minutes)}");
                    if (r == 7)
                    {
                        // the rider goes to bed; mid heads-up, the host leaves the crew
                        me.Teleport(Landing.I.Spawn.position + Vector3.up * 0.05f, 0);
                        yield return Until(() => Sleep.I.Skipping, 40); yield return new WaitForSeconds(1.5f);
                        Check(Sleep.I.Skipping && !Sleep.I.CardUp, "round 7: the rider's heads-up is running: the host leaves now");
                        Coop.Leave(); break;
                    }
                    if (r < 5 || r == 6) { me.Teleport(Sleep.BedSide, 0); yield return new WaitForSeconds(r == 1 ? 12f : 7f); yield return LieDown(); Check(Sleep.I.Lying, $"round {r}: the host lies down"); }
                    else me.Teleport(Landing.I.Spawn.position + Vector3.up * 0.05f, 0);
                    yield return WaitSkip();
                    Check(Sleep.I.LastWake == (r == 5 ? "where I fell" : "in bed") && (r != 6 || Vector3.Distance(me.transform.position, Sleep.BedSide) < 0.6f && Sky.Day == 8 && Sky.Hour < 7.25), $"round {r}: host " + WakeLine());
                }
                else
                {
                    // wait for the host's round to start (its clock reaches us)
                    yield return Until(() => r < 5 ? Sky.Day == r && Sky.Hour >= 20.9 : r == 5 ? Sky.Day == 6 && Sky.Hour >= 1.9 && Sky.Hour < 7 : Sky.Day == r + 1 && Sky.Hour >= 20.9, 60);
                    Debug.Log($"[PB] m3: round {r}: rider at Day {Sky.Day} {DayTime.Clock(Sky.Minutes)}");
                    if (me.AtHelm) { me.LeaveHelm(); yield return new WaitForSeconds(0.5f); }
                    int had = 0; foreach (var sl in me.Bar.S) had += sl.Fish.Count;
                    if (r == 1)
                    {
                        // fish off the pier end; the bite comes 2.5 s into the heads-up, the bar is held 4 s: the host must wait
                        me.Teleport(Landing.I.Spawn.position + Vector3.up * 0.05f, 0);
                        var sea = Flat(Landing.I.Spawn.position - Sleep.Cot.position).normalized; CameraRig.I.Yaw = Mathf.Atan2(sea.x, sea.z) * Mathf.Rad2Deg;
                        me.Select(0); yield return new WaitForSeconds(0.6f); // (slot 0 = the rod)
                        yield return CastOnce(0.55f);
                        yield return Until(() => fish.State == Fishing.St.Waiting, 6);
                        fish.BiteAt = Time.time + 60;                       // (no bite until the heads-up)
                        yield return Until(() => Sleep.I.Skipping, 20);
                        fish.BiteAt = Time.time + 2.5f;
                        yield return Until(() => fish.State == Fishing.St.Bite, 5);
                        yield return new WaitForSeconds(0.35f); Ctl.BotStrike = true; yield return null; Ctl.BotStrike = false;
                        yield return Until(() => fish.State is Fishing.St.Minigame or Fishing.St.Heavy, 3);
                        if (fish.State == Fishing.St.Heavy) { Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false; yield return Until(() => fish.State == Fishing.St.Minigame, 3); }
                        Debug.Log($"[PB] m3: rider fighting a fish ({fish.State}) {Time.time:F1}");
                        yield return new WaitForSeconds(4f);
                        Ctl.BotStrike = true; yield return null; Ctl.BotStrike = false;
                        yield return Until(() => fish.State == Fishing.St.Reveal, 6); yield return new WaitForSeconds(0.8f);
                        Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false;
                    }
                    else if (r == 2) yield return Swim();
                    else if (r == 3)
                    {
                        me.Teleport(s.transform.TransformPoint(new Vector3(0, 1.0f, 0.4f)), s.transform.eulerAngles.y); yield return new WaitForSeconds(0.5f);
                        me.TakeHelm(); yield return Until(() => !s.Remote, 4);
                        if (s.Moored) s.CastOff(); s.Notch = 2;
                        yield return new WaitForSeconds(2f);
                        Debug.Log($"[PB] m3: rider driving at {s.Speed:F1} m/s (owner: {(!s.Remote ? "me" : "not me")})");
                    }
                    else if (r == 4)
                    {
                        me.Teleport(s.transform.TransformPoint(new Vector3(0.3f, 1.0f, 0.6f)), s.transform.eulerAngles.y);
                        // the heads-up toast counts down (systems §1.4.1: "night skips in 5 s" with a 5-segment ring)
                        yield return Until(() => Sleep.I.Skipping, 30); string b0 = Hud.I.ToastBody("Host is going"); yield return new WaitForSecondsRealtime(2.2f);
                        string b1 = Hud.I.ToastBody("Host is going");
                        Check(b0 != null && b1 != null && b0 != b1 && (b1.Contains("in 3 s") || b1.Contains("in 2 s")), $"the heads-up counts down: '{b0}' -> 2.2 s -> '{b1}'");
                        if (shots != null) yield return Shot("coop-headsup");
                    }
                    else if (r == 5) me.Teleport(Landing.I.Spawn.position + Vector3.up * 0.05f + Vector3.right, 0);
                    else if (r == 6)
                    {
                        // the host lies down; a moment later, so do I (the race: the host's heads-up already on its way)
                        me.Teleport(Sleep.FarSide, Quaternion.LookRotation(Flat(Sleep.Cot.position - Sleep.FarSide)).eulerAngles.y);
                        yield return Until(() => Sleep.I.Skipping, 30); Sleep.I.TestLie(); yield return new WaitForSeconds(0.6f);
                        var p0 = me.transform.position; Ctl.BotMove = new Vector2(0, 1); yield return new WaitForSeconds(1f); Ctl.BotMove = Vector2.zero;
                        float moved = Vector3.Distance(Flat(p0), Flat(me.transform.position));
                        Check(Sleep.I.Lying && moved < 0.05f, $"round 6: in the cot with the host; pushing forward 1 s moves me {moved:F2} m (lying: no walking)");
                    }
                    else
                    {
                        // I go to bed; the host leaves mid heads-up: I still sleep, alone
                        yield return new WaitForSeconds(1f); yield return LieDown(); Check(Sleep.I.Lying, "round 7: the rider lies down");
                        yield return Until(() => !Coop.On, 20);
                        Debug.Log($"[PB] m3: round 7: the host is gone ({(Coop.On ? "still on" : "solo now")}) at {DayTime.Clock(Sky.Minutes)}");
                    }
                    if (r != 1) yield return WaitSkip(); else yield return Until(() => !Sleep.I.Skipping, 40);
                    int now = 0; foreach (var sl in me.Bar.S) now += sl.Fish.Count;
                    bool ok = Sleep.I.LastWake != null && Sleep.I.LastWake.Contains(want[r]);
                    if (r == 1) ok &= now == had + 1;
                    if (r == 3) ok &= s.Anchored;
                    if (r == 6) ok &= Vector3.Distance(me.transform.position, Sleep.FarSide) < 0.6f && Sky.Day == 8 && Sky.Hour < 7.25;
                    if (r == 7) ok &= !Coop.On && Sky.Day == 9 && Sky.Hour < 7.25 && !Sleep.I.Skipping && !Sleep.I.Still;
                    Check(ok, $"round {r}: rider ({(r == 1 ? $"fish {had}->{now}" : r == 3 ? (s.Anchored ? "Biscuit anchored" : "Biscuit NOT anchored") : "")}) " + WakeLine());
                }
                if (r < 7) Debug.Log($"[PB] m3: clock after round {r}: Day {Sky.Day} {Sky.Minutes:F1}");
                yield return new WaitForSeconds(2f);
            }
            Debug.Log($"[PB] RESULT sleeptest {(host ? "host" : "rider")}: {(dayFails == 0 ? "PASS" : "FAIL")}");
            yield return new WaitForSeconds(host ? 4 : 1);
            Application.Quit();
        }
    }
}
