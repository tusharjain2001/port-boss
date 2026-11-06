using System.Collections;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    // M7 tests (systems "M7" / §1.12 settings + accessibility):
    //   -pb-settingstest <dir|none>  every new setting through the phone's Settings rows (the saved value, and what it does):
    //     UI scale 80-150% (the HUD canvas; the phone grows only while it fits the screen), FOV, camera shake, reduce
    //     motion (no shake, no FOV punch, no overshoot), the master and effects volumes, sprint hold / tap, easy fishing
    //     (+30% green on a real fish) and the bite caption (on a real bite). The PC's own settings are put back after.
    public partial class Bot
    {
        IEnumerator M7Tests() => Arg("-pb-settingstest") != null ? SettingsTest(Arg("-pb-settingstest")) : null;
        int m7Fails;
        void M7(bool ok, string what) { Debug.Log($"[PB] m7: {(ok ? "ok" : "FAIL")} {what}"); if (!ok) m7Fails++; }

        IEnumerator SettingsTest(string dir)
        {
            Ctl.Bot = true; Sleep.Off = true; shots = dir == "none" ? null : dir; if (shots != null) Directory.CreateDirectory(shots);
            yield return new WaitForSeconds(1.5f);
            // the PC's own settings, put back at the end (the test taps the real rows, which save)
            var keep = (Settings.UiScale, Settings.Fov, Settings.ShakeK, Settings.RollK, Settings.ReduceMotion, Settings.Master, Settings.Sfx, Settings.Ambience, Settings.Mono, Settings.EasyFishing, Settings.Captions, Settings.SprintToggle, Settings.BarSpeed);
            Settings.BarSpeed = 1; Settings.UiScale = 1; Settings.Fov = 55; Settings.ShakeK = 0.7f; Settings.RollK = 0.4f; Settings.ReduceMotion = false; Settings.Master = Settings.Sfx = Settings.Ambience = 1; Settings.EasyFishing = false; Settings.Captions = true; Settings.SprintToggle = false;
            yield return Frames(3);

            // ---------------- the screen
            Phone.I.Show(Phone.Scr.Settings); yield return new WaitForSecondsRealtime(0.6f);
            yield return M5Shot("m7-settings-top");
            Phone.I.ScrollBy(560); yield return Frames(2); yield return M5Shot("m7-settings-middle");
            Phone.I.ScrollBy(2000); yield return Frames(2); yield return M5Shot("m7-settings-bottom");
            Phone.I.ScrollBy(-3000); yield return Frames(2);

            // ---------------- UI scale
            var hudRt = (RectTransform)Hud.I.transform; var cs = Hud.I.GetComponent<UnityEngine.UI.CanvasScaler>();
            for (int i = 0; i < 6; i++) { Phone.I.Tap("UI scale +"); yield return null; }
            yield return Frames(3);
            M7(Mathf.Approximately(Settings.UiScale, 1.5f) && Mathf.Abs(PlayerPrefs.GetFloat("m7.ui", 0) - 1.5f) < 0.01f, $"UI scale + to the top: {Settings.UiScale:P0}, saved {PlayerPrefs.GetFloat("m7.ui", 0):P0}");
            M7(Mathf.Abs(cs.referenceResolution.x - 1280) < 1, $"the HUD canvas grows 150% (reference {cs.referenceResolution})");
            M7(OnScreen(Phone.I.Root, out var r150), $"the phone still fits the screen at 150% ({r150}, scale {Phone.I.Root.localScale.x:F2})");
            yield return new WaitForSecondsRealtime(0.4f);   // (the hotbar's slide aside)
            Clear(hudRt, out var c150); M7(c150 == "", $"at 150% the phone is clear of the bank pill and the hotbar{c150}");
            yield return M5Shot("m7-ui-150");
            for (int i = 0; i < 3; i++) { Phone.I.Tap("UI scale −"); yield return null; }
            yield return new WaitForSecondsRealtime(0.4f);
            Clear(hudRt, out var c120); M7(Mathf.Approximately(Settings.UiScale, 1.2f) && c120 == "" && OnScreen(Phone.I.Root, out _), $"at 120% (the Deck default) the phone fits, clear of the pill and the hotbar{c120}");
            yield return M5Shot("m7-ui-120");
            for (int i = 0; i < 6; i++) { Phone.I.Tap("UI scale −"); yield return null; }
            yield return Frames(3);
            M7(Mathf.Approximately(Settings.UiScale, 0.8f) && Mathf.Abs(cs.referenceResolution.x - 2400) < 1, $"UI scale − to the bottom: {Settings.UiScale:P0}, reference {cs.referenceResolution}");
            M7(OnScreen(Phone.I.Root, out var r80), $"the phone fits at 80% ({r80})");
            yield return M5Shot("m7-ui-80");
            for (int i = 0; i < 2; i++) { Phone.I.Tap("UI scale +"); yield return null; }
            yield return Frames(3);
            M7(Mathf.Approximately(Settings.UiScale, 1f) && Mathf.Abs(cs.referenceResolution.x - 1920) < 1, "back to 100%");

            // ---------------- audio
            for (int i = 0; i < 5; i++) { Phone.I.Tap("Master −"); yield return null; }
            yield return Frames(2);
            M7(Mathf.Abs(AudioListener.volume - 0.5f) < 0.01f, $"Master 50%: the listener at {AudioListener.volume:F2}");
            for (int i = 0; i < 5; i++) { Phone.I.Tap("Master +"); Phone.I.Tap("Effects −"); yield return null; }
            var src = Snd.Play(Synth.Tik, me.transform.position, 1f);
            M7(src != null && Mathf.Abs(src.volume - 0.5f * Snd.Master) < 0.01f, $"Effects 50%: a one-shot plays at {src?.volume:F2}");
            for (int i = 0; i < 5; i++) { Phone.I.Tap("Effects +"); yield return null; }

            // ---------------- camera: FOV, shake, reduce motion
            Phone.I.Hide(); yield return new WaitForSecondsRealtime(0.8f);
            float fov0 = CameraRig.I.Cam.fieldOfView;
            Phone.I.Show(Phone.Scr.Settings); yield return new WaitForSecondsRealtime(0.5f);
            Phone.I.Tap("Field of view +"); Phone.I.Tap("Field of view +"); Phone.I.Hide(); yield return new WaitForSecondsRealtime(0.8f);
            float fov1 = CameraRig.I.Cam.fieldOfView;
            M7(Mathf.Abs(fov1 - fov0 - 10) < 0.6f && Mathf.Approximately(PlayerPrefs.GetFloat("m7.fov", 0), 65), $"Field of view 55 -> 65: the camera {fov0:F1} -> {fov1:F1}°");
            CameraRig.Shake(1); float sh70 = CameraRig.ShakeNow; yield return new WaitForSecondsRealtime(1f);
            Phone.I.Show(Phone.Scr.Settings); yield return new WaitForSecondsRealtime(0.4f);
            for (int i = 0; i < 7; i++) Phone.I.Tap("Camera shake −");
            CameraRig.Shake(1); float sh0 = CameraRig.ShakeNow;
            for (int i = 0; i < 7; i++) Phone.I.Tap("Camera shake +");
            M7(Mathf.Abs(sh70 - 1) < 0.01f && sh0 < 0.001f, $"Camera shake: 70% shakes as tuned ({sh70:F2}), 0% not at all ({sh0:F2})");
            yield return new WaitForSecondsRealtime(1f);
            float ease0 = Util.EaseOutBack(0.8f);
            Phone.I.Tap("Reduce motion"); yield return null;
            CameraRig.Shake(1); CameraRig.I.FovPunch = 3; yield return null;
            M7(Settings.ReduceMotion && CameraRig.ShakeNow < 0.001f && CameraRig.I.FovPunch == 0 && Util.EaseOutBack(0.8f) <= 1 && ease0 > 1,
                $"Reduce motion: no shake ({CameraRig.ShakeNow:F2}), no FOV punch, slides ease in without the overshoot ({ease0:F3} -> {Util.EaseOutBack(0.8f):F3})");
            Phone.I.Tap("Reduce motion"); Phone.I.Tap("Field of view −"); Phone.I.Tap("Field of view −"); yield return null;

            // ---------------- sprint: hold or tap (the input rule, frame by frame)
            Phone.I.Tap("Hold actions"); yield return null;
            bool a1, a2, a3, a4, a5;
            Ctl.BotSprint = true; a1 = Ctl.Run(true); Ctl.BotSprint = false; a2 = Ctl.Run(true); a3 = Ctl.Run(true);
            Ctl.BotSprint = true; a4 = Ctl.Run(true); Ctl.BotSprint = false; Ctl.Run(true);
            Ctl.BotSprint = true; Ctl.Run(true); Ctl.BotSprint = false; a5 = Ctl.Run(false);
            M7(Settings.SprintToggle && a1 && a2 && a3 && !a4 && !a5, $"Sprint: Tap: a tap starts it ({a1}), it runs on without holding ({a2} {a3}), a tap ends it ({!a4}), stopping ends it ({!a5})");
            Phone.I.Tap("Hold actions"); yield return null;
            Ctl.BotSprint = true; a1 = Ctl.Run(true); Ctl.BotSprint = false; a2 = Ctl.Run(true);
            M7(!Settings.SprintToggle && a1 && !a2, "Sprint: Hold: only while held");
            // brace (one edge per frame, so a frame between reads)
            Phone.I.Tap("Hold actions"); yield return null;
            bool b1, b2, b3, b4;
            Ctl.BotBrace = true; yield return null; b1 = Ctl.Brace; Ctl.BotBrace = false; yield return null; b2 = Ctl.Brace;
            Ctl.BotBrace = true; yield return null; b3 = Ctl.Brace; Ctl.BotBrace = false; yield return null; b4 = Ctl.Brace;
            M7(b1 && b2 && !b3 && !b4, $"Brace: Tap: a tap braces ({b1}), it holds without the key ({b2}), the next tap lets go ({!b3} {!b4})");
            Phone.I.Tap("Hold actions"); yield return null;
            Ctl.BotBrace = true; yield return null; b1 = Ctl.Brace; Ctl.BotBrace = false; yield return null; b2 = Ctl.Brace;
            M7(!Settings.SprintToggle && b1 && !b2, "Brace: Hold: only while held");
            Phone.I.ScrollBy(800); yield return Frames(2);
            for (int i = 0; i < 8; i++) { Phone.I.Tap("Bar speed −"); yield return null; }
            M7(Mathf.Approximately(Settings.BarSpeed, 0.7f) && Mathf.Abs(PlayerPrefs.GetFloat("m7.bar", 0) - 0.7f) < 0.01f, $"Bar speed − to the bottom: {Settings.BarSpeed:P0}");
            Phone.I.Hide(); yield return new WaitForSecondsRealtime(0.6f);

            // ---------------- a real fish: easy fishing and the bite caption
            Settings.EasyFishing = true; Fishing.Bot.FightHeavy = true;
            yield return SailOut(false); me.Select(0);
            // the cast charge on "Hold actions: Tap": a click starts it, it keeps charging with the button up, the next click casts
            Settings.SprintToggle = true;
            while (fish.State != Fishing.St.Ready) yield return null;
            Ctl.BotUse = true; Ctl.BotUsePressed = true; yield return null; Ctl.BotUsePressed = false; Ctl.BotUse = false;
            yield return new WaitForSeconds(0.6f); bool charging = fish.State == Fishing.St.Charging;
            Ctl.BotUsePressed = true; yield return null; Ctl.BotUsePressed = false; yield return null; var castSt = fish.State;
            M7(charging && castSt == Fishing.St.Flight, $"Cast: Tap: charging with the button up ({charging}), the next click casts ({castSt})");
            Settings.SprintToggle = false; fish.Abort(); yield return new WaitForSeconds(1.5f);
            string ptrWhat = "no minigame"; bool ptrOk = false;
            string capText = null; bool capSeen = false, zoneOk = false; string zoneWhat = "no minigame";
            for (int attempt = 0; attempt < 3 && !(capSeen && zoneOk); attempt++)
            {
                yield return CastOnce(0.7f);
                float t0 = Time.time; while (fish.State != Fishing.St.Bite && Time.time - t0 < 40) yield return null;
                if (fish.State != Fishing.St.Bite) { fish.Abort(); continue; }
                yield return null; capSeen = Hud.I.CaptionShown(out capText) && capText == "[Splash] Bite!";
                if (capSeen && shots != null) { yield return Shot("m7-bite-caption"); }
                yield return new WaitForSeconds(0.35f); Ctl.BotStrike = true; yield return null; Ctl.BotStrike = false;
                t0 = Time.time; while (fish.State != Fishing.St.Minigame && Time.time - t0 < 6) yield return null;
                if (fish.State == Fishing.St.Minigame)
                {
                    int c = (int)fish.Hooked.Class; float want = Mathf.Min(0.6f, FishDb.Green[c] * fish.Rod.GreenMul * Fishing.ZoneScale * 1.3f), plain = Mathf.Min(0.6f, FishDb.Green[c] * fish.Rod.GreenMul * Fishing.ZoneScale);
                    zoneOk = Mathf.Abs(fish.ZoneW - want) < 0.001f && (want >= 0.6f || fish.ZoneW > plain * 1.29f);
                    zoneWhat = $"{fish.Hooked.Id}: green {fish.ZoneW:F3} (plain {plain:F3}, x{fish.ZoneW / plain:F2})";
                    float plainPtr = FishDb.Speed[c] * fish.Rod.SpeedMul * Fishing.SpeedScale;
                    ptrOk = Mathf.Abs(fish.PtrSpeed - plainPtr * 0.7f) < 0.001f; ptrWhat = $"pointer {fish.PtrSpeed:F3}/s (plain {plainPtr:F3}, x{fish.PtrSpeed / plainPtr:F2})";
                    if (shots != null) { yield return new WaitForSeconds(0.6f); yield return Shot("m7-easy-fishing-bar"); }   // (the bar slides in first)
                }
                // play it out: stop the bar on the zone, take the fish
                t0 = Time.time;
                while (Time.time - t0 < 30 && fish.State != Fishing.St.Ready)
                {
                    if (fish.State == Fishing.St.Minigame && !fish.Frozen && Mathf.Abs(fish.PointerAt(Time.timeAsDouble) - fish.ZoneAt(Time.timeAsDouble)) < fish.ZoneW / 4) { Ctl.BotStrike = true; yield return null; Ctl.BotStrike = false; }
                    if (fish.State == Fishing.St.Reveal && fish.StateT > 0.8f) { Ctl.BotInteract = true; yield return null; Ctl.BotInteract = false; }
                    if (fish.State is Fishing.St.Waiting or Fishing.St.Settle) fish.Abort();
                    yield return null;
                }
            }
            M7(capSeen, $"the bite caption on a real bite: \"{capText}\"");
            M7(zoneOk, $"Easy fishing: the green 30% wider on a real fish ({zoneWhat})");
            M7(ptrOk, $"Bar speed 70%: the pointer at 70% on a real fish ({ptrWhat})");
            yield return new WaitForSecondsRealtime(1.8f);   // (the bite's caption times out)
            Settings.Captions = false; Hud.I.Caption("[Splash] Bite!"); M7(!Hud.I.CaptionShown(out _), "Bite captions off: none shown"); Settings.Captions = true;

            // ---------------- put this PC's settings back
            (Settings.UiScale, Settings.Fov, Settings.ShakeK, Settings.RollK, Settings.ReduceMotion, Settings.Master, Settings.Sfx, Settings.Ambience, Settings.Mono, Settings.EasyFishing, Settings.Captions, Settings.SprintToggle, Settings.BarSpeed) = keep;
            Settings.Put("m7.ui", Settings.UiScale); Settings.Put("m7.fov", Settings.Fov); Settings.Put("m7.shake", Settings.ShakeK); Settings.Put("m7.roll", Settings.RollK); Settings.Put("m7.calm", Settings.ReduceMotion ? 1 : 0);
            Settings.Put("m7.vol", Settings.Master); Settings.Put("m7.sfx", Settings.Sfx); Settings.Put("m7.amb", Settings.Ambience); Settings.Put("m7.mono", Settings.Mono ? 1 : 0);
            Settings.Put("m7.easy", Settings.EasyFishing ? 1 : 0); Settings.Put("m7.cap", Settings.Captions ? 1 : 0); Settings.Put("m7.sprint", Settings.SprintToggle ? 1 : 0); Settings.Put("m7.bar", Settings.BarSpeed);
            Debug.Log($"[PB] RESULT settingstest: {(m7Fails == 0 ? "PASS" : m7Fails + " FAILED")}");
            Application.Quit();
        }

        // the phone overlaps neither the bank pill (when it shows) nor the hotbar: "" when clear, else what overlaps
        static void Clear(RectTransform hud, out string what)
        {
            what = ""; var ph = Sr(Phone.I.Root);
            var pill = hud.Find("bank pill") as RectTransform; if (pill != null && pill.gameObject.activeInHierarchy && Sr(pill).Overlaps(ph)) what += $" · the bank pill {Sr(pill)} overlaps the phone {ph}";
            var bar = hud.Find("hotbar") as RectTransform; if (bar != null && bar.gameObject.activeInHierarchy && Sr(bar).Overlaps(ph)) what += $" · the hotbar {Sr(bar)} overlaps the phone {ph}";
        }
        static Rect Sr(RectTransform r)
        {
            var c = new Vector3[4]; r.GetWorldCorners(c); var cam = CameraRig.I.UiCam; Vector2 lo = new(1e5f, 1e5f), hi = new(-1e5f, -1e5f);
            foreach (var p in c) { var s = (Vector2)cam.WorldToScreenPoint(p); lo = Vector2.Min(lo, s); hi = Vector2.Max(hi, s); }
            return Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
        }

        // a panel wholly on the screen (its corners through the UI camera)
        static bool OnScreen(RectTransform r, out string where)
        {
            var c = new Vector3[4]; r.GetWorldCorners(c); var cam = CameraRig.I.UiCam; Vector2 lo = new(9999, 9999), hi = new(-9999, -9999);
            foreach (var p in c) { var s = (Vector2)cam.WorldToScreenPoint(p); lo = Vector2.Min(lo, s); hi = Vector2.Max(hi, s); }
            where = $"x {lo.x:F0}-{hi.x:F0} y {lo.y:F0}-{hi.y:F0} of {cam.pixelWidth}x{cam.pixelHeight}";   // (the UI camera's target, not the batch window)
            return lo.x >= -1 && lo.y >= -1 && hi.x <= cam.pixelWidth + 1 && hi.y <= cam.pixelHeight + 1;
        }
    }
}
