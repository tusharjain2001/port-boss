using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PortBoss.M1
{
    // M5: the warehouse (systems §1.1.6): the company's one store, E at its door. Left: what's on you (your hotbar's
    // fish, one row per stack: Store / Store all). Right: the racks (12, 24 from T1) and the cold room (12 from T2), a slot
    // = 10 fish of one species + grade, its freshness (the cold room rots at 25%). Click a slot: take one; Shift: the lot.
    public class StoreUi : MonoBehaviour
    {
        public static StoreUi I;
        public static bool Open => I != null && I.root != null && I.root.gameObject.activeSelf;
        static Company C => Firm.C;
        const float W = 1360, H = 820, Pad = 34;
        RectTransform root, left, rack; Text title, sub; Camera cam;
        readonly List<(RectTransform r, Action a)> btns = new(); int focus = -1; string key;

        public static void Create() { var g = new GameObject("StoreUi"); I = g.AddComponent<StoreUi>(); I.Build(); }
        static Text T(RectTransform p, string s, Font f, int size, Color c, float x, float y, float w, float h, TextAnchor a = TextAnchor.UpperLeft)
        { var t = UiKit.Txt(p, s, f, size, c, a); t.rectTransform.Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h), new Vector2(0, 1)); return t; }
        void Build()
        {
            var hud = (RectTransform)Hud.I.transform; cam = CameraRig.I != null ? CameraRig.I.UiCam : null;
            root = UiKit.Card(hud, "warehouse", 32, Ponyo.Paper, true, Ponyo.Line, 6); root.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(W, H));
            var ic = UiKit.Card(root, "icon", 38, Ponyo.Hex("E4F1DC"), false, Ponyo.Line, 0); ic.Place(new Vector2(0, 1), new Vector2(Pad, -30), new Vector2(76, 76), new Vector2(0, 1));
            UiKit.Img(ic, Glyphs.Get("crate"), Ponyo.Ink, "g").rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44));
            title = T(root, "", UiKit.DisplayBold, 40, Ponyo.Ink, Pad + 96, 30, 800, 50); sub = T(root, "", UiKit.Body, 17, Ponyo.InkSoft, Pad + 98, 82, 900, 24);
            var esc = UiKit.Key(root, "Esc", 30); esc.Place(new Vector2(1, 1), new Vector2(-Pad - 64, -40), esc.sizeDelta, new Vector2(1, 1));
            T(root, "Leave", UiKit.Body, 16, Ponyo.InkSoft, W - Pad - 56, 44, 60, 24);
            left = UiKit.Rect("on you", root).Place(new Vector2(0, 1), new Vector2(Pad, -130), new Vector2(420, H - 160), new Vector2(0, 1));
            rack = UiKit.Rect("racks", root).Place(new Vector2(0, 1), new Vector2(Pad + 450, -130), new Vector2(W - 2 * Pad - 450, H - 160), new Vector2(0, 1));
            root.gameObject.SetActive(false);
        }
        public static void Show() => I?.ShowI();
        void ShowI()
        {
            C.EnsureWarehouse(); root.gameObject.SetActive(true); Ctl.Ui = 2; key = null; focus = -1;
            if (!Application.isBatchMode) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            Snd.Play2D(Synth.Creak, 0.3f, 0.9f);
        }
        public void Hide()
        {
            if (!Open) return; root.gameObject.SetActive(false); Ctl.Ui = 0;
            if (!Application.isBatchMode && !Game.Paused) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }
        void Update()
        {
            if (!Open) return;
            var kb = Keyboard.current; var pad = Gamepad.current; var me = Angler.I; var door = Works.I?.WarehouseDoor;
            if (Ctl.Escape || kb != null && kb.escapeKey.wasPressedThisFrame || pad != null && pad.buttonEast.wasPressedThisFrame || me == null || door.HasValue && Vector3.Distance(door.Value, me.transform.position) > 4f)
            { Ctl.UiEsc = Time.frameCount; Hide(); return; }
            var sb = new System.Text.StringBuilder(); foreach (var s in me.Bar.S) sb.Append(s.Fish.Count).Append(';'); foreach (var s in C.Wh) sb.Append(s.Fish.Count).Append(',');
            string k = sb.Append(C.WhTier).Append((int)(Sky.Minutes / 30)).ToString();
            if (k != key) { key = k; Draw(); }
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var mp = mouse.position.ReadValue();
                if (mouse.delta.ReadValue().sqrMagnitude > 0) for (int i = 0; i < btns.Count; i++) if (Hit(btns[i].r, mp)) { if (focus != i) { focus = i; Mark(); } break; }
                if (mouse.leftButton.wasPressedThisFrame) for (int i = 0; i < btns.Count; i++) if (Hit(btns[i].r, mp)) { Press(i); return; }
            }
            int dy = 0, dx = 0;
            if (kb != null) { if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) dy = -1; if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) dy = 1; if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) dx = -1; if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) dx = 1; }
            if (pad != null) { if (pad.dpad.up.wasPressedThisFrame) dy = -1; if (pad.dpad.down.wasPressedThisFrame) dy = 1; if (pad.dpad.left.wasPressedThisFrame) dx = -1; if (pad.dpad.right.wasPressedThisFrame) dx = 1; }
            if ((dx != 0 || dy != 0) && btns.Count > 0)
            {
                if (focus < 0) focus = 0;
                else
                {
                    var at = Centre(btns[focus].r); var move = new Vector2(dx, -dy); int best = -1; float bd = 1e9f;
                    for (int i = 0; i < btns.Count; i++) { if (i == focus) continue; var d = Centre(btns[i].r) - at; if (Vector2.Dot(d.normalized, move) < 0.5f) continue; float s = d.magnitude * (1 + 2 * Mathf.Abs(Vector2.Dot(d.normalized, new Vector2(move.y, -move.x)))); if (s < bd) { bd = s; best = i; } }
                    if (best >= 0) focus = best;
                }
                Mark();
            }
            if ((kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) || pad != null && pad.buttonSouth.wasPressedThisFrame) && focus >= 0 && focus < btns.Count) Press(focus);
        }
        bool Hit(RectTransform r, Vector2 sp) => r != null && RectTransformUtility.RectangleContainsScreenPoint(r, sp, cam);
        static Vector2 Centre(RectTransform r) { var c = r.TransformPoint(r.rect.center); return new Vector2(c.x, c.y); }
        void Press(int i) { focus = i; key = null; btns[i].a(); }
        void Mark() { for (int i = 0; i < btns.Count; i++) { var f = btns[i].r != null ? btns[i].r.Find("focus") : null; if (f != null) f.gameObject.SetActive(i == focus); } }
        RectTransform Btn(RectTransform r, string label, Action a)
        {
            r.name = "btn " + label;
            var f = UiKit.Card(r, "focus", 16, new Color(0, 0, 0, 0), false, Ponyo.Sunflower); f.Stretch(-4); UiKit.Edge(f).color = Ponyo.Hex("E0A82E"); f.gameObject.SetActive(false);
            btns.Add((r, a)); return r;
        }
        public bool Tap(string label) { foreach (var b in btns) if (b.r != null && b.r.name.StartsWith("btn " + label)) { key = null; b.a(); return true; } return false; }
        static bool Shift => Keyboard.current != null && Keyboard.current.shiftKey.isPressed || Gamepad.current != null && Gamepad.current.leftShoulder.isPressed;

        void Draw()
        {
            foreach (Transform t in left) Destroy(t.gameObject); foreach (Transform t in rack) Destroy(t.gameObject); btns.Clear();
            int tier = C.WhTier; string[] names = { "Walt's Old Shed", "Patched Warehouse", "Cold Room", "Big Warehouse" };
            title.text = "Warehouse · " + names[Mathf.Min(tier, 3)];
            int racks = C.Racks, cold = C.ColdSlots, used = 0; foreach (var s in C.Wh) if (s.Fish.Count > 0) used++;
            sub.text = $"{C.WhFish} fish · {used} of {racks + cold} slots used · 10 fish a slot{(cold > 0 ? " · the cold room rots at a quarter speed" : " · Plank & Post builds a cold room (T2)")}";
            // on you
            var me = Angler.I; T(left, "ON YOU", UiKit.Bold, 13, Ponyo.InkSoft, 0, 0, 300, 18);
            float y = 26; var all = new List<Fish>();
            for (int i = 0; i < me.Bar.S.Length; i++)
            {
                var s = me.Bar.S[i]; if (s.Fish.Count == 0) continue; var f0 = s.Fish[0]; all.AddRange(s.Fish);
                var r = UiKit.Card(left, "row", 16, Ponyo.Paper, false, Ponyo.Hex("E2D3BB"), 2); r.Place(new Vector2(0, 1), new Vector2(0, -y), new Vector2(420, 72), new Vector2(0, 1));
                if (Hud.Icons.TryGetValue(f0.Species.Id, out var tex)) { var ri = UiKit.Rect("icon", r).Place(new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(84, 42), new Vector2(0, 0.5f)).gameObject.AddComponent<RawImage>(); ri.texture = tex; ri.raycastTarget = false; }
                T(r, $"{f0.Species.Name}  ×{s.Fish.Count}", UiKit.Bold, 18, Ponyo.Ink, 104, 12, 200, 24);
                T(r, $"{Ponyo.GradeName[f0.Grade]} · {Rot.Names[f0.Fresh]}", UiKit.Body, 14, f0.Fresh >= 3 ? Ponyo.Poppy : Ponyo.InkSoft, 104, 40, 200, 20);
                var fish = new List<Fish>(s.Fish);
                var b = UiKit.Card(r, "store", 18, Ponyo.Sunflower, true, Ponyo.Hex("B88A1E"), 3); b.Place(new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(96, 40), new Vector2(1, 0.5f));
                var bt = UiKit.Txt(b, "Store", UiKit.Bold, 16, Ponyo.Ink, TextAnchor.MiddleCenter); bt.rectTransform.Stretch();
                Btn(b, "store " + f0.Species.Id, () => { Works.StoreFish(fish); Snd.Play2D(Synth.Plunk, 0.35f, 1); });
                y += 80;
            }
            if (all.Count == 0) T(left, "No fish on you. Catch some, then bring them here to keep them out of the sun.", UiKit.Body, 16, Ponyo.InkSoft, 0, y, 400, 60);
            else
            {
                var b = UiKit.Card(left, "store all", 20, Ponyo.Sunflower, true, Ponyo.Hex("B88A1E"), 4); b.Place(new Vector2(0, 1), new Vector2(0, -(y + 8)), new Vector2(420, 50), new Vector2(0, 1));
                var bt = UiKit.Txt(b, $"Store all {all.Count}", UiKit.DisplayBold, 20, Ponyo.Ink, TextAnchor.MiddleCenter); bt.rectTransform.Stretch();
                Btn(b, "store all", () => { Works.StoreFish(all); Snd.Play2D(Synth.Plunk, 0.4f, 0.9f); });
            }
            // the racks and the cold room: 8 across
            float RWd = W - 2 * Pad - 450; const int cols = 8; float cw = (RWd - (cols - 1) * 10) / cols, chh = 92;
            T(rack, "RACKS", UiKit.Bold, 13, Ponyo.InkSoft, 0, 0, 300, 18);
            double now = Sky.Minutes; float ry = 26 + ((racks + cols - 1) / cols) * (chh + 10) + 24; int nr = 0, nc = 0;
            if (cold > 0) T(rack, "COLD ROOM · ¼ ROT", UiKit.Bold, 13, Ponyo.SeaDeep, 0, ry - 22, 300, 18);
            for (int i = 0; i < C.Wh.Count; i++)
            {
                var s = C.Wh[i];
                int k = s.Cold ? nc++ : nr++; float x = (k % cols) * (cw + 10), yy = (s.Cold ? ry : 26) + (k / cols) * (chh + 10);
                var r = UiKit.Card(rack, "slot", 14, s.Cold ? Ponyo.Hex("E6F4F8") : Ponyo.Paper, false, s.Cold ? Ponyo.Hex("A9D6E2") : Ponyo.Hex("E2D3BB"), 2); r.Place(new Vector2(0, 1), new Vector2(x, -yy), new Vector2(cw, chh), new Vector2(0, 1));
                if (s.Fish.Count > 0)
                {
                    var top = s.Fish[^1]; var sp = FishDb.Get(top.Species);
                    if (sp != null && Hud.Icons.TryGetValue(sp.Id, out var tex)) { var ri = UiKit.Rect("icon", r).Place(new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(cw - 20, (cw - 20) / 2), new Vector2(0.5f, 1)).gameObject.AddComponent<RawImage>(); ri.texture = tex; ri.raycastTarget = false; }
                    int st = Rot.Stage(Company.AgeOf(s.Fish[0], now));
                    T(r, $"{s.Fish.Count}/10", UiKit.Bold, 15, Ponyo.Ink, 8, chh - 44, cw - 16, 20);
                    T(r, Rot.Names[st], UiKit.Body, 12, st >= 3 ? Ponyo.Poppy : Ponyo.InkSoft, 8, chh - 24, cw - 16, 18);
                    if (top.Grade > 0) { var g = UiKit.Img(r, UiKit.GradeShape(top.Grade), Ponyo.GradeCol[top.Grade], "grade"); g.rectTransform.Place(new Vector2(1, 0), new Vector2(-8, 8), new Vector2(14, 14), new Vector2(1, 0)); }
                    int ii = i; Btn(r, "take " + i, () => { Works.Take(ii, Shift); });
                }
            }
            T(root, "Click a slot to take one fish · Shift-click (LB) takes the slot · fish here keep for the whole crew", UiKit.Body, 15, Ponyo.InkSoft, Pad, H - 34, W - 2 * Pad, 22);
            Mark();
        }
    }
}
