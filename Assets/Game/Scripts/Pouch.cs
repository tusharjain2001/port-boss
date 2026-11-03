using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PortBoss.M1
{
    // The bait pouch on the rod (fishing §4.3, §6.1): hold R (pad: Y) with the rod out and not cast -> a radial of the
    // pouch's 5 pockets round the screen centre; point with the mouse or a stick, let go to put that bait on the hook.
    // A tap of R: the next bait you have. The pouch holds 3 types; June's tackle pouch ($250) opens the other 2.
    // While it's up you still walk, but don't look or cast (Ctl.Ui 1, like the phone).
    public class Pouch : MonoBehaviour
    {
        public static Pouch I; public static bool Open => I != null && I.open;
        const float HoldS = 0.22f, R = 168;
        RectTransform root; readonly RectTransform[] pocket = new RectTransform[5]; readonly Text[] name_ = new Text[5], count = new Text[5];
        Text midName, midSub; bool open, down, tookUi; float downT; int sel; Vector2 aim;

        public static void Create() { var g = new GameObject("Pouch"); I = g.AddComponent<Pouch>(); I.Build(); }
        void Build()
        {
            root = UiKit.Rect("pouch", Hud.I.transform); root.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 560));
            var mid = UiKit.Card(root, "mid", 80, Ponyo.Paper, true); mid.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(176, 176));
            midName = UiKit.Txt(mid, "", UiKit.DisplayBold, 24, Ponyo.Ink, TextAnchor.MiddleCenter); midName.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(160, 36));
            midSub = UiKit.Txt(mid, "", UiKit.Body, 15, Ponyo.InkSoft, TextAnchor.MiddleCenter); midSub.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(160, 40));
            for (int i = 0; i < 5; i++)
            {
                float a = Mathf.PI / 2 - i * 2 * Mathf.PI / 5;
                var p = UiKit.Card(root, "pocket " + i, 22, Ponyo.Paper, true); p.Place(new Vector2(0.5f, 0.5f), new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * R, new Vector2(132, 92));
                var dot = UiKit.Img(p, Glyphs.Get(Shop.BaitGlyph[i]), Shop.BaitCol(i), "glyph"); dot.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(30, 30));
                name_[i] = UiKit.Txt(p, FishDb.Baits[i].Name, UiKit.Bold, 16, Ponyo.Ink, TextAnchor.MiddleCenter); name_[i].rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(124, 22));
                count[i] = UiKit.Txt(p, "", UiKit.Body, 14, Ponyo.InkSoft, TextAnchor.MiddleCenter); count[i].rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(124, 20));
                p.gameObject.AddComponent<CanvasGroup>(); pocket[i] = p;
            }
            root.gameObject.SetActive(false);
        }

        static Fishing F => Angler.I != null ? Angler.I.Fish : null;
        static Kit K => Firm.I != null ? Firm.I.MyKit : null;
        // a pocket you can use: 0-2 always, 3-4 with June's tackle pouch
        public static bool Usable(int i) => K != null && (!Shelf.NeedsPouch(i) || K.Pouch5);
        bool CanUse()
        {
            var me = Angler.I; var f = F;
            return me != null && f != null && K != null && !Title.Shown && !Game.Paused && !Shop.Open && !Phone.IsOpen && !me.Swimming && !me.AtHelm
                && f.Holding && f.State is Fishing.St.Ready or Fishing.St.Drawing;
        }

        void Update()
        {
            bool held = !Ctl.Bot && ((Keyboard.current != null && Keyboard.current[Key.R].isPressed) || (Gamepad.current != null && Gamepad.current.buttonNorth.isPressed));
            if (open && !CanUse()) { Close(false); down = false; return; }
            if (held && !down) { down = CanUse() && (open || Ctl.Ui == 0); downT = 0; }
            if (down && held)
            {
                downT += Time.unscaledDeltaTime;
                if (!open && downT >= HoldS) Show();
                if (open) Point();
            }
            if (down && !held) { down = false; if (open) Close(true); else Next(); }
        }

        public void Show()
        {
            open = true; aim = Vector2.zero; sel = F.BaitI; root.gameObject.SetActive(true);
            tookUi = Ctl.Ui == 0; if (tookUi) Ctl.Ui = 1;
            Snd.Play2D(Synth.Tik, 0.25f, 1.1f); Paint();
        }
        void Close(bool pick)
        {
            if (pick && sel != F.BaitI) Pick(sel);
            open = false; root.gameObject.SetActive(false); if (tookUi && Ctl.Ui == 1) Ctl.Ui = 0; tookUi = false;
        }

        // the mouse (summed) or a stick points at a pocket; a short nudge keeps the one on the hook
        void Point()
        {
            if (Mouse.current != null) aim = Vector2.ClampMagnitude(aim + Mouse.current.delta.ReadValue(), 140);
            var pad = Gamepad.current;
            if (pad != null) { var s = pad.rightStick.ReadValue(); if (s.sqrMagnitude < 0.2f) s = pad.leftStick.ReadValue(); if (s.sqrMagnitude > 0.2f) aim = s * 140; }
            if (aim.magnitude > 36)
            {
                float a = Mathf.Repeat(Mathf.PI / 2 - Mathf.Atan2(aim.y, aim.x), 2 * Mathf.PI);   // clockwise from the top
                int i = Mathf.RoundToInt(a / (2 * Mathf.PI / 5)) % 5;
                if (i != sel) { sel = i; Snd.Play2D(Synth.Tik, 0.15f, 1.3f); }
            }
            Paint();
        }

        void Paint()
        {
            var k = K;
            for (int i = 0; i < 5; i++)
            {
                bool use = Usable(i), on = i == sel, hook = i == F.BaitI;
                count[i].text = !use ? "Tackle pouch" : k.Bait[i] > 0 ? $"× {k.Bait[i]}" + (hook ? " · on" : "") : "none";
                var fill = UiKit.Fill(pocket[i]); fill.color = on ? Ponyo.Hex("FFF1C9") : Ponyo.Paper;
                UiKit.Edge(pocket[i]).color = on ? Ponyo.Sunflower : Ponyo.Line;
                pocket[i].localScale = Vector3.one * (on ? 1.08f : 1);
                bool has = use && k.Bait[i] > 0; pocket[i].GetComponent<CanvasGroup>().alpha = has ? 1 : 0.86f; name_[i].color = has ? Ponyo.Ink : Ponyo.InkSoft;   // locked/empty: quieter, still readable
            }
            midName.text = FishDb.Baits[sel].Name;
            midSub.text = !Usable(sel) ? "June's tackle pouch, $250" : k.Bait[sel] == 0 ? "None left.\nJune sells it." : sel == F.BaitI ? "On the hook" : "Let go to bait up";
        }

        // put bait i on the hook (the bot calls it too). False: a locked pocket or none of it.
        public bool Pick(int i)
        {
            var f = F; var k = K; if (f == null || k == null) return false;
            if (!Usable(i)) { Alerts.Push("Bait pouch", "That pocket needs June's tackle pouch ($250).", Alerts.Kind.Info, "pouch"); return false; }
            if (k.Bait[i] <= 0) { Alerts.Push("Bait pouch", $"No {FishDb.Baits[i].Name.ToLower()} left. June sells it.", Alerts.Kind.Info, "pouch"); return false; }
            f.BaitI = i; if (Firm.I != null) Firm.I.Dirty = true; Snd.Play2D(Synth.Tik, 0.3f, 0.8f);
            Debug.Log($"[PB] pouch: {FishDb.Baits[i].Name} on the hook ({k.Bait[i]})");
            return true;
        }
        // a tap: the next bait you have
        public bool Next()
        {
            var k = K; var f = F; if (k == null || f == null || !CanUse()) return false;
            for (int d = 1; d < 5; d++) { int i = (f.BaitI + d) % 5; if (Usable(i) && k.Bait[i] > 0) return Pick(i); }
            return false;
        }
        // the bot: the radial up on pocket i (a screenshot)
        public void BotShow(int i) { if (!CanUse()) return; Show(); sel = i; Paint(); }
        public void BotHide() { if (open) Close(false); }
    }
}
