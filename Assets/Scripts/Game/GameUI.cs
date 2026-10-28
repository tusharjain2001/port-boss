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
        public GameObject CratePrefab, BoatPrefab, TrolleyPrefab, PortGamePrefab;
        public Material LitMat, EmissiveMat, ParticleMat, WaterMat, CloudMat;
        public GameObject[] Models; public int[] ModelKit; public Material[] KitMats;
        public Font TitleFont;

        enum Screen { Menu, Join, Settings, Playing }
        Screen screen = Screen.Menu;
        bool paused, showHelp = Settings.ControlsHint;
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
            // Graphics A/B switches for before/after comparisons (test runs only).
            var args = System.Environment.GetCommandLineArgs();
            bool Arg(string a) => System.Array.IndexOf(args, a) >= 0;
            if (Arg("-pb-oldwater")) { var w = new Material(LitMat); w.SetColor("_BaseColor", new Color(0.13f, 0.3f, 0.36f)); w.SetFloat("_Smoothness", 0.88f); WaterMat = w; }
            oldFog = Arg("-pb-oldfog");
            if (Arg("-pb-noclouds")) CloudMat = null;
            Art.Lit = LitMat; Art.Emissive = EmissiveMat; Art.Particle = ParticleMat; Art.WaterMat = WaterMat; Art.Cloud = CloudMat;
            Application.targetFrameRate = 144;
            World.Build();
            Gangplank.Create();
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

        void Update()
        {
            Session.Tick();
            var g = PortGame.I;
            if (g != null) ApplyLight(Mathf.Clamp01(g.DayT.Value / PortGame.DayLength), g.Phase.Value == PortGame.Ph.Night);
            else ApplyLight(0.8f, false); // the title scene sits in golden hour
            amb.volume = 0.35f * Settings.Ambience;
            SettingsKeyCapture();
            music.Play(MusicSlot(g));

            var k = Keyboard.current;
            if (screen == Screen.Playing && k != null)
            {
                var grabKey = Settings.KeyFor("grab");
                if (buyerOpen && (k.escapeKey.wasPressedThisFrame || (grabKey != Key.None && k[grabKey].wasPressedThisFrame))) CloseBuyer();
                else if (k.escapeKey.wasPressedThisFrame || Gamepad.current?.startButton.wasPressedThisFrame == true) SetPaused(!paused);
                if (k.hKey.wasPressedThisFrame) { showHelp = !showHelp; Settings.ControlsHint = showHelp; }
            }
            var nm = NetworkManager.Singleton;
            bool live = nm.IsListening && !nm.ShutdownInProgress;
            if (screen != Screen.Playing && live) screen = Screen.Playing;
            else if (screen == Screen.Playing && !live) BackToMenu();

            bool wantCursor = InputBlocked;
            Cursor.lockState = wantCursor || AutoRun.Active ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = wantCursor;

            // Menu backdrop: slow orbit over the pier.
            if (Player.Local == null)
            {
                // Title scene: a slow drifting camera over the pier and a boat bobbing alongside.
                if (menuBoat == null && !nm.IsListening) menuBoat = MenuBoat();
                float a = Time.unscaledTime * 0.035f;
                var focus = new Vector3(3, 1.2f, 15 + Mathf.Sin(a * 0.7f) * 4);
                cam.transform.position = new Vector3(-16 + Mathf.Sin(a) * 9, 5.5f + Mathf.Sin(a * 1.3f) * 1.2f, -6 + Mathf.Cos(a) * 5);
                cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(focus - cam.transform.position), 1 - Mathf.Exp(-2f * Time.unscaledDeltaTime));
            }
            if (menuBoat != null && nm.IsListening) Destroy(menuBoat);
        }

        string MusicSlot(PortGame g)
        {
            if (screen != Screen.Playing || g == null) return "menu";
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
        Texture2D panelTex, btnTex, btnHot;
        float S => UnityEngine.Screen.height / 1080f;

        Texture2D Tex(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }

        void Styles()
        {
            if (big != null && Mathf.Approximately(big.fontSize, Mathf.RoundToInt(64 * S))) return;
            panelTex ??= Tex(new Color(0.08f, 0.1f, 0.12f, 0.82f));
            btnTex ??= Tex(new Color(0.85f, 0.55f, 0.2f, 0.95f));
            btnHot ??= Tex(new Color(1f, 0.68f, 0.3f, 1f));
            big = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(64 * S), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            mid = new GUIStyle(big) { fontSize = Mathf.RoundToInt(30 * S) };
            small = new GUIStyle(big) { fontSize = Mathf.RoundToInt(20 * S), fontStyle = FontStyle.Normal, alignment = TextAnchor.UpperLeft };
            title = new GUIStyle(big) { fontSize = Mathf.RoundToInt(132 * S), fontStyle = FontStyle.Normal, font = TitleFont, alignment = TextAnchor.MiddleLeft, wordWrap = false };
            menuItem = new GUIStyle(big) { fontSize = Mathf.RoundToInt(40 * S), fontStyle = FontStyle.Normal, font = TitleFont, alignment = TextAnchor.MiddleLeft };
            if (TitleFont != null) { big.font = TitleFont; big.fontStyle = FontStyle.Normal; mid.font = TitleFont; mid.fontStyle = FontStyle.Normal; }
            fadeTex ??= Gradient();
            shoutS = new GUIStyle(big) { fontSize = Mathf.RoundToInt(38 * S * Settings.ShoutSize) };
            panel = new GUIStyle(GUI.skin.box) { normal = { background = panelTex } };
            btn = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(28 * S), fontStyle = FontStyle.Bold };
            btn.normal.background = btnTex; btn.hover.background = btnHot; btn.active.background = btnHot;
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
            if (b) Sfx.Play2D("click", 0.6f);
            return b;
        }

        void Hover(Rect r, string id, bool enabled)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (enabled && r.Contains(Event.current.mousePosition)) { if (hovered != id) { hovered = id; Sfx.Play2D("hover", 0.35f); } }
            else if (hovered == id) hovered = null;
        }

        // Text menu entry for the front door: hover slides it right and lights it up.
        bool MenuItem(Rect r, string s, bool enabled = true)
        {
            bool hot = enabled && r.Contains(Event.current.mousePosition);
            var rr = new Rect(r.x + (hot ? 18 * S : 0), r.y, r.width, r.height);
            Label(rr, (hot ? "› " : "") + s, menuItem, !enabled ? new Color(1, 1, 1, 0.3f) : hot ? new Color(1f, 0.8f, 0.4f) : new Color(1f, 0.96f, 0.88f));
            Hover(r, s, enabled);
            bool b = enabled && GUI.Button(r, GUIContent.none, GUIStyle.none);
            if (b) Sfx.Play2D("click", 0.7f);
            return b;
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
            if (!string.IsNullOrEmpty(Session.Status) && screen != Screen.Playing)
                Label(new Rect(0, H - 80 * S, W, 50 * S), Session.Status, mid, new Color(1f, 0.8f, 0.6f));
        }

        void DrawMenu(float W, float H)
        {
            GUI.DrawTexture(new Rect(0, 0, W * 0.55f, H), fadeTex);
            float x = 90 * S;
            // Title: gold slab type with a deep shadow + outline, a rope rule, a tagline.
            var tr = new Rect(x, H * 0.12f, W, 150 * S);
            var ink = new Color(0.05f, 0.08f, 0.13f, 0.95f);
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                Label(new Rect(tr.x + dx * 3 * S, tr.y + dy * 3 * S + 4 * S, tr.width, tr.height), "PORT BOSS", title, ink);
            Label(tr, "PORT BOSS", title, new Color(0.98f, 0.79f, 0.38f));
            for (int i = 0; i < 26; i++)
                GUI.DrawTexture(new Rect(x + 6 * S + i * 25 * S, tr.yMax + 4 * S + (i % 2) * 3 * S, 18 * S, 6 * S), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.82f, 0.68f, 0.45f, 0.9f), 0, 3 * S);
            Label(new Rect(x + 4 * S, tr.yMax + 22 * S, W, 40 * S), "A FISHING PIER  ·  A BOAT  ·  TWO MINUTES", new GUIStyle(small) { fontSize = Mathf.RoundToInt(22 * S), fontStyle = FontStyle.Bold }, new Color(1f, 0.9f, 0.75f, 0.9f));

            float y = H * 0.46f, gap = 66 * S, w = 520 * S, h = 58 * S;
            if (MenuItem(new Rect(x, y, w, h), "Play")) Play();
            if (MenuItem(new Rect(x, y + gap, w, h), "Host co-op", Session.SteamOk)) Host(false);
            if (MenuItem(new Rect(x, y + gap * 2, w, h), "Join a friend")) { screen = Screen.Join; Refresh(); }
            if (MenuItem(new Rect(x, y + gap * 3, w, h), "Settings")) { settingsFromPause = false; screen = Screen.Settings; }
            if (MenuItem(new Rect(x, y + gap * 4, w, h), "Quit")) Application.Quit();

            Label(new Rect(x, H - 52 * S, W, 30 * S), Session.SteamOk ? $"Steam: {Steamworks.SteamClient.Name}  ·  co-op via test app 480" : "Steam offline: solo works, co-op needs Steam", small, new Color(1, 1, 1, 0.6f));
            Label(new Rect(W - 520 * S, H - 52 * S, 500 * S, 30 * S), $"v{Application.version}  ·  overnight build", new GUIStyle(small) { alignment = TextAnchor.UpperRight }, new Color(1, 1, 1, 0.5f));
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
            GUI.Box(r, "", panel);
            Label(new Rect(r.x, r.y + 20 * S, pw, 60 * S), "Join a port", mid, Color.white);
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
                Label(new Rect(W - 420 * S, 18 * S, 400 * S, 80 * S), $"${g.Money.Value}", new GUIStyle(big) { alignment = TextAnchor.UpperRight }, new Color(1f, 0.88f, 0.4f));
                float u = g.DayT.Value / PortGame.DayLength;
                string tod = g.Phase.Value == PortGame.Ph.Night ? "night" : u < 0.33f ? "afternoon" : u < 0.7f ? "late afternoon" : "sunset";
                Label(new Rect(24 * S, 18 * S, 600 * S, 50 * S), $"DAY {g.DayNum.Value} · {tod}", new GUIStyle(mid) { alignment = TextAnchor.UpperLeft }, new Color(1f, 0.95f, 0.85f));
            }
            else Label(new Rect(0, H * 0.45f, W, 60 * S), "Connecting…", mid, Color.white);

            if (Time.unscaledTime < shoutUntil)
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

            foreach (var (lp, lt) in World.Labels)
            {
                var sp = cam.WorldToScreenPoint(lp);
                float d = Vector3.Distance(cam.transform.position, lp);
                if (sp.z < 0 || d > 45f || Physics.Linecast(cam.transform.position, lp, ~0, QueryTriggerInteraction.Ignore)) continue;
                Label(new Rect(sp.x - 200 * S, H - sp.y - 20 * S, 400 * S, 40 * S), lt, mid, new Color(1f, 0.93f, 0.75f, Mathf.Clamp01((45f - d) / 10f)));
            }

            var pl = Player.Local;
            if (pl != null && !InputBlocked)
            {
                string hint = null;
                if (pl.Swimming) hint = pl.NearLadder() != null ? "E  climb out" : pl.Towing ? "Q  let go of the crate" : pl.FloatingCrateNear() != null ? "E  tow this crate" : "Swim to a ladder or the slipway";
                else if (pl.NearBuyer && pl.SellableCrates().Count > 0) hint = "E  sell to the buyer";
                else if (pl.Pushing) hint = "F  let go of trolley";
                else if (pl.Carrying < Player.MaxStack && pl.FindCrate() != null) hint = pl.Carrying == 0 ? "E  grab crate" : "E  stack another";
                else if (pl.Carrying == 0 && Trolley.I != null && Vector3.Distance(Trolley.I.Handle, pl.transform.position + Vector3.up) < 2.2f) hint = "F  push trolley";
                if (pl.Carrying >= 2 && pl.Teeter > Player.TeeterAt)
                {
                    float a = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 18f);
                    Label(new Rect(0, H * 0.70f, W, 70 * S), "Steady...!", new GUIStyle(big) { fontSize = Mathf.RoundToInt(56 * S) }, new Color(1f, 0.55f, 0.3f, a));
                }
                if (pl.Carrying > 0) Label(new Rect(0, H - 150 * S, W, 40 * S), $"Carrying {pl.Carrying}/{Player.MaxStack}   ·   Q drop   ·   Left click throw", mid, new Color(1, 1, 1, 0.85f));
                if (hint != null) Label(new Rect(0, H * 0.56f, W, 40 * S), hint, mid, new Color(1f, 0.92f, 0.7f));
            }
            if (showHelp)
                Label(new Rect(24 * S, H - 190 * S, 640 * S, 180 * S),
                    "WASD move · Shift sprint · Space jump · Mouse look\nE / Right click: grab, stack (max 5) · E at the buyer: sell\nQ: drop one · Left click: throw · F: push trolley\nH hide · Esc menu", small, new Color(1, 1, 1, 0.75f));
        }

        void DrawBuyer(float W, float H)
        {
            var pl = Player.Local;
            var list = pl != null ? pl.SellableCrates() : new List<Crate>();
            if (pl == null || !pl.NearBuyer || list.Count == 0) { buyerOpen = false; return; }
            // Test bot: sell everything after a moment so captures show the panel.
            if (AutoRun.Active && Time.unscaledTime - buyerOpenedAt > 1.5f) { pl.RequestSellAll(); buyerOpen = false; return; }

            float pw = 640 * S, rowH = 52 * S, ph = 210 * S + rowH * list.Count;
            var r = new Rect(W - pw - 40 * S, (H - ph) / 2, pw, ph);
            GUI.Box(r, "", panel);
            Label(new Rect(r.x, r.y + 14 * S, pw, 50 * S), "THE BUYER", mid, new Color(1f, 0.85f, 0.55f));
            Label(new Rect(r.x, r.y + 56 * S, pw, 30 * S), BuyerLine(list), new GUIStyle(small) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Italic }, new Color(1, 1, 1, 0.85f));
            var st = new GUIStyle(mid) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Normal, fontSize = Mathf.RoundToInt(24 * S) };
            float y = r.y + 100 * S;
            int total = 0;
            foreach (var c in list)
            {
                int v = PortGame.I != null ? PortGame.I.PriceFor(c) : c.Value; total += v;
                var col = c.Grade == "fresh" ? new Color(0.7f, 1f, 0.75f) : c.Grade == "okay" ? Color.white : new Color(0.8f, 0.85f, 0.45f);
                string where = c.transform.parent == pl.transform ? "" : "  (trolley)";
                Label(new Rect(r.x + 26 * S, y, pw, rowH), $"Fish crate · {c.Grade} {Mathf.RoundToInt(c.Fresh.Value * 100)}%{where}", st, col);
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
            float pw = 720 * S, ph = 720 * S, bw = 520 * S;
            var r = new Rect((W - pw - bw - 30 * S) / 2, (H - ph) / 2, pw, ph);
            GUI.Box(r, "", panel);
            Label(new Rect(r.x, r.y + 20 * S, pw, 70 * S), $"End of Day {g.DayNum.Value}", new GUIStyle(big) { fontSize = Mathf.RoundToInt(52 * S) }, new Color(1f, 0.9f, 0.7f));
            Label(new Rect(r.x + 30 * S, r.y + 84 * S, pw - 60 * S, 34 * S), g.DayVerdict(), new GUIStyle(small) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Italic, fontSize = Mathf.RoundToInt(21 * S) }, new Color(1f, 0.92f, 0.8f, 0.9f));
            DrawContracts(new Rect(r.xMax + 30 * S, r.y, bw, ph));
            var st = new GUIStyle(mid) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Normal };
            float y = r.y + 136 * S, x = r.x + 70 * S, lh = 46 * S;
            Label(new Rect(x, y, pw, lh), $"Money earned:  ${g.Earned.Value}", st, Color.white); y += lh;
            Label(new Rect(x, y, pw, lh), $"Crates sold:  {g.Sold.Value}" + (g.SpoiledSold.Value > 0 ? $"  ({g.SpoiledSold.Value} spoiled)" : ""), st, Color.white); y += lh;
            Label(new Rect(x, y, pw, lh), $"Crates in the sea:  {g.InSea.Value}", st, new Color(0.7f, 0.9f, 1f)); y += lh;
            Label(new Rect(x, y, pw, lh), $"Went back out on the boats:  {g.LeftAboard.Value}", st, Color.white); y += lh;
            Label(new Rect(x, y, pw, lh), $"Cash in the tin:  ${g.Money.Value}", st, new Color(1f, 0.88f, 0.4f)); y += lh * 1.4f;

            Label(new Rect(r.x, y, pw, lh), "NIGHT SHOP", mid, new Color(1f, 0.8f, 0.5f)); y += lh * 1.1f;
            string tl = g.HasTrolley.Value ? "Hand trolley: owned" : $"Hand trolley  ${PortGame.TrolleyPrice}  (holds 6 crates)";
            Label(new Rect(x, y, pw, lh), tl, st, Color.white);
            if (!g.HasTrolley.Value && Button(new Rect(r.xMax - 210 * S, y, 160 * S, 46 * S), "Buy", g.Money.Value >= PortGame.TrolleyPrice)) g.BuyTrolleyRpc();
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
            GUI.Box(r, "", panel);
            Label(new Rect(r.x, r.y + 20 * S, r.width, 50 * S), "CONTRACT BOARD", mid, new Color(1f, 0.8f, 0.5f));
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

        void DrawPause(float W, float H)
        {
            GUI.Box(new Rect(0, 0, W, H), "", panel);
            Label(new Rect(0, H * 0.2f, W, 90 * S), "Paused", big, Color.white);
            float bw = 380 * S, bh = 64 * S, x = (W - bw) / 2, y = H * 0.38f, gap = 80 * S;
            if (Button(new Rect(x, y, bw, bh), "Resume")) SetPaused(false);
            if (Session.Lobby != null && Button(new Rect(x, y + gap, bw, bh), "Invite Steam friends")) Session.Invite();
            if (Button(new Rect(x, y + gap * 2, bw, bh), "Settings")) settingsFromPause = true;
            if (Button(new Rect(x, y + gap * 3, bw, bh), "Quit to menu")) BackToMenu();
            if (Button(new Rect(x, y + gap * 4, bw, bh), "Quit game")) Application.Quit();
        }
    }
}
