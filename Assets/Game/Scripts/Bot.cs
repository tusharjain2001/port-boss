using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    // Headless test scenarios (-pb-* switches, the AutoRun pattern, smaller). Every run ends with [PB] RESULT lines.
    //   -pb-fishtest N [csv]  a bot casts N times from Biscuit's rail and plays the bar with σ = -pb-sigma ms (70)
    //   -pb-shots <dir>       the bot fishes and saves screenshots at each step (cast, bobber, bite, minigame, catch, hotbar)
    //   -pb-boattest          drive, turn, anchor: logs roll/pitch and the anchor's hold
    //   -pb-timescale x, -pb-quit s, -pb-rod n, -pb-bait id, -pb-fightheavy, -pb-film <dir> <secs> (30 fps frames)
    public class Bot : MonoBehaviour
    {
        static string Arg(string n) { var a = System.Environment.GetCommandLineArgs(); for (int i = 0; i < a.Length; i++) if (a[i] == n) return i + 1 < a.Length ? a[i + 1] : ""; return null; }
        static float ArgF(string n, float d) { var s = Arg(n); return s != null && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : d; }

        Fishing fish; Angler me; float sigma; string shots; int shotN;
        readonly List<string> rows = new();

        IEnumerator Start()
        {
            var ts = Arg("-pb-timescale"); if (ts != null) Time.timeScale = ArgF("-pb-timescale", 1);
            var q = Arg("-pb-quit"); if (q != null) StartCoroutine(QuitAfter(ArgF("-pb-quit", 60)));
            yield return null; yield return null;
            me = Angler.I; fish = me.Fish;
            var rod = Arg("-pb-rod"); if (rod != null) fish.SetRod((int)ArgF("-pb-rod", 1));
            var bait = Arg("-pb-bait"); if (bait != null) fish.BaitI = Mathf.Max(0, System.Array.FindIndex(FishDb.Baits, b => b.Id == bait));
            Fishing.Bot.FightHeavy = Arg("-pb-fightheavy") != null;
            sigma = ArgF("-pb-sigma", 70) / 1000f;
            fish.OnLog += s => Debug.Log("[PB] fish " + s);
            var film = Arg("-pb-film"); if (film != null) StartCoroutine(Film(film, ArgF("-pb-filmsecs", 60)));
            if (Arg("-pb-fishtest") != null) yield return FishTest((int)ArgF("-pb-fishtest", 50), Arg("-pb-csv"));
            else if (Arg("-pb-shots") != null) { shots = Arg("-pb-shots"); Directory.CreateDirectory(shots); yield return ShotTour(); }
            else if (Arg("-pb-boattest") != null) yield return BoatTest();
        }

        IEnumerator QuitAfter(float s) { yield return new WaitForSecondsRealtime(s); Debug.Log("[PB] quit"); Application.Quit(); }

        // ------------------------------------------------------------------ the fishing bot
        void Board()
        {
            // stand at Biscuit's starboard rail facing out to sea
            var s = Skiff.I; var spot = s.Rails[1] != null ? s.Rails[1] : s.transform;
            me.Cc.enabled = false; me.transform.position = spot.position + Vector3.up * 0.05f; me.transform.rotation = Quaternion.LookRotation(s.transform.right); me.Cc.enabled = true;
            CameraRig.I.Yaw = s.transform.eulerAngles.y + 90; CameraRig.I.Pitch = 22;
        }

        IEnumerator CastOnce(float power)
        {
            Ctl.Bot = true;
            while (fish.State != Fishing.St.Ready) yield return null;
            Ctl.BotUse = true; Ctl.BotUsePressed = true; yield return null; Ctl.BotUsePressed = false;
            // hold for the wanted power on the 1.1 s ping-pong (first rise)
            while (fish.State == Fishing.St.Charging && fish.Power < power - 0.01f && fish.StateT < Fishing.ChargePeriod) yield return null;
            if (shots != null) yield return Shot("1-charge");
            Ctl.BotUse = false; yield return null;
        }

        // Plays one fish to the end. Strikes on the bite after a human-ish reaction, never on a nibble; stops the bar
        // aiming at the zone's centre with a normal error of sigma (the "practised player" of fishing §3.7 is 70 ms).
        IEnumerator PlayFish(System.Action<string> row)
        {
            float settle = -1, bite = -1, planned = 0; int nibbles = 0; string sp = "";
            double plannedPress = -1; var errs = new List<float>();
            float t0 = Time.time;
            while (true)
            {
                var st = fish.State;
                if (st == Fishing.St.Waiting && settle < 0) { settle = Time.time; planned = fish.BiteWait; nibbles = fish.Nibbles; sp = fish.Hooked.Id; }
                if (st == Fishing.St.Bite)
                {
                    if (bite < 0) { bite = Time.time; if (shots != null && shotN < 30) yield return Shot("3-bite"); }
                    if (Time.time - bite > 0.3f) { Ctl.BotStrike = true; yield return null; Ctl.BotStrike = false; }
                }
                if (st == Fishing.St.Minigame && !fish.Frozen)
                {
                    if (plannedPress < 0)
                    {
                        // when does the pointer next cross the zone centre? (scan ahead in 2 ms steps)
                        double now = Time.timeAsDouble; double hitT = now;
                        for (double t = now; t < now + 3.0 / fish.PtrSpeed; t += 0.002)
                            if (Mathf.Abs(fish.PointerAt(t) - fish.ZoneAt(t)) < 0.004f && t > now + 0.15) { hitT = t; break; }
                        float e = Gauss() * sigma; errs.Add(e * 1000);
                        plannedPress = hitT + e;
                    }
                    if (Time.timeAsDouble >= plannedPress)
                    {
                        Bot.PressAt = plannedPress; Ctl.BotStrike = true;
                        if (shots != null && fish.Stop == 1) yield return Shot("4-minigame");
                        yield return null; Ctl.BotStrike = false; plannedPress = -1;
                    }
                }
                if (st == Fishing.St.Reveal && fish.StateT > 0.7f)
                {
                    if (shots != null && fish.StateT < 1.0f) { yield return Shot("5-catch"); }
                    Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false;
                }
                if ((st == Fishing.St.Ready || st == Fishing.St.Recover) && Time.time - t0 > 1f && settle >= 0)
                {
                    string outcome = fish.LastOutcome ?? "landed";
                    row(string.Join(",", sp, settle >= 0 ? planned.ToString("F3", CultureInfo.InvariantCulture) : "", bite >= 0 && settle >= 0 ? (bite - settle).ToString("F3", CultureInfo.InvariantCulture) : "",
                        nibbles, outcome, fish.Hits, fish.Catch?.Grade.ToString() ?? "", string.Join("|", errs.ConvertAll(x => x.ToString("F0", CultureInfo.InvariantCulture)))));
                    fish.LastOutcome = null;
                    yield break;
                }
                if (Time.time - t0 > 60f) { row(string.Join(",", sp, "", "", 0, "stuck " + st, 0, "", "")); Debug.LogWarning("[PB] fishtest stuck in " + st); yield break; }
                yield return null;
            }
        }
        public static double PressAt;

        static float Gauss() { float u1 = 1f - Random.value, u2 = Random.value; return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2); }

        IEnumerator FishTest(int n, string csv)
        {
            yield return new WaitForSeconds(0.5f);
            Board(); me.Select(0);
            var lines = new List<string> { "cast,species,planned_wait_s,measured_wait_s,nibbles,outcome,hits,grade,stop_err_ms" };
            var waits = new List<float>(); var grades = new int[4]; int landed = 0;
            for (int i = 0; i < n; i++)
            {
                yield return CastOnce(0.55f + 0.4f * Random.value);
                string row = null;
                yield return PlayFish(r => row = r);
                lines.Add(i + 1 + "," + row);
                var f = row.Split(',');
                if (float.TryParse(f[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var w)) waits.Add(w);
                if (int.TryParse(f[6], out var g)) { grades[g]++; landed++; }
                if (me.Bar.Full) { for (int k = 1; k < 8; k++) me.Bar.S[k].Fish.Clear(); } // sold, so to speak
                if (i % 25 == 24) Debug.Log($"[PB] fishtest {i + 1}/{n}");
            }
            csv ??= Path.Combine(Application.isEditor ? Application.dataPath + "/../Logs" : Path.GetDirectoryName(Application.dataPath) + "/Logs", "fishtest.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(csv)); File.WriteAllLines(csv, lines);
            waits.Sort();
            float min = waits.Count > 0 ? waits[0] : -1, max = waits.Count > 0 ? waits[^1] : -1, mean = 0; foreach (var w in waits) mean += w; mean /= Mathf.Max(1, waits.Count);
            bool waitOk = min >= 4.0f - 0.02f && max <= fish.Rod.BiteHi + 0.05f;
            // expected grade mix: each stop a hit when |error| ≤ half the window (window = zone / speed, rod-scaled)
            string mix = ""; float[] exp = ExpectedMix(); bool mixOk = true;
            for (int g = 0; g < 4; g++) { float got = grades[g] / (float)Mathf.Max(1, landed); mix += $"{Ponyo.GradeName[g]} {got:P0} (model {exp[g]:P0}) "; if (Mathf.Abs(got - exp[g]) > 0.03f + 1.5f * Mathf.Sqrt(exp[g] * (1 - exp[g]) / Mathf.Max(1, landed))) mixOk = false; }
            Debug.Log($"[PB] fishtest csv {csv}");
            Debug.Log($"[PB] RESULT bites {waits.Count}/{n}: wait min {min:F2} max {max:F2} mean {mean:F2} s (rod {fish.Rod.BiteLo}-{fish.Rod.BiteHi}) -> {(waitOk ? "PASS" : "FAIL")}");
            Debug.Log($"[PB] RESULT grade mix (landed {landed}, sigma {sigma * 1000:0} ms, Everyday on {fish.Rod.Name}): {mix}-> {(mixOk ? "PASS" : "CHECK")}");
            Application.Quit();
        }

        float[] ExpectedMix()
        {
            // Everyday on this rod (the starter's three fish are all Everyday)
            float zone = FishDb.Green[0] * fish.Rod.GreenMul, sp = FishDb.Speed[0] * fish.Rod.SpeedMul;
            float[] p = new float[3];
            for (int s = 0; s < 3; s++) { float half = zone / 2 / (sp * new[] { 1f, 1.08f, 1.16f }[s]); p[s] = Erf(half / (sigma * Mathf.Sqrt(2))); }
            var r = new float[4];
            for (int m = 0; m < 8; m++) { int hits = 0; float pr = 1; for (int s = 0; s < 3; s++) { bool h = (m >> s & 1) == 1; hits += h ? 1 : 0; pr *= h ? p[s] : 1 - p[s]; } r[hits] += pr; }
            return r;
        }
        static float Erf(float x) { float t = 1 / (1 + 0.3275911f * Mathf.Abs(x)); float y = 1 - (((((1.061405429f * t - 1.453152027f) * t) + 1.421413741f) * t - 0.284496736f) * t + 0.254829592f) * t * Mathf.Exp(-x * x); return x >= 0 ? y : -y; }

        // ------------------------------------------------------------------ screenshots
        IEnumerator ShotTour()
        {
            yield return new WaitForSeconds(1.5f);
            yield return Shot("0-start");
            me.Select(0); yield return new WaitForSeconds(0.6f);
            yield return Shot("0-rod-in-hand");
            Board(); yield return new WaitForSeconds(0.8f);
            yield return Shot("0-on-deck");
            for (int i = 0; i < 9; i++)
            {
                yield return CastOnce(i == 0 ? 0.97f : 0.6f + 0.35f * Random.value);
                while (fish.State == Fishing.St.Flight) yield return null;
                if (i == 0) { yield return new WaitForSeconds(0.9f); yield return Shot("2-bobber"); }
                yield return PlayFish(r => Debug.Log("[PB] shots fish " + r));
                yield return new WaitForSeconds(0.3f);
                if (i == 0) { me.Select(1); yield return new WaitForSeconds(0.5f); yield return Shot("6-hotbar-fish"); me.Select(0); yield return new WaitForSeconds(0.5f); }
            }
            Hud.I.ShowDebug = true; yield return new WaitForSeconds(0.2f);
            yield return Shot("6-hotbar");
            Hud.I.ShowDebug = false;
            // the boat from the sea
            CameraRig.I.Yaw += 140; CameraRig.I.Pitch = 12; yield return new WaitForSeconds(1.2f); yield return Shot("7-wide");
            Debug.Log("[PB] RESULT shots " + shotN);
            Application.Quit();
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(shots ?? ".", $"{shotN++:00}-{name}.png");
            Save(path);
        }

        public static void Save(string path)
        {
            var rt = Game.BatchTarget;
            if (rt == null) { ScreenCapture.CaptureScreenshot(path); return; }
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(path, tex.EncodeToPNG()); Destroy(tex);
            Debug.Log("[PB] shot " + path);
        }

        IEnumerator Film(string dir, float secs)
        {
            Directory.CreateDirectory(dir); Time.captureFramerate = 30; int i = 0;
            float end = Time.time + secs;
            while (Time.time < end) { yield return new WaitForEndOfFrame(); Save(Path.Combine(dir, $"f_{i++:00000}.png")); }
            Time.captureFramerate = 0; Debug.Log($"[PB] film {i} frames");
        }

        // ------------------------------------------------------------------ the boat
        IEnumerator BoatTest()
        {
            yield return new WaitForSeconds(0.5f);
            var s = Skiff.I; shots = Arg("-pb-boatshots");
            if (shots != null) Directory.CreateDirectory(shots);
            float maxRoll = 0, maxPitch = 0;
            // at rest, moored: level and floating
            for (float t = 0; t < 5; t += Time.deltaTime) { maxRoll = Mathf.Max(maxRoll, Mathf.Abs(Mathf.DeltaAngle(0, s.transform.eulerAngles.z))); maxPitch = Mathf.Max(maxPitch, Mathf.Abs(Mathf.DeltaAngle(0, s.transform.eulerAngles.x))); yield return null; }
            float draft = s.transform.position.y - Sea.Height(s.transform.position);
            Debug.Log($"[PB] boat moored: max roll {maxRoll:F1}°, pitch {maxPitch:F1}°, height vs sea {draft:F3} m");
            if (shots != null) yield return Shot("boat-moored");
            // drive out: cast off, full, a wide turn
            s.AtHelm = true; s.CastOff(); s.Notch = 3; var start = s.transform.position; maxRoll = 0;
            for (float t = 0; t < 12; t += Time.deltaTime) { s.Steer = t > 4 ? 0.6f : 0; maxRoll = Mathf.Max(maxRoll, Mathf.Abs(Mathf.DeltaAngle(0, s.transform.eulerAngles.z))); yield return null; }
            Debug.Log($"[PB] boat drive: {Vector3.Distance(start, s.transform.position):F1} m in 12 s, speed {s.Speed:F2} m/s, max roll {maxRoll:F1}° (Clear limit 6 + turn lean 3)");
            if (shots != null) yield return Shot("boat-driving");
            s.Notch = 0; s.Steer = 0; s.AtHelm = false;
            for (float t = 0; t < 5; t += Time.deltaTime) yield return null;
            s.ToggleAnchor(); for (float t = 0; t < 2; t += Time.deltaTime) yield return null;
            var a = s.transform.position; float far = 0;
            for (float t = 0; t < 30; t += Time.deltaTime) { far = Mathf.Max(far, Vector3.Distance(Flat(a), Flat(s.transform.position))); yield return null; }
            Debug.Log($"[PB] RESULT boat anchored {s.Anchored}: drifted at most {far:F2} m in 30 s (limit 6) -> {(s.Anchored && far <= 6.05f ? "PASS" : "FAIL")}");
            Application.Quit();
        }
        static Vector3 Flat(Vector3 v) => new(v.x, 0, v.z);
    }
}
