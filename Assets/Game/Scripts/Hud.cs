using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PortBoss.M1
{
    // Notifications live in the LEFT column (MUST), newest on top, max 4, slide in from the left with a small overshoot.
    // During the minigame only warnings show; the rest wait for the reveal to end (systems §2.4).
    public static class Alerts
    {
        public enum Kind { Message, Money, Info, Weather, Warning, Crew, Story }
        // Topic: what a toast is about; Ends: the topics it resolves ('|'-separated). A toast that ends a topic dismisses the
        // older toasts on it ("Hole patched" ends "She's sprung a leak!"), so two toasts never contradict each other.
        public struct A { public string Sender, Text, Topic, Ends, Hint; public Kind K; public float Born, Due; public int N; }   // N: how many merged into it; Due: a countdown's end ("in 5 s")
        public static readonly List<A> Queue = new();
        public static System.Action<A> OnPush;
        // hint (M4): one action on the toast, after a "Tab" key cap ("Open phone")
        // secs (M3): a countdown toast: its "in N s" counts down and a ring of segments round its stamp goes out (the sleep heads-up)
        public static void Push(string sender, string text, Kind k = Kind.Info, string topic = null, string ends = null, string hint = null, float secs = 0)
        {
            var a = new A { Sender = sender, Text = text, K = k, Topic = topic, Ends = ends, Hint = hint, Born = Time.unscaledTime, Due = secs > 0 ? Time.unscaledTime + secs : 0 };
            Queue.Add(a); if (Queue.Count > 100) Queue.RemoveAt(0); // (the phone's Notifications: the 100 newest)
            Debug.Log($"[PB] alert {k}: {sender}: {text}");
            OnPush?.Invoke(a);
        }
        public static Color Col(Kind k) => k switch
        {
            Kind.Message => Ponyo.SeaC, Kind.Money => Ponyo.Sunflower, Kind.Weather => Ponyo.SeaDeep, Kind.Warning => Ponyo.Poppy,
            Kind.Crew => Ponyo.Leaf, Kind.Story => Ponyo.Night, _ => Ponyo.Paper3,
        };
        // two money toasts in a row become one: "+$87 · 9 fish · Pratt" + "+$29 · 4 fish · Pratt" = "+$116 · 13 fish · Pratt";
        // "−$12 · Shrimp ×10" + "−$5 · Worms ×10" = "−$17 · Shrimp ×10, Worms ×10"; longer than one line: "−$168 · 5 buys"
        // (n = how many it now holds; the Ledger keeps every line)
        public static string Merge(string a, string b, int n = 2)
        {
            static (int amt, string rest) Split(string t)
            {
                var m = System.Text.RegularExpressions.Regex.Match(t, @"^([+−-])\$(\d+) · (.*)$");
                return m.Success ? ((m.Groups[1].Value == "+" ? 1 : -1) * int.Parse(m.Groups[2].Value), m.Groups[3].Value) : (0, null);
            }
            var (x, xr) = Split(a); var (y, yr) = Split(b);
            if (xr == null || yr == null) return b;
            var num = new System.Text.RegularExpressions.Regex(@"\d+");
            var mx = num.Match(xr); var my = num.Match(yr);
            int sum = x + y; string many = $"{n} {(sum >= 0 ? "sales" : "buys")}";
            string rest = System.Text.RegularExpressions.Regex.IsMatch(xr, @"^\d+ (sales|buys)$") ? many
                : mx.Success && my.Success && num.Replace(xr, "#", 1) == num.Replace(yr, "#", 1) ? num.Replace(xr, (int.Parse(mx.Value) + int.Parse(my.Value)).ToString(), 1)
                : xr + ", " + yr;
            if (rest.Length > 24) rest = many;   // (about one line of the toast)
            return $"{(sum >= 0 ? "+" : "−")}${Mathf.Abs(sum)} · {rest}";
        }
        // a drawn icon in the stamp (mockup 01: a chat bubble for texts, sun-and-cloud for the weather), else the letter
        public static string Icon(Kind k) => k == Kind.Message ? "msg" : k == Kind.Weather ? "forecast" : null;
        public static void Stamp(Transform st, Kind k, int size)
        {
            Color ink = k == Kind.Info ? Ponyo.Ink : Ponyo.Paper;
            if (Icon(k) != null) UiKit.Img(st, Glyphs.Get(Icon(k)), ink, "glyph").rectTransform.Stretch(size * 0.4f);
            else UiKit.Txt(st, Glyph(k), UiKit.Black, size, ink, TextAnchor.MiddleCenter).rectTransform.Stretch();
        }
        public static string Glyph(Kind k) => k switch { Kind.Message => "…", Kind.Money => "$", Kind.Warning => "!", Kind.Crew => "★", Kind.Story => "W", Kind.Weather => "~", _ => "i" };
    }

    public class Hud : MonoBehaviour
    {
        public static Hud I;
        CanvasScaler scaler; float scaled = -1;                     // M7 step 6: text size (Settings.UiScale)
        void ScaleTick() { if (scaler != null && scaled != Settings.UiScale) { scaled = Settings.UiScale; scaler.referenceResolution = new Vector2(W, H) / scaled; } }
        Canvas canvas; RectTransform root;
        Angler me; Fishing fish;
        public static readonly Dictionary<string, Texture> Icons = new();
        public static Texture RodIcon;

        // hotbar
        readonly RectTransform[] slot = new RectTransform[8]; readonly RawImage[] slotIcon = new RawImage[8]; readonly Text[] slotCount = new Text[8];
        readonly Image[] slotGrade = new Image[8]; readonly Image[] slotFresh = new Image[8]; readonly float[] slotPulse = new float[8];
        RectTransform namePill, hotbarStrip; Text nameT, nameSub; float nameShow;
        RectTransform prompts; float promptsKey; string lastPrompt;
        Text baitTag, baitKey; RectTransform baitChip; Image baitDot;
        // alerts
        readonly List<(RectTransform r, Alerts.A a, float h, Image life)> cards = new();
        readonly List<Alerts.A> held = new();
        // cast
        Image arc, arcTrack; Text distLabel;
        // stamina (Tushar 7 Nov, take 2: "a bar below the character, as in other games"): a slim paper bar under her feet
        // while it isn't full
        RectTransform stam, stamClip; Image stamFill, stamTrack; CanvasGroup stamCg; float stamShow;
        public const float StamW = 96, StamH = 15;
        // minigame
        RectTransform mg;
        RectTransform bar, zone, pointer;
        float mgIn;
        // heavy
        RectTransform heavy; Text heavyT;
        // reveal
        RectTransform rv, rays; RawImage rvFish; Text rvName, rvSub, rvGrade, rvHits, rvFoot; RectTransform rvGradePill, rvNew, rvBest, rvKeep, rvLet;
        readonly Text[] rvStat = new Text[4], rvStatSub = new Text[4]; Image dim; float rvIn;
        RawImage fly; Vector2 flyFrom, flyTo; float flyT = -1; int flySlot;
        // debug overlay
        Text dbg; public bool ShowDebug;
        Image fade; public float FadeA;

        const float W = 1920, H = 1080;

        public static Hud Create(Camera cam, Angler a)
        {
            var g = new GameObject("HUD"); g.layer = CameraRig.UiLayer;
            var h = g.AddComponent<Hud>(); I = h; h.me = a; h.fish = a.Fish;
            h.canvas = g.AddComponent<Canvas>(); h.canvas.renderMode = RenderMode.ScreenSpaceCamera; h.canvas.worldCamera = cam; h.canvas.planeDistance = 0.5f;
            h.canvas.pixelPerfect = false; h.canvas.sortingOrder = 10;
            var cs = g.AddComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(W, H) / Settings.UiScale; cs.matchWidthOrHeight = 0.5f; h.scaler = cs;   // (M7: text size)
            h.root = (RectTransform)g.transform;
            h.Build();
            NameTags.Make(h.root, cam);
            Alerts.OnPush += h.OnAlert;
            return h;
        }

        void OnDestroy() { Alerts.OnPush -= OnAlert; }

        // ------------------------------------------------------------------------------------------------- build
        void Build()
        {
            // hotbar (bottom centre): 8 slots on a paper strip
            float sw = 84, gap = 10, total = 8 * sw + 7 * gap + 28;
            var strip = hotbarStrip = UiKit.Card(root, "hotbar", 22, new Color(1, 0.984f, 0.949f, 0.92f));
            strip.Place(new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(total, sw + 24));
            for (int i = 0; i < 8; i++)
            {
                var s = UiKit.Card(strip, "slot" + (i + 1), 14, Ponyo.Paper2, false);
                s.Place(new Vector2(0, 0.5f), new Vector2(14 + i * (sw + gap) + sw / 2, 0), new Vector2(sw, sw), new Vector2(0.5f, 0.5f));
                var n = UiKit.Txt(s, (i + 1).ToString(), UiKit.Bold, 14, Ponyo.InkSoft, TextAnchor.UpperLeft); n.rectTransform.Stretch(7);
                var ic = UiKit.Rect("icon", s).Place(new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(72, 40)).gameObject.AddComponent<RawImage>(); ic.raycastTarget = false; ic.enabled = false;
                var c = UiKit.Txt(s, "", UiKit.DisplayBold, 22, Ponyo.Ink, TextAnchor.LowerRight); c.rectTransform.Stretch(7); c.rectTransform.offsetMin += new Vector2(0, 2);
                var gr = UiKit.Img(s, null, Color.white, "grade"); gr.rectTransform.Place(new Vector2(1, 1), new Vector2(-10, -10), new Vector2(14, 14), new Vector2(1, 1)); gr.enabled = false;
                var fr = UiKit.Img(s, null, Ponyo.Leaf, "fresh"); fr.rectTransform.Place(new Vector2(0, 0), new Vector2(10, 7), new Vector2(40, 4), new Vector2(0, 0)); fr.enabled = false;
                slot[i] = s; slotIcon[i] = ic; slotCount[i] = c; slotGrade[i] = gr; slotFresh[i] = fr;
            }
            baitChip = UiKit.Card(root, "baitChip", 16, Ponyo.Paper, true);
            baitChip.Place(new Vector2(0.5f, 0), new Vector2(total / 2 + 16, 34), new Vector2(170, 38), new Vector2(0, 0));
            var worm = baitDot = UiKit.Img(baitChip, UiKit.Circle, Ponyo.Peach, "worm"); worm.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(16, 16), new Vector2(0, 0.5f));
            baitTag = UiKit.Txt(baitChip, "", UiKit.Bold, 17, Ponyo.Ink, TextAnchor.MiddleLeft); baitTag.rectTransform.Stretch(); baitTag.rectTransform.offsetMin = new Vector2(36, 0);
            var bk = UiKit.Key(baitChip, "R", 24); bk.Place(new Vector2(1, 0.5f), new Vector2(-10, 0), bk.sizeDelta, new Vector2(1, 0.5f)); baitKey = bk.GetComponentInChildren<Text>(); // hold: the pouch radial

            // item name pill (above the hotbar, 2 s after a switch)
            namePill = UiKit.Card(root, "itemName", 24, Ponyo.Paper, true);
            namePill.Place(new Vector2(0.5f, 0), new Vector2(0, 26 + sw + 24 + 14), new Vector2(520, 50), new Vector2(0.5f, 0));
            nameT = UiKit.Txt(namePill, "", UiKit.Display, 27, Ponyo.Ink, TextAnchor.MiddleLeft); nameT.rectTransform.Stretch(); nameT.rectTransform.offsetMin = new Vector2(22, 0);
            nameSub = UiKit.Txt(namePill, "", UiKit.Body, 16, Ponyo.InkSoft, TextAnchor.MiddleRight); nameSub.rectTransform.Stretch(); nameSub.rectTransform.offsetMax = new Vector2(-22, 0);
            nameShow = 3.5f;

            prompts = UiKit.Rect("prompts", root).Place(new Vector2(0.5f, 0), new Vector2(0, 26 + sw + 24 + 76), new Vector2(1200, 40), new Vector2(0.5f, 0));
            var hl = prompts.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 10; hl.childAlignment = TextAnchor.MiddleCenter; hl.childControlWidth = hl.childControlHeight = false; hl.childForceExpandWidth = false;

            // the cast's power: an arc filling round the distance label over the landing ring, on a faint track (look audit
            // 3 Nov: alone at the screen centre, part-filled, it read as a loose white sliver floating over her hat)
            arcTrack = UiKit.Img(root, UiKit.RingThin, new Color(1, 1, 1, 0.5f), "castArcTrack"); arcTrack.type = Image.Type.Filled; arcTrack.fillMethod = Image.FillMethod.Radial360; arcTrack.fillOrigin = (int)Image.Origin360.Bottom; arcTrack.fillClockwise = false; arcTrack.fillAmount = 60f / 360f;
            arcTrack.rectTransform.Place(new Vector2(0, 0), Vector2.zero, new Vector2(110, 110)); arcTrack.rectTransform.localRotation = Quaternion.Euler(0, 0, -60); arcTrack.enabled = false;
            arc = UiKit.Img(root, UiKit.RingThin, Color.white, "castArc"); arc.type = Image.Type.Filled; arc.fillMethod = Image.FillMethod.Radial360; arc.fillOrigin = (int)Image.Origin360.Bottom; arc.fillClockwise = false;
            arc.rectTransform.Place(new Vector2(0, 0), Vector2.zero, new Vector2(110, 110)); arc.rectTransform.localRotation = Quaternion.Euler(0, 0, -60); arc.enabled = false;
            distLabel = UiKit.Txt(root, "", UiKit.Bold, 20, Color.white, TextAnchor.MiddleCenter); distLabel.rectTransform.Place(new Vector2(0, 0), Vector2.zero, new Vector2(160, 30));
            UiKit.Shade(distLabel, new Color(0.25f, 0.17f, 0.15f, 0.8f), new Vector2(1.5f, -1.5f));

            // stamina: a rounded paper bar (soft shadow, printed edge) whose coloured fill drains to the left, under her feet
            stam = UiKit.Rect("stamina", root).Place(new Vector2(0, 0), Vector2.zero, new Vector2(StamW, StamH)); stamCg = stam.gameObject.AddComponent<CanvasGroup>(); stamCg.alpha = 0;
            var sbar = UiKit.Card(stam, "bar", StamH / 2, new Color(1, 0.984f, 0.949f, 0.96f), true, null, 2); sbar.Stretch();
            stamTrack = UiKit.Fill(sbar); stamTrack.color = Ponyo.Paper3;
            stamClip = UiKit.Rect("clip", sbar); stamClip.anchorMin = new Vector2(0, 0); stamClip.anchorMax = new Vector2(0, 1); stamClip.pivot = new Vector2(0, 0.5f);
            stamClip.offsetMin = new Vector2(3, 3); stamClip.offsetMax = new Vector2(StamW - 3, -3); stamClip.gameObject.AddComponent<RectMask2D>();
            stamFill = UiKit.Img(stamClip, stamTrack.sprite, Ponyo.Leaf, "fill"); stamFill.type = Image.Type.Sliced; stamFill.pixelsPerUnitMultiplier = UiKit.BaseR / ((StamH - 6) / 2);
            stamFill.rectTransform.Place(new Vector2(0, 0.5f), Vector2.zero, new Vector2(StamW - 6, StamH - 6), new Vector2(0, 0.5f));
            UiKit.Edge(sbar).transform.SetAsLastSibling();
            stam.gameObject.SetActive(false);

            BuildMinigame(); BuildHeavy(); BuildReveal();

            fly =UiKit.Rect("fly", root).gameObject.AddComponent<RawImage>(); fly.raycastTarget = false; fly.enabled = false; fly.rectTransform.sizeDelta = new Vector2(96, 54);
            fly.rectTransform.anchorMin = fly.rectTransform.anchorMax = Vector2.zero;

            dbg = UiKit.Txt(root, "", UiKit.Bold, 15, Ponyo.Paper, TextAnchor.UpperRight); dbg.rectTransform.Place(new Vector2(1, 1), new Vector2(-30, -26), new Vector2(560, 400), new Vector2(1, 1));
            UiKit.Shade(dbg, new Color(0.1f, 0.08f, 0.1f, 0.9f), new Vector2(1.5f, -1.5f));

            fade = UiKit.Img(root, null, new Color(0.11f, 0.15f, 0.28f, 0), "fade"); fade.rectTransform.Stretch(); fade.transform.SetAsLastSibling();
        }

        // The minigame is the bar and nothing else (Manan 1 Nov: no card, no title, no stop dots, grade chips or keys).
        void BuildMinigame()
        {
            // the bar just above the hotbar (its centre 172 px up at 1080p), clear of the angler the camera frames above it (look
            // audit 3 Nov: at 294 px it cut her in half while the rod bent)
            mg = UiKit.Rect("minigame", root).Place(new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(900, 262), new Vector2(0.5f, 0));
            mg.gameObject.AddComponent<CanvasGroup>();
            bar = UiKit.Card(mg, "bar", 24, Ponyo.Hex("CFF2F0"), true); bar.Place(new Vector2(0.5f, 1), new Vector2(0, -136), new Vector2(830, 46), new Vector2(0.5f, 0.5f));
            var wave = UiKit.Img(bar, null, new Color(0.27f, 0.84f, 0.78f, 0.25f), "tide"); wave.rectTransform.Stretch(6); wave.rectTransform.offsetMax = new Vector2(-6, -24);
            zone = UiKit.Rect("zone", bar); zone.anchorMin = new Vector2(0.4f, 0); zone.anchorMax = new Vector2(0.6f, 1); zone.offsetMin = new Vector2(0, 3); zone.offsetMax = new Vector2(0, -3);
            var zr = zone.gameObject.AddComponent<RawImage>(); zr.texture = UiKit.Stripes(Ponyo.Leaf, Ponyo.Hex("6AAE58"), 32); zr.raycastTarget = false;
            var zl = UiKit.Img(zone, null, Ponyo.Hex("4E8C40"), "l"); zl.rectTransform.anchorMin = new Vector2(0, 0); zl.rectTransform.anchorMax = new Vector2(0, 1); zl.rectTransform.sizeDelta = new Vector2(3, 0);
            var zr2 = UiKit.Img(zone, null, Ponyo.Hex("4E8C40"), "r"); zr2.rectTransform.anchorMin = new Vector2(1, 0); zr2.rectTransform.anchorMax = new Vector2(1, 1); zr2.rectTransform.sizeDelta = new Vector2(3, 0);
            // pointer: a little red-and-white bobber riding a stick above the bar
            pointer = UiKit.Rect("pointer", bar); pointer.anchorMin = pointer.anchorMax = new Vector2(0, 0.5f); pointer.sizeDelta = new Vector2(30, 80); pointer.anchoredPosition = Vector2.zero;
            var stick = UiKit.Img(pointer, null, Ponyo.Line, "stick"); stick.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(4, 56));
            var foot = UiKit.Img(pointer, UiKit.Circle, Ponyo.Line, "foot"); foot.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -34), new Vector2(12, 12));
            var bw = UiKit.Img(pointer, UiKit.Circle, Ponyo.Paper, "bob"); bw.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 28), new Vector2(28, 28));
            var br = UiKit.Img(bw.transform, UiKit.Circle, Ponyo.Poppy, "red"); br.type = Image.Type.Filled; br.fillMethod = Image.FillMethod.Vertical; br.fillOrigin = 1; br.fillAmount = 0.52f; br.rectTransform.Stretch(2);
            UiKit.Img(bw.transform, UiKit.RingSprite, Ponyo.Line, "edge").rectTransform.Stretch(-1);
            mg.gameObject.SetActive(false);
        }

        // A heavy fish: a small paper prompt before the bar (Manan 1 Nov): Fight it / Let it go, lets go by itself in 4 s.
        void BuildHeavy()
        {
            heavy = UiKit.Card(root, "heavy", 22, Ponyo.Paper, true);
            heavy.Place(new Vector2(0.5f, 0), new Vector2(0, 250), new Vector2(430, 104), new Vector2(0.5f, 0));
            var t = UiKit.Txt(heavy, "Something HEAVY!", UiKit.Display, 28, Ponyo.Poppy, TextAnchor.UpperLeft); t.rectTransform.Stretch(18); t.rectTransform.offsetMax = new Vector2(-18, -12);
            heavyT = UiKit.Txt(heavy, "", UiKit.Bold, 16, Ponyo.InkSoft, TextAnchor.UpperRight); heavyT.rectTransform.Stretch(18); heavyT.rectTransform.offsetMax = new Vector2(-18, -20);
            var kh = UiKit.Rect("keys", heavy).Place(new Vector2(0, 0), new Vector2(18, 14), new Vector2(400, 32), new Vector2(0, 0));
            var hl = kh.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 8; hl.childAlignment = TextAnchor.MiddleLeft; hl.childControlWidth = hl.childControlHeight = false; hl.childForceExpandWidth = false;
            UiKit.Key(kh, "E", 30); UiKit.Txt(kh, "Fight it", UiKit.Bold, 18, Ponyo.Ink).rectTransform.sizeDelta = new Vector2(96, 30);
            UiKit.Key(kh, "Q", 30); UiKit.Txt(kh, "Let it go", UiKit.Bold, 18, Ponyo.Ink).rectTransform.sizeDelta = new Vector2(100, 30);
            heavy.gameObject.SetActive(false);
        }

        void BuildReveal()
        {
            dim = UiKit.Img(root, null, new Color(0.24f, 0.18f, 0.2f, 0), "dim"); dim.rectTransform.Stretch();
            rays = UiKit.Rect("rays", root).Place(new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(1500, 1500));
            var ri = rays.gameObject.AddComponent<RawImage>(); ri.texture = UiKit.Rays(); ri.color = new Color(1, 0.97f, 0.88f, 0.09f); ri.raycastTarget = false;
            rv = UiKit.Card(root, "reveal", 30, Ponyo.Paper, true, null, 5);
            rv.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 46), new Vector2(860, 780));
            rvNew = Stamp(rv, "NEW IN JOURNAL", Ponyo.SeaC, new Vector2(-110, -40), -3f);
            rvBest = Stamp(rv, "PERSONAL BEST", Ponyo.Sunflower, new Vector2(110, -40), 2f);
            rvFish = UiKit.Rect("fish", rv).Place(new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(560, 280), new Vector2(0.5f, 0.5f)).gameObject.AddComponent<RawImage>(); rvFish.raycastTarget = false;
            rvGradePill = UiKit.Card(rv, "gradePill", 20, Ponyo.Sky, false, Ponyo.Line);
            rvGradePill.Place(new Vector2(0.5f, 1), new Vector2(0, -398), new Vector2(170, 44), new Vector2(0.5f, 0.5f));
            rvGrade = UiKit.Txt(rvGradePill, "RARE", UiKit.Black, 19, Ponyo.Paper, TextAnchor.MiddleCenter); rvGrade.rectTransform.Stretch();
            rvName = UiKit.Txt(rv, "", UiKit.Display, 61, Ponyo.Ink, TextAnchor.MiddleCenter); rvName.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -466), new Vector2(800, 76), new Vector2(0.5f, 0.5f));
            rvSub = UiKit.Txt(rv, "", UiKit.Display, 20, Ponyo.InkSoft, TextAnchor.MiddleCenter); rvSub.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -514), new Vector2(800, 28), new Vector2(0.5f, 0.5f));
            string[] lab = { "LENGTH", "WEIGHT", "WORTH NOW", "FRESH FOR" };
            var statsBox = UiKit.Rect("stats", rv).Place(new Vector2(0.5f, 1), new Vector2(0, -600), new Vector2(780, 108), new Vector2(0.5f, 0.5f));
            for (int i = 0; i < 4; i++)
            {
                var c = UiKit.Rect("stat" + i, statsBox).Place(new Vector2(0, 0.5f), new Vector2(i * 195 + 97.5f, 0), new Vector2(195, 108), new Vector2(0.5f, 0.5f));
                var l = UiKit.Txt(c, lab[i], UiKit.Bold, 13, Ponyo.InkSoft, TextAnchor.UpperCenter); l.rectTransform.Stretch(); l.rectTransform.offsetMax = new Vector2(0, -14);
                rvStat[i] = UiKit.Txt(c, "", UiKit.Display, 34, i == 2 ? Ponyo.Hex("D49A1E") : Ponyo.Ink, TextAnchor.MiddleCenter); rvStat[i].rectTransform.Stretch(); rvStat[i].rectTransform.anchoredPosition = new Vector2(0, -2);
                rvStatSub[i] = UiKit.Txt(c, "", UiKit.Body, 14, Ponyo.InkSoft, TextAnchor.LowerCenter); rvStatSub[i].rectTransform.Stretch(); rvStatSub[i].rectTransform.offsetMin = new Vector2(0, 10);
                if (i > 0) { var sep = UiKit.Img(c, null, Ponyo.Paper3, "sep"); sep.rectTransform.anchorMin = new Vector2(0, 0.1f); sep.rectTransform.anchorMax = new Vector2(0, 0.9f); sep.rectTransform.sizeDelta = new Vector2(2, 0); }
            }
            var top = UiKit.Img(statsBox, null, Ponyo.Paper3, "top"); top.rectTransform.anchorMin = new Vector2(0, 1); top.rectTransform.anchorMax = new Vector2(1, 1); top.rectTransform.sizeDelta = new Vector2(0, 2);
            var bot = UiKit.Img(statsBox, null, Ponyo.Paper3, "bot"); bot.rectTransform.anchorMin = new Vector2(0, 0); bot.rectTransform.anchorMax = new Vector2(1, 0); bot.rectTransform.sizeDelta = new Vector2(0, 2);
            rvHits = UiKit.Txt(rv, "", UiKit.Body, 18, Ponyo.InkSoft, TextAnchor.MiddleCenter); rvHits.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -672), new Vector2(780, 26), new Vector2(0.5f, 0.5f));
            rvKeep = Button(rv, "E", "Keep", Ponyo.Sunflower, new Vector2(-120, 48));
            rvLet = Button(rv, "Q", "Let it go", Ponyo.Paper, new Vector2(120, 48));
            rvFoot = UiKit.Txt(root, "", UiKit.Bold, 18, Ponyo.Paper, TextAnchor.MiddleCenter); rvFoot.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -374), new Vector2(900, 30));
            UiKit.Shade(rvFoot, new Color(0.25f, 0.17f, 0.15f, 0.8f), new Vector2(1.5f, -1.5f));
            SetReveal(false);
        }

        RectTransform Stamp(RectTransform parent, string text, Color c, Vector2 pos, float tilt)
        {
            var s = UiKit.Card(parent, "stamp " + text, 16, c, false);
            s.Place(new Vector2(0.5f, 1), pos, new Vector2(196, 38), new Vector2(0.5f, 0.5f)); s.localRotation = Quaternion.Euler(0, 0, tilt);
            var t = UiKit.Txt(s, text, UiKit.Black, 14, Ponyo.Ink, TextAnchor.MiddleCenter); t.rectTransform.Stretch();
            return s;
        }

        RectTransform Button(RectTransform parent, string key, string label, Color c, Vector2 pos)
        {
            var b = UiKit.Card(parent, "btn " + label, 18, c, true, null, 4);
            b.Place(new Vector2(0.5f, 0), pos, new Vector2(216, 58), new Vector2(0.5f, 0.5f));
            var k = UiKit.Key(b, key, 30); k.Place(new Vector2(0, 0.5f), new Vector2(26, 0), k.sizeDelta, new Vector2(0, 0.5f));
            var t = UiKit.Txt(b, label, UiKit.Bold, 21, Ponyo.Ink, TextAnchor.MiddleLeft); t.rectTransform.Stretch(); t.rectTransform.offsetMin = new Vector2(70, 0);
            return b;
        }

        void SetReveal(bool on)
        {
            rv.gameObject.SetActive(on); rays.gameObject.SetActive(on); rvFoot.gameObject.SetActive(on);
            if (!on) dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, 0);
        }

        // ------------------------------------------------------------------------------------------------ world marker
        public static Transform MakeMarker()
        {
            // dashed ring on the water (Ø 0.8 m), gold in the sweet spot, red when the point had to move
            int s = 128; var t = new Texture2D(s, s, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                {
                    float fx = (x + 0.5f) / s * 2 - 1, fy = (y + 0.5f) / s * 2 - 1, r = Mathf.Sqrt(fx * fx + fy * fy);
                    float ring = Mathf.Clamp01(1 - Mathf.Abs(r - 0.86f) / 0.06f);
                    float dash = Mathf.Repeat(Mathf.Atan2(fy, fx) / (2 * Mathf.PI) * 16, 1) < 0.62f ? 1 : 0;
                    float dot = Mathf.Clamp01(1 - r / 0.08f);
                    t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Max(ring * dash, dot)));
                }
            t.Apply();
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(g.GetComponent<Collider>()); g.name = "castMarker";
            var m = Util.Soft(Color.white, t); m.renderQueue = 3200;
            g.GetComponent<MeshRenderer>().sharedMaterial = m; g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            g.transform.localScale = Vector3.one * 0.95f;
            return g.transform;
        }

        // ------------------------------------------------------------------------------------------------ events
        public void Selected(int i) { nameShow = 2.5f; slotPulse[i] = 1; Snd.Play2D(M1Assets.I.Click, 0.25f, 1.3f); }

        public void FlyIn(Vector3 world, int slotI)
        {
            if (slotI < 0) return;
            var cam = canvas.worldCamera; var sp = cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, cam, out var lp);
            flyFrom = lp + root.rect.size * 0.5f;
            var target = slot[slotI];
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, RectTransformUtility.WorldToScreenPoint(cam, target.position), cam, out var tp);
            flyTo = tp + root.rect.size * 0.5f;
            var f = me.Bar.S[slotI].Fish; fly.texture = f.Count > 0 && Icons.TryGetValue(f[^1].Species.Id, out var tex) ? tex : null;
            fly.enabled = fly.texture != null; flyT = 0; flySlot = slotI;
        }

        void OnAlert(Alerts.A a)
        {
            bool fighting = fish != null && fish.State is Fishing.St.Minigame or Fishing.St.Heavy or Fishing.St.Reel or Fishing.St.Reveal;
            if (a.Ends != null) Resolve(a.Ends);
            if (quiet || fighting && a.K != Alerts.Kind.Warning && a.Sender != "NEW species") { if (!held.Exists(x => Same(x, a))) held.Add(a); return; }
            Show(a);
        }

        static bool Same(Alerts.A x, Alerts.A y) => x.Sender == y.Sender && x.Text == y.Text;

        // dismiss the toasts on these topics: the ones up start their fade-out now, the held ones are dropped
        void Resolve(string ends)
        {
            foreach (var t in ends.Split('|'))
            {
                held.RemoveAll(x => x.Topic == t);
                for (int i = 0; i < cards.Count; i++)
                    if (cards[i].a.Topic == t) { var c = cards[i]; c.a.Born = Mathf.Min(c.a.Born, Time.unscaledTime - Life(c.a)); cards[i] = c; }
            }
        }

        // M3: while the night card is up the toast stack fades away (the old day's toasts go); toasts that arrive meanwhile
        // wait and show once it is gone (Sleep.Night)
        bool quiet; float quietA = 1;
        public int ToastsShown(string text = null) => quietA <= 0 ? 0 : cards.FindAll(c => text == null || c.a.Text == text).Count; // (tests)
        public void Quiet(bool on)
        {
            quiet = on;
            if (!on) { foreach (var a in held) Show(a); held.Clear(); }
        }

        void Show(Alerts.A a)
        {
            // the same toast already up: refresh its timer instead of stacking a copy (kept in place, no slide-in again)
            int same = cards.FindIndex(c => Same(c.a, a));
            if (same >= 0) { var c0 = cards[same]; c0.a.Born = Time.unscaledTime - 0.22f; cards[same] = c0; return; }
            // a sale or buy while the last one is still up: one toast with the sum ("+$116 · 13 fish · Pratt")
            int join = a.Sender is "Sold" or "Bought" ? cards.FindIndex(c => c.a.Sender == a.Sender && Time.unscaledTime - c.a.Born < Life(c.a)) : -1;
            if (join >= 0)
            {
                var c0 = cards[join]; c0.a.N = Mathf.Max(1, c0.a.N) + 1; c0.a.Text = Alerts.Merge(c0.a.Text, a.Text, c0.a.N); c0.a.Born = Time.unscaledTime - 0.22f;
                var body = c0.r.Find("body")?.GetComponent<Text>(); if (body != null) body.text = c0.a.Text;
                c0.h = 62 + Mathf.Max(1, Mathf.CeilToInt(c0.a.Text.Length / 36f)) * 28; c0.r.sizeDelta = new Vector2(c0.r.sizeDelta.x, c0.h);
                cards[join] = c0; return;
            }
            var c = UiKit.Card(root, "alert", 20, Ponyo.Paper, true);
            int lines = Mathf.Max(1, Mathf.CeilToInt(a.Text.Length / 36f));
            float h = 62 + lines * 28 + (a.Hint != null ? 34 : 0);
            c.Place(new Vector2(0, 1), new Vector2(-480, -40), new Vector2(452, h), new Vector2(0, 1));
            var st = UiKit.Img(c, UiKit.Circle, Alerts.Col(a.K), "stamp"); st.rectTransform.Place(new Vector2(0, 1), new Vector2(18, -18), new Vector2(40, 40), new Vector2(0, 1));
            UiKit.Img(st.transform, UiKit.RingSprite, Ponyo.Line, "edge").rectTransform.Stretch(-1);
            if (a.Due > 0)
            {
                // the countdown ring: one segment a second, round the stamp from 12 o'clock (systems §1.4.1)
                int n = Mathf.Max(1, Mathf.RoundToInt(a.Due - a.Born)); var ring = UiKit.Rect("ring", st.transform); ring.Stretch();
                for (int i = 0; i < n; i++)
                {
                    float ang = (90 - 360f * i / n) * Mathf.Deg2Rad;
                    var d = UiKit.Img(ring, UiKit.Circle, Ponyo.Leaf, "seg" + i); d.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 27, new Vector2(9, 9), new Vector2(0.5f, 0.5f));
                    UiKit.Img(d.transform, UiKit.RingSprite, Ponyo.Line, "edge").rectTransform.Stretch(-1);
                }
            }
            Alerts.Stamp(st.transform, a.K, 20);
            var s = UiKit.Txt(c, UiKit.Caps(a.Sender), UiKit.Bold, 14, Ponyo.InkSoft, TextAnchor.UpperLeft); s.rectTransform.Stretch(); s.rectTransform.offsetMin = new Vector2(72, 0); s.rectTransform.offsetMax = new Vector2(-16, -16);
            var t = UiKit.Txt(c, a.Text, UiKit.Body, 20, Ponyo.Ink, TextAnchor.UpperLeft); t.name = "body"; t.rectTransform.Stretch(); t.rectTransform.offsetMin = new Vector2(72, 18); t.rectTransform.offsetMax = new Vector2(-18, -38);
            var life = UiKit.Img(c, null, Alerts.Col(a.K == Alerts.Kind.Info ? Alerts.Kind.Crew : a.K), "life"); life.rectTransform.anchorMin = new Vector2(0, 0); life.rectTransform.anchorMax = new Vector2(1, 0); life.rectTransform.pivot = new Vector2(0, 0);
            life.rectTransform.offsetMin = new Vector2(16, 6); life.rectTransform.offsetMax = new Vector2(-16, 10);
            if (a.Hint != null)
            {
                var row = UiKit.Rect("hint", c).Place(new Vector2(0, 0), new Vector2(72, 18), new Vector2(360, 28), new Vector2(0, 0));
                var key = UiKit.Key(row, "Tab", 26); key.Place(new Vector2(0, 0.5f), Vector2.zero, key.sizeDelta, new Vector2(0, 0.5f));
                var ht = UiKit.Txt(row, a.Hint, UiKit.Bold, 16, Ponyo.InkSoft, TextAnchor.MiddleLeft); ht.rectTransform.Stretch(); ht.rectTransform.offsetMin = new Vector2(key.sizeDelta.x + 10, 0);
                t.rectTransform.offsetMin = new Vector2(72, 52);
            }
            var aa = a; aa.Born = Time.unscaledTime;
            cards.Insert(0, (c, aa, h, life));
            Snd.Play2D(a.K == Alerts.Kind.Warning ? Synth.Bloop : M1Assets.I.Click, a.K == Alerts.Kind.Warning ? 0.25f : 0.2f, a.K == Alerts.Kind.Warning ? 0.7f : 1.1f);
        }

        // ~4-5 s (a warning or a text with an action a little longer); every message stays in the phone's Notifications
        static float Life(Alerts.A a) => a.Due > 0 ? Mathf.Max(4.5f, a.Due - a.Born + 1.2f) : a.K == Alerts.Kind.Warning ? 7f : a.Hint != null ? 6.5f : Mathf.Clamp(3.8f + 0.02f * a.Text.Length, 4f, 5.5f);
        const int MaxToasts = 3;

        // a countdown toast, each frame: "in 4 s" ... "now", and the ring's segments go out one a second
        static void Count(RectTransform r, Alerts.A a)
        {
            int left = Mathf.Max(0, Mathf.CeilToInt(a.Due - Time.unscaledTime));
            var body = r.Find("body")?.GetComponent<Text>();
            if (body != null) body.text = System.Text.RegularExpressions.Regex.Replace(a.Text, @"in \d+ s", left > 0 ? $"in {left} s" : "now");
            var ring = r.Find("stamp/ring");
            if (ring != null) for (int i = 0; i < ring.childCount; i++) { var img = ring.GetChild(i).GetComponent<Image>(); var c = img.color; c.a = Mathf.MoveTowards(c.a, i < left ? 1 : 0.12f, Time.unscaledDeltaTime * 6); img.color = c; img.transform.GetChild(0).GetComponent<Image>().color = new Color(Ponyo.Line.r, Ponyo.Line.g, Ponyo.Line.b, c.a); }
        }
        public string ToastBody(string starts) { foreach (var c in cards) if (c.a.Text.StartsWith(starts)) return c.r.Find("body")?.GetComponent<Text>()?.text; return null; } // (tests)

        // ------------------------------------------------------------------------------------------------ per frame
        void LateUpdate()
        {
            ScaleTick();                                             // (M7: text size)
            float dt = Time.unscaledDeltaTime;
            Fx.Tick();
            if (Ctl.Key_(UnityEngine.InputSystem.Key.F3)) ShowDebug = !ShowDebug;
            bool title = Title.Shown; // the title card stands alone: no hotbar or hints under it (look audit 3 Nov)
            if (hotbarStrip.gameObject.activeSelf == title) { hotbarStrip.gameObject.SetActive(!title); prompts.gameObject.SetActive(!title); }
            Hotbar(dt); Prompts(); Cast(); Stamina(dt); Minigame(dt); Heavy(); Reveal(dt); Fly(dt); AlertsTick(dt); Debug_();
            fade.color = new Color(fade.color.r, fade.color.g, fade.color.b, FadeA);
        }

        void Hotbar(float dt)
        {
            var b = me.Bar;
            for (int i = 0; i < 8; i++)
            {
                var s = b.S[i]; bool sel = i == b.Selected && !s.Empty; // an empty slot in hand isn't lit (look audit 3 Nov)
                slotPulse[i] = Mathf.MoveTowards(slotPulse[i], 0, dt * 3);
                float lift = sel ? 8 : 0;
                var p = slot[i].anchoredPosition; p.y = Mathf.Lerp(p.y, lift, 1 - Mathf.Exp(-dt / 0.04f)); slot[i].anchoredPosition = p;
                slot[i].localScale = Vector3.one * (1 + 0.08f * Util.EaseOutBack(slotPulse[i]) * slotPulse[i]);
                UiKit.Fill(slot[i]).color = sel ? Ponyo.Hex("FCE6A4") : Ponyo.Paper2;
                UiKit.Edge(slot[i]).color = sel ? Ponyo.Line : new Color(Ponyo.Line.r, Ponyo.Line.g, Ponyo.Line.b, 0.55f);
                if (s.Rod) { slotIcon[i].texture = RodIcon; slotIcon[i].enabled = RodIcon != null; slotIcon[i].rectTransform.sizeDelta = new Vector2(64, 64); slotCount[i].text = ""; slotGrade[i].enabled = false; slotFresh[i].enabled = false; continue; }
                if (s.Fish.Count == 0) { slotIcon[i].enabled = false; slotCount[i].text = ""; slotGrade[i].enabled = false; slotFresh[i].enabled = false; continue; }
                var f = s.Fish[0];
                slotIcon[i].texture = Icons.TryGetValue(f.Species.Id, out var tex) ? tex : null; slotIcon[i].enabled = slotIcon[i].texture != null; slotIcon[i].rectTransform.sizeDelta = new Vector2(74, 37);
                slotCount[i].text = s.Fish.Count.ToString();
                slotGrade[i].enabled = true; slotGrade[i].sprite = UiKit.GradeShape(f.Grade); slotGrade[i].color = Ponyo.GradeCol[f.Grade];
                // freshness (fishing §5.6): the stack shows its oldest fish; the bar shrinks over the 48 h and turns
                // green -> yellow-green -> sunflower -> peach -> brown by stage
                float oldest = f.CaughtAt; foreach (var x in s.Fish) oldest = Mathf.Min(oldest, x.CaughtAt);
                double age = Sky.Minutes - oldest; int st = Rot.Stage(age);
                slotFresh[i].enabled = true; slotFresh[i].color = FreshCol[st];
                slotFresh[i].rectTransform.sizeDelta = new Vector2(Mathf.Max(4, 40 * (1 - (float)(age / Rot.Full))), 4);
            }
            // the name of the selected item, 2.5 s after a switch (always while it's the rod and nothing else is going on)
            var sl = b.Sel;
            nameShow -= dt;
            bool show = nameShow > 0 && (fish.State is not (Fishing.St.Minigame or Fishing.St.Reveal or Fishing.St.Heavy));
            namePill.gameObject.SetActive(show && !sl.Empty);
            if (sl.Rod) { nameT.text = fish.Rod.Name; nameSub.text = $"{BaitText()} · line {fish.Rod.LineKg:0} kg"; }
            else if (sl.Fish.Count > 0) { var f = sl.Fish[0]; float old = f.CaughtAt; foreach (var x in sl.Fish) old = Mathf.Min(old, x.CaughtAt); nameT.text = f.Species.Name; nameSub.text = $"{Ponyo.GradeName[f.Grade]} · {Rot.Names[Rot.Stage(Sky.Minutes - old)]} · {sl.Fish.Count} of 5 · about ${Mathf.RoundToInt(f.Value * 0.6f)} each"; }
            namePill.sizeDelta = new Vector2(Mathf.Max(460, nameT.preferredWidth + nameSub.preferredWidth + 70), 50);
            baitTag.text = sl.Rod && !me.AtHelm ? (fish.BaitCount[fish.BaitI] > 0 ? BaitText() : "<color=#E2553F>No bait: sprats only</color>") : "";
            baitChip.gameObject.SetActive(baitTag.text != "");
            baitKey.text = Ctl.PadLast ? "Y" : "R"; baitDot.color = Shop.BaitCol(fish.BaitI); baitChip.sizeDelta = new Vector2(baitTag.preferredWidth + 84, 38);
        }

        static readonly Color[] FreshCol = { Ponyo.Leaf, Ponyo.Hex("B5C95A"), Ponyo.Sunflower, Ponyo.Peach, Ponyo.Hex("8A6A4E") };

        string BaitText() => fish.BaitCount[fish.BaitI] > 0 ? $"{FishDb.Baits[fish.BaitI].Name} × {fish.BaitCount[fish.BaitI]}" : "no bait";

        void Prompts()
        {
            var list = new List<(string k, string v)>();
            var st = fish.State;
            if (me.AtHelm) { var s = Skiff.I; if (s.Moored) list.Add(("W", "Cast off")); else { list.Add(("W/S", "Throttle " + Notch(s.Notch))); list.Add(("A/D", "Steer")); if (s.CanMoor) list.Add(("F", "Moor")); } list.Add(("E", "Leave the tiller")); }
            else if (me.Prompt != null && st is Fishing.St.Stowed or Fishing.St.Ready or Fishing.St.Drawing) list.Add(("E", me.Prompt));
            if (!me.AtHelm)
                switch (st)
                {
                    case Fishing.St.Ready: list.Add(fish.WaterNear ? ("LMB", "Hold to cast") : ("", "Too far from the water to cast")); break;
                    case Fishing.St.Charging: list.Add(("LMB", "Release to cast")); list.Add(("RMB", "Cancel")); break;
                    case Fishing.St.Waiting: case Fishing.St.Settle: list.Add(("LMB", "Strike on the bite")); list.Add(("RMB", "Hold to reel in")); break;
                    case Fishing.St.Bite: list.Add(("LMB", "STRIKE!")); break;
                    case Fishing.St.Stowed:
                        if (me.Bar.Sel.Rod && me.Swimming) list.Add(("", "Can't fish while swimming"));
                        else if (me.Aiming) list.Add(("Q", "Let go to toss"));
                        else if (!me.Bar.Sel.Rod && !me.Swimming)
                        {
                            if (me.Bar.Sel.Fish.Count > 0 && me.ThrowT < 0) { list.Add(("Q", "Drop")); list.Add(("Q", "Hold to toss")); }
                            if (Time.time < 20f) list.Add(("1", "Rod")); // the first 20 s of play only (look audit 3 Nov)
                        }
                        break;
                }
            string key = string.Join("|", list.ConvertAll(x => x.k + x.v)) + Ctl.PadLast;   // (M7: a device switch redraws the caps)
            if (key == lastPrompt) return;
            lastPrompt = key;
            foreach (Transform c in prompts) Destroy(c.gameObject);
            foreach (var (k, v) in list)
            {
                if (k != "") UiKit.Key(prompts, k, 34);
                var t = UiKit.Txt(prompts, v, UiKit.Bold, 22, Color.white, TextAnchor.MiddleLeft); t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.rectTransform.sizeDelta = new Vector2(t.preferredWidth + 18, 34);
                UiKit.Shade(t, new Color(0.25f, 0.17f, 0.15f, 0.85f), new Vector2(1.5f, -1.5f));
            }
        }
        static string Notch(int n) => n switch { -1 => "· reverse", 0 => "· stop", 1 => "· 1/3", 2 => "· 2/3", _ => "· full" };

        // Shown while it isn't full (and 1.2 s after it fills), fading in and out: green, peach under 30%, a poppy pulse
        // while winded. Not at the tiller, nor over the fight or the reveal.
        void Stamina(float dt)
        {
            bool want = !Title.Shown && !me.AtHelm && (me.Stamina < 0.995f || me.Winded) && fish.State is not (Fishing.St.Minigame or Fishing.St.Reveal or Fishing.St.Heavy);
            stamShow = want ? 1.2f : stamShow - dt;
            stamCg.alpha = Mathf.MoveTowards(stamCg.alpha, stamShow > 0 ? 1 : 0, dt * 4);
            stam.gameObject.SetActive(stamCg.alpha > 0.001f);
            if (!stam.gameObject.activeSelf) return;
            stamClip.sizeDelta = new Vector2((StamW - 6) * Mathf.Clamp01(me.Stamina), stamClip.sizeDelta.y);   // (the clip's width: the fill keeps its round left end)
            stamFill.color = me.Winded ? Color.Lerp(Ponyo.Poppy, Ponyo.Peach, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10)) : me.Stamina < 0.3f ? Ponyo.Peach : Ponyo.Leaf;
            stamTrack.color = me.Winded ? Color.Lerp(Ponyo.Paper3, Ponyo.Poppy, 0.35f + 0.25f * Mathf.Sin(Time.unscaledTime * 10)) : Ponyo.Paper3;   // (empty and winded: the whole bar blushes)
            // under her feet (in the water: under her body seen through the surface); centred, a little below
            var feet = me.transform.position; if (me.Swimming) feet.y = Sea.Height(feet) - 0.8f;
            var cam = canvas.worldCamera; var sp = cam.WorldToScreenPoint(feet);
            if (sp.z < 0) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, cam, out var lp);
            stam.anchoredPosition = lp + root.rect.size * 0.5f + new Vector2(-StamW / 2, -26 - StamH / 2);
        }

        void Cast()
        {
            bool on = fish.State == Fishing.St.Charging;
            arc.enabled = arcTrack.enabled = on; distLabel.enabled = on;
            if (!on) return;
            arc.fillAmount = fish.Power * 60f / 360f;
            arc.color = Color.Lerp(Color.white, Ponyo.Sunflower, fish.InSweet ? 1 : fish.Power * 0.6f);
            var cam = canvas.worldCamera; var sp = cam.WorldToScreenPoint(fish.Marker + Vector3.up * 0.9f);
            if (sp.z < 0) { distLabel.enabled = arc.enabled = arcTrack.enabled = false; return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, cam, out var lp);
            distLabel.rectTransform.anchoredPosition = lp + root.rect.size * 0.5f;
            arc.rectTransform.anchoredPosition = arcTrack.rectTransform.anchoredPosition = distLabel.rectTransform.anchoredPosition; // round the label
            distLabel.text = $"{fish.MarkerDist:0} m";
            distLabel.color = fish.MarkerSnapped ? Ponyo.Hex("FFB0A0") : fish.InSweet ? Ponyo.Sunflower : Color.white;
        }

        void Minigame(float dt)
        {
            bool on = fish.State == Fishing.St.Minigame;
            mgIn = Mathf.MoveTowards(mgIn, on ? 1 : 0, dt / (on ? 0.22f : 0.16f));
            mg.gameObject.SetActive(mgIn > 0.001f);
            if (!mg.gameObject.activeSelf || fish.Hooked == null) return;
            mg.anchoredPosition = new Vector2(0, 46 - 24 * (1 - Util.EaseOutBack(mgIn))); // (just above the hotbar: see BuildMinigame)
            mg.GetComponent<CanvasGroup>().alpha = mgIn;
            // a heavy fight shows the bar in red (one more try after a miss; the second miss snaps the line)
            UiKit.Fill(bar).color = fish.HeavyFight ? Ponyo.Hex("F6C9BE") : Ponyo.Hex("CFF2F0");
            UiKit.Edge(bar).color = fish.HeavyFight ? Ponyo.Poppy : Ponyo.Line;
            double now = Time.timeAsDouble;
            float zc = fish.Frozen ? fish.ZoneC : fish.ZoneAt(now);
            zone.anchorMin = new Vector2(zc - fish.ZoneW / 2, 0); zone.anchorMax = new Vector2(zc + fish.ZoneW / 2, 1);
            zone.GetComponent<RawImage>().uvRect = new Rect(0, 0, fish.ZoneW * 830 / 32f, 46 / 32f);
            float p = fish.Frozen ? fish.PointerShown : fish.PointerAt(now);
            pointer.anchorMin = pointer.anchorMax = new Vector2(p, 0.5f); pointer.anchoredPosition = new Vector2(0, 8);
            var flash = fish.Frozen && fish.Stop > 0 ? (fish.Result[fish.Stop - 1] > 0 ? Ponyo.Leaf : Ponyo.Hex("C9C2BA")) : Ponyo.Paper;
            pointer.Find("bob").GetComponent<Image>().color = flash;
        }

        void Heavy()
        {
            bool on = fish.State == Fishing.St.Heavy;
            heavy.gameObject.SetActive(on);
            if (on) heavyT.text = $"{Mathf.CeilToInt(Mathf.Max(0, 4 - fish.StateT))}";
        }

        void Reveal(float dt)
        {
            bool on = fish.State == Fishing.St.Reveal && fish.Catch != null;
            if (on && !rv.gameObject.activeSelf)
            {
                SetReveal(true); rvIn = 0;
                var c = fish.Catch; var sp = c.Species;
                rvFish.texture = BigIcon(sp.Id);
                rvName.text = sp.Name.Replace(" (Walt's legend grouper)", "");
                rvSub.text = $"caught on {(fish.BaitId == "None" ? "a bare hook" : FishDb.Baits[fish.BaitI].Name.ToLower())} off Walt's Landing, late afternoon · {sp.Class} species";
                rvGrade.text = (c.Grade > 0 ? "★ " : "") + Ponyo.GradeName[c.Grade].ToUpper();
                UiKit.Fill(rvGradePill).color = Ponyo.GradeCol[c.Grade];
                rvGrade.color = Ponyo.Ink;
                rvStat[0].text = c.Cm >= 100 ? $"{c.Cm / 100f:0.00} m" : $"{c.Cm:0} cm"; rvStatSub[0].text = fish.IsNew ? "first one!" : fish.IsBest ? "best so far" : "";
                rvStat[1].text = c.Kg < 1 ? $"{c.Kg * 1000:0} g" : $"{c.Kg:0.0} kg"; rvStatSub[1].text = fish.HeavyFight ? "line held!" : "";
                rvStat[2].text = $"${Mathf.Max(1, Mathf.RoundToInt(c.Value * 0.6f))}"; rvStatSub[2].text = $"at the market, ×{Ponyo.GradeMul[c.Grade]:0.0}";
                rvStat[3].text = "2 days"; rvStatSub[3].text = "then it's bait";
                rvHits.text = c.Grade > 0 ? "Stopped in the green. <b>Rare!</b>" : "Missed the green: a Basic catch. Stop in the green for <b>Rare</b>.";
                rvNew.gameObject.SetActive(fish.IsNew); rvBest.gameObject.SetActive(fish.IsBest);
            }
            if (!on && rv.gameObject.activeSelf)
            {
                SetReveal(false);
                if (!quiet) { foreach (var a in held) Show(a); held.Clear(); }
            }
            if (!on) return;
            rvIn = Mathf.MoveTowards(rvIn, 1, dt / 0.28f);
            float e = Util.EaseOutBack(rvIn);
            rv.anchoredPosition = new Vector2(0, 46 + 60 * (1 - e)); rv.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, e);
            dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, 0.3f * rvIn);
            rays.localRotation = Quaternion.Euler(0, 0, -Time.unscaledTime * 6f);
            bool full = fish.TargetSlot < 0;
            float auto = Fishing.RevealAuto - fish.StateT;
            rvFoot.text = full ? "Hotbar full · E drops it on the deck · Q lets it go" : $"Goes to slot {fish.TargetSlot + 1} · auto-keeps in {Mathf.CeilToInt(Mathf.Max(0, auto))} s";
            rvKeep.Find("txt").GetComponent<Text>().text = full ? "Drop on deck" : "Keep";
        }

        // A larger icon for the reveal card (rendered on demand, cached).
        static readonly Dictionary<string, Texture> big = new();
        Texture BigIcon(string id)
        {
            if (big.TryGetValue(id, out var t)) return t;
            var pf = M1Assets.I.FishPrefab(id);
            t = pf != null ? IconBaker.Bake(pf, 640, 320, Game.FishMat(FishDb.Get(id))) : null;
            big[id] = t; return t;
        }

        void Fly(float dt)
        {
            if (flyT < 0) return;
            flyT += dt / 0.35f;
            float u = Mathf.Clamp01(flyT);
            var p = Vector2.Lerp(flyFrom, flyTo, u * u * (3 - 2 * u)) + Vector2.up * Mathf.Sin(u * Mathf.PI) * 120;
            fly.rectTransform.anchoredPosition = p; fly.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.6f, 0.8f, u);
            if (flyT >= 1) { flyT = -1; fly.enabled = false; slotPulse[flySlot] = 1; Snd.Play2D(Synth.Thup, 0.6f); }
        }

        void AlertsTick(float dt)
        {
            float y = -40;
            quietA = Mathf.MoveTowards(quietA, quiet ? 0 : 1, dt / 0.4f);
            if (quiet && quietA <= 0) { foreach (var c in cards) Destroy(c.r.gameObject); cards.Clear(); }
            for (int i = 0; i < cards.Count; i++)
            {
                var (r, a, h, life) = cards[i];
                float age = Time.unscaledTime - a.Born, L = Life(a);
                float inT = Mathf.Clamp01(age / 0.22f), outT = Mathf.Clamp01((age - L) / 0.16f);
                float x = 40 - 480 * (1 - Util.EaseOutBack(inT)) - 8 * outT;
                bool visible = i < MaxToasts;
                if (!visible && outT <= 0) { a.Born = Time.unscaledTime - L; cards[i] = (r, a, h, life); outT = 0; }   // a 4th pushes the oldest out (it fades)
                var cg = r.GetComponent<CanvasGroup>() ?? r.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = (1 - outT) * quietA;
                var cur = r.anchoredPosition; r.anchoredPosition = new Vector2(x, Mathf.Lerp(cur.y, y, 1 - Mathf.Exp(-dt / 0.06f)));
                life.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(1 - age / L), 0);
                if (a.Due > 0) Count(r, a);
                if (a.K == Alerts.Kind.Warning && Sky.I != null && Sky.I.StormW > 0.5f) { var st = r.Find("stamp"); if (st != null) st.localScale = Vector3.one * (1 + 0.28f * Mathf.Sin(Mathf.Clamp01((age - 0.15f) / 0.45f) * Mathf.PI)); } // (§2.7: a storm's warnings pulse once)
                y -= h + 14;
                if (outT >= 1) { Destroy(r.gameObject); cards.RemoveAt(i); i--; }
            }
        }

        void Debug_()
        {
            dbg.enabled = ShowDebug;
            if (!ShowDebug) return;
            var f = fish; var sb = new System.Text.StringBuilder();
            sb.AppendLine(Sky.DebugLine());
            sb.AppendLine($"F3 debug · {f.State} {f.StateT:F2}s · fps {1f / Mathf.Max(1e-4f, Time.smoothDeltaTime):0}");
            sb.AppendLine($"rod {f.RodTier} {f.Rod.Name} · bites {f.Rod.BiteLo:0}–{f.Rod.BiteHi:0} s · green ×{f.Rod.GreenMul:0.00} · speed ×{f.Rod.SpeedMul:0.00} (F5)");
            sb.AppendLine($"bait {FishDb.Baits[f.BaitI].Name} ×{f.BaitCount[f.BaitI]} (F6)");
            if (f.State == Fishing.St.Waiting) sb.AppendLine($"bite in {f.BiteAt - Time.time:F2} s of {f.BiteWait:F2} · nibbles {f.NibblesDone}/{f.Nibbles}");
            if (f.Hooked != null) sb.AppendLine($"hooked: {f.Hooked.Name} ({f.Hooked.Class})");
            if (f.State is Fishing.St.Minigame && f.Hooked != null) sb.AppendLine($"window {f.ZoneW / f.PtrSpeed * 1000:0} ms · zone {f.ZoneW:P0} · speed {f.PtrSpeed:F2}/s · passes {f.Passes:F1}");
            sb.AppendLine($"stop error: {f.ErrMs[0]:0} ms{(f.HeavyFight ? $" · retry {f.ErrMs[1]:0} ms" : "")} (≤ 0 = in the green) · offset {Settings.TimingOffsetMs:0} ms");
            var s = Skiff.I; if (s != null) sb.AppendLine($"Biscuit {s.Speed:F1} m/s notch {s.Notch} {(s.Moored ? "moored" : s.Anchored ? "anchored" : "free")} roll {Mathf.DeltaAngle(0, s.transform.eulerAngles.z):F1}°");
            dbg.text = sb.ToString();
        }
    }

    // Item icons are rendered from the real 3D models with the same toon shader and outline (systems §2.6).
    // Co-op name tags (systems §2.5, world-space tags): each crewmate's name over their head on a paper pill edged in their
    // player colour, with a dot of it; smaller further off, gone past 40 m. Placed after the camera has moved (2100 >
    // CameraRig's 2000), so a tag never trails its head by a frame. While they fight a fish, their ghost bar sits on top
    // (fishing §4.2): the green zone, the pointer, a pip per stop, as their machine has it (~0.1 s behind). A ring in
    // their colour pulses round the dot while they talk (systems §3.6), and my own mic shows bottom-left.
    [DefaultExecutionOrder(2100)]
    public class NameTags : MonoBehaviour
    {
        public static NameTags I;
        RectTransform root; Camera cam;
        class Tag { public RectTransform R, Ghost, Zone, Ptr; public Text T; public Color C; public CanvasGroup G; public Image[] Pips; public Image Ring; }
        RectTransform mic; Image micRing, micDot; Text micT;
        // the crew list (systems §2.5): top right, one row a player: colour, speaking ring, name, crew member, what they're doing
        RectTransform crewBox, inviteBtn; readonly List<(ulong id, Image ring, Text status)> crewRows = new(); string crewKey;
        public bool InviteShown => inviteBtn != null && inviteBtn.gameObject.activeInHierarchy;   // (tests)
        public int CrewRows => crewRows.Count;                                  // (tests)
        public bool CrewRing(ulong id) => crewRows.Exists(r => r.id == id && r.ring.enabled);
        public string CrewNames;                                                // (tests) "Host, Rider1, …" as listed
        readonly Dictionary<Angler, Tag> tags = new();

        public static NameTags Make(RectTransform hudRoot, Camera cam)
        {
            var r = UiKit.Rect("nameTags", hudRoot); r.Stretch(); r.SetAsFirstSibling(); // under the rest of the HUD
            var n = r.gameObject.AddComponent<NameTags>(); n.root = r; n.cam = cam; I = n;
            // my mic: open (rings while I'm heard), push-to-talk, or off
            n.mic = UiKit.Card(r, "mic", 18, Ponyo.Paper, true); n.mic.Place(new Vector2(0, 0), new Vector2(28, 30), new Vector2(170, 40), new Vector2(0, 0));
            n.micRing = UiKit.Img(n.mic, UiKit.RingSprite, Color.white, "ring"); n.micRing.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(30, 30), new Vector2(0.5f, 0.5f));
            n.micDot = UiKit.Img(n.mic, UiKit.Circle, Color.white, "dot"); n.micDot.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(14, 14), new Vector2(0.5f, 0.5f));
            n.micT = UiKit.Txt(n.mic, "", UiKit.Bold, 17, Ponyo.Ink, TextAnchor.MiddleLeft); n.micT.rectTransform.Stretch(); n.micT.rectTransform.offsetMin = new Vector2(42, 0);
            n.mic.gameObject.SetActive(false);
            n.crewBox = UiKit.Rect("crew", r).Place(new Vector2(1, 1), new Vector2(-28, -28), new Vector2(330, 200), new Vector2(1, 1));
            return n;
        }

        Tag Build(Angler m)
        {
            var r = UiKit.Card(root, "tag", 17, Ponyo.Paper, true, m.Colour, 2);
            r.pivot = new Vector2(0.5f, 0);
            var dot = UiKit.Img(r, UiKit.Circle, m.Colour, "dot"); dot.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(13, 0), new Vector2(14, 14), new Vector2(0, 0.5f));
            var t = UiKit.Txt(r, "", UiKit.Bold, 19, Ponyo.Ink, TextAnchor.MiddleLeft); t.rectTransform.Stretch(); t.rectTransform.offsetMin = new Vector2(34, 0);
            var ring = UiKit.Img(r, UiKit.RingSprite, m.Colour, "ring"); ring.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(30, 30), new Vector2(0.5f, 0.5f)); ring.enabled = false; // (round the dot's centre)
            // the ghost bar: the minigame's colours (Hud.BuildMinigame) at a glance
            var g = UiKit.Card(r, "ghost", 10, Ponyo.Paper, false, m.Colour, 2);
            g.Place(new Vector2(0.5f, 1), new Vector2(0, 8), new Vector2(176, 24), new Vector2(0.5f, 0));
            var track = UiKit.Img(g, null, Ponyo.Hex("CFF2F0"), "track").rectTransform; track.Stretch(5); track.offsetMax = new Vector2(-38, -5);
            var zone = UiKit.Img(track, null, Ponyo.Leaf, "zone").rectTransform;
            var ptr = UiKit.Img(track, null, Ponyo.Ink, "ptr").rectTransform; ptr.pivot = new Vector2(0.5f, 0.5f);
            var pips = new Image[2];
            for (int i = 0; i < 2; i++) { pips[i] = UiKit.Img(g, UiKit.Circle, Ponyo.Paper3, "pip" + i); pips[i].rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-24 + i * 13, 0), new Vector2(10, 10)); }
            g.gameObject.SetActive(false);
            return new Tag { R = r, T = t, C = m.Colour, G = r.gameObject.AddComponent<CanvasGroup>(), Ghost = g, Zone = zone, Ptr = ptr, Pips = pips, Ring = ring };
        }

        public bool RingOn(Angler m) => tags.TryGetValue(m, out var t) && t.R.gameObject.activeSelf && t.Ring.enabled; // (tests)

        // the speaking ring: pulses out with their voice
        static void Speak(Image ring, bool on, float level, Color c)
        {
            ring.enabled = on; if (!on) return;
            float k = Mathf.Clamp01(level * 2.5f);
            ring.rectTransform.localScale = Vector3.one * (1f + 0.45f * k + 0.1f * Mathf.Sin(Time.time * 14f));
            ring.color = new Color(c.r, c.g, c.b, 0.75f + 0.25f * k);
        }

        static string Doing(Angler a) => a == null ? "" : a.Down ? "knocked over" : a.Swimming ? "swimming" : a.AtHelm ? "at the tiller" : a.Fish != null && a.Fish.State != Fishing.St.Stowed ? "fishing" : "";

        void Crew()
        {
            var c = Coop.I; bool on = Coop.On && c != null && c.Roster.Count > 0;
            if (crewBox.gameObject.activeSelf != on) crewBox.gameObject.SetActive(on);
            if (!on) { crewKey = null; return; }
            var list = new List<Coop.Entry>(c.Roster); list.Sort((a, b) => a.Slot.CompareTo(b.Slot));
            string key = string.Join("|", list.ConvertAll(e => $"{e.Id}/{e.Slot}/{e.Crew}/{e.Name}"));
            if (key != crewKey)
            {
                // (re)built when someone joins, leaves or changes body
                crewKey = key; crewRows.Clear(); foreach (Transform t in crewBox) Destroy(t.gameObject);
                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i]; var col = Coop.Colours[e.Slot % Coop.Colours.Length]; bool me = e.Id == Coop.Me;
                    var row = UiKit.Card(crewBox, "row" + i, 16, Ponyo.Paper, true, col, 2); row.Place(new Vector2(1, 1), new Vector2(0, -i * 50), new Vector2(330, 42), new Vector2(1, 1));
                    var ring = UiKit.Img(row, UiKit.RingSprite, col, "ring"); ring.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(28, 28), new Vector2(0.5f, 0.5f)); ring.enabled = false;
                    var dot = UiKit.Img(row, UiKit.Circle, col, "dot"); dot.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(14, 14), new Vector2(0.5f, 0.5f));
                    var nameT = UiKit.Txt(row, e.Name + (me ? " (you)" : ""), UiKit.Bold, 18, Ponyo.Ink, TextAnchor.MiddleLeft); nameT.rectTransform.Stretch(); nameT.rectTransform.offsetMin = new Vector2(42, 0);
                    var crewN = M1Assets.I.CrewNames != null && e.Crew < M1Assets.I.CrewNames.Length ? M1Assets.I.CrewNames[e.Crew] : "";
                    var st = UiKit.Txt(row, crewN, UiKit.Body, 15, Ponyo.InkSoft, TextAnchor.MiddleRight); st.rectTransform.Stretch(); st.rectTransform.offsetMax = new Vector2(-16, 0);
                    crewRows.Add((e.Id, ring, st));
                }
                CrewNames = string.Join(", ", list.ConvertAll(e => e.Name));
                // M3.1: the invite button under the crew (I, or click it in the pause menu), while there's room
                inviteBtn = null;
                if (list.Count < Coop.MaxCrew)
                {
                    inviteBtn = UiKit.Card(crewBox, "invite", 16, Ponyo.Sunflower, true, null, 2); inviteBtn.Place(new Vector2(1, 1), new Vector2(0, -list.Count * 50), new Vector2(210, 42), new Vector2(1, 1));
                    var k = UiKit.Key(inviteBtn, "I", 28); k.Place(new Vector2(0, 0.5f), new Vector2(10, 0), k.sizeDelta, new Vector2(0, 0.5f));
                    var it = UiKit.Txt(inviteBtn, "Invite a friend", UiKit.Bold, 18, Ponyo.Ink, TextAnchor.MiddleLeft); it.rectTransform.Stretch(); it.rectTransform.offsetMin = new Vector2(50, 0);
                }
            }
            var ms = UnityEngine.InputSystem.Mouse.current;
            if (inviteBtn != null && ms != null && Cursor.lockState != CursorLockMode.Locked && ms.leftButton.wasPressedThisFrame && !Title.Up && Ctl.Ui == 0 && CameraRig.I != null
                && RectTransformUtility.RectangleContainsScreenPoint(inviteBtn, ms.position.ReadValue(), CameraRig.I.UiCam)) { Game.ClosePause(); Coop.Invite(); }
            for (int i = 0; i < crewRows.Count && i < list.Count; i++)
            {
                var (id, ring, st) = crewRows[i]; var e = list[i]; bool me = id == Coop.Me;
                var body = me ? Angler.I : c.Mates.TryGetValue(id, out var m) ? m : null;
                var crewN = M1Assets.I.CrewNames != null && e.Crew < M1Assets.I.CrewNames.Length ? M1Assets.I.CrewNames[e.Crew] : "";
                string doing = Doing(body); string text = doing == "" ? crewN : crewN + " · " + doing; if (st.text != text) st.text = text;
                var v = !me && body != null ? body.GetComponent<VoiceOut>() : null;
                Speak(ring, me ? Coop.Talking : v != null && v.Speaking, me ? Coop.MyLevel : v != null ? v.Level : 0, Coop.Colours[e.Slot % Coop.Colours.Length]);
            }
        }

        void Mic()
        {
            bool on = Coop.On && Angler.I != null; if (mic.gameObject.activeSelf != on) mic.gameObject.SetActive(on);
            if (!on) return;
            var mode = Settings.Voice; var c = Angler.I.Colour;
            micT.text = mode == Settings.VoiceMode.Off ? "Mic off" : mode == Settings.VoiceMode.PushToTalk ? "Hold V to talk" : Coop.SteamVoice || Coop.Talking ? "Mic on" : "Mic (no Steam)";
            micDot.color = mode == Settings.VoiceMode.Off ? Ponyo.Paper3 : c;
            Speak(micRing, Coop.Talking, Coop.MyLevel, c);
        }

        public bool GhostOn(Angler m) => tags.TryGetValue(m, out var t) && t.R.gameObject.activeSelf && t.Ghost.gameObject.activeSelf; // (tests)

        void Ghost(Tag tg, Fishing f)
        {
            bool on = f != null && f.State == Fishing.St.Minigame && f.ZoneW > 0;
            if (tg.Ghost.gameObject.activeSelf != on) tg.Ghost.gameObject.SetActive(on);
            if (!on) return;
            tg.Zone.anchorMin = new Vector2(Mathf.Clamp01(f.ZoneC - f.ZoneW / 2), 0); tg.Zone.anchorMax = new Vector2(Mathf.Clamp01(f.ZoneC + f.ZoneW / 2), 1);
            tg.Zone.offsetMin = tg.Zone.offsetMax = Vector2.zero;
            tg.Ptr.anchorMin = new Vector2(f.PointerShown, 0); tg.Ptr.anchorMax = new Vector2(f.PointerShown, 1);
            tg.Ptr.sizeDelta = new Vector2(4, 8); tg.Ptr.anchoredPosition = Vector2.zero;
            for (int i = 0; i < 2; i++) tg.Pips[i].color = f.Result[i] > 0 ? Ponyo.Leaf : f.Result[i] < 0 ? Ponyo.Hex("E2553F") : Ponyo.Paper3;
        }

        void LateUpdate() => Place();

        // (public: a test moving the camera for a picture places them again)
        public void Place()
        {
            var c = Coop.I;
            Mic(); Crew();
            foreach (var m in new List<Angler>(tags.Keys))
                if (m == null || c == null || !c.Mates.ContainsValue(m) || tags[m].C != m.Colour) { if (tags[m].R != null) Destroy(tags[m].R.gameObject); tags.Remove(m); }
            if (c == null) return;
            foreach (var m in c.Mates.Values)
            {
                if (m == null) continue;
                if (!tags.TryGetValue(m, out var tg)) tags[m] = tg = Build(m);
                // over the top of the head (the kit's heads are big: the joint is at 82% of the height)
                float h = m.Size * 1.78f;
                var at = (m.Head != null ? m.Head.position : m.transform.position + Vector3.up * 0.82f * h) + Vector3.up * (0.12f * h + 0.14f);
                var vp = cam.WorldToViewportPoint(at); float d = Vector3.Distance(cam.transform.position, at);
                bool on = vp.z > 0.2f && d < 40;
                tg.R.gameObject.SetActive(on);
                if (!on) continue;
                tg.R.anchorMin = tg.R.anchorMax = new Vector2(vp.x, vp.y); tg.R.anchoredPosition = Vector2.zero; // (any screen size)
                if (tg.T.text != m.NetName) tg.T.text = m.NetName ?? "";
                tg.R.sizeDelta = new Vector2(tg.T.preferredWidth + 50, 34);
                tg.R.localScale = Vector3.one * Mathf.Lerp(1f, 0.62f, Mathf.InverseLerp(6, 30, d));
                tg.G.alpha = 1 - Mathf.InverseLerp(32, 40, d);
                Ghost(tg, m.Fish);
                var v = m.GetComponent<VoiceOut>(); Speak(tg.Ring, v != null && v.Speaking, v != null ? v.Level : 0, m.Colour);
            }
        }
    }

    public static class IconBaker
    {
        static Camera cam;
        public static Texture Bake(GameObject prefab, int w, int h, Material mat, Vector3? viewDir = null, float pad = 1.08f)
        {
            if (cam == null)
            {
                var g = new GameObject("IconCam"); cam = g.AddComponent<Camera>(); cam.enabled = false; cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0); cam.cullingMask = 1 << 31; cam.nearClipPlane = 0.01f; cam.farClipPlane = 20;
                var d = g.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); d.renderPostProcessing = false; d.SetRenderer(1); d.renderShadows = false;
                d.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None;
            }
            var o = Object.Instantiate(prefab); o.transform.position = new Vector3(0, -800, 0);
            foreach (var t in o.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
            foreach (var c in o.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            if (mat != null) foreach (var r in o.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = mat;
            var b = new Bounds(o.transform.position, Vector3.zero); foreach (var r in o.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            var dir = (viewDir ?? new Vector3(1, 0, 0)).normalized; // look along +X: the head (+Z) sits on the left, like the mockups
            cam.transform.position = b.center - dir * 5; cam.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            float aspect = w / (float)h; cam.aspect = aspect;
            float ext = Mathf.Max(b.extents.y, Mathf.Max(b.extents.z, b.extents.x) / aspect);
            cam.orthographicSize = ext * pad;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8, name = "icon " + prefab.name };
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
            Object.DestroyImmediate(o);
            return rt;
        }
    }
}
