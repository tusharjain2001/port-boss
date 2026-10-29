using System.Collections;
using System.Collections.Generic;
using System.IO;
using PortBoss.Net;
using Unity.Netcode;
using UnityEngine;

namespace PortBoss
{
    // Command-line test harness (no effect without -pb-* args):
    //   -pb-solo | -pb-host-lan | -pb-host-steam | -pb-join-lan <ip> | -pb-join-lobby
    //   -pb-bot  -pb-shots <dir> <seconds>  -pb-quit <seconds>  -pb-timescale <x>  -pb-night <seconds>
    public class AutoRun : MonoBehaviour
    {
        static string Arg(string name, int k = 1)
        {
            var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - k; i++) if (a[i] == name) return a[i + k];
            return null;
        }
        static bool Has(string name) => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), name) >= 0;

        public static void Begin(GameUI ui)
        {
            if (!System.Array.Exists(System.Environment.GetCommandLineArgs(), x => x.StartsWith("-pb-"))) return;
            Active = true;
            new GameObject("AutoRun").AddComponent<AutoRun>().ui = ui;
        }

        public static bool Active;
        GameUI ui;
        float quitAt = -1, nextLog;
        Bot bot;

        IEnumerator Start()
        {
            if (float.TryParse(Arg("-pb-quit"), out var q)) quitAt = q;
            if (float.TryParse(Arg("-pb-timescale"), out var ts)) Time.timeScale = ts;
            yield return null;
            if (Has("-pb-solo")) ui.Play();
            else if (Has("-pb-host-lan")) ui.Host(true);
            else if (Has("-pb-host-steam")) ui.Host(false);
            else if (Arg("-pb-join-lan") != null) ui.JoinIp(Arg("-pb-join-lan"));
            else if (Has("-pb-join-lobby"))
            {
                for (int tries = 0; tries < 20 && !NetworkManager.Singleton.IsListening; tries++)
                {
                    var t = Session.FindLobbies();
                    while (!t.IsCompleted) yield return null;
                    Debug.Log($"[PB] lobby search: {t.Result.Length} found");
                    if (t.Result.Length > 0) { var j = Session.JoinLobby(t.Result[0]); while (!j.IsCompleted) yield return null; break; }
                    yield return new WaitForSecondsRealtime(2);
                }
            }
            if (Has("-pb-bot")) { bot = new Bot(); Player.BotInput = bot.Tick; }
            var dir = Arg("-pb-shots");
            if (dir != null) StartCoroutine(Shots(dir, float.TryParse(Arg("-pb-shots", 2), out var iv) ? iv : 5f));
            if (float.TryParse(Arg("-pb-night"), out var n)) StartCoroutine(ForceNight(n));
            if (Has("-pb-trolleytest")) StartCoroutine(TrolleyTest());
            if (Has("-pb-wobbletest")) StartCoroutine(WobbleTest());
            if (Arg("-pb-charshots") != null) StartCoroutine(CharShots(Arg("-pb-charshots")));
            if (Arg("-pb-crateshots") != null) StartCoroutine(CrateShots(Arg("-pb-crateshots")));
            if (Has("-pb-swimtest")) StartCoroutine(SwimTest());
            if (Has("-pb-piertest")) StartCoroutine(PierTest());
            if (Has("-pb-knocktest")) StartCoroutine(KnockTest());
            if (Has("-pb-shovehost")) StartCoroutine(ShoveHost());
            if (Has("-pb-shovetest")) StartCoroutine(ShoveTest());
            if (Arg("-pb-stagetest") != null) StartCoroutine(StageTest(Arg("-pb-stagetest")));
            if (Has("-pb-contracttest")) StartCoroutine(ContractTest());
            if (int.TryParse(Arg("-pb-day"), out var day)) StartCoroutine(StartOnDay(day));
            if (int.TryParse(Arg("-pb-settings"), out var st)) ui.DebugOpenSettings(st);
            if (Has("-pb-settingstest")) StartCoroutine(SettingsTest());
            if (Has("-pb-storm")) StartCoroutine(StormTest());
            if (Arg("-pb-carrytest") != null) StartCoroutine(CarryTest(Arg("-pb-carrytest")));
            if (Arg("-pb-mapshots") != null) StartCoroutine(MapShots(Arg("-pb-mapshots")));
            if (Arg("-pb-uishots") != null) StartCoroutine(UiShots(Arg("-pb-uishots")));
            if (Arg("-pb-film") != null) StartCoroutine(Film(Arg("-pb-film")));
            if (float.TryParse(Arg("-pb-weather"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var wsev)) StartCoroutine(ForceWeatherSoon(wsev));
            if (Has("-pb-forklifttest")) StartCoroutine(ForkliftTest());
        }

        IEnumerator WaitGame() { while (PortGame.I == null || Player.Local == null) yield return null; yield return new WaitForSeconds(1); }

        // Fall through the pier hole, swim to a floating crate, tow it, then climb the nearest ladder.
        IEnumerator SwimTest()
        {
            yield return WaitGame();
            var pl = Player.Local;
            var crate = PortGame.I.SpawnCrate(new Vector3(-5.2f, -1f, 9f), 1f);
            pl.DebugTeleport(new Vector3(-1.5f, 0.6f, 10.3f), -90);
            for (int i = 0; i < 40 && !pl.Swimming; i++) yield return new WaitForSeconds(0.1f);
            Debug.Log($"[PB] swim test: swimming={pl.Swimming} y={pl.transform.position.y:F2} surface={Water.Height(pl.transform.position):F2}");
            float t0 = Time.time;
            while (Time.time - t0 < 6 && !pl.Towing)
            {
                var to = crate.transform.position - pl.transform.position; to.y = 0;
                bool near = pl.FloatingCrateNear() != null;
                Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = near ? Vector3.zero : to.normalized, grab = near };
                yield return null;
            }
            Debug.Log($"[PB] swim test: towing={pl.Towing} after {Time.time - t0:F1}s");
            var shotDir = Arg("-pb-shots");
            if (shotDir != null)
            {
                // Tow towards open water for a moment so there is a clean swimming frame.
                Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = new Vector3(-0.6f, 0, 0.8f) };
                yield return new WaitForSeconds(2.5f);
                yield return Snap(shotDir, "swim-towing-crate");
            }
            var ladder = World.Ladders[0];
            t0 = Time.time;
            while (Time.time - t0 < 10 && pl.Swimming)
            {
                var to = ladder.Bottom - pl.transform.position; to.y = 0;
                bool near = pl.NearLadder() != null;
                Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = near ? Vector3.zero : to.normalized, grab = near && Time.frameCount % 20 == 0 };
                yield return null;
            }
            Player.BotInput = () => new Player.In();
            yield return new WaitForSeconds(1.5f);
            Debug.Log($"[PB] swim test: out={!pl.Swimming} at {pl.transform.position:F1} crate at {crate.transform.position:F1} inSea={crate.InSea}");
            if (shotDir != null)
            {
                // Jump back in off the side and swim along the pier.
                pl.DebugTeleport(new Vector3(-6.5f, 0.6f, 12f), 0);
                Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = Vector3.forward };
                yield return new WaitForSeconds(3f);
                yield return Snap(shotDir, "swim-open-water");
                // Side-on close-ups: swimming along (stroke), then still (treading water).
                Player.CamOverride = true;
                var cam = Camera.main;
                for (int k = 0; k < 2; k++)
                {
                    if (k == 1) { Player.BotInput = () => new Player.In(); yield return new WaitForSeconds(1.5f); }
                    else yield return new WaitForSeconds(0.5f);
                    var p = pl.transform.position + Vector3.up * SwimCamY;
                    cam.transform.position = p + pl.transform.right * 3.2f + Vector3.up * 0.6f; cam.transform.LookAt(p);
                    var an = pl.GetComponentInChildren<Animator>();
                    if (an != null && an.isHuman)
                    {
                        float s = Water.Height(pl.transform.position);
                        Debug.Log($"[PB] swim pose {(k == 0 ? "stroke" : "tread")}: head {an.GetBoneTransform(HumanBodyBones.Head).position.y - s:F2} chest {an.GetBoneTransform(HumanBodyBones.Chest).position.y - s:F2} hips {an.GetBoneTransform(HumanBodyBones.Hips).position.y - s:F2} feet(transform) {pl.transform.position.y - s:F2} (m above the surface)");
                    }
                    yield return Snap(shotDir, k == 0 ? "swim-side-stroke" : "swim-side-tread");
                }
                Player.CamOverride = false;
                Player.BotInput = () => new Player.In();
            }
        }
        const float SwimCamY = 1.2f;

        // Pile crates on the old pier end and watch it sag.
        IEnumerator PierTest()
        {
            yield return WaitGame();
            for (int i = 0; i < 16; i++) PortGame.I.SpawnCrate(new Vector3(-2f + (i % 4) * 1.2f, 0.7f + (i / 8) * 0.6f, 22.5f + ((i / 4) % 2) * 2.5f), 1f);
            for (int s = 0; s < 6; s++) { yield return new WaitForSeconds(1); Debug.Log($"[PB] pier test: sag={PortGame.I.PierSag.Value:F1} endRot={World.PierEnd.eulerAngles.x:F1}"); }
            Player.CamOverride = true;
            Camera.main.transform.position = new Vector3(14, 3, 24); Camera.main.transform.LookAt(new Vector3(0, 0, 24));
        }

        // Throw a crate at the local player.
        // Stage 2: earn it, end the day, build the quay at night, check the shed is up and tomorrow's boat is bigger.
        IEnumerator StageTest(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return WaitGame();
            var g = PortGame.I;
            g.Money.Value = 1600;
            g.EndDayNow();
            yield return new WaitForSeconds(1f);
            g.BuyStageRpc();
            yield return new WaitForSeconds(1f);
            Debug.Log($"[PB] stage test: stage={g.Stage.Value} money={g.Money.Value} next={g.NextStageName} ${g.NextStageCost} shed={GameObject.Find("Stage2") != null}");
            g.NextDayRpc();
            yield return new WaitForSeconds(8f);
            foreach (var b in Boat.All) Debug.Log($"[PB] stage test: day {g.DayNum.Value} boat cargo={b.CargoCount} (stage 1 would be {Mathf.RoundToInt((8 + (g.DayNum.Value - 1) * 2) * g.Today.Cargo)})");
            Player.CamOverride = true;
            Camera.main.transform.position = new Vector3(14, 9, -12); Camera.main.transform.LookAt(new Vector3(28, 0, -34));
            yield return new WaitForSeconds(1f);
            yield return Snap(dir, "stage2");
            Player.CamOverride = false;
        }

        // Co-op shove (LAN): the host holds 2 crates; the joiner walks up behind and shoves.
        IEnumerator ShoveHost()
        {
            yield return WaitGame();
            Player.Local.DebugGiveCrates(2);
            for (int i = 0; i < 40; i++) { yield return new WaitForSeconds(1f); Debug.Log($"[PB] shove host: carrying={Player.Local.Carrying} pos={Player.Local.transform.position:F1}"); }
        }

        IEnumerator ShoveTest()
        {
            yield return WaitGame();
            while (Player.All.Count < 2) yield return null;
            yield return new WaitForSeconds(4f);
            var me = Player.Local; Player host = null;
            foreach (var p in Player.All) if (p != me) host = p;
            var hp = host.transform.position;
            var from = hp - host.transform.forward * 1.2f;
            me.DebugTeleport(from, Quaternion.LookRotation(hp - from).eulerAngles.y);
            yield return new WaitForSeconds(0.6f);
            Debug.Log($"[PB] shove test: target found={me.DebugShove()} host carrying before={host.Carrying}");
            yield return new WaitForSeconds(2f);
            Debug.Log($"[PB] shove test: host carrying after={host.Carrying}");
        }

        IEnumerator KnockTest()
        {
            yield return WaitGame();
            var pl = Player.Local;
            pl.DebugGiveCrates(2);
            yield return new WaitForSeconds(0.5f);
            var p = pl.transform.position + pl.transform.forward * 4f + Vector3.up * 1.2f;
            var c = PortGame.I.SpawnCrate(p, 1f);
            c.GetComponent<Rigidbody>().linearVelocity = (pl.transform.position + Vector3.up - p).normalized * 11f;
            yield return new WaitForSeconds(1.5f);
            Debug.Log($"[PB] knock test: carrying after hit={pl.Carrying} (was 2)");
        }

        // Book Bjorn at night, start day 2, check the countdown.
        IEnumerator ContractTest()
        {
            yield return WaitGame();
            var g = PortGame.I;
            g.EndDayNow();
            yield return new WaitForSeconds(1);
            g.ChooseContractRpc(1);
            yield return new WaitForSeconds(1);
            g.NextDayRpc();
            yield return new WaitForSeconds(1);
            Debug.Log($"[PB] contract test: day={g.DayNum.Value} contract={g.Today.Captain} dock={g.Today.DockSeconds}");
            Boat b = null;
            for (int i = 0; i < 80 && (b = Boat.DockedAt[0]) == null; i++) yield return new WaitForSeconds(0.5f);
            yield return new WaitForSeconds(1);
            Debug.Log($"[PB] contract test: boat docked={b != null} secondsLeft={(b ? b.SecondsLeft.Value : -1)}");
        }

        // Storm warning now, storm 90 s later (use -pb-timescale to speed it up); crates on the pier get blown.
        // Carried stacks must be visible from the real gameplay camera: carry 1, 3, 5 and count the crates the
        // camera can see (line of sight to any corner of each crate, the carrier's body counts as blocking).
        IEnumerator CarryTest(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return WaitGame();
            PortGame.I.ForceWeather(0);
            var pl = Player.Local;
            pl.DebugTeleport(new Vector3(-12, 0.2f, -6), 30);
            bool allOk = true;
            foreach (var n in new[] { 1, 3, 5 })
            {
                while (pl.Carrying < n) { pl.DebugGiveCrates(n - pl.Carrying); yield return null; }
                yield return new WaitForSeconds(2f);
                var cam = Camera.main;
                int seen = 0;
                foreach (var c in Crate.All)
                {
                    if (!c.Attached.Value || c.transform.parent != pl.transform) continue;
                    // Visible = at least 3 of 7 points on the crate (centre, corners) in clear view.
                    int clear = 0; float h = Crate.Size * 0.45f;
                    foreach (var o in new[] { Vector3.zero, new Vector3(h, h, h), new Vector3(-h, h, h), new Vector3(h, h, -h), new Vector3(-h, h, -h), new Vector3(h, -h, -h), new Vector3(-h, -h, -h) })
                    {
                        var p = c.transform.TransformPoint(o);
                        if (!Physics.Linecast(cam.transform.position, p, out var hit, ~0, QueryTriggerInteraction.Ignore) && BodyClear(cam.transform.position, p, pl)) clear++;
                    }
                    bool vis = clear >= 3;
                    // Rendered = the crate's renderers were actually drawn by a camera last frame (not just "would be in view").
                    var rs = c.GetComponentsInChildren<Renderer>();
                    // (isVisible is useless in batchmode; what broke in Manan's build was the visual flung away from the crate)
                    bool drawn = System.Array.TrueForAll(rs, r => !r.enabled || (r.gameObject.activeInHierarchy && (r.bounds.center - c.transform.position).sqrMagnitude < 1f));
                    var vis0 = c.transform.Find("vis");
                    Debug.Log($"[PB] carry test:   crate {c.NetworkObjectId} slot={c.Slot.Value} local={c.transform.localPosition:F2} world={c.transform.position:F2} scale={c.transform.lossyScale:F2} vis={(vis0 ? vis0.gameObject.activeInHierarchy + " " + vis0.localPosition.ToString("F2") : "none")} r0={(rs.Length > 0 ? rs[0].name + " en=" + rs[0].enabled + " b=" + rs[0].bounds.center.ToString("F2") + " sz=" + rs[0].bounds.size.ToString("F2") : "-")} clear={clear}");
                    if (vis && drawn) seen++;
                }
                Debug.Log($"[PB] carry test: carrying={pl.Carrying} visible={seen} {(seen == n ? "OK" : "FAIL")}");
                allOk &= seen == n;
                yield return Snap(dir, $"carry-{n}");
                if (n == 5)
                {
                    Player.CamOverride = true;
                    var side = pl.transform.position + pl.transform.right * 4.5f + Vector3.up * 2.4f;
                    Camera.main.transform.position = side; Camera.main.transform.LookAt(pl.transform.position + Vector3.up * 2.6f + pl.transform.forward * 0.7f);
                    yield return Snap(dir, "carry-5-side");
                    Player.CamOverride = false;
                }
            }
            Debug.Log($"[PB] carry test: {(allOk ? "PASS" : "FAIL")}");
        }

        // The worker model has no colliders: treat the body as a capsule (feet to 1.85 m, radius 0.3) between camera and point.
        static bool BodyClear(Vector3 a, Vector3 b, Player pl)
        {
            var p0 = pl.transform.position + Vector3.up * 0.3f; var p1 = pl.transform.position + Vector3.up * 1.5f;
            var headC = pl.transform.position + Vector3.up * 1.72f;
            for (int i = 0; i <= 40; i++)
            {
                var q = Vector3.Lerp(a, b, i / 40f);
                var axis = p1 - p0; float t = Mathf.Clamp01(Vector3.Dot(q - p0, axis) / axis.sqrMagnitude);
                if ((q - (p0 + axis * t)).sqrMagnitude < 0.32f * 0.32f) return false; // torso + shoulders
                if ((q - headC).sqrMagnitude < 0.19f * 0.19f) return false;            // head + hard hat
            }
            return true;
        }

        // Trailer frames (windowed, so the menu and HUD are in): fixed 30 fps game time, every frame saved as a JPEG.
        // Title flyover (berth, then the whole harbour), then a bot shift; the weather turns rough near the end.
        IEnumerator Film(string dir)
        {
            Directory.CreateDirectory(dir);
            int n = 0;
            IEnumerator Frame()
            {
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(dir, $"f_{n++:00000}.jpg"), tex.EncodeToJPG(90));
                Destroy(tex);
            }
            yield return new WaitForSecondsRealtime(3f);
            Time.captureFramerate = 30;
            foreach (var (from, secs) in new[] { (1f, 7f), (43f, 6f) })
                for (int f = 0; f < secs * 30; f++) { GameUI.TitleTime = from + f / 30f; yield return Frame(); }
            GameUI.TitleTime = -1;
            ui.Play();
            while (PortGame.I == null || Player.Local == null) yield return Frame();
            bot = new Bot(); Player.BotInput = bot.Tick;
            // 62 s of footage; standing around waiting for a boat is cut (time runs on, frames aren't kept).
            var last = Player.Local.transform.position;
            for (int kept = 0, f = 0; kept < 62 * 30 && f < 400 * 30; f++)
            {
                if (kept == 44 * 30) PortGame.I.ForceWeather(0.85f);
                var p = Player.Local.transform.position;
                bool idle = (p - last).sqrMagnitude < 0.0004f && Player.Local.Carrying == 0 && !Boat.All.Exists(b => b.State.Value == Boat.St.Docked);
                last = p;
                if (idle) { yield return null; continue; }
                kept++;
                yield return Frame();
            }
            Time.captureFramerate = 0;
            Debug.Log($"[PB] film done: {n} frames");
            Application.Quit();
        }

        // The front door: each title shot (camera only in batchmode; with the menu drawn when windowed), the logo texture,
        // then Join, Settings and (in a solo session) Pause.
        IEnumerator UiShots(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return new WaitForSecondsRealtime(3f);
            for (int i = 0; i < 4; i++)
            {
                GameUI.TitleTime = i * 14f + 5f;
                yield return new WaitForSecondsRealtime(1.2f);
                yield return Snap(dir, $"title-{i}");
            }
            var rt = ui.Logo();
            var lc = GameObject.Find("LogoCam"); if (lc != null) lc.GetComponent<Camera>().Render();
            if (rt != null)
            {
                yield return new WaitForEndOfFrame();
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(dir, "logo.png"), tex.EncodeToPNG());
            }
            GameUI.TitleTime = 5f;
            ui.DebugScreen(1); yield return new WaitForSecondsRealtime(1f); yield return Snap(dir, "join");
            ui.DebugOpenSettings(0); yield return new WaitForSecondsRealtime(1f); yield return Snap(dir, "settings");
            ui.DebugScreen(0);
            GameUI.TitleTime = -1;
            ui.Play();
            yield return WaitGame();
            yield return new WaitForSecondsRealtime(3f);
            ui.DebugPause(); yield return new WaitForSecondsRealtime(1f); yield return Snap(dir, "pause");
            Debug.Log("[PB] ui shots done");
        }

        // Fixed cameras over the harbour map (same every build, for A/B), with the frame rate at each.
        IEnumerator MapShots(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return WaitGame();
            PortGame.I.ForceWeather(0);
            yield return new WaitForSeconds(3f);
            Player.CamOverride = true;
            var cam = Camera.main;
            foreach (var (name, pos, look) in new[] {
                ("map-aerial", new Vector3(-40, 70, -90), new Vector3(30, 0, 90)),
                ("map-yard-top", new Vector3(15, 160, -44), new Vector3(15, 0, -43.9f)),
                ("map-berth", new Vector3(-14, 9, -24), new Vector3(10, 0, 20)),
                ("map-rival", new Vector3(0, 4, -6), new Vector3(0, 25, 600)),
                ("map-rival-zoom", new Vector3(0, 30, 250), new Vector3(0, 20, 620)),
                ("map-rival-sign", new Vector3(30, 100, 700), new Vector3(30, 100, 850)),
                ("map-mouth", new Vector3(30, 18, 120), new Vector3(190, 0, 300)),
                ("map-demo-angle", new Vector3(40, 17, 40), new Vector3(5, 0, 5)),
                ("map-back", new Vector3(-10, 10, -35), new Vector3(-10, 2, -75)),
                ("map-quay", new Vector3(-12, 3, 14), new Vector3(10, -0.5f, -2)),
                ("map-kit", new Vector3(2, 5, 6), new Vector3(14, 0, -8)),
                ("map-forklift", new Vector3(14.5f, 2.6f, -7.5f), new Vector3(19, 0.8f, -12)),
                ("map-stall", new Vector3(-8.5f, 2.4f, -25.5f), new Vector3(-4, 1.2f, -31)),
                ("map-to-stall", new Vector3(1, 2.6f, 6), new Vector3(-4, 1, -31)),
                ("map-shelter", new Vector3(5, 2.2f, 1.5f), new Vector3(9, 1.6f, -9)) })
            {
                cam.transform.position = pos; cam.transform.LookAt(look);
                float t0 = Time.realtimeSinceStartup; int f0 = Time.frameCount;
                yield return new WaitForSeconds(1.5f);
                Debug.Log($"[PB] map shot {name}: fps={(Time.frameCount - f0) / (Time.realtimeSinceStartup - t0):0}");
                yield return Snap(dir, name);
            }
            if (Trolley.I != null) Debug.Log($"[PB] trolley at {Trolley.I.transform.position:F1} (spawn {World.TrolleySpawn:F1})");
            Player.CamOverride = false;
        }

        IEnumerator ForceWeatherSoon(float s) { yield return WaitGame(); PortGame.I.ForceWeather(s); }

        IEnumerator StormTest()
        {
            yield return WaitGame();
            for (int i = 0; i < 6; i++) PortGame.I.SpawnCrate(new Vector3(-1.5f + i * 0.7f, 0.6f, 14f), 1f);
            // Same camera at severity 0, 0.5 and 1 (boat docked, crates loose on the pier).
            while (!Boat.All.Exists(b => b.State.Value == Boat.St.Docked)) yield return null;
            var dir = Arg("-pb-shots");
            foreach (var sev in new[] { 0f, 0.5f, 1f })
            {
                PortGame.I.ForceWeather(sev);
                while (Mathf.Abs(Storm.Level - sev) > 0.02f) yield return null;
                yield return new WaitForSeconds(4f);
                Player.CamOverride = true;
                Camera.main.transform.position = new Vector3(-9f, 4.5f, 6f); Camera.main.transform.LookAt(new Vector3(4f, 0.5f, 17f));
                Debug.Log($"[PB] weather test: severity={PortGame.I.Severity.Value:F2} level={Storm.Level:F2} waves={Water.Storm:F2} rain={Storm.Rain:F2} gust={Storm.Gust:F2} inSea={PortGame.I.InSea.Value}");
                if (dir != null) yield return Snap(dir, $"weather-{sev:0.0}");
                Player.CamOverride = false;
            }
            PortGame.I.ForceWeather(-1);
        }

        // Forklift: get in, lift a loaded pallet, drive, corner hard.
        IEnumerator ForkliftTest()
        {
            yield return WaitGame();
            var fl = Instantiate(ui.ForkliftPrefab, new Vector3(20f, 0.4f, -10f), Quaternion.Euler(0, -90, 0)).GetComponent<Forklift>();
            fl.NetworkObject.Spawn(true);
            yield return new WaitForSeconds(1.5f);
            var front = fl.transform.TransformPoint(new Vector3(0, 0, 1.45f));
            var pal = Instantiate(ui.PalletPrefab, new Vector3(front.x, 0.05f, front.z), fl.transform.rotation).GetComponent<Pallet>();
            pal.NetworkObject.Spawn(true);
            yield return new WaitForSeconds(0.5f);
            for (int i = 0; i < 6; i++) PortGame.I.SpawnCrate(pal.transform.TransformPoint(new Vector3(-0.6f + (i % 3) * 0.6f, 0.6f, i < 3 ? -0.3f : 0.3f)), 1f);
            yield return new WaitForSeconds(1.5f);
            var pl = Player.Local;
            pl.DebugTeleport(fl.Seat + fl.transform.right * -1.2f + Vector3.down * 0.9f, 0);
            yield return new WaitForSeconds(0.3f);
            fl.EnterRpc();
            yield return new WaitForSeconds(1f);
            Debug.Log($"[PB] forklift test: driver={(pl.Driving != null)} owner={fl.IsOwner} palletLoad={pal.transform.childCount}");
            Forklift.BotDrive = () => new Vector3(0, 0, 1);          // raise forks
            yield return new WaitForSeconds(0.6f);
            Forklift.BotDrive = () => Vector3.zero;
            yield return new WaitForSeconds(0.5f);
            Debug.Log($"[PB] forklift test: lifted hasPallet={fl.HasPallet.Value} mast={fl.Mast.Value:F2}");
            Forklift.BotDrive = () => new Vector3(0, 1, 0);          // drive forward
            yield return new WaitForSeconds(2.5f);
            Debug.Log($"[PB] forklift test: drove to {fl.transform.position:F1} hasPallet={fl.HasPallet.Value}");
            Forklift.BotDrive = () => new Vector3(1, 1, 1);          // raise high + hard turn at speed
            float maxTilt = 0;
            for (float t = 0; t < 3f; t += Time.deltaTime) { maxTilt = Mathf.Max(maxTilt, Vector3.Angle(fl.transform.up, Vector3.up)); yield return null; }
            Forklift.BotDrive = () => Vector3.zero;
            yield return new WaitForSeconds(1.5f);
            Debug.Log($"[PB] forklift test: hard corner maxTilt={maxTilt:F0} hasPallet={fl.HasPallet.Value} crates loose={Crate.All.FindAll(c => !c.Attached.Value).Count}");
            fl.ExitRpc();
            yield return new WaitForSeconds(1f);
            Debug.Log($"[PB] forklift test: exited driver={(pl.Driving != null)} at {pl.transform.position:F1}");
            Forklift.BotDrive = null;
        }

        IEnumerator StartOnDay(int n)
        {
            yield return WaitGame();
            PortGame.I.DebugStartDay(n);
            if (n >= 3) PortGame.I.DebugGrantKit();
            Debug.Log($"[PB] started on day {n}");
        }

        // Character close-ups: front, side, walking (side), carrying (front).
        IEnumerator CharShots(string dir)
        {
            Directory.CreateDirectory(dir);
            while (Player.Local == null) yield return null;
            var pl = Player.Local;
            pl.DebugTeleport(new Vector3(-20, 0.2f, -8), 90);
            yield return new WaitForSeconds(1.5f);
            Player.CamOverride = true;
            var cam = Camera.main;
            void Look(Vector3 offset) { var t = pl.transform.position + Vector3.up * 1.1f; cam.transform.position = t + offset; cam.transform.LookAt(t); }
            Look(pl.transform.forward * 3.2f + Vector3.up * 0.4f); yield return Snap(dir, "char-front");
            Look(-pl.transform.right * 3.2f + Vector3.up * 0.4f); yield return Snap(dir, "char-side");
            Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = Vector3.right * 0.6f };
            for (int i = 0; i < 40; i++) { Look(Vector3.forward * -4f + Vector3.up * 0.5f); yield return null; }
            yield return Snap(dir, "char-walking-side");
            Player.BotInput = () => new Player.In();
            pl.DebugGiveCrates(3);
            yield return new WaitForSeconds(1f);
            Look(pl.transform.forward * 4f + Vector3.up * 1.2f); yield return Snap(dir, "char-carrying-front");
            Player.CamOverride = false;
        }

        // Settings really reach the renderer, camera, audio and input; the player's own values are put back afterwards.
        IEnumerator SettingsTest()
        {
            yield return WaitGame();
            var urp = (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            float scale = Settings.RenderScale, fov = Settings.Fov, master = Settings.Master; int shadows = Settings.Shadows; var grab = Settings.KeyFor("grab");
            Settings.RenderScale = 0.5f; Settings.Shadows = 0; Settings.Fov = 80; Settings.Master = 0.3f; Settings.Bind("grab", UnityEngine.InputSystem.Key.G);
            ui.ApplySettings();
            yield return null;
            Debug.Log($"[PB] settings test: renderScale={urp.renderScale:F2} sunShadows={System.Array.Find(FindObjectsByType<Light>(FindObjectsSortMode.None), l => l.type == LightType.Directional)?.shadows} fov={Camera.main.fieldOfView:F0} volume={AudioListener.volume:F2} grabKey={Settings.KeyFor("grab")}");
            Settings.RenderScale = scale; Settings.Shadows = shadows; Settings.Fov = fov; Settings.Master = master; Settings.Bind("grab", grab);
            ui.ApplySettings();
            yield return null;
            Debug.Log($"[PB] settings test: restored renderScale={urp.renderScale:F2} fov={Camera.main.fieldOfView:F0} volume={AudioListener.volume:F2} grabKey={Settings.KeyFor("grab")}");
        }

        // Close-ups of the dock crates: fresh, spoiled, damaged, and the lidded empties by the shed.
        IEnumerator CrateShots(string dir)
        {
            Directory.CreateDirectory(dir);
            yield return WaitGame();
            Player.Local.DebugTeleport(new Vector3(-20, 0.2f, -8), 90);
            var fresh = PortGame.I.SpawnCrate(new Vector3(-2.2f, 0.6f, -1f), 1f);
            var spoiled = PortGame.I.SpawnCrate(new Vector3(-1.2f, 0.6f, -1.4f), 0.15f);
            var damaged = PortGame.I.SpawnCrate(new Vector3(-0.2f, 0.6f, -1f), 1f);
            damaged.Damage.Value = 2;
            yield return new WaitForSeconds(2f);
            Player.CamOverride = true;
            var cam = Camera.main;
            void Look(Vector3 at, Vector3 offset) { cam.transform.position = at + offset; cam.transform.LookAt(at); }
            Look(fresh.transform.position, new Vector3(0.3f, 1.1f, -1.3f)); yield return Snap(dir, "crate-fresh");
            Look(spoiled.transform.position + Vector3.left * 0.5f, new Vector3(0.2f, 1.6f, -2.4f)); yield return Snap(dir, "crate-row-fresh-spoiled-damaged");
            Look(new Vector3(12.7f, 0.9f, -12.8f), new Vector3(2.2f, 0.9f, 2.4f)); yield return Snap(dir, "crate-empties-by-shed");
            // A gull sitting on a post (P5).
            Transform gull = null;
            for (float t = 0; t < 40 && (gull = Gull.AnyPerched()) == null; t += 0.5f) yield return new WaitForSeconds(0.5f);
            if (gull != null) { Look(gull.position, new Vector3(1.2f, 0.5f, -1.6f)); yield return Snap(dir, "gull-perched"); }
            Player.CamOverride = false;
        }

        IEnumerator Snap(string dir, string name)
        {
            yield return new WaitForEndOfFrame();
            Capture(Path.Combine(dir, name + ".png"));
            yield return null;
            Debug.Log("[PB] snap " + name);
        }

        IEnumerator WobbleTest()
        {
            while (PortGame.I == null || Player.Local == null) yield return null;
            var pl = Player.Local;
            pl.DebugTeleport(new Vector3(-20, 0.2f, -8), 90);
            pl.DebugGiveCrates(5);
            yield return new WaitForSeconds(1);
            string[] phase = { "walk +x", "stop", "sprint +x", "sprint reverse -x", "sprint zigzag", "stop" };
            for (int p = 0; p < phase.Length; p++)
            {
                float peak = 0, t0 = Time.time;
                while (Time.time - t0 < 1.6f)
                {
                    int ph = p;
                    Player.BotInput = () => new Player.In
                    {
                        hasWorldMove = ph != 1 && ph != 5,
                        worldMove = ph == 3 ? Vector3.left : ph == 4 ? (Mathf.Sin(Time.time * 4) > 0 ? new Vector3(1, 0, 1).normalized : new Vector3(1, 0, -1).normalized) : Vector3.right,
                        sprint = ph >= 2,
                    };
                    peak = Mathf.Max(peak, pl.Teeter);
                    yield return null;
                }
                Debug.Log($"[PB] wobble {phase[p]}: carrying={pl.Carrying} peak lean={peak:F2}");
            }
            Player.BotInput = () => new Player.In();
        }

        IEnumerator TrolleyTest()
        {
            while (PortGame.I == null || Player.Local == null) yield return null;
            yield return new WaitForSeconds(1);
            var g = PortGame.I;
            var t = Instantiate(ui.TrolleyPrefab, new Vector3(-12, 0.4f, -5), Quaternion.Euler(0, 90, 0)).GetComponent<Trolley>();
            t.NetworkObject.Spawn(true);
            yield return new WaitForSeconds(1);
            for (int k = 0; k < 3; k++) g.SpawnCrate(t.transform.position + new Vector3(0, 1.2f + k * 0.7f, 0), 1f);
            yield return new WaitForSeconds(2.5f);
            Debug.Log($"[PB] trolley test: load={t.transform.GetComponentsInChildren<Crate>().Length} at {t.transform.position:F2}");
            // Push: stand behind it, walk +x for 3 s.
            Player.Local.DebugTeleport(t.transform.position - t.transform.forward * 1.4f, 90);
            yield return new WaitForSeconds(0.3f);
            t.SetPusher(Player.Local);
            var start = t.transform.position;
            Player.BotInput = () => new Player.In { hasWorldMove = true, worldMove = Vector3.right };
            yield return new WaitForSeconds(3f);
            Player.BotInput = () => new Player.In();
            Debug.Log($"[PB] trolley test: pushed {Vector3.Distance(start, t.transform.position):F1} m, load={t.transform.GetComponentsInChildren<Crate>().Length}, tilt={Vector3.Angle(t.transform.up, Vector3.up):0}");
            // Let go at the top of the slipway, facing downhill.
            t.SetPusher(null);
            var rb = t.GetComponent<Rigidbody>();
            rb.position = new Vector3(-10, 0.45f, 0.6f); rb.rotation = Quaternion.identity; rb.linearVelocity = Vector3.zero;
            Player.Local.DebugTeleport(new Vector3(-10, 0.3f, -3f), 0);
            for (int k = 0; k < 12; k++)
            {
                yield return new WaitForSeconds(0.5f);
                if (t == null || !t.IsSpawned) break;
                Debug.Log($"[PB] trolley test: rolling at {t.transform.position:F2} v={rb.linearVelocity.magnitude:F1}");
            }
            Debug.Log($"[PB] trolley test end: hasTrolley={g.HasTrolley.Value} inSea={g.InSea.Value}");
        }

        IEnumerator ForceNight(float after)
        {
            yield return new WaitForSeconds(after);
            var g = PortGame.I;
            if (g != null && g.IsServer) g.EndDayNow();
            if (!Has("-pb-shop")) yield break;
            yield return new WaitForSeconds(3);
            g.Money.Value = Mathf.Max(g.Money.Value, PortGame.TrolleyPrice);
            g.BuyTrolleyRpc();
            yield return new WaitForSeconds(3);
            g.NextDayRpc();
        }

        void Update()
        {
            if (quitAt > 0 && Time.realtimeSinceStartup > quitAt) { Debug.Log("[PB] auto quit"); Application.Quit(); }
            if (Time.realtimeSinceStartup > nextLog)
            {
                nextLog = Time.realtimeSinceStartup + 5;
                var g = PortGame.I; var nm = NetworkManager.Singleton;
                var pl = Player.Local;
                string boats = string.Join(",", Boat.All.ConvertAll(b => $"{b.State.Value}@{b.Berth.Value}"));
                foreach (var c in Crate.All)
                    if (c.transform.position.y > 2.5f && (c.transform.parent == null || c.transform.parent.GetComponent<Player>() == null))
                        Debug.Log($"[PB] high crate {c.NetworkObjectId} at {c.transform.position:F1} parent={(c.transform.parent ? c.transform.parent.name : "-")} attached={c.Attached.Value}");
                Debug.Log($"[PB] t={Time.realtimeSinceStartup:0} fps={1f / Mathf.Max(Time.smoothDeltaTime, 1e-4f):0} listening={nm.IsListening} server={nm.IsServer} clients={(nm.IsServer ? nm.ConnectedClientsIds.Count : -1)} " +
                          $"players={Player.All.Count} phase={g?.Phase.Value} day={g?.DayNum.Value} dayT={g?.DayT.Value:0} money={g?.Money.Value} sold={g?.Sold.Value} sea={g?.InSea.Value} left={g?.LeftAboard.Value} " +
                          $"crates={Crate.All.Count} boats=[{boats}] me={(pl ? pl.transform.position.ToString("F1") : "-")} carrying={(pl ? pl.Carrying : 0)} bot={bot?.State}");
            }
        }

        // Batchmode has no screen: render the main camera into a texture instead.
        static void Capture(string path)
        {
            if (!Application.isBatchMode) { ScreenCapture.CaptureScreenshot(path); return; }
            var cam = Camera.main;
            var rt = new RenderTexture(1600, 900, 24);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex); rt.Release();
        }

        IEnumerator Shots(string dir, float every)
        {
            Directory.CreateDirectory(dir);
            int i = 0;
            while (true)
            {
                yield return new WaitForSecondsRealtime(every);
                yield return new WaitForEndOfFrame();
                var path = Path.Combine(dir, $"shot_{i++:000}.png");
                Capture(path);
                Debug.Log("[PB] shot " + path);
            }
        }
    }

    // Scripted player for headless tests: fetch crates (pier, then boat), stack 3, carry to the table, drop to sell.
    public class Bot
    {
        public string State = "seek";
        readonly List<Vector3> path = new();
        Crate target;
        float pulse, stuckT, lastProgress;
        Vector3 lastPos;
        int trips;

        static readonly Vector3 QuayMid = new(1.2f, 0, -1.2f), PierB = new(1.2f, 0, 16f), Gang = new(3.4f, 0, 16f), Deck = new(6.2f, 0, 16f), TableApproach = World.BuyerPos + new Vector3(0, 0, 2.4f);

        static bool OnBoat(Vector3 p) => p.x > 4.3f;
        static bool OnPier(Vector3 p) => p.z > 0.3f && !OnBoat(p);

        void Route(Vector3 from, Vector3 goal)
        {
            path.Clear();
            if (OnBoat(from) && !OnBoat(goal)) { path.Add(Gang); path.Add(PierB); }
            if ((OnPier(from) || OnBoat(from)) && !OnPier(goal) && !OnBoat(goal)) { path.Add(new Vector3(1.2f, 0, Mathf.Min(from.z, 16f))); path.Add(QuayMid); }
            if (!OnPier(from) && !OnBoat(from) && (OnPier(goal) || OnBoat(goal))) path.Add(QuayMid);
            if (OnBoat(goal) && !OnBoat(from)) { path.Add(PierB); path.Add(Gang); path.Add(Deck); }
            else if (OnPier(goal)) path.Add(new Vector3(1.2f, 0, Mathf.Clamp(goal.z, 0.5f, 22f)));
            path.Add(goal);
        }

        Crate PickCrate(Vector3 me)
        {
            Crate best = null; float bd = 1e9f;
            foreach (var c in Crate.All)
            {
                var p = c.transform.position;
                if (c.Attached.Value || c.InSea || p.y < Water.Level + 0.6f ) continue;
                var dock0 = Boat.DockedAt[0];
                if (p.x < -4.3f) continue; // the bot only works the right-hand berth
                if (OnBoat(p) && (dock0 == null || Mathf.Abs(dock0.transform.InverseTransformPoint(p).x) > Boat.HalfW - 0.4f)) continue;
                float d = Vector3.Distance(me, p) + (OnBoat(p) ? 4f : 0f);
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        public Player.In Tick()
        {
            var i = new Player.In();
            var pl = Player.Local;
            if (pl == null || PortGame.I == null || PortGame.I.Phase.Value != PortGame.Ph.Day) return i;
            var me = pl.transform.position;
            pulse -= Time.deltaTime;
            if (pl.Swimming)
            {
                // Overboard: paddle to the nearest ladder and climb out.
                World.Ladder best = default; float bd = 1e9f;
                foreach (var l in World.Ladders) { float d = (l.Bottom - me).sqrMagnitude; if (d < bd) { bd = d; best = l; } }
                var to = best.Bottom - me; to.y = 0;
                i.hasWorldMove = true; i.worldMove = to.normalized;
                if (pl.NearLadder() != null && pulse <= 0) { i.grab = true; pulse = 1f; path.Clear(); target = null; }
                return i;
            }

            // Unstick: if we haven't moved for a while, re-route.
            if ((me - lastPos).sqrMagnitude > 0.04f) { lastPos = me; lastProgress = Time.time; }
            else if (Time.time - lastProgress > 4f) { path.Clear(); target = null; lastProgress = Time.time; i.jump = true; }

            if (State == "seek")
            {
                if (target == null || !target.IsSpawned || target.Attached.Value || target.InSea)
                {
                    target = PickCrate(me);
                    if (target == null) return pl.Carrying > 0 ? GoSell(i, me) : i;
                    Route(me, target.transform.position);
                }
                var tp = target.transform.position;
                var flat = new Vector3(tp.x - me.x, 0, tp.z - me.z);
                if (path.Count <= 1 && flat.magnitude < 1.3f)
                {
                    i.hasWorldMove = true; i.worldMove = flat.normalized * 0.15f;
                    if (pulse <= 0 && Vector3.Angle(pl.transform.forward, flat) < 30f) { i.grab = true; pulse = 0.4f; }
                    if (pl.FindCrate() == null && pulse <= 0) { i.worldMove = flat.normalized; }
                }
                else Follow(ref i, me, tp);
                if (pl.Carrying >= 3 || (pl.Carrying > 0 && target != null && target.Attached.Value && PickNear(me) == null)) { State = "sell"; path.Clear(); }
                if (target != null && target.Attached.Value) target = null;
                return i;
            }
            return GoSell(i, me);
        }

        Crate PickNear(Vector3 me)
        {
            var c = PickCrate(me);
            return c != null && Vector3.Distance(c.transform.position, me) < 5f ? c : null;
        }

        Player.In GoSell(Player.In i, Vector3 me)
        {
            var pl = Player.Local;
            State = "sell";
            if (pl.Carrying == 0) { State = "seek"; path.Clear(); trips++; return i; }
            if (path.Count == 0) Route(me, TableApproach);
            var flat = new Vector3(TableApproach.x - me.x, 0, TableApproach.z - me.z);
            if (path.Count <= 1 && flat.magnitude < 0.45f)
            {
                i.hasWorldMove = true; i.worldMove = new Vector3(0, 0, -0.5f);
                if (pulse <= 0 && !GameUI.BuyerOpen) { i.grab = true; pulse = 1f; }
                return i;
            }
            Follow(ref i, me, TableApproach);
            return i;
        }

        void Follow(ref Player.In i, Vector3 me, Vector3 goal)
        {
            while (path.Count > 0)
            {
                var w = path[0];
                if (new Vector2(w.x - me.x, w.z - me.z).magnitude < 0.6f && path.Count > 1) path.RemoveAt(0);
                else break;
            }
            var to = path.Count > 0 ? path[0] : goal;
            var d = new Vector3(to.x - me.x, 0, to.z - me.z);
            i.hasWorldMove = true;
            i.worldMove = d.magnitude > 0.1f ? d.normalized : Vector3.zero;
            // Keep the camera behind the bot so screenshots show what it's doing.
            i.look.x = Mathf.DeltaAngle(Player.Local.Yaw, Player.Local.transform.eulerAngles.y) * 0.05f;
        }
    }
}
