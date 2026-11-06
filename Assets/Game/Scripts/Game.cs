using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    // M1 = boat + fishing (solo). The scene holds only this; everything else is built here from M1Assets.
    public class Game : MonoBehaviour
    {
        public static Material ToonMat;
        static readonly Dictionary<string, Material> fishMats = new();
        public static bool Paused;
        public static RenderTexture BatchTarget, UiTarget;   // batchmode: the camera renders here (1920x1080) so shots include the UI
        AudioSource sea; float gullT = 6;
        UnityEngine.UI.Text pauseT; RectTransform pause;

        public static Material FishMat(FishSpecies s)
        {
            if (s == null) return ToonMat;
            if (fishMats.TryGetValue(s.Id, out var m)) return m;
            m = new Material(M1Assets.I.Toon) { name = "fish " + s.Id };
            m.SetFloat("_PaintNoise", 0.12f); m.SetFloat("_Wet", 0.8f); m.SetFloat("_SwimFreq", 3f);
            m.SetColor("_RimColor", new Color(1, 1, 0.95f, 0.35f));
            if (s.Id == "anglerfish") { m.SetFloat("_GlowStrength", 2.5f); m.SetColor("_GlowColor", Ponyo.Hex("FFE89A")); }
            fishMats[s.Id] = m; return m;
        }

        void Awake()
        {
            // a test run always ends, even if setup below throws
            var args = System.Environment.GetCommandLineArgs(); int qi = System.Array.IndexOf(args, "-pb-quit");
            if (qi >= 0 && qi + 1 < args.Length && float.TryParse(args[qi + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var qs)) StartCoroutine(Failsafe(qs + 5));
            Application.targetFrameRate = Application.isBatchMode ? -1 : 144;
            QualitySettings.vSyncCount = Application.isBatchMode ? 0 : 1;
            var a = M1Assets.I;
            if (a == null) { Debug.LogError("[PB] M1Assets missing (run M1Build)"); return; }
            FishDb.Load(a.FishCsv.text);
            Debug.Log($"[PB] M1 start: {FishDb.All.Count} fish species, rods {FishDb.Rods.Length}, baits {FishDb.Baits.Length}");
            UiKit.Init(a);
            Ponyo.Apply(a);
            ToonMat = Ponyo.Toon(a, Color.white);
            Sea.Create(Ponyo.SeaMaterial(a));
            Map.Create(a, ToonMat); // map B, Crescent Bay: the island, the town, the anchors, the seabed
            var land = Landing.Create(a.Landing, ToonMat);
            Map.BuildPlaces();
            Skiff.Create(a.Boat, ToonMat, land.Moor);
            Sky.Create(); // M3: the world clock, the 6 lighting states, the weather
            var spawn = land.Spawn != null ? land.Spawn : land.transform;
            var p = Angler.Create(a, spawn.position + Vector3.up * 0.05f, Skiff.I != null ? Quaternion.LookRotation(Skiff.I.transform.position - spawn.position).eulerAngles.y : 0);
            var rig = CameraRig.Create(p.transform);
            rig.Yaw = p.transform.eulerAngles.y + 25;
            if (Application.isBatchMode)
            {
                BatchTarget = new RenderTexture(1920, 1080, 24) { antiAliasing = 4, name = "batch" };
                rig.Cam.targetTexture = BatchTarget;
                // the UI camera renders on its own into a transparent texture; shots composite it over the scene
                var ui = rig.UiCam; var md = rig.Cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); md.cameraStack.Remove(ui);
                var ud = ui.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); ud.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Base;
                ui.clearFlags = CameraClearFlags.SolidColor; ui.backgroundColor = new Color(0, 0, 0, 0);
                UiTarget = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32) { name = "batchUi" }; ui.targetTexture = UiTarget;
            }
            Hud.Create(rig.UiCam, p);
            Sleep.Create(); // M3: Walt's cot, tired, the night skip
            Company.CrewSeed = Sky.Seed; Firm.Create(); Works.Create(); Workers.Create(); Phone.Create(); Shop.Create(); YShop.Create(); Wardrobe.Create(); StoreUi.Create(); Pouch.Create(); // M4: the company, the phone (Tab), the shop counters
            BakeIcons(a);

            sea = gameObject.AddComponent<AudioSource>(); sea.clip = a.Waves; sea.loop = true; sea.volume = 0.32f; sea.spatialBlend = 0; sea.Play();
            if (!Application.isBatchMode) Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            BuildPause();
            Application.quitting += () => { if (!Application.isBatchMode) Save.OnQuit(); }; // M4: closing the window saves too

            // the welcome tips one at a time, 6 s apart (look audit 3 Nov: three at once were a wall of text)
            void Welcome() => StartCoroutine(Tips());
            IEnumerator Tips()
            {
                Alerts.Push("Grandpa Walt", "Biscuit's yours now, kiddo. She leaks a bit, but she floats. The fish are out past the pier.", Alerts.Kind.Story);
                yield return new WaitForSeconds(6f);
                Alerts.Push("How to fish", "1 rod · hold LMB to cast, release · strike on the bite · Space stops the bar", Alerts.Kind.Info);
                yield return new WaitForSeconds(6f);
                Alerts.Push("Biscuit", "E at the tiller to take her out. E at the bow for the anchor.", Alerts.Kind.Info);
            }
            Debug.Log($"[PB] hotbar self-test: {Hotbar.SelfTest()}");
            if (gameObject.GetComponent<Bot>() == null) gameObject.AddComponent<Bot>();
            Coop.Boot(); // -pb-host / -pb-join: co-op (M2) straight away (the tests)
            // otherwise the title menu: solo, or a crew on Steam / this network (not in the tests' batch mode)
            bool menu = Coop.I == null && (!Application.isBatchMode || System.Array.IndexOf(args, "-pb-menutest") >= 0);
            if (menu) { Coop.SteamUp(); Title.Open(); Title.Closed += Welcome; } // (the tips once play starts)
            else Welcome();
        }

        System.Collections.IEnumerator Failsafe(float s) { yield return new WaitForSecondsRealtime(s); Debug.Log("[PB] quit (failsafe)"); Application.Quit(); }

        void BakeIcons(M1Assets a)
        {
            foreach (var s in FishDb.All)
            {
                var pf = a.FishPrefab(s.Id);
                if (pf != null) Hud.Icons[s.Id] = IconBaker.Bake(pf, 192, 96, FishMat(s));
            }
            Hud.RodIcon = UiKit.RodIcon(1); // painted (UiKit): the baked 3D rod was a hairline at slot size
        }

        void BuildPause()
        {
            var hud = Hud.I.transform;
            pause = UiKit.Card(hud, "pause", 26, Ponyo.Paper, true);
            pause.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 360));
            var t = UiKit.Txt(pause, "Paused", UiKit.Display, 49, Ponyo.Ink, TextAnchor.UpperCenter); t.rectTransform.Stretch(30);
            pauseT = UiKit.Txt(pause, PauseText(), UiKit.Body, 22, Ponyo.Ink, TextAnchor.MiddleCenter); pauseT.rectTransform.Stretch(30); pauseT.rectTransform.offsetMax = new Vector2(-30, -60);
            pause.gameObject.SetActive(false);
        }

        // co-op adds: invite friends (anyone in a crew, M3.1), the crew code, leave the crew
        static string PauseText() => "Esc  resume\nQ  save & quit to desktop" + (Coop.On ? "\nI  invite friends" + (Coop.Code != null ? $"  ·  crew code <b>{Coop.Code}</b>" : $"  ·  they join {Coop.LanAddress}") + "\nL  leave the crew (play on alone)" : "") + "\n\nF3 debug overlay · F5 rod tier · F6 bait · hold R: the bait pouch";
        public static void ClosePause() { var g = FindAnyObjectByType<Game>(); if (g != null && Paused) g.SetPaused(false); }
        public void ShowPause(bool on) => SetPaused(on); public string PauseLine => pauseT.text; // (tests)
        void SetPaused(bool on) { Paused = on; Time.timeScale = Paused && !Coop.On ? 0 : 1; pause.gameObject.SetActive(Paused); pauseT.text = PauseText(); Cursor.lockState = Paused ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = Paused; }

        void OnApplicationQuit()
        {
            if (!Coop.SteamOk) return;
            try { Coop.Lobby?.Leave(); Steamworks.SteamClient.Shutdown(); } catch { }
        }

        void Update()
        {
            Coop.SteamTick(); // (lobby callbacks, invites, the mic)
            if (Title.Up || Title.ClosedFrame == Time.frameCount) return; // (the title, or the invite list: Esc is theirs)
            // (co-op never stops the world: the menu opens, the boat and the crew carry on)
            if (Ctl.Escape && !Ctl.Bot && Ctl.Ui == 0 && Ctl.UiEsc != Time.frameCount) SetPaused(!Paused); // (M4: not while the phone/shop/pouch has Esc)
            if (Paused && Ctl.Key_(UnityEngine.InputSystem.Key.Q)) { Save.OnQuit(); Application.Quit(); } // M4: save & quit
            // I: the invite list, from the pause menu or straight from the game (M3.1: Manan's Esc, I did nothing); not over the phone/shop (M4)
            if (Ctl.Key_(UnityEngine.InputSystem.Key.I) && !Ctl.Bot && Ctl.Ui == 0) { if (Paused) SetPaused(false); Coop.Invite(); }
            if (Paused && Coop.On && Ctl.Key_(UnityEngine.InputSystem.Key.L)) { Coop.Leave(); SetPaused(false); Alerts.Push("Crew", "You left the crew. Biscuit's all yours.", Alerts.Kind.Info); }
            if (!Paused && Ctl.Ui == 0 && !Application.isBatchMode && Cursor.lockState != CursorLockMode.Locked && UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame) Cursor.lockState = CursorLockMode.Locked;
            // gulls now and then
            gullT -= Time.deltaTime;
            if (gullT <= 0 && M1Assets.I.Gull != null) { gullT = Random.Range(9f, 22f); var c = Camera.main; if (c != null) Snd.Play(M1Assets.I.Gull, c.transform.position + Random.onUnitSphere * 40 + Vector3.up * 20, Random.Range(0.25f, 0.45f), Random.Range(0.9f, 1.15f), 0.9f); }
        }
    }
}
