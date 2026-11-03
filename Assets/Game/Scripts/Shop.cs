using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PortBoss.M1
{
    // M4: the counter panels (mockup 04-shop). E at a counter opens it in the middle of the screen; the world keeps going
    // (you just stand there). Esc / B leaves.
    // - Hook, Line & Sinker (June): Bait / Tackle / Rods tabs. A row: the item, the pack, what's in your bag, the
    //   pack count (x1 x2 x5 Max) and Buy. Click Buy, or hold E / A on the row for 0.6 s. Why you can't ("Need $12 more",
    //   "Needs the tackle pouch") replaces the button.
    // - Pratt's (60%) and Mari's Tacos (100%): your hotbar's fish, one row per stack, what each fetches here (grade,
    //   freshness and today's saturation in it), Sell / Sell all. At Pratt's an everyday fish also shows what Mari pays.
    public class Shop : MonoBehaviour
    {
        public static Shop I;
        public static bool Open => I != null && I.stall != null;
        public static Action<string> OnLog;
        const float W = 880, H = 640, Pad = 32;
        Firm.Stall stall; string tab = "Bait"; readonly int[] qty = { 1, 1, 1, 1, 1 };
        RectTransform root, body, list; Text title, sub, bank; Image band; Camera cam;
        readonly List<(RectTransform r, Action a, bool hold)> btns = new(); int focus = -1; float hold; bool holdDone;
        string key; float scroll, contentH;
        static void Log(string s) { Debug.Log("[PB] shop: " + s); OnLog?.Invoke(s); }

        public static void Create() { var g = new GameObject("Shop"); I = g.AddComponent<Shop>(); I.Build(); }

        void Build()
        {
            var hud = (RectTransform)Hud.I.transform; cam = CameraRig.I != null ? CameraRig.I.UiCam : null;
            root = UiKit.Card(hud, "shop", 30, Ponyo.Paper, true, Ponyo.Line, 6); root.Place(new Vector2(0.5f, 0.5f), new Vector2(-60, 0), new Vector2(W, H));
            var head = UiKit.Rect("head", root).Place(new Vector2(0, 1), Vector2.zero, new Vector2(W, 118), new Vector2(0, 1));
            band = UiKit.Img(head, null, Ponyo.SeaC, "band"); band.rectTransform.Place(new Vector2(0, 1), new Vector2(Pad, -28), new Vector2(8, 64), new Vector2(0, 1));
            title = UiKit.Txt(head, "", UiKit.DisplayBold, 36, Ponyo.Ink, TextAnchor.UpperLeft); title.rectTransform.Place(new Vector2(0, 1), new Vector2(Pad + 24, -22), new Vector2(560, 46), new Vector2(0, 1));
            sub = UiKit.Txt(head, "", UiKit.Body, 16, Ponyo.InkSoft, TextAnchor.UpperLeft); sub.rectTransform.Place(new Vector2(0, 1), new Vector2(Pad + 24, -68), new Vector2(560, 24), new Vector2(0, 1));
            var pill = UiKit.Card(head, "bank", 22, Ponyo.Hex("EAF6EC"), false, Ponyo.Hex("A9D3B1")); pill.Place(new Vector2(1, 1), new Vector2(-Pad, -28), new Vector2(190, 50), new Vector2(1, 1));
            var cap = UiKit.Txt(pill, "BANK", UiKit.Bold, 12, Ponyo.InkSoft, TextAnchor.MiddleLeft); cap.rectTransform.Stretch(); cap.rectTransform.offsetMin = new Vector2(18, 0);
            bank = UiKit.Txt(pill, "", UiKit.DisplayBold, 26, Ponyo.Ink, TextAnchor.MiddleRight); bank.rectTransform.Stretch(); bank.rectTransform.offsetMax = new Vector2(-18, 0);
            UiKit.Img(root, null, Ponyo.Paper3, "rule").rectTransform.Place(new Vector2(0, 1), new Vector2(0, -118), new Vector2(W, 2), new Vector2(0, 1));
            body = UiKit.Rect("body", root); body.Stretch(); body.offsetMax = new Vector2(0, -120); body.offsetMin = new Vector2(0, 64); body.gameObject.AddComponent<RectMask2D>();
            list = UiKit.Rect("list", body).Place(new Vector2(0, 1), new Vector2(Pad, 0), new Vector2(W - 2 * Pad, 10), new Vector2(0, 1));
            // the footer: how to leave
            var foot = UiKit.Rect("foot", root).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(W, 64), new Vector2(0, 0));
            var k1 = UiKit.Key(foot, "Esc", 30); k1.Place(new Vector2(1, 0.5f), new Vector2(-Pad - 64, 0), new Vector2(48, 30), new Vector2(1, 0.5f));
            var t1 = UiKit.Txt(foot, "Leave", UiKit.Body, 15, Ponyo.InkSoft, TextAnchor.MiddleRight); t1.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-Pad, 0), new Vector2(56, 30), new Vector2(1, 0.5f));
            var k2 = UiKit.Key(foot, "E", 30); k2.Place(new Vector2(0, 0.5f), new Vector2(Pad, 0), new Vector2(34, 30), new Vector2(0, 0.5f));
            var t2 = UiKit.Txt(foot, "Hold on a row to buy or sell · or click", UiKit.Body, 15, Ponyo.InkSoft, TextAnchor.MiddleLeft); t2.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(Pad + 44, 0), new Vector2(420, 30), new Vector2(0, 0.5f));
            root.gameObject.SetActive(false);
        }

        public static void Show(Firm.Stall s) => I?.ShowI(s);
        void ShowI(Firm.Stall s)
        {
            stall = s; tab = "Bait"; key = null; scroll = 0; focus = 0; holdDone = true; // the E that opened it isn't a buy
            root.gameObject.SetActive(true); Ctl.Ui = 2; Snd.Play2D(Synth.Tik, 0.3f, 0.9f);
            if (!Application.isBatchMode) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            band.color = s.K == Firm.Kind.June ? Ponyo.Hex("46B3A6") : s.K == Firm.Kind.Pratt ? Ponyo.Hex("8D96A3") : Ponyo.Hex("3A86E0");
            title.text = s.Name; sub.text = $"{s.Owner} · “{s.Line}”";
            Log("open " + s.Name);
        }
        public void Hide()
        {
            if (stall == null) return;
            Log("close " + stall.Name); stall = null; root.gameObject.SetActive(false); Ctl.Ui = 0;
            if (!Application.isBatchMode && !Game.Paused) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }
        public void Tab(string t) { tab = t; key = null; scroll = 0; focus = 0; }

        void Update()
        {
            if (stall == null) return;
            var kb = Keyboard.current; var pad = Gamepad.current;
            var me = Angler.I;
            if (Ctl.Escape || kb != null && kb.escapeKey.wasPressedThisFrame || pad != null && pad.buttonEast.wasPressedThisFrame || me == null || me.Swimming
                || (me.transform.position - stall.Front).magnitude > 3.5f || !stall.IsOpen(Sky.Minutes) || stall.K == Firm.Kind.Mari && !Firm.C.MariHere(Sky.Minutes))
            { Ctl.UiEsc = Time.frameCount; if (!stall.IsOpen(Sky.Minutes)) Alerts.Push(stall.Name, "Closing up. Come back tomorrow.", Alerts.Kind.Info); Hide(); return; }
            var c = Firm.C; bank.text = "$" + c.Cash.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            string k = $"{tab}|{c.Cash}|{c.MariToday}|{c.Ledger.Count}|{Bag()}|{string.Join(",", qty)}|{Firm.I.Selling.Count}|{(int)(Sky.Minutes / 30)}";
            if (k != key) { key = k; Draw(); }
            Nav(kb, pad);
        }
        static string Bag()
        {
            var me = Angler.I; if (me == null) return ""; var k = Firm.I.MyKit;
            var sb = new System.Text.StringBuilder(); foreach (var s in me.Bar.S) sb.Append(s.Fish.Count).Append(';');
            return sb.Append(string.Join(",", k.Bait)).Append(k.Rod).Append(k.Pouch5).ToString();
        }

        // ---------------------------------------------------------------- input (the phone's way: mouse, arrows, pad)
        void Nav(Keyboard kb, Gamepad pad)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float wh = mouse.scroll.ReadValue().y; if (wh != 0) Scroll(-Mathf.Sign(wh) * 70);
                var mp = mouse.position.ReadValue();
                if (mouse.delta.ReadValue().sqrMagnitude > 0) for (int i = 0; i < btns.Count; i++) if (Hit(btns[i].r, mp)) { if (focus != i) { focus = i; Mark(); } break; }
                if (mouse.leftButton.wasPressedThisFrame) for (int i = 0; i < btns.Count; i++) if (Hit(btns[i].r, mp)) { Press(i); return; }
            }
            int dy = 0, dx = 0;
            if (kb != null) { if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) dy = -1; if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) dy = 1; if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) dx = -1; if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) dx = 1; }
            if (pad != null) { if (pad.dpad.up.wasPressedThisFrame) dy = -1; if (pad.dpad.down.wasPressedThisFrame) dy = 1; if (pad.dpad.left.wasPressedThisFrame) dx = -1; if (pad.dpad.right.wasPressedThisFrame) dx = 1; if (pad.leftShoulder.wasPressedThisFrame) NextTab(-1); if (pad.rightShoulder.wasPressedThisFrame) NextTab(1); }
            if ((dx != 0 || dy != 0) && btns.Count > 0)
            {
                if (focus < 0) focus = 0;
                else
                {
                    var at = Centre(btns[focus].r); var move = new Vector2(dx, -dy); int best = -1; float bd = 1e9f;
                    for (int i = 0; i < btns.Count; i++)
                    {
                        if (i == focus) continue; var d = Centre(btns[i].r) - at; if (Vector2.Dot(d.normalized, move) < 0.5f) continue;
                        float s = d.magnitude * (1 + 2 * Mathf.Abs(Vector2.Dot(d.normalized, new Vector2(move.y, -move.x)))); if (s < bd) { bd = s; best = i; }
                    }
                    if (best >= 0) focus = best;
                }
                Mark(); Reveal(focus);
            }
            // E / A: a tap presses a tab or a pack chip; the buy and sell buttons want a 0.6 s hold
            bool down = kb != null && (kb.eKey.isPressed || kb.enterKey.isPressed) || pad != null && pad.buttonSouth.isPressed;
            if (!down) { holdDone = false; hold = 0; }
            else if (!holdDone && focus >= 0 && focus < btns.Count)
            {
                if (!btns[focus].hold) { holdDone = true; Press(focus); }
                else { hold += Time.unscaledDeltaTime; if (hold >= 0.6f) { holdDone = true; hold = 0; Press(focus); } }
            }
            for (int i = 0; i < btns.Count; i++)
            {
                var ring = btns[i].r != null ? btns[i].r.Find("hold") : null; if (ring == null) continue;
                var img = ring.GetComponent<Image>(); img.fillAmount = i == focus && down && !holdDone ? hold / 0.6f : 0;
            }
        }
        void NextTab(int d) { if (stall.K != Firm.Kind.June) return; string[] t = { "Bait", "Tackle", "Rods" }; Tab(t[(Array.IndexOf(t, tab) + d + 3) % 3]); }
        void Scroll(float d) { scroll = Mathf.Clamp(scroll + d, 0, Mathf.Max(0, contentH - (H - 184))); list.anchoredPosition = new Vector2(Pad, scroll); }
        void Reveal(int i)
        {
            if (i < 0 || i >= btns.Count) return; var r = btns[i].r;
            float top = -list.InverseTransformPoint(r.TransformPoint(new Vector3(0, r.rect.yMax))).y, bot = -list.InverseTransformPoint(r.TransformPoint(new Vector3(0, r.rect.yMin))).y;
            if (top < scroll) Scroll(top - scroll - 8); else if (bot > scroll + H - 184) Scroll(bot - scroll - (H - 184) + 8);
        }
        bool Hit(RectTransform r, Vector2 sp) => r != null && RectTransformUtility.RectangleContainsScreenPoint(r, sp, cam);
        static Vector2 Centre(RectTransform r) { var c = r.TransformPoint(r.rect.center); return new Vector2(c.x, c.y); }
        void Press(int i) { focus = i; Snd.Play2D(Synth.Tik, 0.25f, 1.2f); key = null; btns[i].a(); }
        void Mark() { for (int i = 0; i < btns.Count; i++) { var f = btns[i].r != null ? btns[i].r.Find("focus") : null; if (f != null) f.gameObject.SetActive(i == focus); } }
        public bool Tap(string label)
        {
            for (int i = 0; i < btns.Count; i++) if (btns[i].r != null && btns[i].r.name.StartsWith("btn " + label)) { Press(i); return true; }
            Log($"tap: no button \"{label}\" ({Buttons()})"); return false;
        }
        public string Buttons() => string.Join(", ", btns.ConvertAll(b => b.r != null ? b.r.name.Substring(4) : "?"));

        // ---------------------------------------------------------------- drawing
        RectTransform Btn(RectTransform r, string label, Action a, bool holdToUse = false)
        {
            r.name = "btn " + label;
            var f = UiKit.Card(r, "focus", 18, new Color(0, 0, 0, 0), false, Ponyo.Sunflower); f.Stretch(-4); UiKit.Edge(f).color = Ponyo.Hex("E0A82E"); f.gameObject.SetActive(false);
            if (holdToUse)
            {
                var h = UiKit.Img(r, null, new Color(0.25f, 0.17f, 0.15f, 0.18f), "hold"); h.rectTransform.Stretch(); h.type = Image.Type.Filled; h.fillMethod = Image.FillMethod.Horizontal; h.fillAmount = 0;
                h.sprite = UiKit.Fill(r).sprite; h.type = Image.Type.Filled;
            }
            btns.Add((r, a, holdToUse)); return r;
        }
        RectTransform Pill(RectTransform p, string label, float x, float y, float w, float h, Color fill, Color ink, Action a, bool holdToUse = false, int size = 17)
        {
            var c = UiKit.Card(p, "pill", h / 2, fill, true, Color.Lerp(fill, Ponyo.Line, 0.55f), 3); c.Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h), new Vector2(0, 1));
            var t = UiKit.Txt(c, label, UiKit.Bold, size, ink, TextAnchor.MiddleCenter); t.rectTransform.Stretch();
            return Btn(c, label, a, holdToUse);
        }
        Text T(RectTransform p, string s, Font f, int size, Color c, float x, float y, float w, float h, TextAnchor a = TextAnchor.UpperLeft)
        { var t = UiKit.Txt(p, s, f, size, c, a); t.rectTransform.Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h), new Vector2(0, 1)); return t; }
        RectTransform Row(float y, float h, bool dim = false)
        { var r = UiKit.Card(list, "row", 18, dim ? Ponyo.Paper2 : Ponyo.Paper, false, Ponyo.Hex("E2D3BB"), 2); r.Place(new Vector2(0, 1), new Vector2(0, -y), new Vector2(W - 2 * Pad, h), new Vector2(0, 1)); return r; }
        static string Money(int v) => "$" + v.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

        void Draw()
        {
            foreach (Transform t in list) Destroy(t.gameObject);
            btns.Clear();
            float h = stall.K == Firm.Kind.June ? June() : Sell();
            contentH = h; list.sizeDelta = new Vector2(W - 2 * Pad, h); Scroll(0);
            if (focus >= btns.Count) focus = btns.Count - 1;
            Mark();
        }

        float June()
        {
            float y = 6, x = 0; var k = Firm.I.MyKit; var c = Firm.C;
            foreach (var t in new[] { "Bait", "Tackle", "Rods" })
            {
                float w = t.Length * 11 + 44; var tt = t;
                Pill(list, t, x, y, w, 40, tab == t ? Ponyo.Night : Ponyo.Paper2, tab == t ? Ponyo.Paper : Ponyo.Ink, () => Tab(tt));
                x += w + 8;
            }
            y += 58; float RW = W - 2 * Pad;
            if (tab == "Bait")
            {
                for (int i = 0; i < 5; i++)
                {
                    if (i == 4 && k.Rod < 5) continue;                                   // Walt's Mix: Old Faithful's bait
                    var b = FishDb.Baits[i]; int n = Shelf.PackOf[i];
                    bool locked = Shelf.NeedsPouch(i) && !k.Pouch5;
                    int room = (Shelf.BaitMax - k.Bait[i]) / n, q = Mathf.Clamp(qty[i], 1, Mathf.Max(1, room));
                    string item = "bait:" + i; string why = c.CanBuy(Firm.MyKey, item, q);
                    var r = Row(y, 96, locked);
                    var ic = UiKit.Img(r, Glyphs.Get("worm"), locked ? Ponyo.Hex("B5A99A") : BaitCol(i), "icon"); ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(52, 52), new Vector2(0, 0.5f));
                    T(r, b.Name, UiKit.Bold, 21, Ponyo.Ink, 92, 16, 260, 28);
                    T(r, i == 4 ? "1 tin for $40 · the old man's recipe" : $"{n} for {Money(Shelf.BaitPack[i])} · {Hooks(i)}", UiKit.Body, 14, Ponyo.InkSoft, 92, 46, 320, 20);
                    T(r, locked ? "Needs the tackle pouch (Tackle tab)" : $"In your bag: {k.Bait[i]}", UiKit.Body, 14, locked ? Ponyo.Poppy : Ponyo.Ink, 92, 68, 320, 20);
                    if (!locked)
                    {
                        // the pack chips: x1 x2 x5 Max
                        float cx = 410;
                        foreach (var m in new[] { 1, 2, 5, 0 })
                        {
                            int mq = m == 0 ? Mathf.Max(1, room) : m; bool on = q == mq && (m != 0 || room > 5); int ii = i;
                            var chip = Pill(r, m == 0 ? "Max" : "×" + m, cx, 28, m == 0 ? 54 : 44, 36, on ? Ponyo.Sunflower : Ponyo.Paper2, Ponyo.Ink, () => qty[ii] = mq, false, 14);
                            chip.name = $"btn {(m == 0 ? "Max" : "x" + m)} {b.Id}";
                            cx += (m == 0 ? 54 : 44) + 6;
                        }
                        if (why == "") Pill(r, $"Buy {Money(c.PriceOf(item, q))}", RW - 152, 26, 136, 44, Ponyo.Sunflower, Ponyo.Ink, () => Firm.I.Buy(item, q), true).name = "btn Buy " + b.Id;
                        else T(r, why, UiKit.Bold, 15, Ponyo.Poppy, RW - 172, 38, 156, 22, TextAnchor.UpperRight);
                    }
                    y += 106;
                }
                if (k.Rod < 2) { T(list, "Shrimp brings new fish once you've got the Fibreglass rod: garfish, bream, even a conger.", UiKit.Body, 15, Ponyo.InkSoft, 4, y + 4, RW, 40); y += 44; }
            }
            else if (tab == "Tackle")
            {
                var r = Row(y, 110); string why = c.CanBuy(Firm.MyKey, "pouch", 1);
                var ic = UiKit.Img(r, Glyphs.Get("pouch"), Ponyo.Hex("A0704E"), "icon"); ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(56, 56), new Vector2(0, 0.5f));
                T(r, "Tackle pouch", UiKit.Bold, 21, Ponyo.Ink, 96, 18, 300, 28);
                T(r, "Carries all five baits, not just three. Squid strip needs it.", UiKit.Body, 14, Ponyo.InkSoft, 96, 48, 420, 20);
                T(r, k.Pouch5 ? "Yours" : Money(Shelf.PouchPrice), UiKit.Bold, 15, Ponyo.Ink, 96, 72, 200, 20);
                if (why == "") Pill(r, $"Buy {Money(Shelf.PouchPrice)}", RW - 160, 33, 144, 44, Ponyo.Sunflower, Ponyo.Ink, () => Firm.I.Buy("pouch", 1), true).name = "btn Buy pouch";
                else T(r, why, UiKit.Bold, 15, why == "You own it." ? Ponyo.Hex("3E9A4E") : Ponyo.Poppy, RW - 200, 45, 184, 22, TextAnchor.UpperRight);
                y += 120;
            }
            else
            {
                for (int t = 1; t <= 5; t++)
                {
                    var rod = FishDb.Rods[t - 1]; string item = "rod:" + t; string why = t == 1 ? "You own it." : c.CanBuy(Firm.MyKey, item, 1);
                    bool owned = k.Rod >= t, later = t > 2 && !owned;
                    var r = Row(y, 96, later);
                    var ri = UiKit.Rect("icon", r).Place(new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(68, 68), new Vector2(0, 0.5f)).gameObject.AddComponent<RawImage>(); ri.texture = UiKit.RodIcon(t); ri.raycastTarget = false; if (later) ri.color = new Color(1, 1, 1, 0.45f);
                    T(r, rod.Name, UiKit.Bold, 21, later ? Ponyo.InkSoft : Ponyo.Ink, 96, 16, 320, 28);
                    T(r, $"Tier {t} · line {rod.LineKg:0} kg · casts {rod.RangeMax:0} m", UiKit.Body, 14, Ponyo.InkSoft, 96, 46, 320, 20);
                    T(r, t == 1 ? "Walt's. It's seen things." : owned ? (k.Rod == t ? "In your hands" : "Yours") : later ? (t == 5 ? "Somewhere in Walt's journal" : $"Comes in {why}") : Money(Shelf.RodPrice[t]), UiKit.Body, 14, owned ? Ponyo.Hex("3E9A4E") : Ponyo.Ink, 96, 68, 320, 20);
                    if (why == "") Pill(r, $"Buy {Money(Shelf.RodPrice[t])}", RW - 160, 26, 144, 44, Ponyo.Sunflower, Ponyo.Ink, () => Firm.I.Buy(item, 1), true).name = "btn Buy rod " + t;
                    else if (!owned && !later) T(r, why, UiKit.Bold, 15, Ponyo.Poppy, RW - 200, 38, 184, 22, TextAnchor.UpperRight);
                    else if (owned) { var ck = UiKit.Img(r, UiKit.Check, Ponyo.Hex("3E9A4E"), "owned"); ck.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-28, 0), new Vector2(30, 30), new Vector2(1, 0.5f)); }
                    else { var lk = UiKit.Img(r, Glyphs.Get("lock"), Ponyo.InkSoft, "lock"); lk.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-28, 0), new Vector2(28, 28), new Vector2(1, 0.5f)); }
                    y += 106;
                }
            }
            return y + 10;
        }
        public static Color BaitCol(int i) => i switch { 0 => Ponyo.Hex("D9826A"), 1 => Ponyo.Hex("F29A85"), 2 => Ponyo.Hex("8FA6C0"), 3 => Ponyo.Hex("E7D6C6"), _ => Ponyo.Hex("C98A1A") };
        static string Hooks(int i) => i switch { 0 => "sprat, mullet, mackerel", 1 => "garfish, bream (Fibreglass rod)", 2 => "bigger fish further out", 3 => "squid lovers (Act 2)", _ => "" };

        // the sell panel: the hotbar's fish, one row per stack
        float Sell()
        {
            var c = Firm.C; var me = Angler.I; Buyer b = stall.K == Firm.Kind.Pratt ? Buyer.Pratt : Buyer.Mari; float RW = W - 2 * Pad; double m = Sky.Minutes;
            float y = 4;
            string head = b == Buyer.Pratt ? "Pratt pays 60% of a fish's worth. Less for the same fish again today." : $"Mari pays 100% for everyday fish · {Market.MariDaily - c.MariToday} more today";
            T(list, head, UiKit.Body, 16, Ponyo.InkSoft, 2, y, RW, 24); y += 34;
            var all = new List<Fish>(); int total = 0; int left = b == Buyer.Mari ? Market.MariDaily - c.MariToday : int.MaxValue;
            bool any = false;
            for (int si = 0; si < me.Bar.S.Length; si++)
            {
                var s = me.Bar.S[si]; if (s.Fish.Count == 0) continue; any = true;
                var f0 = s.Fish[0]; var sp = f0.Species;
                var ok = s.Fish.FindAll(f => f.Fresh < 4 && !Firm.I.Selling.Contains(f.Id) && (b == Buyer.Pratt || sp.Class == FishClass.Everyday));
                if (b == Buyer.Mari && ok.Count > left) ok = ok.GetRange(0, Mathf.Max(0, left));
                int[] p = ok.Count > 0 ? c.Quote(b, (all.Count > 0 ? Concat(all, ok) : ok).ConvertAll(Firm.Lot), m, out _) : null;
                if (p != null && all.Count > 0) p = p[all.Count..];                 // priced after the stacks above it (saturation)
                var r = Row(y, 96, ok.Count == 0);
                if (Hud.Icons.TryGetValue(sp.Id, out var tex)) { var ri = UiKit.Rect("icon", r).Place(new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(92, 46), new Vector2(0, 0.5f)).gameObject.AddComponent<RawImage>(); ri.texture = tex; ri.raycastTarget = false; }
                T(r, $"{sp.Name}  ×{s.Fish.Count}", UiKit.Bold, 21, Ponyo.Ink, 120, 14, 300, 28);
                var gr = UiKit.Img(r, UiKit.GradeShape(f0.Grade), Ponyo.GradeCol[f0.Grade], "grade"); gr.rectTransform.Place(new Vector2(0, 1), new Vector2(120, -52), new Vector2(14, 14), new Vector2(0, 1));
                int fr0 = 4, fr1 = 0; foreach (var f in s.Fish) { fr0 = Mathf.Min(fr0, f.Fresh); fr1 = Mathf.Max(fr1, f.Fresh); }
                string fresh = fr0 == fr1 ? Rot.Names[fr0] : $"{Rot.Names[fr0]} to {Rot.Names[fr1].ToLower()}";
                T(r, $"{Ponyo.GradeName[f0.Grade]} · {fresh} · worth ${f0.Value:0} each", UiKit.Body, 14, fr1 >= 3 ? Ponyo.Poppy : Ponyo.InkSoft, 140, 50, 340, 20);
                if (p != null)
                {
                    int sum = 0; foreach (var x in p) sum += x; total += sum; all.AddRange(ok); left -= ok.Count;
                    string each = p.Length == 1 || Array.TrueForAll(p, x => x == p[0]) ? $"${p[0]} each" : $"${p[^1]}–${p[0]} each";
                    T(r, each + (ok.Count < s.Fish.Count ? $" · {ok.Count} of {s.Fish.Count}" : ""), UiKit.Body, 14, Ponyo.Ink, 140, 70, 340, 20);
                    if (b == Buyer.Pratt && sp.Class == FishClass.Everyday && c.Mari == DealSt.Active)
                    { int mp = Market.Price(f0.Value, f0.Fresh, Buyer.Mari, c.SoldToday(Buyer.Mari, sp.Id)); T(r, $"Mari pays ${mp}", UiKit.Body, 13, Ponyo.Hex("A89A88"), RW - 330, 64, 150, 20, TextAnchor.UpperRight); }
                    var sel = ok; int sum2 = sum;
                    Pill(r, $"Sell {Money(sum)}", RW - 168, 26, 152, 44, Ponyo.Sunflower, Ponyo.Ink, () => Firm.I.Sell(b, sel), true).name = $"btn Sell {sp.Id}";
                }
                else
                {
                    string why = fr0 >= 4 ? "Rotten. Nobody wants it." : b == Buyer.Mari && sp.Class != FishClass.Everyday ? "Mari only takes everyday fish" : b == Buyer.Mari && left <= 0 ? (c.MariToday >= Market.MariDaily ? "Mari's had her 10 today" : $"Mari takes {Market.MariDaily} a day") : "";
                    T(r, why, UiKit.Bold, 15, Ponyo.InkSoft, RW - 300, 38, 284, 22, TextAnchor.UpperRight);
                }
                y += 106;
            }
            if (!any) { T(list, "Nothing to sell. Go catch something.", UiKit.Display, 22, Ponyo.InkSoft, 0, y + 40, RW, 40, TextAnchor.UpperCenter); y += 100; }
            else if (all.Count > 1)
            {
                var sa = all;
                Pill(list, $"Sell all {all.Count} · {Money(total)}", RW - 300, y + 6, 300, 50, Ponyo.Sunflower, Ponyo.Ink, () => Firm.I.Sell(b, sa), true, 19).name = "btn Sell all";
                y += 70;
            }
            return y;
        }
        static List<Fish> Concat(List<Fish> a, List<Fish> b) { var l = new List<Fish>(a); l.AddRange(b); return l; }
    }
}
