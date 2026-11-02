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
            Fishing.Bot.FightHeavy = Arg("-pb-fightheavy") != null; Fishing.Bot.LetGo = Arg("-pb-letgo") != null; // -pb-letgo: every catch thrown back
            Fishing.Bot.OldFacing = Arg("-pb-oldfacing") != null; Fishing.Bot.ForceHeavy = Arg("-pb-forceheavy") != null; // test: every hook is heavy
            sigma = ArgF("-pb-sigma", 70) / 1000f;
            fish.OnLog += s => Debug.Log("[PB] fish " + s);
            var icons = Arg("-pb-icons"); // the painted rod icon, as a file (proof)
            if (icons != null) { Directory.CreateDirectory(icons); for (int t = 1; t <= 5; t++) File.WriteAllBytes(Path.Combine(icons, $"rod-icon-tier{t}.png"), UiKit.RodIcon(t).EncodeToPNG()); }
                        var film = Arg("-pb-film"); if (film != null) StartCoroutine(Film(film, ArgF("-pb-filmsecs", 60)));
            if (Arg("-pb-fishtest") != null) yield return FishTest((int)ArgF("-pb-fishtest", 50), Arg("-pb-csv"));
            else if (Arg("-pb-shots") != null) { shots = Arg("-pb-shots"); Directory.CreateDirectory(shots); yield return ShotTour(); }
            else if (Arg("-pb-boattest") != null) yield return BoatTest();
            else if (Arg("-pb-swimtest") != null) yield return SwimTest();
            else if (Arg("-pb-laddertest") != null) yield return LadderTest(Arg("-pb-laddertest"));
            else if (Arg("-pb-camtest") != null) yield return CamTest((int)ArgF("-pb-camtest", 6));
            else if (Arg("-pb-helmshots") != null) { shots = Arg("-pb-helmshots"); Directory.CreateDirectory(shots); yield return HelmShots(); }
            else if (Arg("-pb-scrolltest") != null) yield return ScrollTest();
            else if (Arg("-pb-idleshots") != null) { shots = Arg("-pb-idleshots"); Directory.CreateDirectory(shots); yield return IdleShots(); }
            else if (Arg("-pb-cast8") != null) { shots = Arg("-pb-cast8"); Directory.CreateDirectory(shots); yield return Cast8(); }
            else if (Arg("-pb-tour") != null) yield return Tour(Arg("-pb-tour"), Arg("-pb-side") != null);
            else if (Arg("-pb-feeltest") != null) yield return FeelTest(Arg("-pb-feeltest"));
            else if (Arg("-pb-knocktest") != null) yield return KnockTest(Arg("-pb-knocktest"));
            else if (Arg("-pb-fadetest") != null) { shots = Arg("-pb-fadetest"); Directory.CreateDirectory(shots); yield return FadeTest(); }
        }

        IEnumerator QuitAfter(float s) { yield return new WaitForSecondsRealtime(s); Debug.Log("[PB] quit"); Application.Quit(); }

        // ------------------------------------------------------------------ the fishing bot
        void Board()
        {
            // stand at Biscuit's starboard rail facing out to sea
            var s = Skiff.I;
            bool blockedRight = Physics.Raycast(s.transform.position + Vector3.up * 1.5f, s.transform.right, 12f);
            var spot = (blockedRight ? s.Rails[0] : s.Rails[1]) ?? s.transform; var outward = blockedRight ? -s.transform.right : s.transform.right;
            me.Teleport(spot.position + Vector3.up * 0.05f, Quaternion.LookRotation(outward).eulerAngles.y);
            CameraRig.I.Yaw = Quaternion.LookRotation(outward).eulerAngles.y; CameraRig.I.Pitch = 22;
        }

        // Take the tiller, cast off, motor out past the pier end into open water, stop, drop the anchor at the bow,
        // then stand at a rail facing the open sea (the M1 MUST: fishing from the boat at sea).
        IEnumerator SailOut(bool shotsOn)
        {
            Ctl.Bot = true;
            var s = Skiff.I;
            me.Teleport(s.Helm.position + Vector3.up * 0.05f, s.transform.eulerAngles.y);
            yield return null; me.TakeHelm(); yield return new WaitForSeconds(0.4f);
            if (shotsOn) yield return Shot("0-at-the-tiller");
            Ctl.BotThrottle = 1; yield return new WaitForSeconds(0.6f); Ctl.BotThrottle = 1; // cast off, then 2/3
            var start = s.transform.position;
            float t = 0;
            while (t < 16f) { Ctl.BotSteer = t < 3.5f ? -0.6f : t < 6f ? 0.45f : 0; /* away from the pier (on the right at the berth), then straighten */ t += Time.deltaTime; if (shotsOn && Mathf.Abs(t - 7f) < Time.deltaTime) yield return Shot("0-motoring-out"); yield return null; }
            Ctl.BotSteer = 0; Ctl.BotThrottle = -1; yield return null; Ctl.BotThrottle = -1; yield return null;
            for (float w = 0; Mathf.Abs(s.Speed) > 0.05f && w < 8; w += Time.deltaTime) yield return null;
            me.LeaveHelm(); yield return new WaitForSeconds(0.3f);
            var bow = s.Rails[2] != null ? s.Rails[2].position : s.AnchorPt.position - s.transform.forward * 0.8f;
            me.Teleport(bow + Vector3.up * 0.05f, s.transform.eulerAngles.y);
            yield return new WaitForSeconds(0.2f);
            Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false;
            for (float w = 0; !s.Anchored && w < 4; w += Time.deltaTime) yield return null;
            if (!s.Anchored) Debug.LogWarning("[PB] anchor did not drop (bot not at the bow?)");
            Debug.Log($"[PB] sailed out {Vector3.Distance(Flat(start), Flat(s.transform.position)):F0} m from the berth, anchored at {s.transform.position}");
            Board(); yield return new WaitForSeconds(0.8f);
            if (shotsOn) yield return Shot("0-anchored-open-sea");
            if (Arg("-pb-dumpnear") != null) // debug: everything drawn near the water round the boat
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    var b = r.bounds; if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                    // only what covers the screen point -pb-dumpnear x,y (viewport) at this shot
                    var at = Arg("-pb-dumpnear").Split(','); var vp = new Vector2(float.Parse(at[0], CultureInfo.InvariantCulture), float.Parse(at[1], CultureInfo.InvariantCulture));
                    var cam = CameraRig.I.Cam; float x0 = 9, x1 = -9, y0 = 9, y1 = -9; bool front = false;
                    for (int c = 0; c < 8; c++) { var w = b.center + Vector3.Scale(b.extents, new Vector3((c & 1) * 2 - 1, (c >> 1 & 1) * 2 - 1, (c >> 2 & 1) * 2 - 1)); var v = cam.WorldToViewportPoint(w); if (v.z > 0) front = true; x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x); y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y); }
                    if (!front || vp.x < x0 || vp.x > x1 || vp.y < y0 || vp.y > y1 || b.size.x > 500) continue;
                    Debug.Log($"[PB] near: {r.name} ({r.GetType().Name}) parent {(r.transform.parent ? r.transform.parent.name : "-")} mat {(r.sharedMaterial ? r.sharedMaterial.name + "/" + r.sharedMaterial.shader.name : "none")} at {b.center} size {b.size}");
                }
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
            float settle = -1, bite = -1, planned = 0; int nibbles = 0; string sp = ""; int grade = -1;
            double plannedPress = -1; var errs = new List<float>();
            float t0 = Time.time;
            while (true)
            {
                var st = fish.State;
                if (st == Fishing.St.Waiting && settle < 0) { settle = fish.WaitStart; planned = fish.BiteWait; nibbles = fish.Nibbles; sp = fish.Hooked.Id; }
                if (st == Fishing.St.Bite)
                {
                    if (bite < 0) { bite = fish.BiteSeenAt; if (shots != null && shotN < 30) yield return Shot("3-bite"); }
                    if (Time.time - bite > 0.3f && Time.time - bite < 1.0f) { Ctl.BotStrike = true; yield return null; Ctl.BotStrike = false; }
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
                        if (shots != null && fish.Stop == 0) yield return Shot("4-minigame");
                        yield return null; Ctl.BotStrike = false; plannedPress = -1;
                    }
                }
                if (st == Fishing.St.Reveal && fish.Catch != null) grade = fish.Catch.Grade;
                if (st == Fishing.St.Reveal && fish.StateT > 0.7f)
                {
                    if (shots != null && fish.StateT < 1.0f) { yield return Shot("5-catch"); }
                    Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false;
                }
                if ((st == Fishing.St.Ready || st == Fishing.St.Recover) && Time.time - t0 > 1f && settle >= 0)
                {
                    string outcome = fish.LastOutcome ?? "landed";
                    row(string.Join(",", sp, settle >= 0 ? planned.ToString("F3", CultureInfo.InvariantCulture) : "", bite >= 0 && settle >= 0 ? (bite - settle).ToString("F3", CultureInfo.InvariantCulture) : "",
                        nibbles, outcome, fish.Hits, grade >= 0 ? grade.ToString() : "", string.Join("|", errs.ConvertAll(x => x.ToString("F0", CultureInfo.InvariantCulture)))));
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
            yield return SailOut(false); me.Select(0);
            // nothing to look at in a 500-cast run: stop drawing so game time runs fast with small, even frames
            CameraRig.I.Cam.enabled = false; CameraRig.I.UiCam.enabled = false;
            var lines = new List<string> { "cast,species,planned_wait_s,measured_wait_s,nibbles,outcome,hits,grade,stop_err_ms" };
            var waits = new List<float>(); var grades = new int[2]; int landed = 0; float pmin = 99, pmax = 0, lagMax = 0;
            for (int i = 0; i < n; i++)
            {
                yield return CastOnce(0.55f + 0.4f * Random.value);
                string row = null;
                yield return PlayFish(r => row = r);
                lines.Add(i + 1 + "," + row);
                var f = row.Split(',');
                if (float.TryParse(f[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var w)) waits.Add(w);
                if (float.TryParse(f[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var pw)) { pmin = Mathf.Min(pmin, pw); pmax = Mathf.Max(pmax, pw); lagMax = Mathf.Max(lagMax, w - pw); }
                if (int.TryParse(f[6], out var g)) { grades[g]++; landed++; }
                if (me.Bar.Full) { for (int k = 1; k < 8; k++) me.Bar.S[k].Fish.Clear(); } // sold, so to speak
                if (i % 25 == 24) Debug.Log($"[PB] fishtest {i + 1}/{n}");
            }
            csv ??= Path.Combine(Application.isEditor ? Application.dataPath + "/../Logs" : Path.GetDirectoryName(Application.dataPath) + "/Logs", "fishtest.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(csv)); File.WriteAllLines(csv, lines);
            waits.Sort();
            float min = waits.Count > 0 ? waits[0] : -1, max = waits.Count > 0 ? waits[^1] : -1, mean = 0; foreach (var w in waits) mean += w; mean /= Mathf.Max(1, waits.Count);
            bool waitOk = min >= fish.Rod.BiteLo && max <= fish.Rod.BiteHi;
            // expected grade mix: each stop a hit when |error| ≤ half the window (window = zone / speed, rod-scaled)
            string mix = ""; float[] exp = ExpectedMix(); bool mixOk = true;
            for (int g = 0; g < 2; g++) { float got = grades[g] / (float)Mathf.Max(1, landed); mix += $"{Ponyo.GradeName[g]} {got:P0} (model {exp[g]:P0}) "; if (Mathf.Abs(got - exp[g]) > 0.03f + 1.5f * Mathf.Sqrt(exp[g] * (1 - exp[g]) / Mathf.Max(1, landed))) mixOk = false; }
            Debug.Log($"[PB] fishtest csv {csv}");
            Debug.Log($"[PB] RESULT bites {waits.Count}/{n}: seen on screen after min {min:F3} max {max:F3} mean {mean:F2} s (rod {fish.Rod.BiteLo}-{fish.Rod.BiteHi}, timescale {Time.timeScale}) -> {(waitOk ? "PASS" : "FAIL")}");
            Debug.Log($"[PB] RESULT bite timer (planned) min {pmin:F3} max {pmax:F3} s -> {(pmin >= fish.Rod.BiteLo && pmax <= fish.Rod.BiteHi ? "PASS" : "FAIL")}; frame lag max {lagMax * 1000:F0} ms");
            Debug.Log($"[PB] RESULT grade mix (landed {landed}, sigma {sigma * 1000:0} ms, Everyday on {fish.Rod.Name}): {mix}-> {(mixOk ? "PASS" : "CHECK")}");
            Application.Quit();
        }

        float[] ExpectedMix()
        {
            // Everyday on this rod (the starter's three fish are all Everyday): ONE stop, a hit when |error| <= half the
            // window (window = zone / speed, rod-scaled, zone 1/8 of the old width) -> Rare, else Basic
            float zone = FishDb.Green[0] * fish.Rod.GreenMul * Fishing.ZoneScale, sp = FishDb.Speed[0] * fish.Rod.SpeedMul * Fishing.SpeedScale;
            float p = Erf(zone / 2 / sp / (sigma * Mathf.Sqrt(2)));
            return new[] { 1 - p, p };
        }
        static float Erf(float x) { float t = 1 / (1 + 0.3275911f * Mathf.Abs(x)); float y = 1 - (((((1.061405429f * t - 1.453152027f) * t) + 1.421413741f) * t - 0.284496736f) * t + 0.254829592f) * t * Mathf.Exp(-x * x); return x >= 0 ? y : -y; }

        // ------------------------------------------------------------------ screenshots
        IEnumerator ShotTour()
        {
            yield return new WaitForSeconds(1.5f);
            yield return Shot("0-start");
            me.Select(0); yield return new WaitForSeconds(0.6f);
            yield return Shot("0-rod-in-hand");
            yield return SailOut(true);
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

        public static void Save(string path, int w = 1920, int h = 1080)
        {
            if (!Application.isBatchMode || CameraRig.I == null) { ScreenCapture.CaptureScreenshot(path); return; }
            // batchmode draws nothing by itself: render the scene camera, then the UI camera, each into its own fresh
            // texture, and lay the (premultiplied) UI over the scene, as the stacked cameras do in the game
            var tex = Grab(CameraRig.I.Cam, w, h, false);
            // URP writes alpha 1 for the UI camera's empty background, so recover the UI's coverage by drawing it over
            // black and over white: alpha = 1 - (white - black), colour (premultiplied) = the black pass
            var uc = CameraRig.I.UiCam; var bg = uc.backgroundColor;
            uc.backgroundColor = Color.black; var ub = Grab(uc, w, h, false);
            uc.backgroundColor = Color.white; var uw = Grab(uc, w, h, false);
            uc.backgroundColor = bg;
            var a = tex.GetPixels32(); var b = ub.GetPixels32(); var wh = uw.GetPixels32(); long sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                sum += a[i].r + a[i].g + a[i].b;
                int k = Mathf.Clamp(((wh[i].r - b[i].r) + (wh[i].g - b[i].g) + (wh[i].b - b[i].b)) / 3, 0, 255); // 255 - alpha
                a[i] = new Color32((byte)Mathf.Min(255, b[i].r + a[i].r * k / 255), (byte)Mathf.Min(255, b[i].g + a[i].g * k / 255), (byte)Mathf.Min(255, b[i].b + a[i].b * k / 255), 255);
            }
            tex.SetPixels32(a); tex.Apply();
            File.WriteAllBytes(path, path.EndsWith(".jpg") ? tex.EncodeToJPG(90) : tex.EncodeToPNG()); Destroy(tex); Destroy(ub); Destroy(uw);
            if (!path.EndsWith(".jpg")) Debug.Log($"[PB] shot {path} (scene brightness {sum / (3.0 * a.Length):F0})");
        }

        static Texture2D Grab(Camera c, int w, int h, bool alpha)
        {
            var keep = c.targetTexture;
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
            c.targetTexture = rt; c.Render(); c.targetTexture = keep;
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(w, h, alpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, w, h), 0, 0); t.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            return t;
        }

        // -pb-tour <dir> [-pb-side]: the animation proof. Films (30 fps game time, 1280x720 jpg) walking the deck,
        // two full casts to the catch, then the tiller and a short drive. -pb-side films it all from a fixed 3/4
        // camera beside the angler instead of the game camera (for the contact sheets).
        IEnumerator Tour(string dir, bool side)
        {
            yield return new WaitForSeconds(0.5f);
            yield return SailOut(false);
            var s = Skiff.I; Ctl.Bot = true;
            me.Select(0); yield return new WaitForSeconds(0.8f);
            filmSide = side; StartCoroutine(Film(dir, 999, 1280, 720));
            StartCoroutine(LogBeats());
            yield return new WaitForSeconds(0.8f);
            // walk the deck: aft along the rail, turn, back, then turn to the sea
            float yaw0 = CameraRig.I.Yaw;
            Debug.Log("[PB] tour walk " + Time.time);
            Ctl.BotMove = new Vector2(1, 0); yield return new WaitForSeconds(0.9f);
            Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(0.7f);
            Ctl.BotMove = new Vector2(-1, 0); yield return new WaitForSeconds(0.9f);
            Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(0.9f);
            // once more, then stand a moment (the idle: breathing, the float hanging from the tip)
            Ctl.BotMove = new Vector2(1, 0); yield return new WaitForSeconds(0.8f);
            Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(0.5f);
            Ctl.BotMove = new Vector2(-1, 0); yield return new WaitForSeconds(0.8f);
            Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(2.0f);
            for (int i = 0; i < 2; i++)
            {
                Debug.Log($"[PB] tour cast {i + 1} at {Time.time:F2}");
                yield return CastOnce(i == 0 ? 0.85f : 0.6f);
                yield return PlayFish(r => Debug.Log("[PB] tour fish " + r));
                yield return new WaitForSeconds(0.8f);
            }
            // to the tiller: stow the rod, walk to the helm, take it, anchor up, motor off
            Debug.Log("[PB] tour helm " + Time.time);
            me.Select(1); yield return new WaitForSeconds(0.5f);
            float t = 0;
            while (t < 6 && me.Prompt != "Take the tiller")
            {
                var to = s.Helm.position - me.transform.position; to.y = 0;
                var local = Quaternion.Euler(0, -CameraRig.I.Yaw, 0) * to.normalized;
                Ctl.BotMove = to.magnitude > 0.3f ? new Vector2(local.x, local.z) * 0.6f : Vector2.zero;
                t += Time.deltaTime; yield return null;
            }
            Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(0.3f);
            Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false;
            if (s.Anchored) s.ToggleAnchor();
            yield return new WaitForSeconds(1.8f);
            Ctl.BotThrottle = 1; yield return new WaitForSeconds(0.4f); Ctl.BotThrottle = 1;
            for (t = 0; t < 26; t += Time.deltaTime) { Ctl.BotSteer = t > 3 && t < 7 ? 0.5f : t > 11 && t < 15 ? -0.6f : 0; yield return null; } // a turn each way
            Debug.Log("[PB] RESULT tour done " + Time.time);
            Application.Quit();
        }
        static bool filmSide;
        // the tour's beats with their times, so the proof script can cut contact sheets of them
        IEnumerator LogBeats()
        {
            var last = fish.State;
            while (true)
            {
                if (fish.State != last && fish.State is Fishing.St.Heavy or Fishing.St.Minigame or Fishing.St.Reel or Fishing.St.Reveal or Fishing.St.Bite)
                    Debug.Log($"[PB] tour beat {fish.State} at {Time.time:F2}");
                last = fish.State; yield return null;
            }
        }

        // -pb-cast8 <dir>: stand mid-deck facing the bow, aim the camera at 8 headings round the boat (0 = over the bow,
        // 180 = behind him, over the motor) and cast. The body must face where the line goes, from the charge to the
        // bobber settling: logs the worst angle between his facing and the cast, PASS under 25 degrees.
        IEnumerator Cast8()
        {
            yield return new WaitForSeconds(0.5f);
            yield return SailOut(false);
            var s = Skiff.I; Ctl.Bot = true; me.Select(0);
            float worstAll = 0;
            for (int k = 0; k < 8; k++)
            {
                me.Teleport(s.transform.position + Vector3.up * 0.2f, s.transform.eulerAngles.y);
                float aim = s.transform.eulerAngles.y + 45 * k;
                CameraRig.I.Yaw = aim; CameraRig.I.Pitch = 22;
                yield return new WaitForSeconds(0.8f);
                while (fish.State != Fishing.St.Ready) yield return null;
                Ctl.BotUse = true; Ctl.BotUsePressed = true; yield return null; Ctl.BotUsePressed = false;
                while (fish.State == Fishing.St.Charging && fish.StateT < 0.75f) yield return null;
                float atRelease = Off(); Ctl.BotUse = false; yield return null;
                float worst = 0; bool shot = false;
                for (float t = 0; t < 2.2f; t += Time.deltaTime)
                {
                    if (t > 0.6f) worst = Mathf.Max(worst, Off()); // after the turn-in toward a snapped marker
                    if (!shot && t > 0.25f) { shot = true; yield return Shot($"cast8-{45 * k:000}"); }
                    yield return null;
                }
                var m = fish.Marker - me.transform.position; m.y = 0;
                Debug.Log($"[PB] cast8 aim {45 * k,3} deg (camera vs bow): marker at {Mathf.DeltaAngle(s.transform.eulerAngles.y, Mathf.Atan2(m.x, m.z) * Mathf.Rad2Deg):F0} deg, body off the cast at release {atRelease:F0}, worst after {worst:F0} deg");
                worstAll = Mathf.Max(worstAll, worst);
                Ctl.BotCancel = true; yield return new WaitForSeconds(0.5f); Ctl.BotCancel = false;
                while (fish.State != Fishing.St.Ready) yield return null;
            }
            Debug.Log($"[PB] RESULT cast8 worst facing error {worstAll:F0} deg -> {(worstAll < 25 ? "PASS" : "FAIL")}");
            Application.Quit();
        }
        // -pb-helmshots <dir>: at the tiller, motoring, the tiller hard over each way; 4 close cameras round the stern.
        IEnumerator HelmShots()
        {
            yield return new WaitForSeconds(0.5f);
            var s = Skiff.I; Ctl.Bot = true;
            me.Teleport(s.Helm.position + Vector3.up * 0.05f, s.transform.eulerAngles.y);
            yield return null; me.TakeHelm(); yield return new WaitForSeconds(0.6f);
            Ctl.BotThrottle = 1; yield return new WaitForSeconds(0.5f); Ctl.BotThrottle = 1;
            Ctl.BotSteer = -0.4f; yield return new WaitForSeconds(4f); // off the berth
            var views = new[] { (new Vector3(-1.6f, 1.3f, 1.4f), "front-left"), (new Vector3(1.7f, 1.3f, 1.2f), "front-right"), (new Vector3(-1.4f, 1.6f, -1.8f), "back-left"), (new Vector3(1.3f, 1.8f, -1.6f), "back-right") };
            for (int k = 0; k < views.Length; k++)
            {
                Ctl.BotSteer = k % 2 == 0 ? 1 : -1; yield return new WaitForSeconds(1.0f);
                var bt = s.transform;
                // clearance: the tiller bar (pivot to grip) against the body (not the hand that holds it)
                var an = me.Anim.GetComponent<Animator>(); float clear = 9; string near = "";
                foreach (var (b, r) in new[] { (HumanBodyBones.Chest, 0.17f), (HumanBodyBones.Spine, 0.16f), (HumanBodyBones.Hips, 0.17f), (HumanBodyBones.LeftUpperArm, 0.07f), (HumanBodyBones.LeftLowerArm, 0.06f), (HumanBodyBones.RightUpperArm, 0.07f), (HumanBodyBones.LeftUpperLeg, 0.09f), (HumanBodyBones.LeftLowerLeg, 0.07f), (HumanBodyBones.Head, 0.12f) })
                {
                    var bt0 = an.GetBoneTransform(b); if (bt0 == null) continue;
                    var a0 = s.Motor.position; var g0 = s.TillerGrip.position; var ab = g0 - a0; float h = Mathf.Clamp01(Vector3.Dot(bt0.position - a0, ab) / ab.sqrMagnitude);
                    float d = Vector3.Distance(bt0.position, a0 + ab * h) - r - 0.035f; // bar radius 3.5 cm
                    if (d < clear) { clear = d; near = b.ToString(); }
                }
                Debug.Log($"[PB] helm clearance {views[k].Item2}: {clear * 100:F0} cm (nearest {near}) -> {(clear > 0.02f ? "ok" : "CLIPS")}");
                Debug.Log($"[PB] helm geo: me {bt.InverseTransformPoint(me.transform.position)} grip {bt.InverseTransformPoint(s.TillerGrip.position)} motor {bt.InverseTransformPoint(s.Motor.position)} rudder {s.Rudder:F0} lhand {bt.InverseTransformPoint(me.Anim.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.LeftHand).position)} rhand {bt.InverseTransformPoint(me.Anim.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand).position)}");
                yield return new WaitForEndOfFrame();
                var ct = CameraRig.I.Cam.transform; var a = me.transform;
                ct.position = a.TransformPoint(views[k].Item1);
                ct.rotation = Quaternion.LookRotation(a.TransformPoint(new Vector3(0, 0.75f, -0.15f)) - ct.position);
                Save(Path.Combine(shots, $"helm-{k}-{views[k].Item2}-steer{(k % 2 == 0 ? "R" : "L")}.png"));
            }
            Debug.Log("[PB] RESULT helmshots 4");
            Application.Quit();
        }

        // -pb-scrolltest: the mouse wheel through the real input path (a virtual mouse -> Input System -> Ctl -> Angler).
        // Wheel down = next slot, up = previous, wrapping 8 -> 1; one slot per notch; nothing while a cast/fight is on
        // (charging, minigame, reel, reveal all lock the bar) or while paused.
        IEnumerator ScrollTest()
        {
            yield return new WaitForSeconds(0.5f);
            var m = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("PBTestMouse"); m.MakeCurrent();
            int fails = 0;
            IEnumerator Wheel(float notches, int expect, string what)
            {
                UnityEngine.InputSystem.InputSystem.QueueDeltaStateEvent(m.scroll, new Vector2(0, -notches * WheelUnit));
                yield return null; yield return null;
                bool ok = me.Bar.Selected == expect; if (!ok) fails++;
                Debug.Log($"[PB] scroll {what}: slot {me.Bar.Selected + 1} (want {expect + 1}) {(ok ? "ok" : "FAIL")}");
            }
            me.Select(0); yield return null;
            for (int i = 1; i < 8; i++) yield return Wheel(1, i, $"down x{i}");
            yield return Wheel(1, 0, "down past 8 wraps to 1");
            yield return Wheel(-1, 7, "up from 1 wraps to 8");
            yield return Wheel(-1, 6, "up");
            yield return Wheel(3, 1, "3 notches in one frame");
            // locked while fishing: start a cast (Charging locks the bar, as do the minigame, the reel and the reveal)
            me.Select(0); yield return new WaitForSeconds(0.6f);
            Ctl.Bot = true; Ctl.BotUse = true; Ctl.BotUsePressed = true; yield return null; Ctl.BotUsePressed = false; yield return null;
            bool locked = fish.Locked;
            yield return Wheel(1, 0, $"while charging (locked {locked})");
            Ctl.BotUse = false; Ctl.BotCancel = true; yield return new WaitForSeconds(2.5f); Ctl.BotCancel = false;
            while (fish.State != Fishing.St.Ready) yield return null;
            // line out, waiting for a bite: the wheel switches (and reels in, bait kept)
            Ctl.BotUse = true; Ctl.BotUsePressed = true; yield return null; Ctl.BotUsePressed = false; yield return new WaitForSeconds(0.5f); Ctl.BotUse = false;
            while (fish.State != Fishing.St.Waiting) yield return null;
            yield return Wheel(1, 1, "while waiting for a bite (reels in)");
            yield return new WaitForSeconds(1f);
            Debug.Log($"[PB] scroll: after switching away the line is {(fish.State is Fishing.St.Stowed or Fishing.St.Recover ? "in" : "STILL OUT (" + fish.State + ")")}");
            if (fish.State is not (Fishing.St.Stowed or Fishing.St.Recover)) fails++;
            me.Select(0); yield return new WaitForSeconds(0.6f);
            // a whole catch, with the wheel turned mid-minigame: nothing moves
            bool mgDone = false;
            IEnumerator DuringMinigame()
            {
                while (fish.State != Fishing.St.Minigame) yield return null;
                yield return Wheel(2, 0, "during the minigame"); mgDone = true;
            }
            StartCoroutine(DuringMinigame());
            yield return CastOnce(0.7f); yield return PlayFish(_ => { });
            if (!mgDone) { fails++; Debug.Log("[PB] scroll: minigame never reached"); }
            while (fish.State != Fishing.St.Ready) yield return null;
            me.Select(0); yield return new WaitForSeconds(0.3f);
            Ctl.Bot = false;
            Time.timeScale = 0; yield return Wheel(1, 0, "while paused"); Time.timeScale = 1;
            yield return Wheel(1, 1, "after unpausing");
            Debug.Log($"[PB] RESULT scroll test: {(fails == 0 ? "PASS" : fails + " FAIL")} (wheel unit {WheelUnit})");
            Application.Quit();
        }
        static float WheelUnit => Ctl.WheelNotch;

        // -pb-idleshots <dir>: rod in hand at rest and then with the line out, from 3/4 front at 3 distances (Kufu:
        // the rod must read as a full straight rod with the line hanging from the tip).
        IEnumerator IdleShots()
        {
            yield return new WaitForSeconds(0.5f);
            yield return SailOut(false); Ctl.Bot = true; me.Select(0);
            yield return new WaitForSeconds(1.5f);
            IEnumerator Three(string what)
            {
                foreach (var d in new[] { 1.8f, 3.5f, 6f })
                {
                    yield return new WaitForEndOfFrame();
                    var ct = CameraRig.I.Cam.transform; var a = me.transform; var keep = (ct.position, ct.rotation);
                    ct.position = a.TransformPoint(new Vector3(-0.7f, 0.25f, 0.7f).normalized * d + Vector3.up * 1.2f);
                    ct.rotation = Quaternion.LookRotation(a.TransformPoint(new Vector3(0, 1.25f, 0.6f)) - ct.position);
                    Save(Path.Combine(shots, $"idle-{what}-{d:0.0}m.png"));
                    ct.SetPositionAndRotation(keep.position, keep.rotation);
                }
            }
            if (Arg("-pb-norod") != null)
            {
                // empty hands (the arms hang free: do they clear the body?), standing, then walking the deck
                me.Select(1); yield return new WaitForSeconds(1.5f);
                yield return Three("hands");
                Ctl.BotMove = Vector2.up; yield return new WaitForSeconds(0.6f);
                yield return Three("walk");
                Ctl.BotMove = Vector2.zero;
                Debug.Log("[PB] RESULT idleshots 6 (no rod)");
                Application.Quit(); yield break;
            }
            yield return Three("ready");
            yield return CastOnce(0.6f); while (fish.State != Fishing.St.Waiting) yield return null;
            yield return new WaitForSeconds(1.0f);
            yield return Three("waiting");
            Debug.Log("[PB] RESULT idleshots 6");
            Application.Quit();
        }

        float Off()
        {
            var to = (fish.State is Fishing.St.Charging or Fishing.St.Flight ? fish.Marker : fish.BobPos) - me.transform.position; to.y = 0;
            return Mathf.Abs(Mathf.DeltaAngle(me.transform.eulerAngles.y, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg));
        }

        IEnumerator Film(string dir, float secs, int w = 1920, int h = 1080)
        {
            Directory.CreateDirectory(dir); Time.captureFramerate = 30; int i = 0; Debug.Log($"[PB] film start {Time.time:F3}");
            float end = Time.time + secs;
            while (Time.time < end)
            {
                yield return new WaitForEndOfFrame();
                var ct = CameraRig.I.Cam.transform; var keep = (ct.position, ct.rotation);
                if (filmSide && Angler.I != null)
                {
                    // a fixed 3/4 front view from the angler's left, framing head to feet and the rod
                    var a = Angler.I.transform;
                    ct.position = a.TransformPoint(new Vector3(-2.9f, 1.55f, 2.2f));
                    ct.rotation = Quaternion.LookRotation(a.TransformPoint(new Vector3(0.1f, 1.15f, 0.3f)) - ct.position);
                }
                Save(Path.Combine(dir, $"f_{i++:00000}{(w < 1920 ? ".jpg" : ".png")}"), w, h);
                ct.SetPositionAndRotation(keep.position, keep.rotation);
            }
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
            for (float t = 0; t < 12; t += Time.deltaTime) { s.Steer = t < 3 ? -0.6f : t > 6 ? 0.6f : 0; maxRoll = Mathf.Max(maxRoll, Mathf.Abs(Mathf.DeltaAngle(0, s.transform.eulerAngles.z))); yield return null; }
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

        // Jump over the side from the rail, swim to the stern ladder, climb back aboard (Manan's M1 check).
        IEnumerator SwimTest()
        {
            shots = Arg("-pb-swimshots"); if (shots != null) Directory.CreateDirectory(shots);
            yield return new WaitForSeconds(0.5f);
            yield return SailOut(false);
            var s = Skiff.I; Ctl.Bot = true; me.Select(1);
            CameraRig.I.Yaw = me.transform.eulerAngles.y;
            Ctl.BotMove = Vector2.up; yield return new WaitForSeconds(0.25f);
            Ctl.BotJump = true; yield return null; Ctl.BotJump = false;
            float t = 0; while (!me.Swimming && t < 3) { t += Time.deltaTime; yield return null; }
            Ctl.BotMove = Vector2.zero;
            Debug.Log($"[PB] swim: jumped overboard {(me.Swimming ? "yes" : "NO")} in {t:F2} s");
            yield return new WaitForSeconds(1f);
            if (shots != null) yield return Shot("swim-in-water");
            // swim round to the ladder (the bot steers straight at it)
            t = 0;
            while (t < 25 && Vector3.Distance(Flat(me.transform.position), Flat(s.LadderBottom.position)) > 1.2f)
            {
                var to = s.LadderBottom.position - me.transform.position; to.y = 0;
                CameraRig.I.Yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg; Ctl.BotMove = Vector2.up;
                t += Time.deltaTime; yield return null;
            }
            Ctl.BotMove = Vector2.zero;
            Debug.Log($"[PB] swim: reached the ladder in {t:F1} s (prompt: {me.Prompt})");
            Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false;
            yield return new WaitForSeconds(1f);
            if (shots != null) yield return Shot("swim-back-aboard");
            Debug.Log($"[PB] RESULT swim: back aboard {(me.OnBoat ? "PASS" : "FAIL")} (swimming {me.Swimming})");
            Application.Quit();
        }

        // -pb-laddertest <dir>: in the water by the pier ladder, E: straight up onto the pier (Tushar 1 Nov). Shots before
        // and after; PASS = standing on the pier deck, not swimming.
        IEnumerator LadderTest(string dir)
        {
            shots = dir; Directory.CreateDirectory(dir);
            yield return new WaitForSeconds(0.5f);
            var l = Landing.I; Ctl.Bot = true;
            var inward = l.LadderTop.position - l.LadderBottom.position; inward.y = 0; inward.Normalize();
            var at = l.LadderBottom.position - inward * 1.2f; at.y = Sea.Height(at) - Angler.SwimDepth;
            me.Teleport(at, Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg);
            CameraRig.I.Yaw = Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg + 55; CameraRig.I.Pitch = 14;
            yield return new WaitForSeconds(1.5f);
            Debug.Log($"[PB] ladder: in the water {me.Swimming}, prompt '{me.Prompt}'");
            yield return Shot("ladder-0-water");
            Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false;
            int n = 1; yield return null;
            yield return Shot($"ladder-{n++}-top");
            yield return new WaitForSeconds(1f);
            yield return Shot($"ladder-{n++}-after");
            var p = me.transform.position;
            float deckY = l.LadderTop.position.y, inPast = Vector3.Dot(p - l.LadderTop.position, inward);
            bool ok = !me.Swimming && me.Cc.isGrounded && Mathf.Abs(p.y - deckY) < 0.15f && inPast > 0.3f;
            Debug.Log($"[PB] RESULT ladder: {(ok ? "PASS" : "FAIL")} (swimming {me.Swimming}, grounded {me.Cc.isGrounded}, feet {p.y - deckY:F2} m from the deck, {inPast:F2} m in from the ladder top)");
            Application.Quit();
        }

        // -pb-feeltest <dir> [-pb-side]: the feel spec (replan/feel) measured on the pier's longest straight, filmed:
        // from a stand to a run (0 -> 90% in 0.22 s), the stop (to 10% in 0.18 s), the sprint's run-up (0.6 s), a hard
        // reverse (a skid), a held and a tapped jump (0.8 / 0.45 m after a 0.08 s crouch), then 5 crates' worth of fish
        // (50 kg): start 0.45 s, stop 0.55 s, top speed 2.5 m/s, no sprint. Crossing times are interpolated between frames.
        IEnumerator FeelTest(string dir)
        {
            yield return new WaitForSeconds(0.5f);
            var l = Landing.I; Ctl.Bot = true; me.Select(1);
            // the longest straight run of level pier through the spawn (8 headings), started from its far end
            var from = me.transform.position; float best = 0; Vector3 axis = Vector3.forward;
            for (int k = 0; k < 8; k++)
            {
                var d = Quaternion.Euler(0, k * 45 + l.transform.eulerAngles.y, 0) * Vector3.forward;
                float r = Room(from, d) + Room(from, -d);
                if (r > best) { best = r; axis = d; }
            }
            var start = from - axis * Mathf.Max(0, Room(from, -axis) - 0.6f);
            float axisYaw = Mathf.Atan2(axis.x, axis.z) * Mathf.Rad2Deg;
            me.Teleport(start, axisYaw);
            CameraRig.I.Yaw = axisYaw + 35; // 3/4 behind, so the runs read side-on too
            Debug.Log($"[PB] feel: {best:F1} m of straight pier along {axis}");
            yield return new WaitForSeconds(1f);
            filmSide = Arg("-pb-side") != null; StartCoroutine(Film(dir, 999, 1280, 720));
            yield return new WaitForSeconds(0.5f);
            bool ok = true; float sign = 1;
            void Check(string what, float got, float want, float tol) { bool p = Mathf.Abs(got - want) <= tol; ok &= p; Debug.Log($"[PB] feel {what}: {got:F2} (spec {want:F2} ± {tol:F2}) {(p ? "ok" : "OFF")}"); }
            // the stick for "along the axis" (sign = which way), whatever the camera's yaw
            Vector2 Fwd() { float a = (axisYaw - CameraRig.I.Yaw) * Mathf.Deg2Rad; return new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * sign; }
            float Sp() => new Vector3(me.Vel.x, 0, me.Vel.z).magnitude;
            // time from now until the speed crosses `thr` (up or down), interpolated between frames; -1 after `max` s
            float tOut = -1;
            IEnumerator Until(float thr, bool up, float max)
            {
                float t0 = Time.time, lt = 0, lv = Sp(); tOut = -1;
                while (Time.time - t0 < max)
                {
                    yield return null;
                    float t = Time.time - t0, v = Sp();
                    if (up ? v >= thr : v <= thr) { tOut = lt + (thr - lv) / (v - lv + 1e-6f) * (t - lt); yield break; }
                    lt = t; lv = v;
                }
            }
            IEnumerator StartStop(string tag, float run, float wantStart, float wantStop, float tol)
            {
                Ctl.BotMove = Fwd(); yield return Until(0.9f * run, true, 2); Check(tag + " start 0 -> 90%", tOut, wantStart, tol);
                yield return new WaitForSeconds(0.8f);
                Check(tag + " top speed", Sp(), run, 0.12f);
                Ctl.BotMove = Vector2.zero; yield return Until(0.1f * run, false, 2); Check(tag + " stop to 10%", tOut, wantStop, tol);
                yield return new WaitForSeconds(0.6f); sign = -sign;
            }
            Debug.Log("[PB] feel run " + Time.time);
            yield return StartStop("run", 4.2f, 0.22f, 0.18f, 0.035f);
            // sprint: from a run, the run-up to 90% of the way to 5.8 m/s
            Debug.Log("[PB] feel sprint " + Time.time);
            Ctl.BotMove = Fwd(); yield return new WaitForSeconds(0.6f);
            Ctl.BotSprint = true; yield return Until(4.2f + 0.9f * 1.6f, true, 2); Check("sprint run-up to 90%", tOut, 0.6f, 0.15f);
            yield return new WaitForSeconds(0.4f); Check("sprint top speed", Sp(), 5.8f, 0.12f);
            Ctl.BotSprint = false; Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(0.9f); sign = -sign;
            // a hard reverse at a run: the skid, then turned round and running the other way
            Debug.Log("[PB] feel skid " + Time.time);
            Ctl.BotMove = Fwd(); yield return new WaitForSeconds(0.9f);
            sign = -sign; Ctl.BotMove = Fwd(); float ts = Time.time; bool skid = false;
            while (Time.time - ts < 1.0f) { skid |= me.Skidding; yield return null; }
            float off = Vector3.Angle(me.transform.forward, axis * sign);
            Debug.Log($"[PB] feel skid: {(skid ? "yes" : "NO")}, then facing {off:F0}° off the new way {(skid && off < 15 ? "ok" : "OFF")}");
            ok &= skid && off < 15;
            Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(0.9f); sign = -sign;
            // jumps, standing: held, then tapped
            foreach (bool held in new[] { true, false })
            {
                Debug.Log($"[PB] feel jump {(held ? "held" : "tap")} " + Time.time);
                float y0 = me.transform.position.y, peak = y0, tp = Time.time, up = -1;
                Ctl.BotJumpHeld = held; Ctl.BotJump = true; yield return null; Ctl.BotJump = false;
                while (Time.time - tp < 2f)
                {
                    float y = me.transform.position.y; peak = Mathf.Max(peak, y);
                    if (up < 0 && y > y0 + 0.005f) up = Time.time - tp;
                    if (up > 0 && me.Cc.isGrounded && Time.time - tp > up + 0.1f) break;
                    yield return null;
                }
                Check((held ? "held" : "tapped") + " jump height", peak - y0, held ? 0.8f : 0.45f, 0.06f);
                if (held) Check("crouch before leaving the ground (+ a frame)", up, 0.08f + Time.deltaTime, 0.04f);
                Ctl.BotJumpHeld = true; yield return new WaitForSeconds(0.8f);
            }
            // 5 crates' worth of fish (50 kg in slot 8)
            Debug.Log("[PB] feel loaded " + Time.time);
            for (int i = 0; i < 5; i++) me.Bar.S[7].Fish.Add(new Fish { Species = FishDb.All[0], Kg = 10 });
            Debug.Log($"[PB] feel load {me.Load:F1} crates");
            yield return StartStop("loaded", 4.2f * (1 - 0.08f * 5), 0.45f, 0.55f, 0.05f);
            Ctl.BotMove = Fwd(); Ctl.BotSprint = true; yield return new WaitForSeconds(1.2f);
            Check("loaded, sprint held: speed", Sp(), 2.52f, 0.12f);
            Ctl.BotSprint = false; Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(1f);
            me.Bar.S[7].Fish.Clear();
            Debug.Log($"[PB] RESULT feel: {(ok ? "PASS" : "FAIL")}");
            Application.Quit();
        }

        // How far the pier runs level and clear from `p` along `d` (deck under the feet, nothing in the way), up to 30 m.
        static float Room(Vector3 p, Vector3 d)
        {
            float r = 0;
            for (; r < 30; r += 0.5f)
            {
                var q = p + d * (r + 0.5f);
                if (!Physics.Raycast(q + Vector3.up * 1.2f, Vector3.down, out var hit, 2f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(hit.point.y - p.y) > 0.25f) break;
                bool blocked = false;
                foreach (var h in Physics.SphereCastAll(p + Vector3.up * 0.9f + d * r, 0.35f, d, 0.5f, ~0, QueryTriggerInteraction.Ignore))
                    if (h.collider.GetComponentInParent<Angler>() == null) blocked = true;
                if (blocked) break;
            }
            return r;
        }

        // -pb-knocktest <dir>: ragdoll-lite (replan/feel §4), filmed. On the pier, knocked over forward, back and sideways,
        // then off the tiller as a crash would. Each must really go down (hips under 0.45 m off the floor), be up within
        // 4.5 s (or treading water, if it flung her off the edge), stand up where she came to rest (within 1 m of her hips)
        // and walk again.
        IEnumerator KnockTest(string dir)
        {
            yield return new WaitForSeconds(0.5f);
            Ctl.Bot = true; me.Select(1);
            CameraRig.I.Yaw = me.transform.eulerAngles.y + 60;
            yield return new WaitForSeconds(1f);
            StartCoroutine(Film(dir, 999, 1280, 720));
            yield return new WaitForSeconds(0.5f);
            var hips = me.Anim.GetComponent<Ragdoll>().Hips;
            bool ok = true;
            foreach (var way in new[] { "forward", "back", "side", "tiller" })
            {
                if (way == "tiller") { me.TakeHelm(); yield return new WaitForSeconds(1.5f); }
                var a = me.transform; var p0 = a.position; float floor = p0.y;
                Debug.Log($"[PB] knock {way} {Time.time:F2}");
                var push = way switch
                {
                    "forward" => a.forward * 2.5f + Vector3.up * 1.5f,
                    "back" => -a.forward * 2.5f + Vector3.up * 1.5f,
                    "side" => a.right * 2f + Vector3.up * 1.2f,
                    _ => Skiff.I.transform.forward * 4.5f * 0.25f + Vector3.up * 0.8f, // as Skiff: a full-speed crash
                };
                me.Knock(push);
                if (!me.Down) { Debug.Log($"[PB] knock {way}: NOT knocked over"); ok = false; continue; }
                float t = 0, low = 9; var rest = hips.position;
                while (me.Down && t < 6) { low = Mathf.Min(low, hips.position.y - floor); if (me.Limp) rest = hips.position; t += Time.deltaTime; yield return null; }
                yield return new WaitForSeconds(0.3f);
                float moved = Vector3.Distance(Flat(rest), Flat(me.transform.position)), stand = hips.position.y - me.transform.position.y;
                var w0 = me.transform.position; Ctl.BotMove = Vector2.up; yield return new WaitForSeconds(0.6f); Ctl.BotMove = Vector2.zero;
                float walked = Vector3.Distance(Flat(w0), Flat(me.transform.position));
                bool pass = low < 0.45f && t < 4.5f && (moved < 1f || me.Swimming) && (stand > 0.55f || me.Swimming) && walked > 0.4f;
                ok &= pass;
                Debug.Log($"[PB] knock {way}: hips down to {low:F2} m, up in {t:F2} s, up {moved:F2} m from where she lay, hips {stand:F2} m up after, walked {walked:F2} m{(me.Swimming ? " (in the water)" : "")}{(me.OnBoat ? " (on the boat)" : "")} {(pass ? "ok" : "OFF")}");
                yield return new WaitForSeconds(1f);
                if (me.Swimming || me.OnBoat) { me.Teleport(Landing.I.Spawn.position + Vector3.up * 0.05f, me.transform.eulerAngles.y); yield return new WaitForSeconds(0.8f); }
            }
            // walking off the pier's end into the sea (Falling Into Pool's topple, then treading water)
            var l = Landing.I; var outward = l.LadderBottom.position - l.LadderTop.position; outward.y = 0; outward.Normalize();
            me.Teleport(l.LadderTop.position - outward * 0.8f, Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg);
            CameraRig.I.Yaw = Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg - 70; yield return new WaitForSeconds(1f);
            Debug.Log($"[PB] knock walk-off {Time.time:F2}");
            Ctl.BotMove = new Vector2(Mathf.Sin(70 * Mathf.Deg2Rad), Mathf.Cos(70 * Mathf.Deg2Rad)) * 0.5f;
            float tw = 0; while (!me.Swimming && tw < 4) { tw += Time.deltaTime; yield return null; }
            Ctl.BotMove = Vector2.zero; yield return new WaitForSeconds(1.5f);
            Debug.Log($"[PB] knock walk-off: in the water {(me.Swimming ? "yes" : "NO")} after {tw:F2} s");
            ok &= me.Swimming;
            Debug.Log($"[PB] RESULT knock: {(ok ? "PASS" : "FAIL")}");
            Application.Quit();
        }

        // -pb-fadetest <dir>: walls between the camera and her fade (feel spec). Behind the shack, then swimming beside the
        // boat with the camera across it: shots, and PASS when the blocker is fading (and nothing fades in the open).
        IEnumerator FadeTest()
        {
            yield return new WaitForSeconds(0.5f);
            Ctl.Bot = true; me.Select(1);
            bool ok = true;
            IEnumerator Look(string name, Vector3 at, Vector3 from, bool swim)
            {
                var y = swim ? Sea.Height(at) - Angler.SwimDepth : at.y;
                if (!swim && Physics.Raycast(at + Vector3.up * 3, Vector3.down, out var g, 6f, ~0, QueryTriggerInteraction.Ignore)) y = g.point.y;
                var dir = at - from; dir.y = 0;
                me.Teleport(new Vector3(at.x, y, at.z), Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg);
                CameraRig.I.Yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; CameraRig.I.Pitch = swim ? 12 : 22;
                yield return new WaitForSeconds(1.5f);
                int n = CameraRig.I.Fading;
                Debug.Log($"[PB] fade {name}: {n} model(s) fading");
                yield return Shot("fade-" + name);
                ok &= n > 0;
            }
            // behind the shack, looking across it
            var shack = Util.Find(Landing.I.transform, "Shack").GetComponent<Renderer>().bounds;
            var away = shack.center - Landing.I.Spawn.position; away.y = 0; away.Normalize();
            yield return Look("shack", shack.center + away * (Mathf.Max(shack.extents.x, shack.extents.z) + 1.2f), shack.center - away * 6, false);
            // in the water beside Biscuit, the camera across the boat
            // the open-water spot round the boat farthest from the pier (nothing overhead), the camera across the boat from it
            var hull = Skiff.I.Hull; var c = Skiff.I.transform.TransformPoint(hull.center); c.y = Skiff.I.transform.position.y;
            Vector3 side = Vector3.zero; float far = -1;
            for (int k = 0; k < 8; k++)
            {
                var d = Quaternion.Euler(0, k * 45, 0) * Vector3.forward;
                var q = c + d * 1.7f;
                if (Physics.Raycast(q + Vector3.down * 0.3f, Vector3.up, 8f, ~0, QueryTriggerInteraction.Ignore)) continue;
                float dist = Vector3.Distance(Flat(q), Flat(Landing.I.Spawn.position));
                if (dist > far) { far = dist; side = d; }
            }
            yield return Look("boat", c + side * 1.7f, c - side * 6, true);
            // out in the open on the pier: nothing fades
            me.Teleport(Landing.I.Spawn.position + Vector3.up * 0.05f, 0); CameraRig.I.Pitch = 30;
            yield return new WaitForSeconds(1.5f);
            Debug.Log($"[PB] fade open: {CameraRig.I.Fading} model(s) fading");
            ok &= CameraRig.I.Fading == 0;
            yield return Shot("fade-open");
            Debug.Log($"[PB] RESULT fade: {(ok ? "PASS" : "FAIL")}");
            Application.Quit();
        }

        // -pb-camtest N: N casts from the pier at 60 fps, the camera's turn per frame measured round each release
        // (Tushar 1 Nov: "the screen stutters when I cast"). A snap is one frame turning far more than its neighbours.
        IEnumerator CamTest(int n)
        {
            Application.targetFrameRate = 60;
            yield return new WaitForSeconds(0.5f);
            var l = Landing.I; Ctl.Bot = true;
            var outward = l.LadderBottom.position - l.LadderTop.position; outward.y = 0;
            me.Teleport(l.LadderTop.position - outward.normalized * 0.6f, Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg);
            CameraRig.I.Yaw = Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg;
            me.Select(0); yield return new WaitForSeconds(1f);
            StartCoroutine(CamWatch());
            for (int i = 0; i < n; i++)
            {
                CameraRig.I.Yaw = Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg + (i - n / 2) * 12; // the test turns the view: not counted
                yield return new WaitForSeconds(0.8f);
                camWorst = camJerk = camMove = 0; camJerkState = "";
                yield return CastOnce(0.3f + 0.65f * i / Mathf.Max(1, n - 1));
                while (fish.State is Fishing.St.Flight or Fishing.St.Settle) yield return null;
                yield return new WaitForSeconds(0.5f);
                Ctl.BotCancel = true; float h = 0; while (fish.State != Fishing.St.Ready && h < 4) { h += Time.deltaTime; yield return null; }
                Ctl.BotCancel = false; yield return new WaitForSeconds(0.6f);
                Debug.Log($"[PB] camtest cast {i}: biggest turn in one frame {camWorst:F2} deg, biggest jump in turn speed {camJerk:F2} deg/frame (in {camJerkState}), biggest move in one frame {camMove:F2} m");
                worstAll = Mathf.Max(worstAll, camWorst); jerkAll = Mathf.Max(jerkAll, camJerk);
            }
            Debug.Log($"[PB] RESULT camtest: {(jerkAll < 1.0f ? "PASS" : "FAIL")} (worst one-frame turn {worstAll:F2} deg, worst jump in turn speed {jerkAll:F2} deg/frame, 60 fps)");
            Application.Quit();
        }
        float camWorst, camJerk, camMove, worstAll, jerkAll; string camJerkState;

        // Every frame after the camera moved: how far it turned, and how much that differs from the frame before.
        IEnumerator CamWatch()
        {
            var cam = CameraRig.I.transform; var last = cam.forward; var lastP = cam.position; float lastA = -1;
            while (true)
            {
                yield return new WaitForEndOfFrame();
                float a = Vector3.Angle(last, cam.forward); last = cam.forward;
                camWorst = Mathf.Max(camWorst, a); camMove = Mathf.Max(camMove, Vector3.Distance(lastP, cam.position)); lastP = cam.position;
                if (lastA >= 0 && Mathf.Abs(a - lastA) > camJerk) { camJerk = Mathf.Abs(a - lastA); camJerkState = fish.State + (CameraRig.I.Blocker != null ? ", camera pulled in by " + CameraRig.I.Blocker : ""); }
                lastA = a;
            }
        }
    }
}
