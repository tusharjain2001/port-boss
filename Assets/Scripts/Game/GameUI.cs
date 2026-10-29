using System.Collections.Generic;
using PortBoss.Net;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PortBoss
{
    // Bootstrap + all UI (IMGUI): main menu, pause, HUD, shouts, money popups, end-of-day card, night shop.
    public partial class GameUI : MonoBehaviour
    {
        public static GameUI I;
        public GameObject CratePrefab, BoatPrefab, TrolleyPrefab, PortGamePrefab, ForkliftPrefab, PalletPrefab;
        public Material LitMat, EmissiveMat, ParticleMat, WaterMat, CloudMat, LandMat;
        public Skin Skin; // Toon Harbor slots (empty = own art)
        public GameObject[] Models; public int[] ModelKit; public Material[] KitMats;
        public Font TitleFont;

        enum Screen { Menu, Join, Settings, Playing }
        Screen screen = Screen.Menu;
        bool paused, showHelp;
        string lanIp = "127.0.0.1";
        Steamworks.Data.Lobby[] lobbies = new Steamworks.Data.Lobby[0];
        bool searching;

        static string shout, shoutWho; static float shoutUntil;
        static string toast; static float toastUntil;
        struct Pop { public Vector3 p; public string s; public Color c; public float t0; }
        static readonly List<Pop> pops = new();

        Light sun;
        bool oldFog;
        AudioSource amb;
        Music music;
        GameObject menuBoat;
        Screen settingsReturn = Screen.Menu;
        bool settingsFromPause;
        string hovered;
        Camera cam;
        float lastEarned;

        public static bool BuyerOpen => I != null && I.buyerOpen;
        bool buyerOpen; float buyerOpenedAt;
        public static void OpenBuyer() { if (I == null) return; I.buyerOpen = true; I.buyerOpenedAt = Time.unscaledTime; Sfx.Play2D("click", 0.5f); }
        public static void CloseBuyer() { if (I != null) I.buyerOpen = false; }

        public static bool InputBlocked => I == null || I.screen != Screen.Playing || I.paused || I.buyerOpen || I.statsOpen
            || (PortGame.I != null && PortGame.I.Phase.Value == PortGame.Ph.Night);

        void Awake()
        {
            I = this;
            showHelp = Settings.ControlsHint;
            // Graphics A/B switches for before/after comparisons (test runs only).
            var args = System.Environment.GetCommandLineArgs();
            bool Arg(string a) => System.Array.IndexOf(args, a) >= 0;
            if (Arg("-pb-oldwater")) { var w = new Material(LitMat); w.SetColor("_BaseColor", new Color(0.13f, 0.3f, 0.36f)); w.SetFloat("_Smoothness", 0.88f); WaterMat = w; }
            oldFog = Arg("-pb-oldfog");
            if (Arg("-pb-noclouds")) CloudMat = null;
            Art.Lit = LitMat; Art.Emissive = EmissiveMat; Art.Particle = ParticleMat; Art.WaterMat = WaterMat; Art.Cloud = CloudMat; Art.Land = LandMat;
            Skin.I = Skin;
            if (Skin.On && !Arg("-pb-ownart"))
            {
                Art.Toon = Skin.Toon;
                // (The sea is our own toon water, PortBoss/Water: the pack's tiled normal map shimmered in the distance.)
            }
            else Skin.I = null; // -pb-ownart: the pre-pack look, for A/B shots
            Application.targetFrameRate = 144;
            World.Build();
            Storm.Create();
            Gangplank.Create(0);
            Gangplank.Create(1);
            Gull.Spawn(7);
            SetupCameraAndLight();
            Session.InitSteam();
            amb = gameObject.AddComponent<AudioSource>();
            amb.clip = Sfx.Clip("waves"); amb.loop = true; amb.volume = 0.35f; amb.spatialBlend = 0;
            if (amb.clip != null) amb.Play();
            music = Music.Create();
        }


        void Start()
        {
            ApplySettings();
            var nm = NetworkManager.Singleton;
            nm.OnServerStarted += () => { if (nm.IsServer) Instantiate(PortGamePrefab).GetComponent<NetworkObject>().Spawn(); };
            nm.OnClientDisconnectCallback += id =>
            {
                if (!nm.IsServer && id == nm.LocalClientId) { BackToMenu(); Session.Status = "Disconnected from host."; }
            };
            AutoRun.Begin(this);
        }

        void OnApplicationQuit() { Session.Leave(); Session.ShutdownSteam(); }

        void SetupCameraAndLight()
        {
            cam = Camera.main;
            if (cam == null) { cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>(); cam.tag = "MainCamera"; }
            cam.farClipPlane = 1500; cam.fieldOfView = 62;
            var data = cam.GetComponent<UniversalAdditionalCameraData>() ?? cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            sun = FindFirstObjectByType<Light>();
            if (sun == null || sun.type != LightType.Directional) sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.85f;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0021f;
            if (oldFog) { RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 70; RenderSettings.fogEndDistance = 650; }
            var sky = RenderSettings.skybox;
            if (sky != null && sky.HasProperty("_SunSize")) { sky.SetFloat("_SunSize", 0.035f); sky.SetFloat("_AtmosphereThickness", 1.15f); sky.SetColor("_SkyTint", new Color(0.55f, 0.6f, 0.7f)); }

            var vol = new GameObject("PostFX").AddComponent<Volume>();
            vol.isGlobal = true;
            if (Skin.On)
            {
                // Toon Harbor's own sky, fog and grading (its demo scene), minus depth of field + chromatic aberration (gameplay).
                RenderSettings.skybox = new Material(Skin.I.Sky);
                DynamicGI.UpdateEnvironment(); // reflections (the toon water) follow the new sky, not the one baked into the scene
                RenderSettings.reflectionIntensity = 0.55f; // less pale sky in the water: the demo's deep blue
                RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 300; RenderSettings.fogEndDistance = 1200;
                var tp = Instantiate(Skin.I.Post);
                if (tp.TryGet(out DepthOfField dof)) dof.active = false;
                if (tp.TryGet(out ChromaticAberration chr)) chr.active = false;
                if (!tp.TryGet(out ColorAdjustments tca)) tca = tp.Add<ColorAdjustments>();
                tca.postExposure.Override(0f); colorAdj = tca; exposureBase = 0f;
                vol.profile = tp;
                ApplyLight(0.15f, false);
                return;
            }
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var tm = prof.Add<Tonemapping>(); tm.mode.Override(TonemappingMode.ACES);
            var bl = prof.Add<Bloom>(); bl.intensity.Override(0.45f); bl.threshold.Override(1.05f);
            var ca = prof.Add<ColorAdjustments>(); ca.saturation.Override(8f); ca.postExposure.Override(0.25f); ca.contrast.Override(8f); colorAdj = ca;
            var vg = prof.Add<Vignette>(); vg.intensity.Override(0.22f);
            var wb = prof.Add<WhiteBalance>(); wb.temperature.Override(12f);
            vol.profile = prof;
            ApplyLight(0.15f, false);
        }

        // u = 0..1 through the day: early afternoon → golden → sunset; night = blue dusk.
        void ApplyLight(float u, bool night)
        {
            if (Skin.On) { ApplyToonLight(u, night); return; }
            // Night: sun below the horizon (dark sky), moonlight comes from the blue ambient.
            float elev = night ? -14f : Mathf.Lerp(42f, 9f, u);
            sun.transform.rotation = Quaternion.Euler(elev, Mathf.Lerp(20f, 35f, u), 0);
            var day = Color.Lerp(new Color(1f, 0.9f, 0.76f), new Color(1f, 0.62f, 0.38f), u * u);
            sun.color = day;
            sun.intensity = night ? 0f : Mathf.Lerp(1.7f, 1.2f, u);
            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure"))
                RenderSettings.skybox.SetFloat("_Exposure", night ? 0.12f : Mathf.Lerp(1.1f, 0.85f, u));
            RenderSettings.ambientSkyColor = night ? new Color(0.22f, 0.28f, 0.45f) : Color.Lerp(new Color(0.62f, 0.68f, 0.78f), new Color(0.6f, 0.5f, 0.5f), u);
            RenderSettings.ambientEquatorColor = night ? new Color(0.14f, 0.16f, 0.24f) : Color.Lerp(new Color(0.62f, 0.56f, 0.48f), new Color(0.7f, 0.5f, 0.38f), u);
            RenderSettings.ambientGroundColor = night ? new Color(0.05f, 0.05f, 0.08f) : new Color(0.28f, 0.24f, 0.2f);
            RenderSettings.fogColor = night ? new Color(0.08f, 0.1f, 0.16f) : Color.Lerp(new Color(0.76f, 0.76f, 0.74f), new Color(0.86f, 0.64f, 0.48f), u);
            // Sky colours for the water reflection, and cloud tint (white → golden → dusk).
            Shader.SetGlobalColor("_PB_SkyZenith", night ? new Color(0.03f, 0.05f, 0.1f) : Color.Lerp(new Color(0.32f, 0.5f, 0.75f), new Color(0.35f, 0.38f, 0.6f), u));
            Shader.SetGlobalColor("_PB_SkyHorizon", night ? new Color(0.1f, 0.12f, 0.2f) : Color.Lerp(new Color(0.82f, 0.82f, 0.8f), new Color(0.98f, 0.66f, 0.42f), u));
            if (Art.Cloud != null) Art.Cloud.SetColor("_BaseColor", night ? new Color(0.16f, 0.18f, 0.26f) : Color.Lerp(new Color(0.98f, 0.96f, 0.93f), new Color(1f, 0.74f, 0.55f), u * u));
        }

        // Toon Harbor demo lighting (white sun, warm trilight, blue linear fog), warming a little towards sunset.
        void ApplyToonLight(float u, bool night)
        {
            float elev = night ? -14f : Mathf.Lerp(48f, 14f, u);
            sun.transform.rotation = Quaternion.Euler(elev, Mathf.Lerp(110f, 125f, u), 0);
            sun.color = Color.Lerp(Color.white, new Color(1f, 0.8f, 0.62f), u * u);
            sun.intensity = night ? 0f : Mathf.Lerp(1.05f, 0.9f, u);
            RenderSettings.ambientSkyColor = night ? new Color(0.22f, 0.28f, 0.45f) : Color.Lerp(new Color(1f, 0.941f, 0.825f), new Color(1f, 0.8f, 0.66f), u * u);
            RenderSettings.ambientEquatorColor = night ? new Color(0.14f, 0.16f, 0.24f) : Color.Lerp(new Color(0.689f, 0.49f, 0.387f), new Color(0.72f, 0.46f, 0.36f), u * u);
            RenderSettings.ambientGroundColor = night ? new Color(0.05f, 0.05f, 0.08f) : new Color(0.292f, 0.276f, 0.244f);
            RenderSettings.fogColor = night ? new Color(0.08f, 0.1f, 0.16f) : Color.Lerp(new Color(0.419f, 0.603f, 0.689f), new Color(0.78f, 0.62f, 0.52f), u * u);
            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure"))
                RenderSettings.skybox.SetFloat("_Exposure", night ? 0.12f : Mathf.Lerp(1.0f, 0.85f, u * u));
            Shader.SetGlobalColor("_PB_SkyZenith", night ? new Color(0.03f, 0.05f, 0.1f) : new Color(0.3f, 0.55f, 0.85f));
            Shader.SetGlobalColor("_PB_SkyHorizon", night ? new Color(0.1f, 0.12f, 0.2f) : RenderSettings.fogColor);
            if (Art.Cloud != null) Art.Cloud.SetColor("_BaseColor", night ? new Color(0.16f, 0.18f, 0.26f) : Color.Lerp(Color.white, new Color(1f, 0.8f, 0.65f), u * u));
        }

        void Update()
        {
            Session.Tick();
            // Test bot: sell everything after a moment so captures show the panel (here, not in OnGUI: batchmode has no GUI).
            if (buyerOpen && AutoRun.Active && Time.unscaledTime - buyerOpenedAt > 1.5f) { buyerOpen = false; Player.Local?.RequestSellAll(); }
            var g = PortGame.I;
            if (g != null) ApplyLight(Mathf.Clamp01(g.DayT.Value / PortGame.DayLength), g.Phase.Value == PortGame.Ph.Night);
            else ApplyLight(0.8f, false); // the title scene sits in golden hour
            ApplyStormLight();
            UpdatePunch();
            NightTick();
            amb.volume = 0.35f * (1f + Storm.Level * 1.3f) * Settings.Ambience; // the sea gets louder as it gets rougher
            SettingsKeyCapture();
            music.Play(MusicSlot(g));

            var k = Keyboard.current;
            if (screen == Screen.Playing && k != null)
            {
                var grabKey = Settings.KeyFor("grab");
                if (statsOpen && k.escapeKey.wasPressedThisFrame) statsOpen = false;
                else if (buyerOpen && (k.escapeKey.wasPressedThisFrame || (grabKey != Key.None && k[grabKey].wasPressedThisFrame))) CloseBuyer();
                else if (k.escapeKey.wasPressedThisFrame || Gamepad.current?.startButton.wasPressedThisFrame == true) SetPaused(!paused);
                if (k.hKey.wasPressedThisFrame) { showHelp = !showHelp; Settings.ControlsHint = showHelp; }
                // Debug (host): F9 cycles the weather: random → calm → rain → storm → random.
                if (k.f9Key.wasPressedThisFrame && NetworkManager.Singleton.IsServer && g != null)
                {
                    float f = g.ForcedSeverity, next = f < 0 ? 0f : f < 0.25f ? 0.5f : f < 0.75f ? 1f : -1f;
                    g.ForceWeather(next);
                    Shout("", next < 0 ? "[debug] weather: random" : $"[debug] weather forced to {next:0.0}", 2f);
                }
            }
            var nm = NetworkManager.Singleton;
            bool live = nm.IsListening && !nm.ShutdownInProgress;
            if (screen != Screen.Playing && live) screen = Screen.Playing;
            else if (screen == Screen.Playing && !live) BackToMenu();

            bool wantCursor = InputBlocked;
            Cursor.lockState = wantCursor || AutoRun.Active ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = wantCursor;

            if (Player.Local == null)
            {
                if (menuBoat == null && !nm.IsListening) menuBoat = MenuBoat();
                TitleScene(nm);
            }
            if (menuWorker != null && nm.IsListening) Destroy(menuWorker);
            if (menuBoat != null && nm.IsListening) Destroy(menuBoat);
        }

        // Darken and grey the scene with the storm; lightning flashes on top.
        void ApplyStormLight()
        {
            float L = Storm.Level;
            sun.intensity = sun.intensity * (1 - 0.65f * L) + Storm.Flash * 2.5f;
            sun.color = Color.Lerp(sun.color, new Color(0.75f, 0.8f, 0.9f), L);
            RenderSettings.ambientSkyColor *= 1 - 0.35f * L;
            RenderSettings.ambientEquatorColor *= 1 - 0.35f * L;
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, new Color(0.42f, 0.45f, 0.5f), L);
            if (!oldFog) RenderSettings.fogDensity = 0.0021f * fogScale * (1 + 2.5f * L);
            if (Art.Cloud != null) Art.Cloud.SetColor("_BaseColor", Color.Lerp(Art.Cloud.GetColor("_BaseColor"), new Color(0.45f, 0.47f, 0.52f), L));
            Shader.SetGlobalColor("_PB_SkyHorizon", Color.Lerp(Shader.GetGlobalColor("_PB_SkyHorizon"), new Color(0.45f, 0.48f, 0.52f), L));
            Shader.SetGlobalColor("_PB_SkyZenith", Color.Lerp(Shader.GetGlobalColor("_PB_SkyZenith"), new Color(0.25f, 0.28f, 0.33f), L));
            // The pack's procedural sky greys over and dims with the weather.
            var sk = RenderSettings.skybox;
            if (sk != null && sk.HasProperty("_SkyTint") && Skin.On)
            {
                sk.SetColor("_SkyTint", Color.Lerp(Skin.I.Sky.GetColor("_SkyTint"), new Color(0.32f, 0.34f, 0.37f), L));
                sk.SetFloat("_Exposure", sk.GetFloat("_Exposure") * (1f - 0.45f * L));
            }
        }

        string MusicSlot(PortGame g)
        {
            if (screen != Screen.Playing || g == null) return "menu";
            if (Storm.Level > 0.75f) return "storm";
            // Evening (from sunset) and night share the night track.
            if (g.Phase.Value == PortGame.Ph.Night || g.DayT.Value / PortGame.DayLength > 0.72f) return "night";
            foreach (var b in Boat.All)
                if (b.State.Value == Boat.St.Docked && b.SecondsLeft.Value >= 0 && b.SecondsLeft.Value <= 30) return "rush";
            return "day";
        }

        // Decorative boat for the title scene (visuals only).
        GameObject MenuBoat()
        {
            var b = Instantiate(BoatPrefab);
            // Strip networking, or NGO would treat it as an in-scene object and spawn it when a session starts.
            DestroyImmediate(b.GetComponent<Boat>());
            DestroyImmediate(b.GetComponent<Unity.Netcode.Components.NetworkTransform>());
            DestroyImmediate(b.GetComponent<NetworkObject>());
            b.AddComponent<Bob>();
            return b;
        }

        class Bob : MonoBehaviour
        {
            void Update()
            {
                float t = Time.time;
                var p = Boat.DockPos;
                transform.SetPositionAndRotation(new Vector3(p.x, Water.Height(p.x, p.z, t) + 0.1f, p.z),
                    Quaternion.LookRotation(Vector3.back) * Quaternion.Euler(Mathf.Sin(t * 0.63f) * 1.5f, 0, Mathf.Sin(t * 0.9f) * 3f));
            }
        }

        void SetPaused(bool p)
        {
            paused = p;
            if (Session.Solo) Time.timeScale = p ? 0 : 1;
        }

        void BackToMenu()
        {
            SetPaused(false);
            Session.Leave();
            screen = Screen.Menu;
        }

        public void Play() => Session.StartSolo();
        public void Host(bool lan) => _ = Session.HostCoop(lan);
        public void JoinIp(string ip) => Session.JoinLan(ip);

        // ---------- static feeds from gameplay ----------
        public static void Shout(string who, string s, float secs) { shoutWho = who; shout = s; shoutUntil = Time.unscaledTime + secs; }
        public static void Toast(string s) { toast = s; toastUntil = Time.unscaledTime + 2.5f; }

        // A beat on a big splash: the camera punches in for ~1.4 s; solo play also slows time a moment (in co-op
        // that would slow everyone, so it's camera-only). Three splashes inside 2 s = a wipeout, the bigger beat.
        static float punchT = -1, punchStrength, lastPunch = -99, splashWindow, savedTimeScale = 1;
        static int splashCount;
        public static void Punch(Vector3 p, float strength)
        {
            if (I == null || I.cam == null || (p - I.cam.transform.position).sqrMagnitude > 35f * 35f) return;
            if (Time.unscaledTime - splashWindow > 2f) { splashWindow = Time.unscaledTime; splashCount = 0; }
            if (++splashCount >= 3) strength = 1f;
            if (punchT >= 0) { punchStrength = Mathf.Max(punchStrength, strength); return; }
            if (Time.unscaledTime - lastPunch < 5f && strength < 1f) return;
            lastPunch = Time.unscaledTime; punchT = 0; punchStrength = strength;
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsServer && nm.ConnectedClientsIds.Count <= 1 && !I.paused) { savedTimeScale = Time.timeScale; Time.timeScale = savedTimeScale * Mathf.Lerp(0.6f, 0.3f, strength); }
        }

        void UpdatePunch()
        {
            if (punchT < 0 || cam == null) return;
            punchT += Time.unscaledDeltaTime / 1.4f;
            cam.fieldOfView = Settings.Fov - 10f * punchStrength * Mathf.Sin(Mathf.Clamp01(punchT) * Mathf.PI);
            if (punchT >= 1) { punchT = -1; cam.fieldOfView = Settings.Fov; if (Time.timeScale < savedTimeScale) Time.timeScale = savedTimeScale; }
        }
        public static void Popup(Vector3 p, string s, Color c) => pops.Add(new Pop { p = p, s = s, c = c, t0 = Time.unscaledTime });

        // ---------- drawing ----------
        GUIStyle big, mid, small, btn, panel, shoutS, title, menuItem;
        Texture2D fadeTex;

        // Left-to-right dark fade behind the menu column.
        static Texture2D Gradient()
        {
            var t = new Texture2D(256, 1) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < 256; x++) t.SetPixel(x, 0, new Color(0.03f, 0.05f, 0.08f, 0.78f * Mathf.Pow(1 - x / 255f, 1.4f)));
            t.Apply();
            return t;
        }
        Texture2D panelTex;
        float S => UnityEngine.Screen.height / 1080f;

        Texture2D Tex(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }

        void Styles()
        {
            if (big != null && Mathf.Approximately(big.fontSize, Mathf.RoundToInt(64 * S))) return;
            panelTex ??= Tex(new Color(0.08f, 0.1f, 0.12f, 0.82f));
            big = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(64 * S), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            mid = new GUIStyle(big) { fontSize = Mathf.RoundToInt(30 * S) };
            small = new GUIStyle(big) { fontSize = Mathf.RoundToInt(20 * S), fontStyle = FontStyle.Normal, alignment = TextAnchor.UpperLeft };
            title = new GUIStyle(big) { fontSize = Mathf.RoundToInt(132 * S), fontStyle = FontStyle.Normal, font = TitleFont, alignment = TextAnchor.MiddleLeft, wordWrap = false };
            menuItem = new GUIStyle(big) { fontSize = Mathf.RoundToInt(40 * S), fontStyle = FontStyle.Normal, font = TitleFont, alignment = TextAnchor.MiddleLeft };
            if (TitleFont != null) { big.font = TitleFont; big.fontStyle = FontStyle.Normal; mid.font = TitleFont; mid.fontStyle = FontStyle.Normal; }
            fadeTex ??= Gradient();
            shoutS = new GUIStyle(big) { fontSize = Mathf.RoundToInt(38 * S * Settings.ShoutSize) };
            panel = new GUIStyle(GUI.skin.box) { normal = { background = panelTex } };
            ThemeTextures();
            btn = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(28 * S), fontStyle = FontStyle.Normal, font = TitleFont, border = new RectOffset(10, 10, 10, 10) };
            btn.normal.background = plateTex; btn.hover.background = plateHot; btn.active.background = plateHot;
            btn.normal.textColor = btn.hover.textColor = btn.active.textColor = new Color(0.1f, 0.07f, 0.04f);
        }

        void Label(Rect r, string s, GUIStyle st, Color c)
        {
            var old = st.normal.textColor;
            st.normal.textColor = new Color(0, 0, 0, 0.75f * c.a);
            GUI.Label(new Rect(r.x + 2 * S, r.y + 3 * S, r.width, r.height), s, st);
            st.normal.textColor = c;
            GUI.Label(r, s, st);
            st.normal.textColor = old;
        }

        // A copy of st that keeps s on one line (never wraps or clips), shrunk until it fits maxW.
        static GUIStyle OneLine(GUIStyle st, string s, float maxW)
        {
            var o = new GUIStyle(st) { wordWrap = false, clipping = TextClipping.Overflow };
            var c = new GUIContent(s);
            while (o.fontSize > 10 && o.CalcSize(c).x > maxW) o.fontSize = Mathf.FloorToInt(o.fontSize * 0.92f);
            return o;
        }

        bool Button(Rect r, string s, bool enabled = true)
        {
            GUI.enabled = enabled;
            bool b = GUI.Button(r, s, btn);
            GUI.enabled = true;
            Hover(r, s, enabled);
            if (b) Sfx.Play2D("blip", 0.6f);
            return b;
        }

        void Hover(Rect r, string id, bool enabled)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (enabled && r.Contains(Event.current.mousePosition)) { if (hovered != id) { hovered = id; Sfx.Play2D("clank", 0.35f); } }
            else if (hovered == id) hovered = null;
        }

        void OnGUI()
        {
            Styles();
            float W = UnityEngine.Screen.width, H = UnityEngine.Screen.height;
            if (screen == Screen.Menu) DrawMenu(W, H);
            else if (screen == Screen.Join) DrawJoin(W, H);
            else if (screen == Screen.Settings) DrawSettings(W, H);
            else
            {
                DrawHud(W, H);
                if (PortGame.I != null && PortGame.I.Phase.Value == PortGame.Ph.Night) { if (!statsOpen) DrawNight(W, H); }
                else if (buyerOpen) DrawBuyer(W, H);
                if (paused) { if (settingsFromPause) DrawSettings(W, H); else if (!statsOpen) DrawPause(W, H); }
                if (statsOpen) DrawStats(W, H);
            }
            if (!string.IsNullOrEmpty(Session.Status) && screen == Screen.Join) // the menu has its own Steam note
                Label(new Rect(0, H - 80 * S, W, 50 * S), Session.Status, mid, new Color(1f, 0.8f, 0.6f));
        }

        async void Refresh()
        {
            if (searching) return;
            searching = true;
            lobbies = await Session.FindLobbies();
            searching = false;
        }

        void DrawJoin(float W, float H)
        {
            float pw = 760 * S, ph = 620 * S;
            var r = new Rect((W - pw) / 2, (H - ph) / 2, pw, ph);
            SteelPanel(r);
            Label(new Rect(r.x, r.y + 30 * S, pw, 60 * S), "Join a port", mid, new Color(1f, 0.85f, 0.55f));
            float y = r.y + 100 * S;
            if (!Session.SteamOk) Label(new Rect(r.x + 30 * S, y, pw, 40 * S), "Steam isn't running.", small, Color.white);
            else if (searching) Label(new Rect(r.x + 30 * S, y, pw, 40 * S), "Looking for Steam lobbies…", small, Color.white);
            else if (lobbies.Length == 0) Label(new Rect(r.x + 30 * S, y, pw - 60 * S, 60 * S), "No open ports found. Ask your friend to Host co-op, or accept their Steam invite.", small, Color.white);
            foreach (var l in lobbies)
            {
                if (y > r.y + 380 * S) break;
                Label(new Rect(r.x + 30 * S, y + 8 * S, pw, 40 * S), $"{l.GetData("name")}  ({l.MemberCount}/4)", small, Color.white);
                if (Button(new Rect(r.xMax - 190 * S, y, 160 * S, 48 * S), "Join")) { var lob = l; _ = Session.JoinLobby(lob); }
                y += 60 * S;
            }
            float by = r.yMax - 190 * S;
            Label(new Rect(r.x + 30 * S, by + 10 * S, 200 * S, 40 * S), "LAN / IP:", small, Color.white);
            lanIp = GUI.TextField(new Rect(r.x + 160 * S, by + 4 * S, 300 * S, 44 * S), lanIp, new GUIStyle(GUI.skin.textField) { fontSize = Mathf.RoundToInt(24 * S) });
            if (Button(new Rect(r.xMax - 190 * S, by, 160 * S, 48 * S), "Connect")) JoinIp(lanIp.Trim());
            if (Button(new Rect(r.x + 30 * S, r.yMax - 90 * S, 220 * S, 56 * S), "Refresh")) Refresh();
            if (Button(new Rect(r.xMax - 250 * S, r.yMax - 90 * S, 220 * S, 56 * S), "Back")) screen = Screen.Menu;
        }

        void DrawHud(float W, float H)
        {
            var g = PortGame.I;
            if (g != null && Settings.Hud)
            {
                Pill(new Rect(W - 330 * S, 12 * S, 318 * S, 150 * S));
                Label(new Rect(W - 420 * S, 18 * S, 400 * S, 80 * S), $"${g.Money.Value}", new GUIStyle(big) { alignment = TextAnchor.UpperRight }, new Color(1f, 0.88f, 0.4f));
                // One shared tin for the whole crew (everyone earns the same), and the goalpost it's saving toward.
                var tiny = new GUIStyle(small) { alignment = TextAnchor.UpperRight, fontSize = Mathf.RoundToInt(18 * S) };
                Label(new Rect(W - 420 * S, 92 * S, 400 * S, 26 * S), Player.All.Count > 1 ? $"crew tin · shared by all {Player.All.Count}" : "crew tin", tiny, new Color(1f, 0.92f, 0.7f, 0.8f));
                float goal = Mathf.Clamp01(g.Money.Value / (float)g.NextStageCost);
                var bar = new Rect(W - 300 * S, 122 * S, 280 * S, 12 * S);
                GUI.DrawTexture(bar, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0, 0, 0, 0.45f), 0, 4 * S);
                GUI.DrawTexture(new Rect(bar.x, bar.y, Mathf.Max(8 * S, bar.width * goal), bar.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.98f, 0.79f, 0.38f, 0.95f), 0, 4 * S);
                Label(new Rect(W - 420 * S, 136 * S, 400 * S, 26 * S), $"next: {g.NextStageName}  ${g.Money.Value} / ${g.NextStageCost}", tiny, new Color(1f, 0.92f, 0.7f, 0.85f));
                float u = g.DayT.Value / PortGame.DayLength;
                string tod = g.Phase.Value == PortGame.Ph.Night ? "night" : u < 0.33f ? "afternoon" : u < 0.7f ? "late afternoon" : "sunset";
                Label(new Rect(24 * S, 18 * S, 600 * S, 50 * S), $"DAY {g.DayNum.Value} · {tod}", new GUIStyle(mid) { alignment = TextAnchor.UpperLeft }, new Color(1f, 0.95f, 0.85f));
                // Between boats (after the day-1 JOB line is gone): when the next one comes.
                bool firstDayJob = g.DayNum.Value == 1 && g.Sold.Value < 10;
                if (!firstDayJob && g.Phase.Value == PortGame.Ph.Day && g.NextBoatIn > 0 && !Boat.All.Exists(b => b.State.Value != Boat.St.Leaving))
                {
                    Pill(new Rect(18 * S, 64 * S, 200 * S, 30 * S));
                    Label(new Rect(28 * S, 68 * S, 600 * S, 30 * S), $"next boat in {(int)g.NextBoatIn / 60}:{(int)g.NextBoatIn % 60:00}", new GUIStyle(small) { fontSize = Mathf.RoundToInt(20 * S), alignment = TextAnchor.UpperLeft }, new Color(1f, 0.92f, 0.7f));
                }
            }
            else Label(new Rect(0, H * 0.45f, W, 60 * S), "Connecting…", mid, Color.white);

            if (Time.unscaledTime < shoutUntil && g?.Phase.Value != PortGame.Ph.Night) // the night card has the floor
            {
                float a = Mathf.Clamp01((shoutUntil - Time.unscaledTime) * 2);
                var band = new Rect(W * 0.14f, H * 0.11f, W * 0.72f, 150 * S);
                var old = GUI.color; GUI.color = new Color(1, 1, 1, a * 0.85f);
                GUI.Box(band, "", panel);
                GUI.color = old;
                if (!string.IsNullOrEmpty(shoutWho))
                    Label(new Rect(band.x, band.y + 8 * S, band.width, 34 * S), shoutWho, new GUIStyle(small) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(22 * S) },
                        shoutWho == "BUYER" ? new Color(0.6f, 0.9f, 0.6f, a) : new Color(1f, 0.75f, 0.35f, a));
                Label(new Rect(band.x + 20 * S, band.y + 36 * S, band.width - 40 * S, band.height - 40 * S), shout, shoutS, new Color(1f, 0.97f, 0.9f, a));
            }
            if (Time.unscaledTime < toastUntil)
                Label(new Rect(0, H * 0.62f, W, 50 * S), toast, OneLine(mid, toast, W * 0.94f), new Color(0.85f, 0.95f, 1f, Mathf.Clamp01((toastUntil - Time.unscaledTime) * 2)));

            for (int i = pops.Count - 1; i >= 0; i--)
            {
                var p = pops[i];
                float t = Time.unscaledTime - p.t0;
                if (t > 1.6f || cam == null) { pops.RemoveAt(i); continue; }
                var sp = cam.WorldToScreenPoint(p.p + Vector3.up * t * 0.9f);
                if (sp.z < 0) continue;
                var c = p.c; c.a = Mathf.Clamp01((1.6f - t) * 2);
                var ps = OneLine(new GUIStyle(mid) { fontSize = Mathf.RoundToInt(40 * S) }, p.s, W * 0.9f);
                float pw = ps.CalcSize(new GUIContent(p.s)).x + 20 * S, px = Mathf.Clamp(sp.x - pw / 2, 10 * S, W - pw - 10 * S); // whole line on screen
                Label(new Rect(px, H - sp.y - 30 * S, pw, 60 * S), p.s, ps, c);
            }

            bool nightCard = PortGame.I != null && PortGame.I.Phase.Value == PortGame.Ph.Night;
            if (!nightCard && !Skin.On) foreach (var (lp, lt) in World.Labels) // with the skin the stall has a painted sign
            {
                var sp = cam.WorldToScreenPoint(lp);
                float d = Vector3.Distance(cam.transform.position, lp);
                if (sp.z < 0 || d > 45f || Physics.Linecast(cam.transform.position, lp, ~0, QueryTriggerInteraction.Ignore)) continue;
                Label(new Rect(sp.x - 200 * S, H - sp.y - 20 * S, 400 * S, 40 * S), lt, mid, new Color(1f, 0.93f, 0.75f, Mathf.Clamp01((45f - d) / 10f)));
            }

            var pl = Player.Local;
            if (pl != null && !nightCard && Settings.Hud) DrawCrateRings(H, pl);
            if (pl != null && g != null && !nightCard && Settings.Hud) DrawObjective(W, H, g, pl);
            if (pl != null && !InputBlocked)
            {
                string hint = null;
                var fk = Forklift.I;
                if (pl.Driving != null) hint = "WASD drive · Shift raise forks · Q lower · E get out";
                else if (fk != null && fk.Free && pl.Carrying == 0 && Vector3.Distance(fk.Seat, pl.transform.position + Vector3.up) < 2.4f) hint = "E  drive the forklift";
                else if (pl.Swimming) hint = pl.NearLadder() != null ? "E  climb out" : pl.Towing ? "Q  let go of the crate" : pl.FloatingCrateNear() != null ? "E  tow this crate" : "Swim to a ladder or the slipway";
                else if (pl.NearBuyer && pl.SellableCrates().Count > 0) hint = "E  sell to the buyer";
                else if (pl.Carrying > 0 && pl.PutTarget(out int load, out int cap) is var spot && spot != Player.PutSpot.Ground)
                    hint = $"Q  {(spot == Player.PutSpot.Rack ? "store on the rack" : spot == Player.PutSpot.Trolley ? "put in the trolley" : "put on the pallet")} ({load}/{cap})   ·   hold Q: all";
                else if (PortGame.I != null && Storm.Gust > 0.15f && pl.Carrying == 0 && pl.FindCrate() is Crate tc && !tc.Attached.Value) hint = tc.Tied.Value ? "F  untie crate" : "F  tie this crate down";
                else if (pl.Pushing) hint = "F  let go of trolley";
                else if (pl.Carrying == 0 && pl.ShoveTarget() != null) hint = "F  shove";
                else if (pl.Carrying < Player.MaxStack && pl.FindCrate() is Crate gc)
                {
                    var from = gc.Stored ? " from the rack" : gc.transform.parent != null && gc.transform.parent.GetComponent<Trolley>() ? " from the trolley" : gc.transform.parent != null && gc.transform.parent.GetComponent<Pallet>() ? " off the pallet" : "";
                    hint = (pl.Carrying == 0 ? "E  pick up" : "E  stack another") + from + "   ·   hold E: more";
                    // A little marker over the crate E will take.
                    var sp = cam != null ? cam.WorldToScreenPoint(gc.transform.position + Vector3.up * (0.62f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f))) : Vector3.back;
                    if (sp.z > 0) Label(new Rect(sp.x - 20 * S, H - sp.y - 20 * S, 40 * S, 40 * S), "▼", new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(26 * S), alignment = TextAnchor.MiddleCenter }, new Color(1f, 0.92f, 0.6f, 0.9f));
                }
                else if (pl.Carrying == 0 && Trolley.I != null && Vector3.Distance(Trolley.I.Handle, pl.transform.position + Vector3.up) < 2.2f) hint = "F  push trolley";
                if (pl.Carrying >= 2 && pl.Teeter > Player.TeeterAt)
                {
                    float a = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 18f);
                    Label(new Rect(0, H * 0.70f, W, 70 * S), "Steady...!", new GUIStyle(big) { fontSize = Mathf.RoundToInt(56 * S) }, new Color(1f, 0.55f, 0.3f, a));
                }
                if (pl.Carrying > 0) { var cl = $"Carrying {pl.Carrying}/{Player.MaxStack}   ·   Q put down   ·   Left click throw"; Label(new Rect(0, H - 150 * S, W, 40 * S), cl, OneLine(mid, cl, W * 0.94f), new Color(1, 1, 1, 0.85f)); }
                if (pl.Swimming || pl.Stamina < 0.999f)
                {
                    // Stamina: drains while swimming, refills on land. Low = slow; empty = fished out.
                    float st = pl.Stamina, bw = 320 * S;
                    var br = new Rect((W - bw) / 2, H - 196 * S, bw, 16 * S);
                    Pill(new Rect(br.x - 112 * S, br.y - 10 * S, bw + 124 * S, 36 * S));
                    Label(new Rect(br.x - 106 * S, br.y - 9 * S, 100 * S, 34 * S), "STAMINA", new GUIStyle(small) { fontSize = Mathf.RoundToInt(17 * S), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft }, new Color(0.8f, 0.92f, 1f));
                    GUI.DrawTexture(br, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0, 0, 0, 0.45f), 0, 5 * S);
                    var col = st > 0.5f ? new Color(0.45f, 0.8f, 1f) : st > 0.25f ? new Color(1f, 0.8f, 0.3f) : new Color(1f, 0.35f, 0.3f, 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 14f));
                    GUI.DrawTexture(new Rect(br.x, br.y, Mathf.Max(6 * S, br.width * st), br.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, col, 0, 5 * S);
                }
                if (hint != null)
                {
                    var hs = OneLine(mid, hint, W * 0.9f);
                    float hw = hs.CalcSize(new GUIContent(hint)).x + 40 * S;
                    Pill(new Rect((W - hw) / 2, H * 0.56f - 2 * S, hw, 44 * S));
                    Label(new Rect(0, H * 0.56f, W, 40 * S), hint, hs, new Color(1f, 0.92f, 0.7f));
                }
            }
            if (showHelp && !nightCard)
                Label(new Rect(24 * S, H - 190 * S, 640 * S, 180 * S),
                    "WASD move · Shift sprint · Space jump · Mouse look\nE / Right click: pick up, stack (max 5; hold for more) · E at the buyer: sell\nQ: put down (on the rack / in the trolley when next to it; hold for all)\nLeft click: throw · F: trolley / shove a mate · H hide · Esc menu", small, new Color(1, 1, 1, 0.75f));
        }

        void Pill(Rect r) => GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.03f, 0.05f, 0.08f, 0.5f), 0, 10 * S);

        // Freshness rings over the crates near you: the ring drains as the fish goes off and empties as the crate drops
        // a grade (green fresh $15 → amber okay $12 → red spoiled $4), so a nearly empty ring means sell it now. The buyer's
        // price is in the middle; red pips under it = knocks (-25% each). Carried crates show theirs beside the stack.
        static Texture2D[] ringTex;
        const int RingSteps = 48;
        static Texture2D RingTex(int step)
        {
            ringTex ??= new Texture2D[RingSteps + 1];
            if (ringTex[step] != null) return ringTex[step];
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float fill = step / (float)RingSteps;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - n / 2f, dy = y + 0.5f - n / 2f, d = Mathf.Sqrt(dx * dx + dy * dy) / (n / 2f);
                    float band = Mathf.Clamp01(1f - Mathf.Abs(d - 0.8f) / 0.16f * 1.0f) * 3f; // soft-edged ring
                    float ang = Mathf.Repeat(Mathf.Atan2(dx, dy) / (2 * Mathf.PI), 1f);    // 0 at 12 o'clock, clockwise
                    float a = ang <= fill ? Mathf.Clamp01(band) : 0f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply();
            return ringTex[step] = t;
        }

        void DrawCrateRings(float H, Player pl)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var price = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            foreach (var c in Crate.All)
            {
                if (c == null || !c.IsSpawned) continue;
                bool mine = c.transform.parent == pl.transform;
                var at = c.transform.position + (mine ? cam.transform.right * 0.5f : Vector3.up * 0.5f);
                float d = Vector3.Distance(cam.transform.position, at), near = mine ? 99f : 11f;
                if (!mine && Vector3.Distance(pl.transform.position, c.transform.position) > near) continue;
                var sp = cam.WorldToScreenPoint(at);
                if (sp.z < 0.3f) continue;
                float f = c.Fresh.Value, k, alpha = mine ? 1f : Mathf.Clamp01((near - Vector3.Distance(pl.transform.position, c.transform.position)) / 3f);
                Color col;
                if (f >= Crate.FreshAt) { k = (f - Crate.FreshAt) / (1f - Crate.FreshAt); col = new Color(0.45f, 0.95f, 0.45f); }
                else if (f >= Crate.Spoiled) { k = (f - Crate.Spoiled) / (Crate.FreshAt - Crate.Spoiled); col = new Color(1f, 0.75f, 0.25f); }
                else { k = 1f; col = new Color(1f, 0.32f, 0.28f); }
                // About to drop a grade: the ring blinks.
                if (f >= Crate.Spoiled && k < 0.15f) alpha *= 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 10f);
                float size = Mathf.Clamp(46f * S * 6f / Mathf.Max(d, 1f), 26f * S, 50f * S);
                var r = new Rect(sp.x - size / 2, H - sp.y - size / 2, size, size);
                GUI.DrawTexture(new Rect(r.x - 2 * S, r.y - 2 * S, r.width + 4 * S, r.height + 4 * S), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.02f, 0.04f, 0.06f, 0.55f * alpha), 0, size);
                GUI.DrawTexture(r, RingTex(RingSteps), ScaleMode.StretchToFill, true, 0, new Color(1, 1, 1, 0.16f * alpha), 0, 0);
                GUI.DrawTexture(r, RingTex(Mathf.Clamp(Mathf.CeilToInt(k * RingSteps), 0, RingSteps)), ScaleMode.StretchToFill, true, 0, new Color(col.r, col.g, col.b, alpha), 0, 0);
                price.fontSize = Mathf.RoundToInt(size * 0.34f);
                Label(r, "$" + PortGame.I?.PriceFor(c), price, new Color(1, 1, 1, alpha));
                for (int i = 0; i < c.Damage.Value; i++)
                    GUI.DrawTexture(new Rect(sp.x - (c.Damage.Value * 9f * S) / 2 + i * 9f * S, r.yMax + 2 * S, 6 * S, 6 * S), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(1f, 0.3f, 0.25f, alpha), 0, 3 * S);
            }
        }

        // First day only: one line saying what to do next, and a bobbing marker over where to do it.
        void DrawObjective(float W, float H, PortGame g, Player pl)
        {
            if (g.DayNum.Value != 1 || g.Sold.Value >= 10) return;
            Boat boat = null; Crate near = null; float best = 1e9f;
            foreach (var b in Boat.All) if (b.State.Value != Boat.St.Leaving) boat = b;
            if (boat != null && boat.State.Value == Boat.St.Docked)
                foreach (var c in Crate.All)
                {
                    if (c == null || c.Attached.Value) continue;
                    float d = (c.transform.position - boat.transform.position).sqrMagnitude;
                    if (d < 100f && d < best) { best = d; near = c; }
                }
            string job; Vector3? at = null;
            if (pl.Carrying > 0 && near != null && boat != null) { job = pl.Carrying < 3 ? "Stack a few more (E), then put them on the CRATE STORE rack (Q)" : "Quick, before she sails: put them on the CRATE STORE rack (Q)"; at = Storage.Center + Vector3.up * 4.2f; }
            else if (pl.Carrying > 0) { job = "Take the fish to the FISH MARKET: out the back gate, across the road (mind the lorries)"; at = World.BuyerPos + Vector3.up * 3.6f; }
            else if (near != null) { job = "Unload the boat: walk up to a fish crate and press E"; at = near.transform.position + Vector3.up * 1.6f; }
            else if (Storage.Count > 0) { job = "Fetch fish from the rack (E) and sell it at the FISH MARKET"; at = Storage.Center + Vector3.up * 4.2f; }
            else if (boat != null && boat.State.Value == Boat.St.Arriving) { job = "A boat is coming in: meet it at the end of the jetty"; at = boat.BerthPos + Vector3.up * 2.5f; }
            else job = g.NextBoatIn > 0 ? $"Next boat in {(int)g.NextBoatIn / 60}:{(int)g.NextBoatIn % 60:00}: wait at the jetty" : "That was the last boat today";
            ThemeTextures();
            var r = new Rect(24 * S, 74 * S, 640 * S, 34 * S);
            var jt = OneLine(new GUIStyle(small) { fontSize = Mathf.RoundToInt(22 * S), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft }, job, W - r.x - 72 * S - 30 * S);
            float tw = jt.CalcSize(new GUIContent(job)).x;
            Pill(new Rect(r.x, r.y, 84 * S + tw, r.height));
            GUI.DrawTexture(new Rect(r.x, r.y, 60 * S, r.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.98f, 0.76f, 0.1f), 0, 6 * S);
            var js = new GUIStyle(small) { fontSize = Mathf.RoundToInt(18 * S), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            js.normal.textColor = new Color(0.1f, 0.1f, 0.11f);
            GUI.Label(new Rect(r.x, r.y, 60 * S, r.height), "JOB", js);
            Label(new Rect(r.x + 72 * S, r.y, tw + 10 * S, r.height), job, jt, new Color(1f, 0.95f, 0.82f));
            if (at == null || cam == null) return;
            var sp = cam.WorldToScreenPoint(at.Value + Vector3.up * (0.25f * Mathf.Sin(Time.unscaledTime * 4f)));
            if (sp.z < 0) return;
            float x = Mathf.Clamp(sp.x, 40 * S, W - 40 * S), y = Mathf.Clamp(H - sp.y, 60 * S, H - 120 * S);
            var arrow = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(44 * S), alignment = TextAnchor.MiddleCenter };
            Label(new Rect(x - 40 * S, y - 40 * S, 80 * S, 80 * S), "▼", arrow, new Color(1f, 0.8f, 0.2f, 0.95f));
        }

        void DrawBuyer(float W, float H)
        {
            var pl = Player.Local;
            var list = pl != null ? pl.SellableCrates() : new List<Crate>();
            if (pl == null || !pl.NearBuyer || list.Count == 0) { buyerOpen = false; return; }

            float pw = 640 * S, rowH = 52 * S, ph = 210 * S + rowH * list.Count;
            var r = new Rect(W - pw - 40 * S, (H - ph) / 2, pw, ph);
            SteelPanel(r);
            Label(new Rect(r.x, r.y + 24 * S, pw, 50 * S), "THE BUYER", mid, new Color(1f, 0.85f, 0.55f));
            Label(new Rect(r.x, r.y + 56 * S, pw, 30 * S), BuyerLine(list), new GUIStyle(small) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Italic }, new Color(1, 1, 1, 0.85f));
            var st = new GUIStyle(mid) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Normal, fontSize = Mathf.RoundToInt(24 * S) };
            float y = r.y + 100 * S;
            int total = 0;
            foreach (var c in list)
            {
                int v = PortGame.I != null ? PortGame.I.PriceFor(c) : c.Value; total += v;
                var col = c.Grade == "fresh" ? new Color(0.7f, 1f, 0.75f) : c.Grade == "okay" ? Color.white : new Color(0.8f, 0.85f, 0.45f);
                string where = c.transform.parent == pl.transform ? "" : "  (trolley)";
                Label(new Rect(r.x + 26 * S, y, pw, rowH), $"Fish crate · {c.Grade} {Mathf.RoundToInt(c.Fresh.Value * 100)}%{(c.Damage.Value > 0 ? " · damaged" : "")}{where}", st, col);
                Label(new Rect(r.xMax - 260 * S, y, 100 * S, rowH), $"${v}", new GUIStyle(st) { alignment = TextAnchor.MiddleRight }, new Color(1f, 0.88f, 0.4f));
                if (Button(new Rect(r.xMax - 140 * S, y + 6 * S, 110 * S, rowH - 12 * S), "Sell")) pl.RequestSell(c);
                y += rowH;
            }
            y += 14 * S;
            if (Button(new Rect(r.x + 26 * S, y, 300 * S, 58 * S), $"Sell all  ${total}")) pl.RequestSellAll();
            if (Button(new Rect(r.xMax - 210 * S, y, 180 * S, 58 * S), "Close")) buyerOpen = false;
            Label(new Rect(r.x, r.yMax - 34 * S, pw, 30 * S), "E / Esc to close", new GUIStyle(small) { alignment = TextAnchor.UpperCenter }, new Color(1, 1, 1, 0.5f));
        }

        static string BuyerLine(List<Crate> list)
        {
            int spoiled = list.FindAll(c => c.Grade == "spoiled").Count, fresh = list.FindAll(c => c.Grade == "fresh").Count;
            if (spoiled == list.Count) return "\"That smell... I'll give you scrap money.\"";
            if (spoiled > 0) return "\"Some of this has turned. I'm not paying full for those.\"";
            if (fresh == list.Count) return "\"Now THAT is fresh. Top price.\"";
            return "\"Decent. Standard rate.\"";
        }

        // ---- End of day: the card slides up, the report plays in a row at a time (numbers count up, the money
        // lands with a ka-ching, the crew tin fills toward the next stage), then the night shop. A click skips ahead.
        int nightDay = -1, nightLanded;
        float nightAt, nightSkip, nightTickAt;
        bool statsOpen;
        const float RowsStart = 0.7f, RowGap = 0.45f, RowCount = 0.75f, NightRows = 5;
        float NightT => Time.unscaledTime - nightAt + nightSkip;
        float NightDone => RowsStart + (NightRows - 1) * RowGap + RowCount;
        static float EaseOut(float x) { x = Mathf.Clamp01(x); return 1 - (1 - x) * (1 - x) * (1 - x); }
        static float EaseBack(float x) { x = Mathf.Clamp01(x); const float c = 1.6f; return 1 + (c + 1) * Mathf.Pow(x - 1, 3) + c * Mathf.Pow(x - 1, 2); }

        // Every frame (sounds live here, not in OnGUI, which runs several times a frame).
        void NightTick()
        {
            var g = PortGame.I;
            if (g == null || g.Phase.Value != PortGame.Ph.Night) { if (g != null && g.Phase.Value == PortGame.Ph.Day) nightDay = -1; return; }
            if (nightDay != g.DayNum.Value)
            {
                nightDay = g.DayNum.Value; nightAt = Time.unscaledTime; nightSkip = 0; nightLanded = 0;
                Stats.DayDone(g);
                Sfx.Play2D("whoosh", 0.6f);
            }
            float t = NightT;
            while (nightLanded < NightRows && t >= RowsStart + nightLanded * RowGap + RowCount)
            {
                Sfx.Play2D(nightLanded == 0 || nightLanded == 4 ? "kaching" : "place", nightLanded == 0 ? 0.55f : 0.4f);
                nightLanded++;
            }
            // Ticks while a number is counting up.
            float into = t - RowsStart - nightLanded * RowGap;
            if (nightLanded < NightRows && into > 0 && into < RowCount && Time.unscaledTime > nightTickAt)
            {
                nightTickAt = Time.unscaledTime + 0.065f;
                Sfx.Play2D("tick", 0.25f);
            }
        }

        void DrawNight(float W, float H)
        {
            var g = PortGame.I;
            float t = NightT, pw = 720 * S, ph = 780 * S, bw = 520 * S;
            var r = new Rect((W - pw - bw - 30 * S) / 2, (H - ph) / 2, pw, ph);
            float enter = EaseBack(t / 0.55f), fade = Mathf.Clamp01(t / 0.3f);
            r.y += (1 - enter) * 140 * S;
            bool done = t >= NightDone;
            // Click on the card while it plays: skip to the end.
            if (!done && Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition)) { nightSkip += NightDone + 1; Event.current.Use(); }

            GUI.color = new Color(1, 1, 1, fade);
            SteelPanel(r);
            Label(new Rect(r.x, r.y + 26 * S, pw, 70 * S), $"End of Day {g.DayNum.Value}", new GUIStyle(big) { fontSize = Mathf.RoundToInt(52 * S) }, new Color(1f, 0.9f, 0.7f));
            // The verdict types itself out.
            var verdict = g.DayVerdict();
            int chars = Mathf.Clamp(Mathf.FloorToInt((t - 0.35f) * 70f), 0, verdict.Length);
            Label(new Rect(r.x + 30 * S, r.y + 84 * S, pw - 60 * S, 34 * S), verdict.Substring(0, chars), new GUIStyle(small) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Italic, fontSize = Mathf.RoundToInt(21 * S) }, new Color(1f, 0.92f, 0.8f, 0.9f));

            // The contract board slides in from the right a moment later.
            float cIn = EaseOut((t - 0.25f) / 0.6f);
            GUI.color = new Color(1, 1, 1, cIn * fade);
            DrawContracts(new Rect(r.xMax + 30 * S + (1 - cIn) * 160 * S, (H - ph) / 2, bw, ph), done);
            GUI.color = new Color(1, 1, 1, fade);

            var st = new GUIStyle(mid) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Normal };
            float y = r.y + 136 * S, x = r.x + 70 * S, lh = 46 * S;
            int row = 0;
            // One report line: slides in from the left, its number counts up.
            void Line(string label, int value, string extra, Color c, bool money = false)
            {
                float t0 = RowsStart + row * RowGap, a = Mathf.Clamp01((t - t0) / 0.25f);
                row++;
                if (a > 0)
                {
                    int shown = Mathf.RoundToInt(value * EaseOut((t - t0) / RowCount));
                    bool landed = t >= t0 + RowCount;
                    var col = new Color(c.r, c.g, c.b, a);
                    // A little pop as the number lands.
                    float pop = landed ? Mathf.Clamp01(1 - (t - t0 - RowCount) / 0.25f) : 0;
                    var lst = pop > 0 ? new GUIStyle(st) { fontSize = Mathf.RoundToInt(st.fontSize * (1 + 0.12f * pop)) } : st;
                    Label(new Rect(x - (1 - a) * 40 * S, y, pw, lh), $"{label}:  {(money ? "$" : "")}{shown}" + (landed ? extra : ""), lst, col);
                }
                y += lh;
            }
            Line("Money earned", g.Earned.Value, "", Color.white, true);
            Line("Crates sold", g.Sold.Value, g.SpoiledSold.Value > 0 ? $"  ({g.SpoiledSold.Value} spoiled)" : "", Color.white);
            Line("Crates in the sea", g.InSea.Value, "", new Color(0.7f, 0.9f, 1f));
            Line("Went back out on the boats", g.LeftAboard.Value, "", Color.white);
            Line("Crew tin", g.Money.Value, Player.All.Count > 1 ? $"  (shared, same for all {Player.All.Count})" : "", new Color(1f, 0.88f, 0.4f), true);

            // Crew tin → next stage: a bar that fills with the last line.
            var note = new GUIStyle(small) { alignment = TextAnchor.MiddleLeft, fontSize = Mathf.RoundToInt(20 * S) };
            float barIn = EaseOut((t - (RowsStart + 4 * RowGap)) / (RowCount + 0.3f));
            var bar = new Rect(x, y + 6 * S, pw - 140 * S, 14 * S);
            GUI.DrawTexture(bar, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0, 0, 0, 0.45f * fade), 0, 6 * S);
            float fill = Mathf.Clamp01(g.Money.Value / (float)Mathf.Max(1, g.NextStageCost)) * barIn;
            if (fill > 0) GUI.DrawTexture(new Rect(bar.x, bar.y, Mathf.Max(10 * S, bar.width * fill), bar.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(1f, 0.78f, 0.25f, fade), 0, 6 * S);
            y += 26 * S;
            var tm = g.Tomorrow;
            Label(new Rect(x, y, pw - 80 * S, 30 * S), $"Saving for: {g.NextStageName}  ${Mathf.RoundToInt(g.Money.Value * barIn)} / ${g.NextStageCost}", note, new Color(1f, 0.92f, 0.75f, 0.9f)); y += 30 * S;
            Label(new Rect(x, y, pw - 80 * S, 30 * S), $"Tomorrow: Captain {g.NextCaptain[0]}{g.NextCaptain.Substring(1).ToLowerInvariant()}, {tm.boats} boats, first one at {Mathf.FloorToInt(tm.first / 60)}:{Mathf.FloorToInt(tm.first % 60):00}", note, new Color(1f, 0.92f, 0.75f, 0.9f)); y += 30 * S * 1.3f;

            // The shop and the buttons come up once the report has played.
            float shopIn = EaseOut((t - NightDone) / 0.4f);
            if (!done)
            {
                Label(new Rect(r.x, r.yMax - 60 * S, pw, 30 * S), "click to skip", new GUIStyle(small) { alignment = TextAnchor.UpperCenter }, new Color(1, 1, 1, 0.45f * (0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 4f))));
                GUI.color = Color.white;
                return;
            }
            GUI.color = new Color(1, 1, 1, shopIn);
            y += (1 - shopIn) * 30 * S;
            Label(new Rect(r.x, y, pw, lh), "NIGHT SHOP", mid, new Color(1f, 0.8f, 0.5f)); y += lh * 1.05f;
            // One row per item: label left of the Buy button, never under it.
            float lw = r.xMax - 250 * S - x;
            void Row(bool owned, string ownedText, string forSale, int price, System.Action buy)
            {
                Label(new Rect(x, y, lw, lh), owned ? ownedText : forSale, st, owned ? new Color(0.7f, 1f, 0.7f) : Color.white);
                if (!owned && Button(new Rect(r.xMax - 236 * S, y + (lh - 46 * S) / 2, 190 * S, 46 * S), $"Buy ${price}", g.Money.Value >= price)) buy();
                y += lh * 1.2f;
            }
            Row(g.HasTrolley.Value, "Hand trolley: owned", "Hand trolley (6 crates)", PortGame.TrolleyPrice, g.BuyTrolleyRpc);
            if (!Skin.On) Row(g.PierRepaired.Value, "Pier: repaired", "Repair the pier", PortGame.RepairPrice, g.BuyRepairRpc);
            if (g.ForkliftForSale) Row(g.HasForklift.Value, "Forklift: in the yard", "Forklift + 2 pallets", PortGame.ForkliftPrice, g.BuyForkliftRpc);
            Row(g.Stage.Value >= 2, "Concrete quay: built", "Concrete quay", PortGame.QuayCost, g.BuyStageRpc);
            var foot = new Rect(r.x + 40 * S, r.yMax - 90 * S, pw - 80 * S, 64 * S);
            if (Button(new Rect(foot.x, foot.y, 220 * S, foot.height), "Stats")) statsOpen = true;
            if (NetworkManager.Singleton.IsServer)
            {
                if (Button(new Rect(foot.xMax - 360 * S, foot.y, 360 * S, foot.height), $"Start Day {g.DayNum.Value + 1}")) g.NextDayRpc();
            }
            else Label(new Rect(foot.x + 240 * S, foot.y, foot.width - 240 * S, foot.height), "Waiting for the host to start the next day…", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(21 * S) }, Color.white);
            GUI.color = Color.white;
        }

        // Career stats: this run next to all time (kept on this PC). From the night card or the pause menu.
        void DrawStats(float W, float H)
        {
            ThemeTextures();
            GUI.DrawTexture(new Rect(0, 0, W, H), dimTex);
            float pw = 860 * S, lh = 40 * S, ph = 250 * S + Stats.Rows.Length * lh;
            var r = new Rect((W - pw) / 2, (H - ph) / 2, pw, ph);
            SteelPanel(r);
            Label(new Rect(r.x, r.y + 26 * S, pw, 60 * S), "Career Stats", new GUIStyle(big) { fontSize = Mathf.RoundToInt(48 * S) }, new Color(1f, 0.9f, 0.7f));
            var head = new GUIStyle(small) { alignment = TextAnchor.MiddleRight, fontSize = Mathf.RoundToInt(20 * S), fontStyle = FontStyle.Bold };
            var name = new GUIStyle(small) { alignment = TextAnchor.MiddleLeft, fontSize = Mathf.RoundToInt(23 * S) };
            var val = new GUIStyle(name) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold };
            float x = r.x + 50 * S, c1 = r.xMax - 330 * S, c2 = r.xMax - 170 * S, cw = 120 * S, y = r.y + 100 * S;
            Label(new Rect(c1, y, cw, 30 * S), "THIS RUN", head, new Color(1f, 0.8f, 0.5f));
            Label(new Rect(c2, y, cw, 30 * S), "ALL TIME", head, new Color(1f, 0.8f, 0.5f));
            y += 40 * S;
            for (int i = 0; i < Stats.Rows.Length; i++)
            {
                var (key, label, unit) = Stats.Rows[i];
                if (i % 2 == 0) GUI.DrawTexture(new Rect(x - 16 * S, y, pw - 68 * S, lh), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0, 0, 0, 0.18f), 0, 6 * S);
                Label(new Rect(x, y, c1 - x, lh), label, name, Color.white);
                Label(new Rect(c1, y, cw, lh), Stats.Show(key, unit, Stats.Run(key)), val, Color.white);
                Label(new Rect(c2, y, cw, lh), Stats.Show(key, unit, Stats.AllTime(key)), val, new Color(1f, 0.88f, 0.4f));
                y += lh;
            }
            if (Button(new Rect(r.x + (pw - 240 * S) / 2, r.yMax - 84 * S, 240 * S, 60 * S), "Close")) statsOpen = false;
        }

        // The contract board (it hangs on the shed wall): pick tomorrow's captain. Host decides.
        void DrawContracts(Rect r, bool live)
        {
            var g = PortGame.I;
            SteelPanel(r);
            Label(new Rect(r.x, r.y + 26 * S, r.width, 50 * S), "CONTRACT BOARD", mid, new Color(1f, 0.8f, 0.5f));
            Label(new Rect(r.x, r.y + 64 * S, r.width, 30 * S), "Who brings the fish tomorrow?", new GUIStyle(small) { alignment = TextAnchor.UpperCenter }, new Color(1, 1, 1, 0.7f));
            float y = r.y + 110 * S;
            bool host = NetworkManager.Singleton.IsServer;
            for (int i = 0; i < PortGame.Contracts.Length; i++)
            {
                var c = PortGame.Contracts[i];
                bool chosen = g.NextContractId.Value == i;
                var card = new Rect(r.x + 24 * S, y, r.width - 48 * S, 230 * S);
                GUI.color = chosen ? new Color(1f, 0.85f, 0.5f, 1f) : new Color(1, 1, 1, 0.6f);
                GUI.Box(card, "", panel);
                GUI.color = Color.white;
                Label(new Rect(card.x + 20 * S, card.y + 14 * S, card.width, 44 * S), "Captain " + c.Captain[0] + c.Captain.Substring(1).ToLowerInvariant(), new GUIStyle(mid) { alignment = TextAnchor.UpperLeft }, chosen ? new Color(1f, 0.85f, 0.5f) : Color.white);
                Label(new Rect(card.x + 20 * S, card.y + 64 * S, card.width - 40 * S, 90 * S), c.Pitch, new GUIStyle(small) { fontSize = Mathf.RoundToInt(21 * S), wordWrap = true }, new Color(1, 1, 1, 0.85f));
                if (chosen) Label(new Rect(card.x + 20 * S, card.yMax - 58 * S, card.width, 44 * S), "✓ Booked for tomorrow", new GUIStyle(small) { fontSize = Mathf.RoundToInt(21 * S), fontStyle = FontStyle.Bold }, new Color(0.7f, 1f, 0.7f));
                else if (host && live && Button(new Rect(card.xMax - 190 * S, card.yMax - 62 * S, 170 * S, 48 * S), "Book")) g.ChooseContractRpc((byte)i);
                y += 250 * S;
            }
            if (!host) Label(new Rect(r.x, r.yMax - 50 * S, r.width, 30 * S), "The host books the boats.", new GUIStyle(small) { alignment = TextAnchor.UpperCenter }, new Color(1, 1, 1, 0.6f));
        }
    }
}
