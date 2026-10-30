using PortBoss.Net;
using Unity.Netcode;
using UnityEngine;

namespace PortBoss
{
    // The front door: a title camera cutting between flyovers of the harbour, a logo painted on a real container,
    // the menu as a cargo manifest on a clipboard, steel container-door panels for Settings/Join, bolted plate buttons.
    public partial class GameUI
    {
        // ---------- title scene ----------
        // (from, to, look-from, look-to): each shot is a slow straight move; cuts dip to black.
        static readonly (Vector3 p0, Vector3 p1, Vector3 l0, Vector3 l1)[] TitleShots = {
            (new Vector3(-13, 2.2f, -12), new Vector3(-9, 2.6f, -11), new Vector3(10, 3, 22), new Vector3(14, 3, 24)),      // our berth, the worker
            (new Vector3(125, 38, -45), new Vector3(125, 42, 35), new Vector3(185, 0, 40), new Vector3(185, 0, 120)),        // over the East yard
            (new Vector3(30, 9, 60), new Vector3(70, 12, 120), new Vector3(165, 8, 150), new Vector3(200, 8, 260)),        // down the basin past the freighter
            (new Vector3(-70, 55, -80), new Vector3(-30, 60, -70), new Vector3(60, 0, 110), new Vector3(90, 0, 130)),      // the whole harbour
        };
        const float ShotLen = 14f;
        public static float TitleTime = -1; // tests pin the title clock
        float titleFade;
        GameObject menuWorker;
        public void DebugScreen(int s) { screen = (Screen)s; if (s == 0) settingsFromPause = false; }
        public void DebugPause() { SetPaused(true); settingsFromPause = false; }

        void TitleScene(NetworkManager nm)
        {
            if (!Skin.On)
            {
                // Own-art fallback: a slow orbit over the pier.
                float a = Time.unscaledTime * 0.035f;
                var focus = new Vector3(3, 1.2f, 15 + Mathf.Sin(a * 0.7f) * 4);
                cam.transform.position = new Vector3(-16 + Mathf.Sin(a) * 9, 5.5f + Mathf.Sin(a * 1.3f) * 1.2f, -6 + Mathf.Cos(a) * 5);
                cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(focus - cam.transform.position), 1 - Mathf.Exp(-2f * Time.unscaledDeltaTime));
                return;
            }
            if (menuWorker == null && !nm.IsListening) menuWorker = MenuWorker();
            float tt = TitleTime >= 0 ? TitleTime : Time.unscaledTime;
            int si = (int)(tt / ShotLen) % TitleShots.Length;
            float local = tt % ShotLen, u = local / ShotLen;
            var s = TitleShots[si];
            cam.transform.position = Vector3.Lerp(s.p0, s.p1, u);
            cam.transform.rotation = Quaternion.LookRotation(Vector3.Lerp(s.l0, s.l1, u) - cam.transform.position);
            titleFade = TitleTime >= 0 ? 0 : Mathf.Clamp01(1 - Mathf.Min(local, ShotLen - local) / 0.7f);
        }

        // The player's dock worker, idling on the quay in the opening shot.
        GameObject MenuWorker()
        {
            if (Skin.I.Worker != null) return OurMenuWorker(new Vector3(-5.2f, 0, -6.5f), 215); // ours faces the other way to the pack's
            var w = Art.Place(Skin.Pick(Skin.I.Players, 0), null, new Vector3(-5.2f, 0, -6.5f), 35, 1f);
            if (w == null) return null;
            var an = w.GetComponentInChildren<Animator>();
            if (an != null) an.gameObject.AddComponent<CharAnim>().Init(an, Skin.I);
            return w;
        }

        // ---------- logo: PORT BOSS stencilled on a real pack container, rendered off-stage ----------
        RenderTexture logoRT;
        Camera logoCam;
        const int LogoLayer = 31;

        public RenderTexture Logo()
        {
            if (logoRT != null || !Skin.On || TitleFont == null) return logoRT;
            var root = new GameObject("Logo").transform;
            root.position = new Vector3(0, -400, 0);
            var c = Art.Place(Skin.Get("cont1"), root, Vector3.zero, 90, 1f);
            if (c == null) return null;
            var b = Art.Bounds(c);
            float face = b.min.z - 0.03f;
            void Paint(string s, Vector2 at, float width, Color col, TextAnchor anchor, float z = 0)
            {
                var t = new GameObject("paint_" + s).transform;
                t.SetParent(root, false);
                var tm = t.gameObject.AddComponent<TextMesh>();
                tm.font = TitleFont; tm.fontSize = 96; tm.characterSize = 0.1f; tm.anchor = anchor; tm.alignment = TextAlignment.Center;
                tm.text = s; tm.color = col;
                t.GetComponent<MeshRenderer>().sharedMaterial = TitleFont.material;
                float w = Mathf.Max(0.01f, t.GetComponent<MeshRenderer>().bounds.size.x);
                t.localScale = Vector3.one * (width / w);
                t.position = new Vector3(b.center.x + at.x, b.center.y + at.y, face - z);
            }
            // Name in cream paint with a worn dark edge; real-container markings in the corners.
            Paint("PORT BOSS", new Vector2(0.05f, 0.11f), b.size.x * 0.84f, new Color(0.22f, 0.08f, 0.02f, 0.95f), TextAnchor.MiddleCenter);
            Paint("PORT BOSS", new Vector2(0, 0.16f), b.size.x * 0.84f, new Color(1f, 0.42f, 0.02f), TextAnchor.MiddleCenter, 0.01f);
            Paint("HARBOUR CO-OP  ·  BERTH 7", new Vector2(0, -0.82f), b.size.x * 0.42f, new Color(0.98f, 0.93f, 0.8f, 0.9f), TextAnchor.MiddleCenter, 0.01f);
            Paint("PBSU 290726 [1]", new Vector2(-b.size.x * 0.33f, b.size.y * 0.36f), b.size.x * 0.2f, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleCenter, 0.01f);
            Paint("MAX GROSS 30,480 KG", new Vector2(b.size.x * 0.32f, -b.size.y * 0.36f), b.size.x * 0.22f, new Color(1, 1, 1, 0.75f), TextAnchor.MiddleCenter, 0.01f);
            foreach (var tr in root.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = LogoLayer;
            // Turned 15 degrees so the end face shows and the corrugations catch the light; framed exactly on the silhouette.
            const float turn = 15f;
            float projW = b.size.x * Mathf.Cos(turn * Mathf.Deg2Rad) + b.size.z * Mathf.Sin(turn * Mathf.Deg2Rad), aspect = projW / b.size.y;
            logoRT = new RenderTexture(2048, Mathf.RoundToInt(2048 / aspect), 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            logoCam = new GameObject("LogoCam").AddComponent<Camera>();
            logoCam.orthographic = true;
            logoCam.orthographicSize = b.size.y * 0.5f;
            logoCam.aspect = aspect;
            var look = Quaternion.Euler(0, turn, 0);
            logoCam.transform.SetPositionAndRotation(b.center - look * Vector3.forward * 12f, look);
            logoCam.cullingMask = 1 << LogoLayer;
            logoCam.clearFlags = CameraClearFlags.SolidColor; logoCam.backgroundColor = new Color(0.2f, 0.1f, 0.08f, 1);
            logoCam.targetTexture = logoRT;
            logoCam.depth = -10;
            // Turn the rig so the painted side faces the (low, golden) sun.
            logoCam.transform.SetParent(root, true);
            var toSun = sun != null ? -sun.transform.forward : Vector3.back; toSun.y = 0;
            if (toSun.sqrMagnitude > 0.01f) root.rotation = Quaternion.LookRotation(-toSun.normalized);
            cam.cullingMask &= ~(1 << LogoLayer);
            return logoRT;
        }

        // ---------- theme pieces ----------
        Texture2D steelTex, hazardTex, plateTex, plateHot, dimTex;

        void ThemeTextures()
        {
            if (steelTex != null) return;
            var rng = new System.Random(7);
            // Container-door steel: vertical corrugations, a few rust freckles.
            steelTex = new Texture2D(256, 128) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < 256; x++)
                for (int y = 0; y < 128; y++)
                {
                    float v = Mathf.Abs(x % 16 - 8) / 8f, k = 0.78f + 0.3f * (1 - v * v);
                    var c = new Color(0.1f * k, 0.19f * k, 0.25f * k, 0.94f);
                    if (rng.NextDouble() < 0.012) c = Color.Lerp(c, new Color(0.45f, 0.24f, 0.12f, 0.94f), 0.6f);
                    steelTex.SetPixel(x, y, c);
                }
            steelTex.Apply();
            hazardTex = new Texture2D(32, 16) { wrapMode = TextureWrapMode.Repeat };
            for (int x = 0; x < 32; x++) for (int y = 0; y < 16; y++) hazardTex.SetPixel(x, y, (x + y) / 8 % 2 == 0 ? new Color(0.98f, 0.76f, 0.1f) : new Color(0.1f, 0.1f, 0.11f));
            hazardTex.Apply();
            plateTex = Plate(new Color(0.95f, 0.72f, 0.16f)); plateHot = Plate(new Color(1f, 0.84f, 0.32f));
            dimTex = Tex(new Color(0.02f, 0.04f, 0.06f, 0.6f));
        }

        // A painted steel plate with a bolt in each corner (9-sliced, so bolts stay put).
        static Texture2D Plate(Color c)
        {
            var t = new Texture2D(48, 32) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < 48; x++)
                for (int y = 0; y < 32; y++)
                {
                    bool edge = x < 2 || y < 2 || x > 45 || y > 29;
                    var col = edge ? c * 0.55f : c * (0.92f + 0.08f * y / 31f);
                    foreach (var (bx, by) in new[] { (6, 6), (41, 6), (6, 25), (41, 25) })
                        if ((x - bx) * (x - bx) + (y - by) * (y - by) <= 4) col = new Color(0.3f, 0.28f, 0.25f);
                    col.a = 1; t.SetPixel(x, y, col);
                }
            t.Apply();
            return t;
        }

        void Hazard(Rect r) => GUI.DrawTextureWithTexCoords(r, hazardTex, new Rect(0, 0, r.width / (r.height * 2), 1));

        // Settings / Join: a steel container-door panel with a hazard-striped header and rivets.
        void SteelPanel(Rect r)
        {
            ThemeTextures();
            GUI.DrawTexture(new Rect(r.x + 8 * S, r.y + 10 * S, r.width, r.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0, 0, 0, 0.35f), 0, 10 * S);
            GUI.DrawTexture(r, steelTex, ScaleMode.StretchToFill, true, 0, Color.white, 0, 8 * S);
            Hazard(new Rect(r.x, r.y, r.width, 14 * S));
            for (int i = 0; i < 2; i++)
                for (float x = r.x + 18 * S; x < r.xMax - 10 * S; x += (r.width - 36 * S) / 9f)
                    GUI.DrawTexture(new Rect(x - 3 * S, (i == 0 ? r.y + 24 * S : r.yMax - 14 * S), 6 * S, 6 * S), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.55f, 0.6f, 0.62f, 0.8f), 0, 3 * S);
        }

        // A clipboard with a cargo manifest; returns the clicked row or -1.
        int Clipboard(Rect r, string header, (string item, string note, bool on)[] rows)
        {
            var ink = new Color(0.13f, 0.15f, 0.2f);
            var paper = ClipboardPaper(r, header);
            var hs = new GUIStyle(small) { fontSize = Mathf.RoundToInt(19 * S), font = TitleFont, alignment = TextAnchor.MiddleLeft };
            var it = new GUIStyle(menuItem) { fontSize = Mathf.RoundToInt(36 * S) };
            var no = new GUIStyle(hs) { fontSize = Mathf.RoundToInt(17 * S), fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleRight };
            int hit = -1;
            float rowH = (paper.yMax - paper.y - 86 * S) / rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                var row = new Rect(paper.x + 14 * S, paper.y + 76 * S + i * rowH, paper.width - 28 * S, rowH);
                bool hot = rows[i].on && row.Contains(Event.current.mousePosition);
                if (hot) GUI.DrawTexture(new Rect(row.x + 50 * S, row.center.y - 18 * S, row.width - 60 * S, 36 * S), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(1f, 0.85f, 0.15f, 0.55f), 0, 4 * S);
                hs.normal.textColor = new Color(0.55f, 0.18f, 0.12f, rows[i].on ? 0.9f : 0.4f);
                GUI.Label(new Rect(row.x + 8 * S, row.y, 50 * S, row.height), $"{i + 1:00}", hs);
                it.normal.textColor = rows[i].on ? ink : new Color(ink.r, ink.g, ink.b, 0.35f);
                GUI.Label(new Rect(row.x + 62 * S + (hot ? 10 * S : 0), row.y, row.width, row.height), rows[i].item, it);
                if (!rows[i].on) GUI.DrawTexture(new Rect(row.x + 58 * S, row.center.y, 230 * S, 3 * S), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.55f, 0.18f, 0.12f, 0.8f), 0, 0);
                no.normal.textColor = new Color(ink.r, ink.g, ink.b, 0.55f);
                GUI.Label(new Rect(row.x, row.y, row.width - 10 * S, row.height), rows[i].note, no);
                GUI.DrawTexture(new Rect(row.x + 50 * S, row.yMax - 1 * S, row.width - 50 * S, 1 * S), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.3f, 0.45f, 0.7f, 0.35f), 0, 0);
                Hover(row, header + i, rows[i].on);
                if (rows[i].on && GUI.Button(row, GUIContent.none, GUIStyle.none)) { Sfx.Play2D("blip", 0.7f); hit = i; }
            }
            return hit;
        }

        // The clipboard itself (board, paper, clip, red header + rule); returns the paper's rect.
        Rect ClipboardPaper(Rect r, string header)
        {
            GUI.DrawTexture(new Rect(r.x + 10 * S, r.y + 14 * S, r.width, r.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0, 0, 0, 0.4f), 0, 18 * S);
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.45f, 0.3f, 0.17f), 0, 18 * S);
            var paper = new Rect(r.x + 20 * S, r.y + 34 * S, r.width - 40 * S, r.height - 54 * S);
            GUI.DrawTexture(paper, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.97f, 0.94f, 0.85f), 0, 3 * S);
            // The clip.
            var clip = new Rect(r.center.x - 95 * S, r.y + 8 * S, 190 * S, 50 * S);
            GUI.DrawTexture(clip, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.62f, 0.64f, 0.66f), 0, 10 * S);
            GUI.DrawTexture(new Rect(clip.x + 14 * S, clip.y + 10 * S, clip.width - 28 * S, 14 * S), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.38f, 0.4f, 0.42f), 0, 7 * S);
            var hs = new GUIStyle(small) { fontSize = Mathf.RoundToInt(19 * S), font = TitleFont, alignment = TextAnchor.MiddleLeft };
            hs.normal.textColor = new Color(0.55f, 0.18f, 0.12f);
            GUI.Label(new Rect(paper.x + 22 * S, paper.y + 30 * S, paper.width, 30 * S), header, hs);
            GUI.DrawTexture(new Rect(paper.x + 18 * S, paper.y + 64 * S, paper.width - 36 * S, 2 * S), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.55f, 0.18f, 0.12f, 0.6f), 0, 0);
            return paper;
        }

        void DrawMenu(float W, float H)
        {
            ThemeTextures();
            GUI.DrawTexture(new Rect(0, 0, W * 0.5f, H), fadeTex);
            float x = 80 * S;
            var logo = Logo();
            if (logo != null)
            {
                // The container, hung slightly crooked, as if on a crane hook.
                var lr = new Rect(x, 46 * S, 600 * S, 600 * S * logo.height / logo.width);
                var m = GUI.matrix;
                GUIUtility.RotateAroundPivot(-2f + Mathf.Sin(Time.unscaledTime * 0.6f) * 0.6f, new Vector2(lr.center.x, lr.y - 40 * S));
                GUI.DrawTexture(new Rect(lr.x + 10 * S, lr.y + 14 * S, lr.width, lr.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0, 0, 0, 0.35f), 0, 6 * S);
                GUI.DrawTexture(lr, logo);
                GUI.matrix = m;
            }
            else Label(new Rect(x, 90 * S, W, 150 * S), "PORT BOSS", title, new Color(0.98f, 0.79f, 0.38f));

            var board = new Rect(x + 20 * S, 380 * S, 540 * S, 560 * S);
            var m2 = GUI.matrix;
            GUIUtility.RotateAroundPivot(1.2f, board.center);
            int pick = Clipboard(board, "CARGO MANIFEST  ·  SHIFT 1", new[] {
                ("Play", "solo shift", true),
                ("Host co-op", Session.SteamOk ? "steam lobby" : "needs steam", Session.SteamOk),
                ("Join a friend", "steam / lan", true),
                ("Character", "your dock worker", true),
                ("Settings", "", true),
                ("Quit", "clock off", true) });
            GUI.matrix = m2;
            if (pick == 0) FirstLook(Play);
            else if (pick == 1) FirstLook(() => Host(false));
            else if (pick == 2) FirstLook(() => { screen = Screen.Join; Refresh(); });
            else if (pick == 3) OpenCharacter();
            else if (pick == 4) { settingsFromPause = false; screen = Screen.Settings; }
            else if (pick == 5) Application.Quit();
            BottomBar(W, H);
            if (titleFade > 0) GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0, 0, 0, titleFade), 0, 0);
        }

        void BottomBar(float W, float H)
        {
            var bar = new Rect(0, H - 40 * S, W, 40 * S);
            GUI.DrawTexture(bar, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.03f, 0.05f, 0.07f, 0.8f), 0, 0);
            Hazard(new Rect(0, bar.y - 4 * S, W, 4 * S));
            var st = new GUIStyle(small) { fontSize = Mathf.RoundToInt(17 * S), alignment = TextAnchor.MiddleLeft };
            GUI.DrawTexture(new Rect(24 * S, bar.center.y - 6 * S, 12 * S, 12 * S), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Session.SteamOk ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.9f, 0.45f, 0.3f), 0, 6 * S);
            st.normal.textColor = new Color(1, 1, 1, 0.75f);
            GUI.Label(new Rect(46 * S, bar.y, W, bar.height), Session.SteamOk ? $"STEAM  ·  {Steamworks.SteamClient.Name}  ·  co-op ready" : "STEAM OFFLINE  ·  solo works, co-op needs Steam", st);
            st.alignment = TextAnchor.MiddleRight; st.normal.textColor = new Color(1, 1, 1, 0.5f);
            GUI.Label(new Rect(0, bar.y, W - 24 * S, bar.height), $"v{Application.version}  ·  early build", st);
        }

        void DrawPause(float W, float H)
        {
            ThemeTextures();
            GUI.DrawTexture(new Rect(0, 0, W, H), dimTex);
            var rows = new System.Collections.Generic.List<(string, string, bool)> { ("Resume", "back to work", true) };
            if (Session.Lobby != null) rows.Add(("Invite friends", "steam", true));
            rows.Add(("Character", "your look", true)); rows.Add(("Settings", "", true)); rows.Add(("Stats", "career", true)); rows.Add(("Quit to menu", "", true)); rows.Add(("Quit game", "clock off", true));
            var board = new Rect((W - 520 * S) / 2, H * 0.16f, 520 * S, 120 * S + rows.Count * 74 * S);
            int pick = Clipboard(board, Session.Solo ? "SHIFT ON HOLD" : "SHIFT ON HOLD  ·  THE PORT KEEPS RUNNING", rows.ToArray());
            if (pick < 0) return;
            string what = rows[pick].Item1;
            if (what == "Resume") SetPaused(false);
            else if (what == "Invite friends") Session.Invite();
            else if (what == "Character") OpenCharacter();
            else if (what == "Settings") settingsFromPause = true;
            else if (what == "Stats") statsOpen = true;
            else if (what == "Quit to menu") BackToMenu();
            else Application.Quit();
        }
    }
}
