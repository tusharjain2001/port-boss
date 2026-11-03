using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PortBoss.M1
{
    // M4: Walt's old phone on Tab (gdd systems §1.2, MUST; mockup 03-phone). A red rubber case with a cracked top-right
    // corner slides up on the RIGHT (420 x 860 at 1080p) so the left toasts stay in view. The world keeps running: you
    // walk with it out (walk speed), the mouse is on the phone (no looking or casting). Blocked in the fishing fight and
    // while swimming. Tab / Esc / B closes it; hold Tab 0.4 s to open the newest unread straight away.
    // Small but useful: 8 apps on one home screen, never deeper than 2 levels, every action within 3 taps.
    // The time sits top-left on every app; Home has the only big clock in the game (no HUD clock, Manan 31 Oct).
    // Mouse, keys (arrows, Enter, Backspace) or a pad (d-pad, A, B) drive it.
    public class Phone : MonoBehaviour
    {
        public static Phone I;
        public static bool IsOpen => I != null && I.open;
        public enum Scr { Home, Messages, Thread, Contracts, Crew, Map, Bank, Forecast, Journal, Settings }
        public Scr Screen = Scr.Home;
        public string Thread;                                   // the contact shown in Thread ("Notifications" = the toasts)
        public static bool H24;                                 // Settings: 24-hour clock on the phone
        public static Action<string> OnLog;

        const float W = 420, H = 860, Inset = 16, Bar = 46, Pad = 18;
        const float CW = W - 2 * Inset - 2 * Pad;                // 352: the content width
        bool open; float slide; float tabHeld = -1; string key; float scroll, contentH; float shownAt;
        RectTransform root, phone, view, list; Text statusTime; Camera cam;
        readonly List<(RectTransform r, Action a)> btns = new(); int focus = -1;
        readonly HashSet<int> read = new();                     // texts I've read (my own: co-op copies never undo it)
        // the map
        RawImage mapImg; static Texture2D chart; string mapFilter = "All";
        readonly List<(RectTransform r, Func<Vector3?> at)> dots = new();
        Text mapInfo;

        public static readonly Color Case = Ponyo.Hex("E0603E"), CaseDk = Ponyo.Hex("A8402A"), SubInk = Ponyo.Hex("76605A");

        public static void Create()
        {
            var g = new GameObject("Phone"); I = g.AddComponent<Phone>(); I.Build();
            PhoneHud.Make();
        }

        static void Log(string s) { Debug.Log("[PB] phone: " + s); OnLog?.Invoke(s); }

        void Build()
        {
            var hud = (RectTransform)Hud.I.transform; cam = CameraRig.I != null ? CameraRig.I.UiCam : null;
            root = UiKit.Rect("phone", hud).Place(new Vector2(1, 0.5f), new Vector2(-48, -1100), new Vector2(W, H), new Vector2(1, 0.5f));
            phone = root;
            // the rugged case: red rubber, a darker lip, two side buttons
            var shell = UiKit.Card(root, "case", 60, Case, true, CaseDk, 5); shell.Stretch();
            var lip = UiKit.Img(shell, null, CaseDk, "button"); lip.rectTransform.Place(new Vector2(1, 1), new Vector2(4, -170), new Vector2(6, 70), new Vector2(0, 1));
            var lip2 = UiKit.Img(shell, null, CaseDk, "button"); lip2.rectTransform.Place(new Vector2(0, 1), new Vector2(-4, -150), new Vector2(6, 46), new Vector2(1, 1));
            var scr = UiKit.Card(root, "screen", 44, Ponyo.Paper, false, Ponyo.Hex("B5543A")); scr.Stretch(Inset);
            scr.gameObject.AddComponent<RectMask2D>();
            // status bar: time top-left, Gull Tel, signal, battery
            var st = UiKit.Rect("status", scr).Place(new Vector2(0, 1), new Vector2(0, 0), new Vector2(W - 2 * Inset, Bar), new Vector2(0, 1));
            statusTime = UiKit.Txt(st, "", UiKit.Bold, 17, Ponyo.Ink, TextAnchor.MiddleLeft); statusTime.rectTransform.Stretch(); statusTime.rectTransform.offsetMin = new Vector2(28, -4);
            var carrier = UiKit.Txt(st, "Gull Tel", UiKit.Bold, 14, Ponyo.Ink, TextAnchor.MiddleRight); carrier.rectTransform.Stretch(); carrier.rectTransform.offsetMax = new Vector2(-64, -4);
            for (int i = 0; i < 4; i++) UiKit.Img(st, null, Ponyo.Ink, "signal").rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-132 + i * 5, -6 + i * 1.5f), new Vector2(3, 5 + i * 3), new Vector2(0, 0.5f));
            var bat = UiKit.Img(st, UiKit.RingThin, Ponyo.Ink, "battery"); bat.sprite = null; bat.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-30, -2), new Vector2(22, 11), new Vector2(1, 0.5f));
            var batIn = UiKit.Img(bat.transform, null, Ponyo.Paper, "in"); batIn.rectTransform.Stretch(1.6f);
            var batFill = UiKit.Img(batIn.transform, null, Ponyo.Ink, "fill"); batFill.rectTransform.Stretch(1.4f); batFill.rectTransform.offsetMax = new Vector2(-5, -1.4f);
            // the crack (Walt dropped it off the pier the day Voss took over): thin lines from the top-right corner
            var crack = UiKit.Rect("crack", root).Place(new Vector2(1, 1), new Vector2(-Inset - 4, -Inset - 4), new Vector2(90, 90), new Vector2(1, 1));
            void Line(Vector2 a, Vector2 b, float w)
            {
                var d = b - a; var l = UiKit.Img(crack, null, new Color(1, 1, 1, 0.9f), "crack line");
                l.rectTransform.Place(new Vector2(1, 1), a, new Vector2(d.magnitude, w), new Vector2(0, 0.5f)); l.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                var sh = UiKit.Img(crack, null, new Color(0.42f, 0.3f, 0.26f, 0.45f), "crack shade"); sh.rectTransform.Place(new Vector2(1, 1), a + new Vector2(0.8f, -1.2f), new Vector2(d.magnitude, w * 0.7f), new Vector2(0, 0.5f)); sh.rectTransform.localRotation = l.rectTransform.localRotation; sh.transform.SetSiblingIndex(l.transform.GetSiblingIndex());
            }
            Line(new Vector2(-2, -4), new Vector2(-30, -26), 1.6f); Line(new Vector2(-30, -26), new Vector2(-44, -58), 1.3f); Line(new Vector2(-30, -26), new Vector2(-66, -34), 1.1f);
            Line(new Vector2(-44, -58), new Vector2(-40, -80), 0.9f); Line(new Vector2(-14, -2), new Vector2(-24, -16), 1.0f);
            // the screen's content, under the status bar (scrolls)
            view = UiKit.Rect("view", scr); view.Stretch(); view.offsetMax = new Vector2(0, -Bar); view.gameObject.AddComponent<RectMask2D>();
            list = UiKit.Rect("list", view).Place(new Vector2(0, 1), new Vector2(Pad, 0), new Vector2(CW, 10), new Vector2(0, 1));
            root.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- open, close
        public bool CanOpen(out string why)
        {
            why = null; var me = Angler.I;
            if (Title.Shown || Shop.Open || Pouch.Open || Game.Paused) { why = ""; return false; }
            if (me == null) return true;
            if (me.Swimming) { why = "Not while swimming. It's waterproof; you aren't."; return false; }
            if (me.Fish.State is Fishing.St.Bite or Fishing.St.Strike or Fishing.St.Minigame or Fishing.St.Heavy or Fishing.St.Reel or Fishing.St.Reveal) { why = ""; return false; }
            return true;
        }
        public void Show(Scr s = Scr.Home, string thread = null)
        {
            if (!CanOpen(out var why)) { if (!string.IsNullOrEmpty(why)) Alerts.Push("Phone", why, Alerts.Kind.Info, "phone"); return; }
            if (!open) { open = true; root.gameObject.SetActive(true); shownAt = Time.unscaledTime; Snd.Play2D(Synth.Tik, 0.3f, 0.8f); Log("open"); }
            Go(s, thread);
            Ctl.Ui = 1; if (!Application.isBatchMode) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }
        public void Hide()
        {
            if (!open) return;
            open = false; Ctl.Ui = 0; Snd.Play2D(Synth.Tik, 0.25f, 0.65f); Log("close");
            if (!Application.isBatchMode && !Game.Paused) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }
        public void Go(Scr s, string thread = null)
        {
            Screen = s; Thread = thread ?? Thread; scroll = 0; key = null; focus = -1;
            if (s == Scr.Thread) MarkRead(Thread);
            Log("screen " + s + (s == Scr.Thread ? " " + Thread : ""));
        }
        void MarkRead(string who)
        {
            var c = Firm.C; if (c == null) return;
            foreach (var m in c.Msgs) if (m.From == who) { read.Add(m.Id); if (Sky.Authority) m.Read = true; }
        }
        public int Unread(string who = null)
        {
            var c = Firm.C; if (c == null) return 0; int n = 0;
            foreach (var m in c.Msgs) if (!m.Me && !m.Read && !read.Contains(m.Id) && (who == null || m.From == who)) n++;
            return n;
        }
        string NewestUnread() { var c = Firm.C; if (c == null) return null; for (int i = c.Msgs.Count - 1; i >= 0; i--) { var m = c.Msgs[i]; if (!m.Me && !m.Read && !read.Contains(m.Id)) return m.From; } return null; }

        void Update()
        {
            var kb = Keyboard.current; var pad = Gamepad.current;
            // Tab: a tap toggles; held 0.4 s opens the newest unread
            bool tabDown = kb != null && kb.tabKey.isPressed || pad != null && pad.selectButton.isPressed;
            if (tabDown) { if (tabHeld < 0) tabHeld = 0; else if (tabHeld >= 0) tabHeld += Time.unscaledDeltaTime; if (tabHeld >= 0.4f && tabHeld < 1e5f) { tabHeld = 1e6f; var u = NewestUnread(); Show(u != null ? Scr.Thread : Scr.Messages, u); } }
            else if (tabHeld >= 0) { if (tabHeld < 0.4f) { if (open) Hide(); else Show(); } tabHeld = -1; }
            if (open && !CanOpen(out _)) Hide();                        // a bite, the water: put it away
            slide = Mathf.MoveTowards(slide, open ? 1 : 0, Time.unscaledDeltaTime / 0.25f);
            float e = 1 - (1 - slide) * (1 - slide) * (1 - slide);
            root.anchoredPosition = new Vector2(-48, Mathf.Lerp(-1100, 0, e));
            if (slide <= 0 && !open && root.gameObject.activeSelf) root.gameObject.SetActive(false);
            if (!open) return;
            if (Ctl.Escape || kb != null && kb.escapeKey.wasPressedThisFrame || pad != null && pad.buttonEast.wasPressedThisFrame && Screen == Scr.Home) { Ctl.UiEsc = Time.frameCount; Hide(); return; }
            bool back = kb != null && kb.backspaceKey.wasPressedThisFrame || pad != null && pad.buttonEast.wasPressedThisFrame;
            if (back) { Go(Screen == Scr.Thread ? Scr.Messages : Scr.Home); }
            statusTime.text = Clock(Sky.Minutes, false);
            // rebuild when what it shows changed
            string k = Key();
            if (k != key) { key = k; Draw(); }
            Input_(kb, pad);
            if (Screen == Scr.Map) MapDots();
        }

        public static string Clock(double m, bool ampm = true)
        {
            int t = (int)Math.Floor(DayTime.OfDay(m)), h = t / 60, mi = t % 60;
            if (H24) return $"{h:00}:{mi:00}";
            return $"{(h % 12 == 0 ? 12 : h % 12)}:{mi:00}" + (ampm ? (h < 12 ? " AM" : " PM") : "");
        }

        string Key()
        {
            var c = Firm.C; int minute = (int)Sky.Minutes;
            return $"{Screen}|{Thread}|{minute}|{c?.Cash}|{c?.Msgs.Count}|{c?.Mari}|{c?.Ledger.Count}|{c?.MariTracked}|{Unread()}|{Alerts.Queue.Count}|{mapFilter}|{(int)scroll}|{Settings.MouseSens:F2}|{Settings.Invert}|{Settings.TimingOffsetMs}|{Settings.Voice}|{H24}|{Firm.I?.MyKit.Rod}";
        }

        // ---------------------------------------------------------------- input: mouse, keys, pad
        void Input_(Keyboard kb, Gamepad pad)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float wheel = mouse.scroll.ReadValue().y; if (wheel != 0) { scroll = Mathf.Clamp(scroll - Mathf.Sign(wheel) * 60, 0, Mathf.Max(0, contentH - (H - 2 * Inset - Bar))); list.anchoredPosition = new Vector2(Pad, scroll); }
                var mp = mouse.position.ReadValue();
                if (mouse.delta.ReadValue().sqrMagnitude > 0) for (int i = 0; i < btns.Count; i++) if (Hit(btns[i].r, mp)) { if (focus != i) { focus = i; Mark(); } break; }
                if (mouse.leftButton.wasPressedThisFrame) for (int i = 0; i < btns.Count; i++) if (Hit(btns[i].r, mp)) { Press(i); return; }
            }
            if (pad != null) { var rs = pad.rightStick.ReadValue().y; if (Mathf.Abs(rs) > 0.3f) { scroll = Mathf.Clamp(scroll - rs * 900 * Time.unscaledDeltaTime, 0, Mathf.Max(0, contentH - (H - 2 * Inset - Bar))); list.anchoredPosition = new Vector2(Pad, scroll); } }
            Vector2 move = Vector2.zero;
            if (kb != null) { if (kb.upArrowKey.wasPressedThisFrame) move.y = 1; if (kb.downArrowKey.wasPressedThisFrame) move.y = -1; if (kb.leftArrowKey.wasPressedThisFrame) move.x = -1; if (kb.rightArrowKey.wasPressedThisFrame) move.x = 1; }
            if (pad != null) { if (pad.dpad.up.wasPressedThisFrame) move.y = 1; if (pad.dpad.down.wasPressedThisFrame) move.y = -1; if (pad.dpad.left.wasPressedThisFrame) move.x = -1; if (pad.dpad.right.wasPressedThisFrame) move.x = 1; }
            if (move != Vector2.zero && btns.Count > 0)
            {
                if (focus < 0) focus = 0;
                else
                {
                    Vector2 at = Centre(btns[focus].r); int best = -1; float bd = 1e9f;
                    for (int i = 0; i < btns.Count; i++)
                    {
                        if (i == focus) continue; var d = Centre(btns[i].r) - at;
                        if (Vector2.Dot(d.normalized, move) < 0.5f) continue;
                        float s = d.magnitude * (1 + 2 * Mathf.Abs(Vector2.Dot(d.normalized, new Vector2(move.y, -move.x))));
                        if (s < bd) { bd = s; best = i; }
                    }
                    if (best >= 0) focus = best;
                }
                Mark();
            }
            bool ok = kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) || pad != null && pad.buttonSouth.wasPressedThisFrame;
            if (ok && focus >= 0 && focus < btns.Count) Press(focus);
        }
        bool Hit(RectTransform r, Vector2 sp) => r != null && RectTransformUtility.RectangleContainsScreenPoint(r, sp, cam);
        static Vector2 Centre(RectTransform r) { var c = r.TransformPoint(r.rect.center); return new Vector2(c.x, c.y); }
        void Press(int i) { focus = i; var a = btns[i].a; Snd.Play2D(Synth.Tik, 0.25f, 1.2f); key = null; a(); }
        void Mark()
        {
            for (int i = 0; i < btns.Count; i++)
            {
                var r = btns[i].r; if (r == null) continue;
                var ring = r.Find("focus"); if (ring != null) ring.gameObject.SetActive(i == focus);
            }
        }
        // (tests) press the button whose label starts with this text
        public bool Tap(string label)
        {
            for (int i = 0; i < btns.Count; i++) if (btns[i].r != null && btns[i].r.name.StartsWith("btn " + label)) { Press(i); return true; }
            Log($"tap: no button \"{label}\" on {Screen}"); return false;
        }
        public string Buttons() => string.Join(", ", btns.ConvertAll(b => b.r != null ? b.r.name.Substring(4) : "?"));

        // ---------------------------------------------------------------- building blocks
        RectTransform At(RectTransform p, float x, float y, float w, float h, string n = "r") => UiKit.Rect(n, p).Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h), new Vector2(0, 1));
        RectTransform Card(RectTransform p, float x, float y, float w, float h, Color? fill = null, float rad = 18, bool shadow = false, Color? line = null)
        { var c = UiKit.Card(p, "card", rad, fill ?? Ponyo.Paper, shadow, line ?? Ponyo.Hex("D9C9B0"), 2); c.Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h), new Vector2(0, 1)); return c; }
        static Text Fit(Text t, int min) { t.resizeTextForBestFit = true; t.resizeTextMinSize = min; t.resizeTextMaxSize = t.fontSize; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; return t; }
        Text T(RectTransform p, string s, Font f, int size, Color c, float x, float y, float w, float h, TextAnchor a = TextAnchor.UpperLeft)
        { var t = UiKit.Txt(p, s, f, size, c, a); t.rectTransform.Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h), new Vector2(0, 1)); return t; }
        Text Caps(RectTransform p, string s, float x, float y, float w = 300, Color? c = null, TextAnchor a = TextAnchor.UpperLeft, int size = 12) => T(p, UiKit.Caps(s), UiKit.Bold, size, c ?? SubInk, x, y, w, 18, a);
        Image Ico(RectTransform p, string g, Color c, float x, float y, float s) { var i = UiKit.Img(p, Glyphs.Get(g), c, "icon " + g); i.rectTransform.Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(s, s), new Vector2(0, 1)); return i; }
        RectTransform Button(RectTransform r, string label, Action a)
        {
            r.name = "btn " + label;
            var f = UiKit.Card(r, "focus", 20, new Color(0, 0, 0, 0), false, Ponyo.Sunflower); f.Stretch(-4); f.gameObject.SetActive(false);
            var fe = UiKit.Edge(f); fe.color = Ponyo.Hex("E0A82E");
            btns.Add((r, a)); return r;
        }
        RectTransform Pill(RectTransform p, string label, float x, float y, float w, float h, Color fill, Color ink, Action a, Font f = null, int size = 17)
        {
            var c = UiKit.Card(p, "pill", h / 2, fill, true, Color.Lerp(fill, Ponyo.Line, 0.55f), 3); c.Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h), new Vector2(0, 1));
            var t = UiKit.Txt(c, label, f ?? UiKit.Bold, size, ink, TextAnchor.MiddleCenter); t.rectTransform.Stretch();
            return Button(c, label, a);
        }
        Image Avatar(RectTransform p, string who, float x, float y, float s)
        {
            var col = who == Company.MariName ? Ponyo.Hex("2FB5A3") : who == "Notifications" ? Ponyo.Hex("8C7468") : Ponyo.Hex("4C5BB8");
            var a = UiKit.Img(p, UiKit.Circle, col, "avatar"); a.rectTransform.Place(new Vector2(0, 1), new Vector2(x, -y), new Vector2(s, s), new Vector2(0, 1));
            string ini = who == "Notifications" ? "!" : string.Concat(Array.ConvertAll(who.Split(' '), w => w.Length > 0 ? w.Substring(0, 1) : ""));
            var t = UiKit.Txt(a.transform, ini, UiKit.Black, Mathf.RoundToInt(s * 0.36f), Ponyo.Paper, TextAnchor.MiddleCenter); t.rectTransform.Stretch();
            return a;
        }
        void Badge(RectTransform p, int n, Vector2 at)
        {
            if (n <= 0) return;
            var b = UiKit.Img(p, UiKit.Circle, Ponyo.Poppy, "badge"); b.rectTransform.Place(new Vector2(1, 1), at, new Vector2(24, 24), new Vector2(0.5f, 0.5f));
            UiKit.Img(b.transform, UiKit.RingSprite, Ponyo.Paper, "ring").rectTransform.Stretch(-2);
            var t = UiKit.Txt(b.transform, n.ToString(), UiKit.Black, 13, Ponyo.Paper, TextAnchor.MiddleCenter); t.rectTransform.Stretch();
        }
        void Dots(RectTransform p, int n, float x, float y, Color on)
        {
            for (int i = 0; i < 4; i++) UiKit.Img(p, UiKit.Circle, i < n ? on : Ponyo.Hex("DCD2C2"), "dot").rectTransform.Place(new Vector2(0, 1), new Vector2(x + i * 13, -y), new Vector2(9, 9), new Vector2(0, 1));
        }
        void Header(string title, string right = null)
        {
            if (Screen != Scr.Home) { var b = At(list, -6, 6, 40, 40, "back"); Ico(b, "back", Ponyo.Ink, 8, 8, 24); Button(b, "Back", () => Go(Screen == Scr.Thread ? Scr.Messages : Scr.Home)); }
            T(list, title, UiKit.DisplayBold, 32, Ponyo.Ink, 34, 2, 250, 44);
            if (right != null) Caps(list, right, 150, 18, CW - 150, SubInk, TextAnchor.UpperRight);
        }
        static string Money(int v) => (v < 0 ? "−$" : "$") + Math.Abs(v).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        static string Signed(int v) => (v < 0 ? "−$" : "+$") + Math.Abs(v).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        static string When(double at)
        {
            int d = DayTime.Day(at), today = DayTime.Day(Sky.Minutes);
            return (d == today ? "Today " : d == today - 1 ? "Yesterday " : $"Day {d} ") + Clock(at);
        }

        // ---------------------------------------------------------------- the screens
        void Draw()
        {
            foreach (Transform t in list) Destroy(t.gameObject);
            btns.Clear(); dots.Clear(); mapImg = null; mapInfo = null;
            float h = Screen switch
            {
                Scr.Home => Home(), Scr.Messages => Messages(), Scr.Thread => ThreadView(), Scr.Contracts => Contracts(), Scr.Crew => Crew(),
                Scr.Map => MapView(), Scr.Bank => Bank(), Scr.Forecast => Forecast(), Scr.Journal => Journal(), _ => SettingsView(),
            };
            contentH = h + 20; list.sizeDelta = new Vector2(CW, contentH);
            scroll = Mathf.Clamp(scroll, 0, Mathf.Max(0, contentH - (H - 2 * Inset - Bar))); list.anchoredPosition = new Vector2(Pad, scroll);
            if (focus >= btns.Count) focus = -1;
            Mark();
        }

        // Home: the date + big clock, Dot's forecast, the next thing that wants you, the bank, the app grid
        float Home()
        {
            var c = Firm.C; double m = Sky.Minutes; int day = DayTime.Day(m);
            float y = 8;
            Caps(list, $"{DayTime.DayName(day)} · Day {day}", 2, y, 300, SubInk, TextAnchor.UpperLeft, 13); y += 18;
            var big = T(list, Clock(m, false), UiKit.DisplayBold, 70, Ponyo.Ink, -2, y - 4, 260, 84); big.horizontalOverflow = HorizontalWrapMode.Overflow;
            int hh = (int)(DayTime.OfDay(m) / 60);
            if (!H24) T(list, hh < 12 ? "AM" : "PM", UiKit.Bold, 22, Ponyo.Hex("C98A1A"), big.preferredWidth + 6, y + 44, 60, 30);
            y += 86;
            // Dot's forecast: the three blocks of today (tomorrow's from 6 PM: the Forecast app)
            var fc = Card(list, 0, y, CW, 182);
            Caps(fc, "Dot's forecast", 16, 14);
            var (bNow, _) = (Weather.BlockAt(m).b, 0);
            var lines = Sky.W.Forecast(m);
            Caps(fc, DayTime.OfDay(m) >= 1080 || DayTime.OfDay(m) < 420 ? "Tomorrow at 6 PM" : "Today", 140, 14, CW - 156, SubInk, TextAnchor.UpperRight);
            float tw = (CW - 32 - 12) / 3f;
            for (int b = 0; b < 3; b++)
            {
                var ln = lines[b]; bool now = b == Mathf.Min(bNow, 2) && DayTime.OfDay(m) >= 420;
                var tile = Card(fc, 16 + b * (tw + 6), 38, tw, 104, now ? Ponyo.Night : Ponyo.Paper2, 14, false, now ? Ponyo.Night : Ponyo.Hex("E5D8C2"));
                Color ink = now ? Ponyo.Paper : Ponyo.Ink;
                Caps(tile, Weather.BlockName[b], 0, 10, tw, now ? Ponyo.Hex("C9D2F0") : SubInk, TextAnchor.UpperCenter, 12);
                Ico(tile, ln.Shown.ToString(), ink, tw / 2 - 18, 30, 36);
                T(tile, ln.Shown.ToString(), UiKit.DisplayBold, 19, ink, 0, 70, tw, 26, TextAnchor.UpperCenter);
                if (ln.Shown == Wx.Storm) UiKit.Edge(tile).color = Ponyo.Poppy;
            }
            T(fc, "How sure Dot is", UiKit.Body, 14, SubInk, 16, 152, 200, 20);
            Dots(fc, lines[Mathf.Clamp(bNow, 0, 2)].Dots, CW - 16 - 48, 157, Ponyo.SeaC);
            var fb = Button(fc, "Forecast card", () => Go(Scr.Forecast));
            y += 194;
            // next up: an unanswered request, else what Mari's up to, else the next thing worth buying
            var nx = Card(list, 0, y, CW, 76);
            UiKit.Img(nx, null, Ponyo.SeaC, "accent").rectTransform.Place(new Vector2(0, 0.5f), new Vector2(2, 0), new Vector2(5, 52), new Vector2(0, 0.5f));
            string t1, t2, av = null; Action go;
            if (c.Mari == DealSt.Offered) { av = Company.MariName; t1 = "Mari wants to moor here"; t2 = "Mooring request · waiting for you"; go = () => Go(Scr.Thread, Company.MariName); }
            else if (c.Mari == DealSt.Active && !c.MariHere(m)) { av = Company.MariName; t1 = "Mari's on her way"; t2 = "Her taco boat ties up at B1 within the hour"; go = () => Go(Scr.Thread, Company.MariName); }
            else if (c.Mari == DealSt.Active && DayTime.OfDay(m) >= 540 && DayTime.OfDay(m) < 1320 && c.MariToday < Market.MariDaily) { av = Company.MariName; t1 = "Mari buys fish at 100%"; t2 = $"Up to {Market.MariDaily - c.MariToday} more today · sprat, mullet, mackerel"; go = () => Go(Scr.Contracts); }
            else
            {
                var k = Firm.I.MyKit; int need = k.Rod < 2 ? Shelf.RodPrice[2] : !k.Pouch5 ? Shelf.PouchPrice : 0;
                t1 = need == 0 ? "Fish, sell, repeat" : k.Rod < 2 ? "Next: the Fibreglass rod" : "Next: the tackle pouch";
                t2 = need == 0 ? "Pratt pays 60%. Mari pays 100%." : c.Cash >= need ? $"${need} at June's · you can afford it" : $"${need} at June's · need {Money(need - c.Cash)} more";
                go = () => Go(Scr.Bank);
            }
            if (av != null) { Avatar(nx, av, 18, 15, 46); T(nx, t1, UiKit.Bold, 18, Ponyo.Ink, 76, 14, CW - 90, 24); T(nx, t2, UiKit.Body, 14, SubInk, 76, 40, CW - 90, 20); }
            else { Ico(nx, "rod", Ponyo.Ink, 22, 20, 36); T(nx, t1, UiKit.Bold, 18, Ponyo.Ink, 76, 14, CW - 90, 24); T(nx, t2, UiKit.Body, 14, SubInk, 76, 40, CW - 90, 20); }
            Button(nx, "Next up", go);
            y += 88;
            // the bank strip with tomorrow's 07:00 money
            var bk = Card(list, 0, y, CW, 72);
            Caps(bk, "Company bank", 16, 12);
            T(bk, Money(c.Cash), UiKit.DisplayBold, 30, Ponyo.Ink, 14, 28, 200, 40);
            var due = Due(c);
            T(bk, due.Count > 0 ? due[0].what + " at 7 AM" : "Nothing due at 7 AM", UiKit.Body, 14, SubInk, 150, 16, CW - 166, 18, TextAnchor.UpperRight);
            if (due.Count > 0) T(bk, Signed(due[0].amount), UiKit.Bold, 16, due[0].amount >= 0 ? Ponyo.Hex("3E9A4E") : Ponyo.Poppy, 150, 36, CW - 166, 22, TextAnchor.UpperRight);
            Button(bk, "Bank strip", () => Go(Scr.Bank));
            y += 86;
            // the app grid: 4 x 2, unread badges
            var apps = new (string name, string g, Color col, Scr s, int badge, bool locked)[]
            {
                ("Messages", "msg", Ponyo.Hex("2FB5A3"), Scr.Messages, Unread(), false), ("Contracts", "contract", Ponyo.Hex("E39A4C"), Scr.Contracts, c.Mari == DealSt.Offered ? 1 : 0, false),
                ("Crew", "crew", Ponyo.Hex("4C5BB8"), Scr.Crew, 0, true), ("Map", "map", Ponyo.Hex("5B8FE0"), Scr.Map, 0, false),
                ("Bank", "bank", Ponyo.Hex("4FAF6E"), Scr.Bank, 0, false), ("Forecast", "forecast", Ponyo.Hex("4A90C8"), Scr.Forecast, 0, false),
                ("Journal", "journal", Ponyo.Hex("A0704E"), Scr.Journal, 0, false), ("Settings", "settings", Ponyo.Hex("6B6450"), Scr.Settings, 0, false),
            };
            float cw = CW / 4f;
            for (int i = 0; i < apps.Length; i++)
            {
                var a = apps[i]; float x = (i % 4) * cw + (cw - 66) / 2, yy = y + (i / 4) * 100;
                var cell = At(list, (i % 4) * cw, yy, cw, 96, "app");
                var tile = UiKit.Card(cell, "tile", 20, a.col, true, Color.Lerp(a.col, Ponyo.Line, 0.5f), 4); tile.Place(new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(66, 66), new Vector2(0.5f, 1));
                UiKit.Img(tile, null, new Color(1, 1, 1, 0.14f), "shine").rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -5), new Vector2(50, 22), new Vector2(0.5f, 1));
                var ic = UiKit.Img(tile, Glyphs.Get(a.g), Ponyo.Paper, "glyph"); ic.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38, 38));
                if (a.locked) { ic.color = new Color(1, 1, 1, 0.55f); var lk = UiKit.Img(tile, Glyphs.Get("lock"), Ponyo.Paper, "lock"); lk.rectTransform.Place(new Vector2(1, 0), new Vector2(-6, 6), new Vector2(18, 18), new Vector2(1, 0)); }
                Badge(tile, a.badge, new Vector2(-4, -4));
                T(cell, a.name, UiKit.Body, 14, Ponyo.Ink, 0, 72, cw, 20, TextAnchor.UpperCenter);
                var s = a.s; Button(cell, a.name, () => Go(s));
            }
            return y + 200;
        }

        static List<(string what, int amount)> Due(Company c)
        {
            var l = new List<(string, int)>();
            if (c.Mari == DealSt.Active) l.Add(("Mari's rent", c.MariPrice));
            return l;
        }

        // Messages: one thread per contact, and the Notifications (every toast, the 100 newest)
        float Messages()
        {
            var c = Firm.C; Header("Messages", Unread() > 0 ? $"{Unread()} unread" : null);
            float y = 58;
            var who = new List<string>(); for (int i = c.Msgs.Count - 1; i >= 0; i--) if (!who.Contains(c.Msgs[i].From)) who.Add(c.Msgs[i].From);
            foreach (var w in who)
            {
                var last = c.Msgs.FindLast(x => x.From == w); int un = Unread(w);
                var row = Card(list, 0, y, CW, 78, un > 0 ? Ponyo.Hex("FFF4DC") : Ponyo.Paper);
                Avatar(row, w, 14, 15, 48);
                T(row, w, UiKit.Bold, 18, Ponyo.Ink, 74, 13, 180, 24);
                Caps(row, When(last.At), 160, 16, CW - 176, SubInk, TextAnchor.UpperRight, 12);
                var line = (last.Me ? "You: " : "") + last.Text; if (line.Length > 38) line = line.Substring(0, 37) + "…";
                T(row, line, un > 0 ? UiKit.Bold : UiKit.Body, 14, un > 0 ? Ponyo.Ink : SubInk, 74, 42, CW - 110, 22);
                Badge(row, un, new Vector2(-22, -54));
                string ww = w; Button(row, w, () => Go(Scr.Thread, ww));
                y += 88;
            }
            var nr = Card(list, 0, y, CW, 78);
            Avatar(nr, "Notifications", 14, 15, 48);
            T(nr, "Notifications", UiKit.Bold, 18, Ponyo.Ink, 74, 13, 200, 24);
            var lastA = Alerts.Queue.Count > 0 ? Alerts.Queue[^1] : default;
            var nl = Alerts.Queue.Count > 0 ? $"{lastA.Sender}: {lastA.Text}" : "Nothing yet"; if (nl.Length > 38) nl = nl.Substring(0, 37) + "…";
            T(nr, nl, UiKit.Body, 14, SubInk, 74, 42, CW - 96, 22);
            Button(nr, "Notifications", () => Go(Scr.Thread, "Notifications"));
            y += 88;
            if (who.Count == 0) { T(list, "No texts yet. Folks around the harbour text when they want something from the new owner.", UiKit.Body, 15, SubInk, 4, y + 6, CW - 8, 60); y += 70; }
            return y;
        }

        float ThreadView()
        {
            var c = Firm.C; float y = 4;
            var b = At(list, -6, 10, 40, 40, "back"); Ico(b, "back", Ponyo.Ink, 8, 8, 24); Button(b, "Back", () => Go(Scr.Messages));
            Avatar(list, Thread, 38, 4, 48);
            T(list, Thread, UiKit.Bold, 20, Ponyo.Ink, 96, 6, 240, 26);
            T(list, Thread == Company.MariName ? "Mari's Tacos · boat" : Thread == "Notifications" ? "Everything that popped up, newest first" : "", UiKit.Body, 14, SubInk, 96, 32, 250, 20);
            UiKit.Img(list, null, Ponyo.Hex("E5D8C2"), "rule").rectTransform.Place(new Vector2(0, 1), new Vector2(-Pad, -62), new Vector2(W, 2), new Vector2(0, 1));
            y = 76;
            if (Thread == "Notifications")
            {
                for (int i = Alerts.Queue.Count - 1; i >= 0 && i >= Alerts.Queue.Count - 40; i--)
                {
                    var a = Alerts.Queue[i];
                    var st = UiKit.Img(list, UiKit.Circle, Alerts.Col(a.K), "stamp"); st.rectTransform.Place(new Vector2(0, 1), new Vector2(0, -y - 2), new Vector2(26, 26), new Vector2(0, 1));
                    var g = UiKit.Txt(st.transform, Alerts.Glyph(a.K), UiKit.Black, 13, a.K == Alerts.Kind.Info ? Ponyo.Ink : Ponyo.Paper, TextAnchor.MiddleCenter); g.rectTransform.Stretch();
                    Caps(list, a.Sender, 36, y, CW - 36, SubInk, TextAnchor.UpperLeft, 12);
                    var t = T(list, a.Text, UiKit.Body, 15, Ponyo.Ink, 36, y + 16, CW - 36, 20); float th = Mathf.Max(20, t.preferredHeight); t.rectTransform.sizeDelta = new Vector2(CW - 36, th);
                    y += 26 + th;
                }
                return y;
            }
            var msgs = c.Msgs.FindAll(x => x.From == Thread);
            double lastAt = -1e9; int lastOffer = msgs.FindLastIndex(x => x.Offer != null);
            for (int i = 0; i < msgs.Count; i++)
            {
                var msg = msgs[i];
                if (msg.At - lastAt > 90) { Caps(list, When(msg.At), 0, y, CW, SubInk, TextAnchor.UpperCenter, 12); y += 26; }
                lastAt = msg.At;
                float bw = 262;
                var bub = UiKit.Card(list, "bubble", 16, msg.Me ? Ponyo.Hex("D3F1EC") : Ponyo.Hex("F8EEDC"), false, msg.Me ? Ponyo.Hex("A6DCD3") : Ponyo.Hex("E5D3B5"));
                var tx = UiKit.Txt(bub, msg.Text, UiKit.Body, 16, Ponyo.Ink, TextAnchor.UpperLeft); tx.rectTransform.Place(new Vector2(0, 1), new Vector2(14, -10), new Vector2(bw - 28, 10), new Vector2(0, 1));
                float th = tx.preferredHeight; tx.rectTransform.sizeDelta = new Vector2(bw - 28, th);
                float fit = Mathf.Min(bw, tx.preferredWidth + 30);
                if (th <= 24) { tx.rectTransform.sizeDelta = new Vector2(fit - 28, th); bw = fit; }
                bub.Place(new Vector2(0, 1), new Vector2(msg.Me ? CW - bw : 0, -y), new Vector2(bw, th + 20), new Vector2(0, 1));
                y += th + 30;
                if (msg.Offer == "mari" && i == lastOffer) y = OfferCard(c, y) + 10;
            }
            // quick replies (never free text)
            if (Thread == Company.MariName && c.Mari == DealSt.Offered)
            {
                y += 6; float x = 0;
                foreach (var (label, act) in new (string, Action)[] { ("Sounds good!", () => Firm.I.AnswerMari(true)), ("Can you pay more?", () => Firm.I.HaggleMari()), ("Not now", () => Firm.I.AnswerMari(false)) })
                {
                    float w = label.Length * 8.6f + 30; if (x + w > CW) { x = 0; y += 46; }
                    Pill(list, label, x, y, w, 38, Ponyo.Paper, Ponyo.Ink, act, UiKit.Body, 15); x += w + 8;
                }
                y += 46;
            }
            return y;
        }

        // the mooring request, embedded in Mari's thread (and the same card in Contracts)
        float OfferCard(Company c, float y)
        {
            bool live = c.Mari == DealSt.Offered;
            var card = Card(list, 0, y, CW, live ? 236 : 186, Ponyo.Paper, 18, true, Ponyo.Hex("2C4F9E"));
            var head = UiKit.Card(card, "head", 16, Ponyo.Hex("3A72D6"), false, Ponyo.Hex("2C4F9E")); head.Place(new Vector2(0, 1), Vector2.zero, new Vector2(CW, 48), new Vector2(0, 1));
            Ico(head, "anchor", Ponyo.Paper, 16, 12, 24);
            T(head, "Mooring request", UiKit.DisplayBold, 20, Ponyo.Paper, 48, 10, 260, 30);
            float ry = 60;
            foreach (var (k, v) in new[] { ("Boat", "Mari's Tacos · 7 m"), ("Berth", "B1 · old pier"), ("From", c.Mari == DealSt.Active ? $"Day {c.MariFirstPay}, 7 AM" : "Tomorrow, 7 AM") })
            { T(card, k, UiKit.Body, 14, SubInk, 16, ry, 120, 20); T(card, v, UiKit.Body, 14, Ponyo.Ink, 120, ry, CW - 136, 20, TextAnchor.UpperRight); ry += 24; }
            T(card, "Pays", UiKit.Body, 14, SubInk, 16, ry + 6, 120, 20);
            T(card, $"${c.MariPrice} / day", UiKit.DisplayBold, 24, Ponyo.Hex("C98A1A"), 120, ry, CW - 136, 32, TextAnchor.UpperRight);
            ry += 40;
            if (live)
            {
                float bw = (CW - 16 * 2 - 10) / 2;
                Pill(card, "Decline", 16, ry, bw, 46, Ponyo.Paper2, Ponyo.Ink, () => Firm.I.AnswerMari(false));
                Pill(card, "Accept", 16 + bw + 10, ry, bw, 46, Ponyo.Sunflower, Ponyo.Ink, () => Firm.I.AnswerMari(true));
            }
            else
            {
                string s = c.Mari switch
                {
                    DealSt.Active => $"Accepted{(Coop.On && c.MariBy != null ? " by " + c.MariBy : "")} · {(c.MariHere(Sky.Minutes) ? "tied up at B1" : "on her way")}",
                    DealSt.Declined => $"Declined · she asks again tomorrow at ${Company.MariFee + Company.MariStep * c.MariAsk}",
                    DealSt.GaveUp => "Declined · she found another spot",
                    _ => "",
                };
                T(card, s, UiKit.Bold, 14, c.Mari == DealSt.Active ? Ponyo.Hex("3E9A4E") : SubInk, 16, ry - 4, CW - 32, 22);
            }
            return y + (live ? 236 : 186);
        }

        float Contracts()
        {
            var c = Firm.C; double m = Sky.Minutes;
            Header("Contracts", c.Mari == DealSt.Active ? "1 active" : c.Mari == DealSt.Offered ? "1 offered" : null);
            float y = 58;
            if (c.Mari == DealSt.None) { T(list, "No deals yet. Earn a bit and the harbour will notice the new owner: Mari has asked about Walt's old berth.", UiKit.Body, 15, SubInk, 4, y, CW - 8, 80); return y + 80; }
            var card = Card(list, 0, y, CW, 262, Ponyo.Paper, 18, true);
            Avatar(card, Company.MariName, 16, 16, 44);
            T(card, "Mari's mooring", UiKit.DisplayBold, 21, Ponyo.Ink, 72, 14, 220, 28);
            T(card, "Mari Duarte · Mari's Tacos", UiKit.Body, 14, SubInk, 72, 42, 240, 20);
            string status = c.Mari switch { DealSt.Offered => "Waiting for you", DealSt.Active => c.MariHere(m) ? "Active · tied up at B1" : "Active · on her way", DealSt.Declined => "Declined", _ => "She found another spot" };
            var chip = UiKit.Card(card, "status", 12, c.Mari == DealSt.Active ? Ponyo.Hex("DDF1E0") : c.Mari == DealSt.Offered ? Ponyo.Hex("FFF0CC") : Ponyo.Paper2, false); chip.Place(new Vector2(1, 1), new Vector2(-14, -16), new Vector2(110, 24), new Vector2(1, 1));
            var ct = UiKit.Txt(chip, UiKit.Caps(c.Mari == DealSt.Active ? "Active" : c.Mari == DealSt.Offered ? "Offered" : c.Mari == DealSt.Declined ? "Declined" : "Closed"), UiKit.Bold, 12, Ponyo.Ink, TextAnchor.MiddleCenter); ct.rectTransform.Stretch();
            float ry = 78; int paid = c.Ledger.FindAll(e => e.Tx.StartsWith("rent:mari")).Count;
            foreach (var (k, v) in new[] { ("Pays", $"${c.MariPrice} a day at 7 AM"), ("Berth", "B1 · old pier"), ("Status", status), ("Paid so far", paid > 0 ? $"{paid} × ${c.MariPrice} = {Money(paid * c.MariPrice)}" : "Nothing yet"), ("Perk", "Buys 10 fish a day at 100%") })
            { T(card, k, UiKit.Body, 14, SubInk, 16, ry, 120, 20); T(card, v, UiKit.Body, 14, Ponyo.Ink, 110, ry, CW - 126, 20, TextAnchor.UpperRight); ry += 25; }
            ry += 8; float bw = (CW - 32 - 10) / 2;
            if (c.Mari == DealSt.Offered) { Pill(card, "Decline", 16, ry, bw, 44, Ponyo.Paper2, Ponyo.Ink, () => Firm.I.AnswerMari(false)); Pill(card, "Accept", 26 + bw, ry, bw, 44, Ponyo.Sunflower, Ponyo.Ink, () => Firm.I.AnswerMari(true)); }
            else if (c.Mari == DealSt.Active) Pill(card, c.MariTracked ? "Tracking ✓" : "Track", 16, ry, CW - 32, 44, c.MariTracked ? Ponyo.Hex("DDF1E0") : Ponyo.Paper2, Ponyo.Ink, () => Firm.I.Track(!c.MariTracked));
            else T(card, c.Mari == DealSt.Declined ? $"She'll ask again tomorrow at 7 AM (${Company.MariFee + Company.MariStep * c.MariAsk} a day)." : "Mari gave up after three noes.", UiKit.Body, 14, SubInk, 16, ry + 10, CW - 32, 40);
            return y + 280;
        }

        float Crew()
        {
            Header("Crew");
            var card = Card(list, 0, 70, CW, 250, Ponyo.Paper2);
            Ico(card, "lock", SubInk, CW / 2 - 28, 30, 56);
            T(card, "Rosie's hiring board", UiKit.DisplayBold, 22, Ponyo.Ink, 0, 100, CW, 30, TextAnchor.UpperCenter);
            T(card, "Opens in Act 2. Hire deckhands at The Mooring Line, give them a boat and bait, and they fish while you do something else.", UiKit.Body, 15, SubInk, 24, 136, CW - 48, 90, TextAnchor.UpperCenter);
            return 340;
        }

        float Bank()
        {
            var c = Firm.C; double m = Sky.Minutes; Header("Bank", "Company account");
            float y = 58;
            var bal = Card(list, 0, y, CW, 128, Ponyo.Hex("EAF6EC"), 18, true, Ponyo.Hex("A9D3B1"));
            Caps(bal, "Company bank", 18, 16);
            T(bal, Money(c.Cash), UiKit.DisplayBold, 46, Ponyo.Ink, 16, 34, CW - 32, 56);
            double from = DayTime.At(DayTime.Day(m - DayTime.Wake), DayTime.Wake);
            var (inS, outS, _) = c.DaySummary(from, m + 1);
            T(bal, $"Today {Signed(inS)} in · {Money(outS)} out", UiKit.Body, 14, SubInk, 18, 96, CW - 36, 20);
            y += 142;
            Caps(list, "At 7 AM tomorrow", 2, y); y += 22;
            var due = Due(c);
            if (due.Count == 0) { T(list, "Nothing due. No wages, no upkeep yet.", UiKit.Body, 14, SubInk, 2, y, CW, 20); y += 30; }
            foreach (var d in due) { var r = Card(list, 0, y, CW, 46); T(r, d.what, UiKit.Body, 15, Ponyo.Ink, 14, 12, 200, 22); T(r, Signed(d.amount), UiKit.Bold, 16, d.amount >= 0 ? Ponyo.Hex("3E9A4E") : Ponyo.Poppy, 140, 12, CW - 154, 22, TextAnchor.UpperRight); y += 54; }
            y += 6; Caps(list, $"Ledger · {c.Ledger.Count} lines", 2, y); y += 24;
            if (c.Ledger.Count == 0) { T(list, "Every dollar in and out shows up here: who, what and when.", UiKit.Body, 14, SubInk, 2, y, CW, 40); y += 40; }
            for (int i = c.Ledger.Count - 1; i >= 0 && i >= c.Ledger.Count - 40; i--)
            {
                var e = c.Ledger[i];
                Fit(T(list, e.What, UiKit.Bold, 15, Ponyo.Ink, 2, y, CW - 100, 22), 13);   // one line: a long one shrinks a little
                T(list, $"{e.Who} · {When(e.At)}", UiKit.Body, 14, SubInk, 2, y + 21, CW - 100, 18);
                T(list, Signed(e.Amount), UiKit.Bold, 16, e.Amount >= 0 ? Ponyo.Hex("3E9A4E") : Ponyo.Poppy, CW - 110, y + 6, 108, 22, TextAnchor.UpperRight);
                UiKit.Img(list, null, Ponyo.Hex("EADFCC"), "rule").rectTransform.Place(new Vector2(0, 1), new Vector2(0, -y - 46), new Vector2(CW, 1.5f), new Vector2(0, 1));
                y += 52;
            }
            return y;
        }

        float Forecast()
        {
            double m = Sky.Minutes; Header("Forecast", "Dot · the lighthouse");
            float y = 58; var lines = Sky.W.Forecast(m);
            bool storm = lines.Exists(l => l.Shown == Wx.Storm);
            if (storm)
            {
                var sc = Card(list, 0, y, CW, 64, Ponyo.Hex("FCE3DD"), 16, false, Ponyo.Poppy);
                Ico(sc, "Storm", Ponyo.Poppy, 14, 14, 36);
                var sl = lines.Find(l => l.Shown == Wx.Storm);
                T(sc, $"Storm {(sl.Day == DayTime.Day(m) ? "today" : "tomorrow")}, {Weather.BlockName[sl.Block].ToLower()}", UiKit.Bold, 16, Ponyo.Poppy, 62, 12, CW - 76, 22);
                T(sc, "Get Biscuit tied up before it hits.", UiKit.Body, 14, Ponyo.Ink, 62, 34, CW - 76, 20);
                y += 76;
            }
            var dotLine = T(list, "“" + Sky.Word(Sky.W.Block(DayTime.Day(m), Mathf.Min(Weather.BlockAt(m).b, 2))) + "”", UiKit.Display, 17, Ponyo.Ink, 2, y, CW - 4, 48);
            y += Mathf.Max(30, dotLine.preferredHeight) + 10;
            for (int part = 0; part < 2; part++)
            {
                var ls = lines.FindAll(l => part == 0 ? l.Day == lines[0].Day : l.Day != lines[0].Day);
                if (part == 1 && ls.Count == 0) { var nc = Card(list, 0, y, CW, 56, Ponyo.Paper2); T(nc, "Tomorrow's forecast comes at 6 PM.", UiKit.Body, 15, SubInk, 16, 18, CW - 32, 22); y += 66; break; }
                Caps(list, part == 0 ? $"Today · {DayTime.DayName(lines[0].Day)}" : $"Tomorrow · {DayTime.DayName(ls[0].Day)}", 2, y); y += 22;
                var card = Card(list, 0, y, CW, ls.Count * 62 + 8);
                for (int i = 0; i < ls.Count; i++)
                {
                    var l = ls[i]; float ry = 8 + i * 62; Color ink = l.Shown == Wx.Storm ? Ponyo.Poppy : Ponyo.Ink;
                    Ico(card, l.Shown.ToString(), ink, 14, ry + 8, 40);
                    T(card, Weather.BlockName[l.Block], UiKit.Bold, 17, Ponyo.Ink, 66, ry + 8, 150, 22);
                    T(card, l.Shown.ToString(), UiKit.Body, 14, ink, 66, ry + 30, 150, 20);
                    T(card, "how sure", UiKit.Body, 14, SubInk, CW - 120, ry + 12, 64, 18);
                    Dots(card, l.Dots, CW - 70, ry + 16, Ponyo.SeaC);
                    if (i < ls.Count - 1) UiKit.Img(card, null, Ponyo.Hex("EADFCC"), "rule").rectTransform.Place(new Vector2(0, 1), new Vector2(14, -ry - 60), new Vector2(CW - 28, 1.5f), new Vector2(0, 1));
                }
                y += ls.Count * 62 + 20;
            }
            T(list, "Four dots: Dot's sure of it. One: she's guessing.", UiKit.Body, 14, SubInk, 2, y, CW, 20);
            return y + 26;
        }

        float Journal()
        {
            var k = Firm.I.MyKit; int n = 0; foreach (var s in FishDb.All) if (k.Best.ContainsKey(s.Id)) n++;
            Header("Journal", $"{n} / {FishDb.All.Count} fish");
            float y = 58;
            T(list, "Walt's fish book. Every fish you land goes in.", UiKit.Body, 14, SubInk, 2, y, CW, 20); y += 30;
            foreach (var s in FishDb.All)
            {
                bool got = k.Best.TryGetValue(s.Id, out var best);
                var row = Card(list, 0, y, CW, 60, got ? Ponyo.Paper : Ponyo.Paper2);
                if (got && Hud.Icons.TryGetValue(s.Id, out var tex)) { var ri = UiKit.Rect("icon", row).Place(new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(72, 36), new Vector2(0, 0.5f)).gameObject.AddComponent<RawImage>(); ri.texture = tex; ri.raycastTarget = false; }
                else Ico(row, "fish", Ponyo.Hex("CDBFA8"), 26, 12, 36);
                T(row, got ? s.Name : "???", UiKit.Bold, 16, got ? Ponyo.Ink : SubInk, 92, 10, 200, 22);
                T(row, got ? $"Best {best:0} cm · {s.Class}" : $"{s.Class} · rod tier {s.RodTier}", UiKit.Body, 14, SubInk, 92, 32, 220, 18);
                if (got) T(row, $"${s.Price:0}", UiKit.Bold, 14, Ponyo.Hex("C98A1A"), CW - 80, 20, 66, 20, TextAnchor.UpperRight);
                y += 66;
            }
            return y;
        }

        float SettingsView()
        {
            Header("Settings");
            float y = 58;
            void Row(string name, string value, Action minus, Action plus)
            {
                var r = Card(list, 0, y, CW, 58);
                T(r, name, UiKit.Bold, 16, Ponyo.Ink, 16, 18, 170, 22);
                if (minus != null)
                {
                    var bm = UiKit.Card(r, "minus", 15, Ponyo.Paper2, false); bm.Place(new Vector2(1, 0.5f), new Vector2(-120, 0), new Vector2(30, 30), new Vector2(1, 0.5f));
                    UiKit.Img(bm, Glyphs.Get("minus"), Ponyo.Ink, "g").rectTransform.Stretch(7); Button(bm, name + " −", minus);
                }
                var vt = UiKit.Txt(r, value, UiKit.Bold, 15, Ponyo.Ink, TextAnchor.MiddleCenter); vt.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(minus != null ? -50 : -14, 0), new Vector2(minus != null ? 72 : 110, 30), new Vector2(1, 0.5f));
                if (plus != null && minus != null)
                {
                    var bp = UiKit.Card(r, "plus", 15, Ponyo.Paper2, false); bp.Place(new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(30, 30), new Vector2(1, 0.5f));
                    UiKit.Img(bp, Glyphs.Get("plus"), Ponyo.Ink, "g").rectTransform.Stretch(7); Button(bp, name + " +", plus);
                }
                else if (plus != null) Button(r, name, plus);
                y += 66;
            }
            Row("Mouse look", $"{Settings.MouseSens:0.0}×", () => SetF("m1.mouse", ref Settings.MouseSens, -0.1f, 0.2f, 3f), () => SetF("m1.mouse", ref Settings.MouseSens, 0.1f, 0.2f, 3f));
            Row("Pad look", $"{Settings.PadSens:0.0}×", () => SetF("m1.pad", ref Settings.PadSens, -0.1f, 0.2f, 3f), () => SetF("m1.pad", ref Settings.PadSens, 0.1f, 0.2f, 3f));
            Row("Invert look", Settings.Invert ? "On" : "Off", null, () => Settings.Invert = !Settings.Invert);
            Row("Bar timing", $"{(Settings.TimingOffsetMs >= 0 ? "+" : "")}{Settings.TimingOffsetMs:0} ms", () => SetF("m1.offset", ref Settings.TimingOffsetMs, -10, -100, 100), () => SetF("m1.offset", ref Settings.TimingOffsetMs, 10, -100, 100));
            Row("Voice chat", Settings.Voice == Settings.VoiceMode.Open ? "Open mic" : Settings.Voice == Settings.VoiceMode.PushToTalk ? "Hold V" : "Off", null, () =>
            {
                Settings.Voice = (Settings.VoiceMode)(((int)Settings.Voice + 1) % 3);
                try { PlayerPrefs.SetString("m1.voice", Settings.Voice == Settings.VoiceMode.Open ? "open" : Settings.Voice == Settings.VoiceMode.PushToTalk ? "ptt" : "off"); PlayerPrefs.Save(); } catch { }
            });
            Row("Phone clock", H24 ? "24-hour" : "12-hour", null, () => { H24 = !H24; try { PlayerPrefs.SetInt("m4.h24", H24 ? 1 : 0); } catch { } });
            y += 6;
            T(list, "Bar timing nudges the fishing bar if your screen or pad lags. Esc pauses; F3 shows the debug overlay.", UiKit.Body, 14, SubInk, 2, y, CW - 4, 60);
            return y + 60;
        }
        static void SetF(string k, ref float v, float d, float lo, float hi)
        {
            v = Mathf.Clamp(Mathf.Round((v + d) * 100) / 100, lo, hi);
            try { PlayerPrefs.SetFloat(k, v); PlayerPrefs.Save(); } catch { }
        }

        // ---------------------------------------------------------------- the map: the island chart from map B
        // World metres: our dock at the origin, +x east, +z north (map B's sheet: x = svg x - 2750, z = 2550 - svg y).
        const float MapW = CW, MapH = 560, MapX0 = -2050, MapZ0 = 2420, MapScale = 3700f / CW;   // metres per pixel
        static Vector2 ToMap(Vector3 w) => new((w.x - MapX0) / MapScale, (MapZ0 - w.z) / MapScale);
        static Vector3 FromSvg(float x, float y) => new(x - 2750, 0, 2550 - y);

        // places on the chart: the map's anchors when map B is in, else map B's sheet positions
        static readonly (string id, string name, string kind, float sx, float sy)[] Places =
        {
            ("tackle", "Hook, Line & Sinker", "shop", 2900, 2480), ("pratts", "Pratt's Fish Market", "shop", 3000, 2570), ("doyle", "Doyle Boatworks", "shop", 2880, 2620),
            ("plank", "Plank & Post", "shop", 2960, 2650), ("pub", "The Mooring Line", "shop", 2950, 2510), ("surf", "Swell Surf Co.", "shop", 2600, 3260),
            ("post", "Post office", "shop", 2990, 2440), ("bay_shallows", "Bay Shallows", "fish", 2500, 2700), ("gullrock", "Gullrock", "fish", 2250, 2550),
            ("lantern_key", "Lantern Key", "fish", 1100, 2600), ("wreck", "Wreck of the Margaret", "fish", 1000, 1700), ("lighthouse", "Dot's lighthouse", "place", 1880, 1640),
        };

        float MapView()
        {
            float y = 0;
            float x = 0;
            foreach (var f in new[] { "All", "Fish", "Shops", "Crew" })
            {
                float w = f.Length * 9 + 34; var ff = f;
                Pill(list, f, x, y + 4, w, 32, mapFilter == f ? Ponyo.Night : Ponyo.Paper, mapFilter == f ? Ponyo.Paper : Ponyo.Ink, () => mapFilter = ff, UiKit.Bold, 14);
                x += w + 6;
            }
            y += 46;
            var frame = Card(list, 0, y, MapW, MapH, Ponyo.Hex("9ADBD3"), 18, false, Ponyo.Hex("6BB7AE"));
            var mask = UiKit.Rect("mask", frame); mask.Stretch(2); mask.gameObject.AddComponent<RectMask2D>();
            mapImg = UiKit.Rect("chart", mask).Place(new Vector2(0, 1), Vector2.zero, new Vector2(MapW, MapH), new Vector2(0, 1)).gameObject.AddComponent<RawImage>();
            mapImg.texture = Chart(); mapImg.raycastTarget = false;
            // pins (the labels below step off them)
            var placed = new List<Rect>();
            foreach (var pl in Places)
            {
                if (mapFilter == "Crew") continue;
                if (mapFilter == "Fish" && pl.kind != "fish") continue;
                if (mapFilter == "Shops" && pl.kind != "shop") continue;
                var a = Firm.Anchor(pl.id); var w = a != null ? a.position : FromSvg(pl.sx, pl.sy);
                if (pl.id == "tackle" || pl.id == "pratts") { var st = Firm.I.Stalls.Find(s => s.Id == pl.id); if (st != null && st.Real) w = st.At.position; }
                Color col = pl.kind == "fish" ? Ponyo.SeaDeep : pl.kind == "shop" ? Ponyo.Hex("E39A4C") : Ponyo.Paper;
                bool shut = pl.id == "tackle" && !Firm.I.Stalls[0].IsOpen(Sky.Minutes) || pl.id == "pratts" && !Market.IsOpen(Buyer.Pratt, Sky.Minutes);
                if (shut) col = Ponyo.Hex("B5A99A");
                var pin = UiKit.Img(mapImg.rectTransform, pl.kind == "fish" ? UiKit.RingSprite : UiKit.Circle, col, "pin " + pl.id); var mp = ToMap(w);
                pin.rectTransform.Place(new Vector2(0, 1), new Vector2(mp.x, -mp.y), pl.kind == "fish" ? new Vector2(16, 16) : new Vector2(11, 11), new Vector2(0.5f, 0.5f));
                float ps = pl.kind == "fish" ? 16 : 11; placed.Add(new Rect(mp.x - ps / 2, mp.y - ps / 2, ps, ps));
                if (pl.kind != "fish") UiKit.Img(pin.transform, UiKit.RingSprite, Ponyo.Paper, "ring").rectTransform.Stretch(-2);
            }
            // labels (map B's names), most important first: each one inside the frame, nudged up or down off the ones
            // already placed and off the pins (the harbour names sit close together at this scale); drawn over the pins
            void Label(string s, float sx, float sy, int size, Color c, bool bold = true, bool italic = false)
            {
                var p = ToMap(FromSvg(sx, sy));
                var t = UiKit.Txt(mapImg.rectTransform, italic ? s : UiKit.Caps(s), italic ? UiKit.Display : bold ? UiKit.Bold : UiKit.Body, size, c, TextAnchor.MiddleCenter);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                float w = t.preferredWidth + 6, h = size + 4, cx = Mathf.Clamp(p.x, w / 2 + 4, MapW - w / 2 - 4);
                var at = new Rect(cx - w / 2, p.y - h / 2, w, h);
                foreach (var dy in new[] { 0f, 1, -1, 2, -2, 3, -3 })
                {
                    var r = new Rect(at.x, Mathf.Clamp(p.y + dy * (h + 2), h / 2 + 4, MapH - h / 2 - 4) - h / 2, w, h);
                    if (!placed.Exists(o => o.Overlaps(r))) { at = r; break; }
                }
                placed.Add(at);
                t.rectTransform.Place(new Vector2(0, 1), new Vector2(at.center.x, -at.center.y), new Vector2(w, h), new Vector2(0.5f, 0.5f));
            }
            Color ink = Ponyo.Hex("2F5D62"), land = Ponyo.Hex("5E5035");
            Label("Hale's Landing", 2640, 2400, 11, Ponyo.Hex("C2552D")); Label("Old Saltmouth", 3330, 2330, 11, land);
            Label("Crescent Isle", 3380, 2950, 16, land, false, true); Label("Voss Harbour", 3950, 3030, 10, Ponyo.Hex("B03A2E"));
            Label("Gullrock", 2250, 2420, 10, ink); Label("Lantern Key", 1100, 2420, 10, ink); Label("Sorrel Isle", 4100, 1050, 10, ink);
            Label("Mango Cay", 4300, 4120, 10, ink); Label("Breaker Point", 2800, 4000, 10, ink);
            Label("Wreck of the Margaret", 1000, 1600, 10, ink);
            // live dots: crewmates, Mari's boat, Biscuit, me (on top)
            if (mapFilter is "All" or "Crew")
            {
                if (Coop.On && Coop.I != null) foreach (var kv in Coop.I.Mates) { var a = kv.Value; if (a != null) Dot(a.Colour, 12, () => a != null ? a.transform.position : (Vector3?)null); }
                if (Firm.C.MariHere(Sky.Minutes)) { var mb = Firm.Anchor("mari_boat"); var mpos = mb != null ? mb.position : Firm.I.Stalls[2].At.position; Dot(Ponyo.Hex("2FB5A3"), 10, () => mpos); }
                if (Skiff.I != null) Dot(Ponyo.SeaC, 13, () => Skiff.I != null ? Skiff.I.transform.position : (Vector3?)null, true);
            }
            Dot(Ponyo.Ink, 14, () => Angler.I != null ? Angler.I.transform.position : (Vector3?)null, true);
            y += MapH + 10;
            // what's here
            var info = Card(list, 0, y, CW, 104);
            mapInfo = T(info, "", UiKit.DisplayBold, 19, Ponyo.Ink, 16, 12, CW - 32, 26);
            var sub = T(info, "", UiKit.Body, 14, SubInk, 16, 40, CW - 32, 20); sub.name = "sub";
            float lx = 16;
            foreach (var (lab, col) in Legend())
            {
                UiKit.Img(info, UiKit.Circle, col, "key").rectTransform.Place(new Vector2(0, 1), new Vector2(lx, -76), new Vector2(10, 10), new Vector2(0, 1));
                var lt = Caps(info, lab, lx + 14, 72, 120, Ponyo.Ink, TextAnchor.UpperLeft, 11); lt.horizontalOverflow = HorizontalWrapMode.Overflow;
                lx += 24 + lab.Length * 8;
            }
            return y + 114;
        }
        List<(string, Color)> Legend()
        {
            var l = new List<(string, Color)> { ("You", Ponyo.Ink), ("Biscuit", Ponyo.SeaC) };
            if (Coop.On && Coop.I != null) foreach (var kv in Coop.I.Mates) if (kv.Value != null && l.Count < 4) l.Add((kv.Value.NetName ?? "Crew", kv.Value.Colour));
            if (l.Count < 4) l.Add(("Shops", Ponyo.Hex("E39A4C")));
            return l;
        }
        void Dot(Color c, float s, Func<Vector3?> at, bool ring = false)
        {
            var d = UiKit.Img(mapImg.rectTransform, UiKit.Circle, c, "dot"); d.rectTransform.Place(new Vector2(0, 1), Vector2.zero, new Vector2(s, s), new Vector2(0.5f, 0.5f));
            UiKit.Img(d.transform, UiKit.RingSprite, Ponyo.Paper, "ring").rectTransform.Stretch(-2.5f);
            dots.Add((d.rectTransform, at));
        }
        void MapDots()
        {
            foreach (var (r, at) in dots) { var p = at(); if (r == null) continue; r.gameObject.SetActive(p.HasValue); if (p.HasValue) { var mp = ToMap(p.Value); r.anchoredPosition = new Vector2(Mathf.Clamp(mp.x, 4, MapW - 4), -Mathf.Clamp(mp.y, 4, MapH - 4)); } }
            if (mapInfo != null && Angler.I != null)
            {
                var me = Angler.I.transform.position; var home = Landing.I != null ? Landing.I.transform.position : Vector3.zero;
                string near = "Open water"; float bd = 1e9f;
                foreach (var pl in Places) { var a = Firm.Anchor(pl.id); var w = a != null ? a.position : FromSvg(pl.sx, pl.sy); float d = Vector3.Distance(new Vector3(w.x, 0, w.z), new Vector3(me.x, 0, me.z)); if (d < bd) { bd = d; near = pl.name; } }
                float dh = Vector3.Distance(new Vector3(home.x, 0, home.z), new Vector3(me.x, 0, me.z));
                if (dh < 120) { near = "Hale's Landing"; bd = dh; }
                mapInfo.text = bd < 250 ? near : "Out at sea";
                var sub = mapInfo.transform.parent.Find("sub")?.GetComponent<Text>();
                if (sub != null) sub.text = dh < 60 ? "You're here · our dock, inside the bay" : $"You're here · {(dh >= 1000 ? (dh / 1000f).ToString("0.0") + " km" : dh.ToString("0") + " m")} from Hale's Landing";
            }
        }

        // the chart (painted once): a pale sea with wave hatching, the shallows, sand and land of map B's outline
        static Texture2D Chart()
        {
            if (chart != null) return chart;
            int w = (int)MapW, h = (int)MapH;
            var island = Smooth(new[] { (1900, 1700), (2300, 1450), (2900, 1400), (3400, 1650), (3700, 2100), (3780, 2600), (3650, 3150), (3300, 3550), (2800, 3700), (2250, 3600), (1900, 3350), (2150, 3200), (2550, 3250), (2850, 3050), (3000, 2650), (2950, 2200), (2700, 1900), (2300, 1880), (2050, 1950) });
            var islets = new List<Vector2[]>();
            foreach (var (x, y, rx, ry) in new[] { (2250f, 2550f, 120f, 90f), (1100f, 2600f, 220f, 120f), (4100f, 900f, 120f, 90f), (4300f, 4300f, 150f, 110f) })
            {
                var pts = new (int, int)[12]; for (int i = 0; i < 12; i++) { float a = i * Mathf.PI / 6; pts[i] = ((int)(x + rx * Mathf.Cos(a) * (1 + .12f * Mathf.Sin(3 * a))), (int)(y + ry * Mathf.Sin(a) * (1 + .1f * Mathf.Cos(2 * a)))); }
                islets.Add(Smooth(pts));
            }
            var voss = Smooth(new[] { (3700, 2350), (3980, 2300), (4080, 2600), (3980, 2900), (3720, 2850) });
            var town = Smooth(new[] { (2880, 2450), (2990, 2400), (3030, 2560), (2960, 2700), (2870, 2640) });
            Color sea = Ponyo.Hex("A6E1DA"), sea2 = Ponyo.Hex("8ED3CB"), shallow = Ponyo.Hex("C9F0EA"), sand = Ponyo.Hex("EADBC0"), landC = Ponyo.Hex("D9C796"), green = Ponyo.Hex("B8C98A"), townC = Ponyo.Hex("E6B98F"), grey = Ponyo.Hex("B9BFC4"), coast = Ponyo.Hex("7E6A4E");
            var px = new Color[w * h];
            for (int j = 0; j < h; j++) for (int i = 0; i < w; i++)
                {
                    // pixel centre -> svg metres
                    float wx = MapX0 + (i + 0.5f) * MapScale, wz = MapZ0 - (j + 0.5f) * MapScale;
                    var q = new Vector2(wx + 2750, 2550 - wz);
                    float d = SDist(q, island); foreach (var il in islets) d = Mathf.Min(d, SDist(q, il));
                    // the sea: wave hatching, darker away from land
                    float wave = Mathf.Sin((q.x * 0.03f) + Mathf.Sin(q.y * 0.012f) * 2.2f) * Mathf.Sin(q.y * 0.045f);
                    Color c = Color.Lerp(sea, sea2, Mathf.Clamp01(d / 900f));
                    if (wave > 0.82f && d > 60) c = Color.Lerp(c, sea2 * 0.92f, 0.6f);
                    if (d < 90) c = Color.Lerp(shallow, c, Mathf.Clamp01(d / 90f));
                    float px1 = MapScale;
                    if (d < 0) { c = Color.Lerp(sand, landC, Mathf.Clamp01(-d / 40f)); if (-d > 90) c = Color.Lerp(landC, green, Mathf.Clamp01((-d - 90) / 160f)); }
                    float coastA = Mathf.Clamp01(1 - Mathf.Abs(d) / (px1 * 0.9f));
                    c = Color.Lerp(c, coast, coastA * 0.8f);
                    float dv = SDist(q, voss); if (dv < 0) c = grey; else c = Color.Lerp(c, coast, Mathf.Clamp01(1 - Mathf.Abs(dv) / px1) * 0.6f);
                    if (SDist(q, town) < 0) c = townC;
                    px[(h - 1 - j) * w + i] = c;
                }
            chart = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "phone chart" };
            chart.SetPixels(px); chart.Apply();
            return chart;
        }
        static Vector2[] Smooth((int, int)[] pts)
        {
            var o = new List<Vector2>(); int n = pts.Length;
            Vector2 P(int i) { var (x, y) = pts[(i + n) % n]; return new Vector2(x, y); }
            for (int i = 0; i < n; i++)
                for (int k = 0; k < 8; k++)
                {
                    float t = k / 8f; Vector2 p0 = P(i - 1), p1 = P(i), p2 = P(i + 1), p3 = P(i + 2);
                    o.Add(0.5f * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t));
                }
            return o.ToArray();
        }
        // signed distance to a closed polygon (negative inside)
        static float SDist(Vector2 p, Vector2[] v)
        {
            float d = 1e9f; bool inside = false;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
            {
                Vector2 a = v[j], b = v[i], ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                d = Mathf.Min(d, (p - (a + ab * t)).sqrMagnitude);
                if ((b.y > p.y) != (a.y > p.y) && p.x < (a.x - b.x) * (p.y - b.y) / (a.y - b.y) + b.x) inside = !inside;
            }
            d = Mathf.Sqrt(d); return inside ? -d : d;
        }
    }

    // The HUD's bank pill (top-right, shows on a change and pulses) and the Today list under it (tracked deals, max 3)
    // (gdd systems §2.5).
    public class PhoneHud : MonoBehaviour
    {
        RectTransform pill, today; Text cash; Text todayT; int shown = int.MinValue; float showT; string todayKey;
        public static void Make() { var g = new GameObject("PhoneHud"); g.AddComponent<PhoneHud>().Build(); }
        void Build()
        {
            var hud = (RectTransform)Hud.I.transform;
            pill = UiKit.Card(hud, "bank pill", 20, Ponyo.Paper, true); pill.Place(new Vector2(1, 1), new Vector2(-40, -32), new Vector2(200, 46), new Vector2(1, 1));
            var cap = UiKit.Txt(pill, "BANK", UiKit.Bold, 12, Phone.SubInk, TextAnchor.MiddleLeft); cap.rectTransform.Stretch(); cap.rectTransform.offsetMin = new Vector2(18, 0);
            cash = UiKit.Txt(pill, "", UiKit.DisplayBold, 24, Ponyo.Ink, TextAnchor.MiddleRight); cash.rectTransform.Stretch(); cash.rectTransform.offsetMax = new Vector2(-18, 0);
            today = UiKit.Card(hud, "today", 18, Ponyo.Paper, true); today.Place(new Vector2(1, 1), new Vector2(-40, -90), new Vector2(300, 70), new Vector2(1, 1));
            var tc = UiKit.Txt(today, "TODAY", UiKit.Bold, 12, Phone.SubInk, TextAnchor.UpperLeft); tc.rectTransform.Stretch(); tc.rectTransform.offsetMin = new Vector2(18, 0); tc.rectTransform.offsetMax = new Vector2(-18, -12);
            todayT = UiKit.Txt(today, "", UiKit.Body, 15, Ponyo.Ink, TextAnchor.UpperLeft); todayT.rectTransform.Stretch(); todayT.rectTransform.offsetMin = new Vector2(18, 8); todayT.rectTransform.offsetMax = new Vector2(-18, -32);
            today.gameObject.SetActive(false); pill.gameObject.SetActive(false);
        }
        void LateUpdate()
        {
            var c = Firm.C; if (c == null || Title.Shown) { pill.gameObject.SetActive(false); today.gameObject.SetActive(false); return; }
            if (c.Cash != shown) { if (shown != int.MinValue) showT = 5; shown = c.Cash; cash.text = "$" + c.Cash.ToString("N0", System.Globalization.CultureInfo.InvariantCulture); }
            showT -= Time.unscaledDeltaTime;
            bool on = showT > 0 || Phone.IsOpen || Shop.Open;
            if (pill.gameObject.activeSelf != on) pill.gameObject.SetActive(on);
            float pulse = showT > 4.4f ? 1 + 0.08f * Mathf.Sin((5 - showT) * Mathf.PI / 0.6f) : 1; pill.localScale = Vector3.one * pulse;
            // the Today list: tracked deals (M4: Mari's mooring)
            var lines = new List<string>();
            if (c.MariTracked && c.Mari == DealSt.Active)
                lines.Add(c.MariHere(Sky.Minutes) ? $"Mari's mooring · ${c.MariPrice} at 7 AM · sells {Market.MariDaily - c.MariToday} more" : "Mari's on her way to B1");
            string k = string.Join("|", lines);
            if (k != todayKey) { todayKey = k; todayT.text = string.Join("\n", lines); today.sizeDelta = new Vector2(320, 44 + lines.Count * 22); }
            bool tOn = lines.Count > 0 && !Phone.IsOpen;
            if (today.gameObject.activeSelf != tOn) today.gameObject.SetActive(tOn);
            // (under the bank pill when it's up)
            today.anchoredPosition = new Vector2(-40, on ? -90 : -32);
        }
    }
}
