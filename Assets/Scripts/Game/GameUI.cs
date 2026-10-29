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
        public Material LitMat, EmissiveMat, ParticleMat, WaterMat, CloudMat;
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

        public static bool InputBlocked => I == null || I.screen != Screen.Playing || I.paused || I.buyerOpen
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
            Art.Lit = LitMat; Art.Emissive = EmissiveMat; Art.Particle = ParticleMat; Art.WaterMat = WaterMat; Art.Cloud = CloudMat;
            Skin.I = Skin;
            if (Skin.On && !Arg("-pb-ownart"))
            {
                Art.Toon = Skin.Toon;
                if (Skin.ToonWater != null)
                {
                    // The pack water, a touch calmer in pattern and more opaque near the surface (swimmers sit chest-deep in it).
                    var wm = new Material(Skin.ToonWater);
                    wm.SetFloat("_DistortionAmount1", 0.5f); wm.SetFloat("_DistortionAmount2", 0.3f); wm.SetFloat("_DepthFadeDistance", 1.2f);
                    // Fresnel ~1 everywhere: the surface is sky reflection + colour, never the refracted screen (it smeared the quay into the water).
                    wm.SetFloat("_FresnelPower", 0.02f);
                    wm.SetColor("_MainColor", new Color(0.0f, 0.075f, 0.13f)); // + the sky reflection = deep harbour blue
                    Art.WaterMat = wm;
                }
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
            amb.volume = 0.35f * (1f + Storm.Level * 1.3f) * Settings.Ambience; // the sea gets louder as it gets rougher
            SettingsKeyCapture();
            music.Play(MusicSlot(g));

            var k = Keyboard.current;
            if (screen == Screen.Playing && k != null)
            {
                var grabKey = Settings.KeyFor("grab");
                if (buyerOpen && (k.escapeKey.wasPressedThisFrame || (grabKey != Key.None && k[grabKey].wasPressedThisFrame))) CloseBuyer();
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
                if (PortGame.I != null && PortGame.I.Phase.Value == PortGame.Ph.Night) DrawNight(W, H);
                else if (buyerOpen) DrawBuyer(W, H);
                if (paused) { if (settingsFromPause) DrawSettings(W, H); else DrawPause(W, H); }
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
                Label(new Rect(0, H * 0.62f, W, 50 * S), toast, mid, new Color(0.85f, 0.95f, 1f, Mathf.Clamp01((toastUntil - Time.unscaledTime) * 2)));

            for (int i = pops.Count - 1; i >= 0; i--)
            {
                var p = pops[i];
                float t = Time.unscaledTime - p.t0;
                if (t > 1.6f || cam == null) { pops.RemoveAt(i); continue; }
                var sp = cam.WorldToScreenPoint(p.p + Vector3.up * t * 0.9f);
                if (sp.z < 0) continue;
                var c = p.c; c.a = Mathf.Clamp01((1.6f - t) * 2);
                Label(new Rect(sp.x - 200 * S, H - sp.y - 30 * S, 400 * S, 60 * S), p.s, new GUIStyle(mid) { fontSize = Mathf.RoundToInt(40 * S) }, c);
            }

            bool nightCard = PortGame.I != null && PortGame.I.Phase.Value == PortGame.Ph.Night;
            if (!nightCard) foreach (var (lp, lt) in World.Labels)
            {
                var sp = cam.WorldToScreenPoint(lp);
                float d = Vector3.Distance(cam.transform.position, lp);
                if (sp.z < 0 || d > 45f || Physics.Linecast(cam.transform.position, lp, ~0, QueryTriggerInteraction.Ignore)) continue;
                Label(new Rect(sp.x - 200 * S, H - sp.y - 20 * S, 400 * S, 40 * S), lt, mid, new Color(1f, 0.93f, 0.75f, Mathf.Clamp01((45f - d) / 10f)));
            }

            var pl = Player.Local;
            if (pl != null && g != null && !nightCard && Settings.Hud) DrawObjective(W, H, g, pl);
            if (pl != null && !InputBlocked)
            {
                string hint = null;
                var fk = Forklift.I;
                if (pl.Driving != null) hint = "WASD drive · Shift raise forks · Q lower · E get out";
                else if (fk != null && fk.Free && pl.Carrying == 0 && Vector3.Distance(fk.Seat, pl.transform.position + Vector3.up) < 2.4f) hint = "E  drive the forklift";
                else if (pl.Swimming) hint = pl.NearLadder() != null ? "E  climb out" : pl.Towing ? "Q  let go of the crate" : pl.FloatingCrateNear() != null ? "E  tow this crate" : "Swim to a ladder or the slipway";
                else if (pl.NearBuyer && pl.SellableCrates().Count > 0) hint = "E  sell to the buyer";
                else if (PortGame.I != null && Storm.Gust > 0.15f && pl.Carrying == 0 && pl.FindCrate() is Crate tc && !tc.Attached.Value) hint = tc.Tied.Value ? "F  untie crate" : "F  tie this crate down";
                else if (pl.Pushing) hint = "F  let go of trolley";
                else if (pl.Carrying == 0 && pl.ShoveTarget() != null) hint = "F  shove";
                else if (pl.Carrying < Player.MaxStack && pl.FindCrate() != null) hint = pl.Carrying == 0 ? "E  grab crate" : "E  stack another";
                else if (pl.Carrying == 0 && Trolley.I != null && Vector3.Distance(Trolley.I.Handle, pl.transform.position + Vector3.up) < 2.2f) hint = "F  push trolley";
                if (pl.Carrying >= 2 && pl.Teeter > Player.TeeterAt)
                {
                    float a = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 18f);
                    Label(new Rect(0, H * 0.70f, W, 70 * S), "Steady...!", new GUIStyle(big) { fontSize = Mathf.RoundToInt(56 * S) }, new Color(1f, 0.55f, 0.3f, a));
                }
                if (pl.Carrying > 0) Label(new Rect(0, H - 150 * S, W, 40 * S), $"Carrying {pl.Carrying}/{Player.MaxStack}   ·   Q drop   ·   Left click throw", mid, new Color(1, 1, 1, 0.85f));
                if (hint != null)
                {
                    float hw = mid.CalcSize(new GUIContent(hint)).x + 40 * S;
                    Pill(new Rect((W - hw) / 2, H * 0.56f - 2 * S, hw, 44 * S));
                    Label(new Rect(0, H * 0.56f, W, 40 * S), hint, mid, new Color(1f, 0.92f, 0.7f));
                }
            }
            if (showHelp && !nightCard)
                Label(new Rect(24 * S, H - 190 * S, 640 * S, 180 * S),
                    "WASD move · Shift sprint · Space jump · Mouse look\nE / Right click: grab, stack (max 5) · E at the buyer: sell\nQ: drop one · Left click: throw · F: trolley / shove a mate\nH hide · Esc menu", small, new Color(1, 1, 1, 0.75f));
        }

        void Pill(Rect r) => GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.03f, 0.05f, 0.08f, 0.5f), 0, 10 * S);

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
            if (pl.Carrying > 0) { job = near != null && pl.Carrying < 3 ? "Stack a few more (E), then take them to the FISH BUYER" : "Take the fish to the FISH BUYER and press E to sell"; at = World.BuyerPos + Vector3.up * 3.6f; }
            else if (near != null) { job = "Unload the boat: walk up to a fish crate and press E"; at = near.transform.position + Vector3.up * 1.6f; }
            else if (boat != null && boat.State.Value == Boat.St.Arriving) { job = "A boat is coming in: meet it at the end of the jetty"; at = boat.BerthPos + Vector3.up * 2.5f; }
            else job = g.NextBoatIn > 0 ? $"Next boat in {(int)g.NextBoatIn / 60}:{(int)g.NextBoatIn % 60:00}: wait at the jetty" : "That was the last boat today";
            ThemeTextures();
            var r = new Rect(24 * S, 74 * S, 640 * S, 34 * S);
            Pill(new Rect(r.x, r.y, 84 * S + small.CalcSize(new GUIContent(job)).x * 1.1f, r.height));
            GUI.DrawTexture(new Rect(r.x, r.y, 60 * S, r.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.98f, 0.76f, 0.1f), 0, 6 * S);
            var js = new GUIStyle(small) { fontSize = Mathf.RoundToInt(18 * S), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            js.normal.textColor = new Color(0.1f, 0.1f, 0.11f);
            GUI.Label(new Rect(r.x, r.y, 60 * S, r.height), "JOB", js);
            Label(new Rect(r.x + 72 * S, r.y, r.width, r.height), job, new GUIStyle(small) { fontSize = Mathf.RoundToInt(22 * S), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft }, new Color(1f, 0.95f, 0.82f));
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

        void DrawNight(float W, float H)
        {
            var g = PortGame.I;
            float pw = 720 * S, ph = 780 * S, bw = 520 * S;
            var r = new Rect((W - pw - bw - 30 * S) / 2, (H - ph) / 2, pw, ph);
            SteelPanel(r);
            Label(new Rect(r.x, r.y + 26 * S, pw, 70 * S), $"End of Day {g.DayNum.Value}", new GUIStyle(big) { fontSize = Mathf.RoundToInt(52 * S) }, new Color(1f, 0.9f, 0.7f));
            Label(new Rect(r.x + 30 * S, r.y + 84 * S, pw - 60 * S, 34 * S), g.DayVerdict(), new GUIStyle(small) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Italic, fontSize = Mathf.RoundToInt(21 * S) }, new Color(1f, 0.92f, 0.8f, 0.9f));
            DrawContracts(new Rect(r.xMax + 30 * S, r.y, bw, ph));
            var st = new GUIStyle(mid) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Normal };
            float y = r.y + 136 * S, x = r.x + 70 * S, lh = 46 * S;
            Label(new Rect(x, y, pw, lh), $"Money earned:  ${g.Earned.Value}", st, Color.white); y += lh;
            Label(new Rect(x, y, pw, lh), $"Crates sold:  {g.Sold.Value}" + (g.SpoiledSold.Value > 0 ? $"  ({g.SpoiledSold.Value} spoiled)" : ""), st, Color.white); y += lh;
            Label(new Rect(x, y, pw, lh), $"Crates in the sea:  {g.InSea.Value}", st, new Color(0.7f, 0.9f, 1f)); y += lh;
            Label(new Rect(x, y, pw, lh), $"Went back out on the boats:  {g.LeftAboard.Value}", st, Color.white); y += lh;
            Label(new Rect(x, y, pw, lh), $"Crew tin:  ${g.Money.Value}" + (Player.All.Count > 1 ? $"  (shared, same for all {Player.All.Count})" : ""), st, new Color(1f, 0.88f, 0.4f)); y += lh;
            var note = new GUIStyle(small) { alignment = TextAnchor.MiddleLeft, fontSize = Mathf.RoundToInt(20 * S) };
            var tm = g.Tomorrow;
            Label(new Rect(x, y, pw - 80 * S, 30 * S), $"Saving for: {g.NextStageName}  ${g.Money.Value} / ${g.NextStageCost}", note, new Color(1f, 0.92f, 0.75f, 0.9f)); y += 30 * S;
            Label(new Rect(x, y, pw - 80 * S, 30 * S), $"Tomorrow: Captain {g.NextCaptain[0]}{g.NextCaptain.Substring(1).ToLowerInvariant()}, {tm.boats} boats, first one at {Mathf.FloorToInt(tm.first / 60)}:{Mathf.FloorToInt(tm.first % 60):00}", note, new Color(1f, 0.92f, 0.75f, 0.9f)); y += 30 * S * 1.4f;

            Label(new Rect(r.x, y, pw, lh), "NIGHT SHOP", mid, new Color(1f, 0.8f, 0.5f)); y += lh * 1.1f;
            // One row per item: label left of the Buy button, never under it.
            float lw = r.xMax - 250 * S - x;
            void Row(bool owned, string ownedText, string forSale, int price, System.Action buy)
            {
                Label(new Rect(x, y, lw, lh), owned ? ownedText : forSale, st, owned ? new Color(0.7f, 1f, 0.7f) : Color.white);
                if (!owned && Button(new Rect(r.xMax - 236 * S, y + (lh - 46 * S) / 2, 190 * S, 46 * S), $"Buy ${price}", g.Money.Value >= price)) buy();
                y += lh * 1.25f;
            }
            Row(g.HasTrolley.Value, "Hand trolley: owned", "Hand trolley (6 crates)", PortGame.TrolleyPrice, g.BuyTrolleyRpc);
            if (!Skin.On) Row(g.PierRepaired.Value, "Pier: repaired", "Repair the pier", PortGame.RepairPrice, g.BuyRepairRpc);
            if (g.ForkliftForSale) Row(g.HasForklift.Value, "Forklift: in the yard", "Forklift + 2 pallets", PortGame.ForkliftPrice, g.BuyForkliftRpc);
            Row(g.Stage.Value >= 2, "Concrete quay: built", "Concrete quay", PortGame.QuayCost, g.BuyStageRpc);
            y += lh * 1.6f;
            if (NetworkManager.Singleton.IsServer)
            {
                if (Button(new Rect(r.x + (pw - 360 * S) / 2, r.yMax - 90 * S, 360 * S, 64 * S), $"Start Day {g.DayNum.Value + 1}")) g.NextDayRpc();
            }
            else Label(new Rect(r.x, r.yMax - 90 * S, pw, 60 * S), "Waiting for the host to start the next day…", st, Color.white);
        }

        // The contract board (it hangs on the shed wall): pick tomorrow's captain. Host decides.
        void DrawContracts(Rect r)
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
                else if (host && Button(new Rect(card.xMax - 190 * S, card.yMax - 62 * S, 170 * S, 48 * S), "Book")) g.ChooseContractRpc((byte)i);
                y += 250 * S;
            }
            if (!host) Label(new Rect(r.x, r.yMax - 50 * S, r.width, 30 * S), "The host books the boats.", new GUIStyle(small) { alignment = TextAnchor.UpperCenter }, new Color(1, 1, 1, 0.6f));
        }
    }
}
