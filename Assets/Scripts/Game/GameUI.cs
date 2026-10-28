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
    public class GameUI : MonoBehaviour
    {
        public static GameUI I;
        public GameObject CratePrefab, BoatPrefab, TrolleyPrefab, PortGamePrefab;
        public Material LitMat, EmissiveMat, ParticleMat, WaterMat;
        public GameObject[] Models; public int[] ModelKit; public Material[] KitMats;

        enum Screen { Menu, Join, Playing }
        Screen screen = Screen.Menu;
        bool paused, showHelp = true;
        string lanIp = "127.0.0.1";
        Steamworks.Data.Lobby[] lobbies = new Steamworks.Data.Lobby[0];
        bool searching;

        static string shout; static float shoutUntil;
        static string toast; static float toastUntil;
        struct Pop { public Vector3 p; public string s; public Color c; public float t0; }
        static readonly List<Pop> pops = new();

        Light sun;
        Camera cam;
        float lastEarned;

        public static bool InputBlocked => I == null || I.screen != Screen.Playing || I.paused
            || (PortGame.I != null && PortGame.I.Phase.Value == PortGame.Ph.Night);

        void Awake()
        {
            I = this;
            Art.Lit = LitMat; Art.Emissive = EmissiveMat; Art.Particle = ParticleMat; Art.WaterMat = WaterMat;
            Application.targetFrameRate = 144;
            World.Build();
            Gangplank.Create();
            SetupCameraAndLight();
            Session.InitSteam();
            var amb = gameObject.AddComponent<AudioSource>();
            amb.clip = Sfx.Clip("waves"); amb.loop = true; amb.volume = 0.35f; amb.spatialBlend = 0;
            if (amb.clip != null) amb.Play();
        }

        float nextGull = 6f;

        void Start()
        {
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
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 70; RenderSettings.fogEndDistance = 650;

            var vol = new GameObject("PostFX").AddComponent<Volume>();
            vol.isGlobal = true;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var tm = prof.Add<Tonemapping>(); tm.mode.Override(TonemappingMode.ACES);
            var bl = prof.Add<Bloom>(); bl.intensity.Override(0.45f); bl.threshold.Override(1.05f);
            var ca = prof.Add<ColorAdjustments>(); ca.saturation.Override(8f); ca.postExposure.Override(0.25f); ca.contrast.Override(8f);
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
                RenderSettings.skybox.SetFloat("_Exposure", night ? 0.25f : 1.3f);
            RenderSettings.ambientSkyColor = night ? new Color(0.22f, 0.28f, 0.45f) : Color.Lerp(new Color(0.62f, 0.68f, 0.78f), new Color(0.6f, 0.5f, 0.5f), u);
            RenderSettings.ambientEquatorColor = night ? new Color(0.14f, 0.16f, 0.24f) : Color.Lerp(new Color(0.62f, 0.56f, 0.48f), new Color(0.7f, 0.5f, 0.38f), u);
            RenderSettings.ambientGroundColor = night ? new Color(0.05f, 0.05f, 0.08f) : new Color(0.28f, 0.24f, 0.2f);
            RenderSettings.fogColor = night ? new Color(0.08f, 0.1f, 0.16f) : Color.Lerp(new Color(0.78f, 0.74f, 0.66f), new Color(0.85f, 0.62f, 0.45f), u);
        }

        void Update()
        {
            Session.Tick();
            var g = PortGame.I;
            if (g != null) ApplyLight(Mathf.Clamp01(g.DayT.Value / PortGame.DayLength), g.Phase.Value == PortGame.Ph.Night);

            var k = Keyboard.current;
            if (screen == Screen.Playing && k != null)
            {
                if (k.escapeKey.wasPressedThisFrame) SetPaused(!paused);
                if (k.hKey.wasPressedThisFrame) showHelp = !showHelp;
            }
            var nm = NetworkManager.Singleton;
            bool live = nm.IsListening && !nm.ShutdownInProgress;
            if (screen != Screen.Playing && live) screen = Screen.Playing;
            else if (screen == Screen.Playing && !live) BackToMenu();

            bool wantCursor = InputBlocked;
            Cursor.lockState = wantCursor || AutoRun.Active ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = wantCursor;

            if (Time.time > nextGull)
            {
                nextGull = Time.time + Random.Range(10f, 24f);
                var c0 = cam.transform.position;
                Sfx.Play("gull", c0 + new Vector3(Random.Range(-25f, 25f), 12f, Random.Range(5f, 30f)), 0.5f);
            }

            // Menu backdrop: slow orbit over the pier.
            if (Player.Local == null)
            {
                float a = Time.unscaledTime * 0.04f;
                var c = new Vector3(2, 0, 14);
                cam.transform.position = c + new Vector3(Mathf.Sin(a) * 26, 9, -Mathf.Cos(a) * 26);
                cam.transform.LookAt(c + Vector3.up * 1.5f);
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
        public static void Shout(string s, float secs) { shout = s; shoutUntil = Time.unscaledTime + secs; }
        public static void Toast(string s) { toast = s; toastUntil = Time.unscaledTime + 2.5f; }
        public static void Popup(Vector3 p, string s, Color c) => pops.Add(new Pop { p = p, s = s, c = c, t0 = Time.unscaledTime });

        // ---------- drawing ----------
        GUIStyle big, mid, small, btn, panel, shoutS;
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
            shoutS = new GUIStyle(big) { fontSize = Mathf.RoundToInt(38 * S) };
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
            if (b) Sfx.Play2D("click", 0.6f);
            return b;
        }

        void OnGUI()
        {
            Styles();
            float W = UnityEngine.Screen.width, H = UnityEngine.Screen.height;
            if (screen == Screen.Menu) DrawMenu(W, H);
            else if (screen == Screen.Join) DrawJoin(W, H);
            else
            {
                DrawHud(W, H);
                if (PortGame.I != null && PortGame.I.Phase.Value == PortGame.Ph.Night) DrawNight(W, H);
                if (paused) DrawPause(W, H);
            }
            if (!string.IsNullOrEmpty(Session.Status) && screen != Screen.Playing)
                Label(new Rect(0, H - 80 * S, W, 50 * S), Session.Status, mid, new Color(1f, 0.8f, 0.6f));
        }

        void DrawMenu(float W, float H)
        {
            Label(new Rect(0, H * 0.12f, W, 120 * S), "PORT BOSS", new GUIStyle(big) { fontSize = Mathf.RoundToInt(110 * S) }, new Color(1f, 0.93f, 0.8f));
            Label(new Rect(0, H * 0.12f + 115 * S, W, 50 * S), "A fishing pier. A boat. Two minutes.", mid, new Color(1f, 0.85f, 0.65f));
            float bw = 380 * S, bh = 64 * S, x = (W - bw) / 2, y = H * 0.42f, gap = 80 * S;
            if (Button(new Rect(x, y, bw, bh), "Play")) Play();
            if (Button(new Rect(x, y + gap, bw, bh), "Host co-op (Steam)", Session.SteamOk)) Host(false);
            if (Button(new Rect(x, y + gap * 2, bw, bh), "Join")) { screen = Screen.Join; Refresh(); }
            if (Button(new Rect(x, y + gap * 3, bw, bh), "Quit")) Application.Quit();
            Label(new Rect(20 * S, H - 40 * S, W, 30 * S), Session.SteamOk ? $"Steam: {Steamworks.SteamClient.Name} · app 480 (test)" : "Steam: offline", small, new Color(1, 1, 1, 0.6f));
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
            if (g != null)
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
                Label(new Rect(W * 0.1f, H * 0.13f, W * 0.8f, 140 * S), shout, shoutS, new Color(1f, 0.96f, 0.86f, a));
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
                if (pl.Pushing) hint = "F  let go of trolley";
                else if (pl.Carrying < Player.MaxStack && pl.FindCrate() != null) hint = pl.Carrying == 0 ? "E  grab crate" : "E  stack another";
                else if (pl.Carrying == 0 && Trolley.I != null && Vector3.Distance(Trolley.I.Handle, pl.transform.position + Vector3.up) < 2.2f) hint = "F  push trolley";
                if (pl.Carrying > 0) Label(new Rect(0, H - 150 * S, W, 40 * S), $"Carrying {pl.Carrying}/{Player.MaxStack}   ·   Q drop   ·   Left click throw", mid, new Color(1, 1, 1, 0.85f));
                if (hint != null) Label(new Rect(0, H * 0.56f, W, 40 * S), hint, mid, new Color(1f, 0.92f, 0.7f));
            }
            if (showHelp)
                Label(new Rect(24 * S, H - 190 * S, 640 * S, 180 * S),
                    "WASD move · Shift sprint · Space jump · Mouse look\nE / Right click: grab, stack (max 5)\nQ: drop one · Left click: throw · F: push trolley\nDrop crates on the buyer's table to sell · H hide · Esc menu", small, new Color(1, 1, 1, 0.75f));
        }

        void DrawNight(float W, float H)
        {
            var g = PortGame.I;
            float pw = 720 * S, ph = 680 * S;
            var r = new Rect((W - pw) / 2, (H - ph) / 2, pw, ph);
            GUI.Box(r, "", panel);
            Label(new Rect(r.x, r.y + 20 * S, pw, 70 * S), $"End of Day {g.DayNum.Value}", new GUIStyle(big) { fontSize = Mathf.RoundToInt(52 * S) }, new Color(1f, 0.9f, 0.7f));
            var st = new GUIStyle(mid) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Normal };
            float y = r.y + 110 * S, x = r.x + 70 * S, lh = 48 * S;
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

        void DrawPause(float W, float H)
        {
            GUI.Box(new Rect(0, 0, W, H), "", panel);
            Label(new Rect(0, H * 0.2f, W, 90 * S), "Paused", big, Color.white);
            float bw = 380 * S, bh = 64 * S, x = (W - bw) / 2, y = H * 0.38f, gap = 80 * S;
            if (Button(new Rect(x, y, bw, bh), "Resume")) SetPaused(false);
            if (Session.Lobby != null && Button(new Rect(x, y + gap, bw, bh), "Invite Steam friends")) Session.Invite();
            if (Button(new Rect(x, y + gap * 2, bw, bh), "Quit to menu")) BackToMenu();
            if (Button(new Rect(x, y + gap * 3, bw, bh), "Quit game")) Application.Quit();
        }
    }
}
