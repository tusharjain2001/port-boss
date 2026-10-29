using UnityEngine;

namespace PortBoss
{
    // Character editor: before the first game, and from the title menu and the pause menu. A live 3D preview of our dock
    // worker (rendered off-stage into a texture) on a steel panel, the choices on a clipboard. Saved on this PC; in a game
    // the host checks it and every peer sees it (Player.LookNet).
    public partial class GameUI
    {
        bool charOpen;
        System.Action charThen;       // what Save goes on to (start the game), null = just close
        Look charLook;
        Worker preview, menuLook;
        Camera previewCam;
        RenderTexture previewRT;
        float previewYaw = -25f, previewSpin;
        bool dragging;
        const int PreviewLayer = 30;
        static readonly Vector3 PreviewAt = new(0, -700, 0);
        static readonly System.Random lookDice = new();

        public RenderTexture RenderPreview() { if (previewCam != null) previewCam.Render(); return previewRT; } // tests: grab a frame now
        public void DebugLook(Look l) { charLook = l; previewYaw = -25f; previewSpin = 0; if (preview != null) { preview.Apply(l); preview.transform.parent.rotation = Quaternion.Euler(0, previewYaw, 0); } }

        int LookSlot => Player.Local != null ? (int)Player.Local.OwnerClientId : 0;

        // Play / Host / Join go through here: the very first time, the player makes their worker first.
        void FirstLook(System.Action go) { if (Look.Saved || AutoRun.Active) go(); else OpenCharacter(go); }

        public void OpenCharacter(System.Action then = null)
        {
            charOpen = true; charThen = then;
            charLook = Player.Local != null && Player.Local.LookNet.Value.Set != 0 ? Player.Local.LookNet.Value : Look.Load(LookSlot);
            PreviewStage();
            if (preview != null) preview.Apply(charLook);
        }

        void CloseCharacter(bool save)
        {
            charOpen = false;
            if (previewCam != null) previewCam.enabled = false;
            if (save)
            {
                charLook.Set = 1; charLook.Save();
                if (Player.Local != null) Player.Local.SetLook(charLook);
                if (menuLook != null) menuLook.Apply(charLook);
                Debug.Log("[PB] look saved " + charLook);
                var then = charThen; charThen = null; then?.Invoke();
            }
            charThen = null;
        }

        void PreviewStage()
        {
            if (previewCam != null) { previewCam.enabled = true; return; }
            if (Skin.I == null || Skin.I.Worker == null) return;
            var root = new GameObject("CharacterPreview").transform;
            root.position = PreviewAt;
            preview = Worker.Make(Skin.I.Worker, root);
            var an = preview.GetComponentInChildren<Animator>();
            if (an != null) an.gameObject.AddComponent<CharAnim>().Init(an, Skin.I);
            // A studio key light of its own (short range: nothing else is near), so the preview reads at any time of day.
            var key = new GameObject("PreviewKey").AddComponent<Light>();
            key.type = LightType.Point; key.range = 9; key.intensity = 6f; key.color = new Color(1f, 0.95f, 0.88f); key.shadows = LightShadows.None;
            key.transform.SetParent(root, false); key.transform.position = PreviewAt + new Vector3(-1.6f, 2.6f, 2.6f);
            key.transform.SetParent(null, true); // stays put while the worker turns
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = PreviewLayer;
            previewRT = new RenderTexture(720, 1000, 24) { antiAliasing = 4, name = "CharacterPreview" };
            previewCam = new GameObject("PreviewCam").AddComponent<Camera>();
            previewCam.transform.SetPositionAndRotation(PreviewAt + new Vector3(0, 1.08f, 3.9f), Quaternion.LookRotation(new Vector3(0, -0.07f, -1)));
            previewCam.fieldOfView = 30; previewCam.nearClipPlane = 0.1f; previewCam.farClipPlane = 20;
            previewCam.cullingMask = 1 << PreviewLayer;
            previewCam.clearFlags = CameraClearFlags.SolidColor; previewCam.backgroundColor = new Color(0.2f, 0.25f, 0.29f, 1);
            previewCam.targetTexture = previewRT; previewCam.depth = -11;
            cam.cullingMask &= ~(1 << PreviewLayer);
        }

        // The worker idling on the quay in the opening shot wears your look.
        GameObject OurMenuWorker(Vector3 at, float yaw)
        {
            var root = new GameObject("menuWorker").transform;
            root.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            menuLook = Worker.Make(Skin.I.Worker, root);
            menuLook.Apply(Look.Load(0));
            var an = menuLook.GetComponentInChildren<Animator>();
            if (an != null) an.gameObject.AddComponent<CharAnim>().Init(an, Skin.I);
            return root.gameObject;
        }

        void DrawCharacter(float W, float H)
        {
            ThemeTextures();
            GUI.DrawTexture(new Rect(0, 0, W, H), dimTex);
            float ph = Mathf.Min(H - 120 * S, 860 * S), pw = ph * 0.68f, cw = Mathf.Min(W - pw - 140 * S, 820 * S);
            float x0 = (W - pw - cw - 40 * S) / 2, y0 = (H - ph) / 2 - 20 * S;

            // Preview: drag to turn it, otherwise it turns slowly by itself.
            var pr = new Rect(x0, y0, pw, ph);
            SteelPanel(pr);
            var view = new Rect(pr.x + 18 * S, pr.y + 40 * S, pr.width - 36 * S, pr.height - 58 * S);
            if (previewRT != null)
            {
                var e = Event.current;
                if (e.type == EventType.MouseDown && view.Contains(e.mousePosition)) dragging = true;
                if (e.type == EventType.MouseUp) dragging = false;
                if (e.type == EventType.MouseDrag && dragging) { previewYaw -= e.delta.x * 0.5f; previewSpin = 0; }
                if (e.type == EventType.Repaint)
                {
                    if (!dragging) { previewSpin = Mathf.Min(1, previewSpin + Time.unscaledDeltaTime * 0.5f); previewYaw += 22f * previewSpin * Time.unscaledDeltaTime; }
                    preview.transform.parent.rotation = Quaternion.Euler(0, previewYaw, 0);
                }
                GUI.DrawTexture(view, previewRT, ScaleMode.ScaleAndCrop, false);
                var hint = new GUIStyle(small) { fontSize = Mathf.RoundToInt(16 * S), alignment = TextAnchor.LowerCenter };
                Label(new Rect(view.x, view.y, view.width, view.height - 10 * S), "drag to turn", hint, new Color(1, 1, 1, 0.55f));
            }
            else Label(view, "(preview needs the Toon Harbor art)", small, Color.white);

            // Choices: a cargo-manifest style sheet.
            var board = new Rect(pr.xMax + 40 * S, y0, cw, ph);
            var paper = ClipboardPaper(board, "DOCK WORKER  ·  ID CARD");
            var ink = new Color(0.13f, 0.15f, 0.2f);
            var lab = new GUIStyle(small) { fontSize = Mathf.RoundToInt(21 * S), font = TitleFont, alignment = TextAnchor.MiddleLeft };
            lab.normal.textColor = ink;
            var val = new GUIStyle(lab) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(22 * S) };
            float rh = (paper.height - 170 * S) / 10f, y = paper.y + 76 * S, lx = paper.x + 26 * S, lw = 150 * S;
            float vx = lx + lw, vw = 230 * S, sx = vx + vw + 24 * S, sw = paper.xMax - sx - 22 * S;
            var l = charLook;
            bool changed = false;

            void Row(string name, ref byte v, string[] names, ref byte col, Color[] cols, bool show = true)
            {
                if (!show) return;
                var r = new Rect(lx, y, paper.width - 48 * S, rh);
                GUI.Label(new Rect(lx, y, lw, rh), name, lab);
                if (names != null)
                {
                    var bw = 38 * S;
                    if (Button(new Rect(vx, r.center.y - bw / 2, bw, bw), "<")) { v = (byte)((v + names.Length - 1) % names.Length); changed = true; }
                    GUI.Label(new Rect(vx + bw, y, vw - 2 * bw, rh), names[v], OneLine(val, names[v], vw - 2 * bw - 6 * S));
                    if (Button(new Rect(vx + vw - bw, r.center.y - bw / 2, bw, bw), ">")) { v = (byte)((v + 1) % names.Length); changed = true; }
                }
                if (cols != null)
                {
                    float sz = Mathf.Min(rh * 0.62f, (sw - (cols.Length - 1) * 4 * S) / cols.Length);
                    for (int i = 0; i < cols.Length; i++)
                    {
                        var c = new Rect(sx + i * (sz + 4 * S), r.center.y - sz / 2, sz, sz);
                        if (i == col) GUI.DrawTexture(new Rect(c.x - 3 * S, c.y - 3 * S, c.width + 6 * S, c.height + 6 * S), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, ink, 0, 5 * S);
                        GUI.DrawTexture(c, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, cols[i], 0, 4 * S);
                        if (GUI.Button(c, GUIContent.none, GUIStyle.none)) { col = (byte)i; changed = true; Sfx.Play2D("blip", 0.5f); }
                    }
                }
                GUI.DrawTexture(new Rect(lx, y + rh - 1 * S, paper.width - 48 * S, 1 * S), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0.3f, 0.45f, 0.7f, 0.35f), 0, 0);
                y += rh;
            }
            byte none = 0;
            Row("Face", ref l.Face, Look.Faces, ref l.SkinTone, Look.SkinTones);
            Row("Hair", ref l.Hair, Look.Hairs, ref l.HairColor, Look.HairColors);
            Row("Beard", ref l.Beard, Look.Beards, ref none, null);
            Row("Headwear", ref l.Hat, Look.Hats, ref l.HatColor, Look.Cloth);
            Row("Top", ref l.Top, Look.Tops, ref l.TopColor, Look.Cloth);
            Row(l.Top == 2 ? "Overalls" : "Vest", ref none, null, ref l.AccentColor, Look.Cloth, l.Top == 1 || l.Top == 2);
            Row("Trousers", ref none, null, ref l.LegsColor, Look.Cloth, l.Top != 2);
            Row("Boots", ref none, null, ref l.BootsColor, Look.Cloth);
            Row("Gloves", ref l.Gloves, Look.GloveNames, ref l.GloveColor, Look.Cloth);

            float by = paper.yMax - 80 * S, bw2 = (paper.width - 70 * S) / 3;
            if (Button(new Rect(paper.x + 20 * S, by, bw2, 56 * S), "Randomise")) { l = Look.Random(lookDice); changed = true; }
            if (Button(new Rect(paper.x + 30 * S + bw2, by, bw2, 56 * S), "Back")) { CloseCharacter(false); return; }
            if (Button(new Rect(paper.x + 40 * S + 2 * bw2, by, bw2, 56 * S), charThen != null ? "Save & go" : "Save")) { charLook = l; CloseCharacter(true); return; }
            if (changed) { charLook = l; if (preview != null) preview.Apply(l); }
        }
    }
}
