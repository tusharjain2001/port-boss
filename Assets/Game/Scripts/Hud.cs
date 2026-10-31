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
        public struct A { public string Sender, Text; public Kind K; public float Born; }
        public static readonly List<A> Queue = new();
        public static System.Action<A> OnPush;
        public static void Push(string sender, string text, Kind k = Kind.Info)
        {
            var a = new A { Sender = sender, Text = text, K = k, Born = Time.unscaledTime };
            Queue.Add(a); Debug.Log($"[PB] alert {k}: {sender}: {text}");
            OnPush?.Invoke(a);
        }
        public static Color Col(Kind k) => k switch
        {
            Kind.Message => Ponyo.SeaC, Kind.Money => Ponyo.Sunflower, Kind.Weather => Ponyo.SeaDeep, Kind.Warning => Ponyo.Poppy,
            Kind.Crew => Ponyo.Leaf, Kind.Story => Ponyo.Night, _ => Ponyo.Paper3,
        };
        public static string Glyph(Kind k) => k switch { Kind.Message => "…", Kind.Money => "$", Kind.Warning => "!", Kind.Crew => "★", Kind.Story => "W", Kind.Weather => "~", _ => "i" };
    }

    public class Hud : MonoBehaviour
    {
        public static Hud I;
        Canvas canvas; RectTransform root;
        Angler me; Fishing fish;
        public static readonly Dictionary<string, Texture> Icons = new();
        public static Texture RodIcon;

        // hotbar
        readonly RectTransform[] slot = new RectTransform[8]; readonly RawImage[] slotIcon = new RawImage[8]; readonly Text[] slotCount = new Text[8];
        readonly Image[] slotGrade = new Image[8]; readonly Image[] slotFresh = new Image[8]; readonly float[] slotPulse = new float[8];
        RectTransform namePill; Text nameT, nameSub; float nameShow;
        RectTransform prompts; float promptsKey; string lastPrompt;
        Text baitTag;
        // alerts
        readonly List<(RectTransform r, Alerts.A a, float h, Image life)> cards = new();
        readonly List<Alerts.A> held = new();
        // cast
        Image arc; Text distLabel;
        // minigame
        RectTransform mg; Text mgTitle, mgSub, mgStopLbl, mgHeavy; readonly Image[] pip = new Image[3]; readonly Text[] pipX = new Text[3]; readonly Image[] pipChk = new Image[3];
        RectTransform bar, zone, pointer, ghost; Text barNote; readonly RectTransform[] ladder = new RectTransform[4];
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
            var cs = g.AddComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(W, H); cs.matchWidthOrHeight = 0.5f;
            h.root = (RectTransform)g.transform;
            h.Build();
            Alerts.OnPush += h.OnAlert;
            return h;
        }

        void OnDestroy() { Alerts.OnPush -= OnAlert; }

        // ------------------------------------------------------------------------------------------------- build
        void Build()
        {
            // hotbar (bottom centre): 8 slots on a paper strip
            float sw = 84, gap = 10, total = 8 * sw + 7 * gap + 28;
            var strip = UiKit.Card(root, "hotbar", 22, new Color(1, 0.984f, 0.949f, 0.92f));
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
            baitTag = UiKit.Txt(root, "", UiKit.Bold, 17, Ponyo.Paper, TextAnchor.LowerLeft);
            baitTag.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(total / 2 + 22, 38), new Vector2(340, 30), new Vector2(0, 0));
            UiKit.Shade(baitTag, new Color(0.25f, 0.17f, 0.15f, 0.75f), new Vector2(1.5f, -1.5f));

            // item name pill (above the hotbar, 2 s after a switch)
            namePill = UiKit.Card(root, "itemName", 24, Ponyo.Paper, true);
            namePill.Place(new Vector2(0.5f, 0), new Vector2(0, 26 + sw + 24 + 14), new Vector2(520, 50), new Vector2(0.5f, 0));
            nameT = UiKit.Txt(namePill, "", UiKit.Display, 27, Ponyo.Ink, TextAnchor.MiddleLeft); nameT.rectTransform.Stretch(); nameT.rectTransform.offsetMin = new Vector2(22, 0);
            nameSub = UiKit.Txt(namePill, "", UiKit.Body, 16, Ponyo.InkSoft, TextAnchor.MiddleRight); nameSub.rectTransform.Stretch(); nameSub.rectTransform.offsetMax = new Vector2(-22, 0);
            nameShow = 3.5f;

            prompts = UiKit.Rect("prompts", root).Place(new Vector2(0.5f, 0), new Vector2(0, 26 + sw + 24 + 76), new Vector2(1200, 40), new Vector2(0.5f, 0));
            var hl = prompts.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 10; hl.childAlignment = TextAnchor.MiddleCenter; hl.childControlWidth = hl.childControlHeight = false; hl.childForceExpandWidth = false;

            // cast arc (beside the crosshair) + distance label
            arc = UiKit.Img(root, UiKit.RingThin, Color.white, "castArc"); arc.type = Image.Type.Filled; arc.fillMethod = Image.FillMethod.Radial360; arc.fillOrigin = (int)Image.Origin360.Bottom; arc.fillClockwise = false;
            arc.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(130, 130)); arc.rectTransform.localRotation = Quaternion.Euler(0, 0, -60); arc.enabled = false;
            distLabel = UiKit.Txt(root, "", UiKit.Bold, 20, Color.white, TextAnchor.MiddleCenter); distLabel.rectTransform.Place(new Vector2(0, 0), Vector2.zero, new Vector2(160, 30));
            UiKit.Shade(distLabel, new Color(0.25f, 0.17f, 0.15f, 0.8f), new Vector2(1.5f, -1.5f));

            BuildMinigame(); BuildHeavy(); BuildReveal();

            fly = UiKit.Rect("fly", root).gameObject.AddComponent<RawImage>(); fly.raycastTarget = false; fly.enabled = false; fly.rectTransform.sizeDelta = new Vector2(96, 54);
            fly.rectTransform.anchorMin = fly.rectTransform.anchorMax = Vector2.zero;

            dbg = UiKit.Txt(root, "", UiKit.Bold, 15, Ponyo.Paper, TextAnchor.UpperRight); dbg.rectTransform.Place(new Vector2(1, 1), new Vector2(-30, -26), new Vector2(560, 400), new Vector2(1, 1));
            UiKit.Shade(dbg, new Color(0.1f, 0.08f, 0.1f, 0.9f), new Vector2(1.5f, -1.5f));

            fade = UiKit.Img(root, null, new Color(0.11f, 0.15f, 0.28f, 0), "fade"); fade.rectTransform.Stretch(); fade.transform.SetAsLastSibling();
        }

        void BuildMinigame()
        {
            mg = UiKit.Card(root, "minigame", 26, Ponyo.Paper, true);
            mg.Place(new Vector2(0.5f, 0), new Vector2(0, 168), new Vector2(900, 252), new Vector2(0.5f, 0));
            var stamp = UiKit.Img(mg, UiKit.Circle, Ponyo.SeaDeep, "stamp"); stamp.rectTransform.Place(new Vector2(0, 1), new Vector2(32, -26), new Vector2(46, 46), new Vector2(0, 1));
            var st = UiKit.Txt(stamp.transform, "><>", UiKit.Black, 16, Ponyo.Paper, TextAnchor.MiddleCenter); st.rectTransform.Stretch();
            mgTitle = UiKit.Txt(mg, "Fighting!", UiKit.Display, 34, Ponyo.Ink, TextAnchor.MiddleLeft); mgTitle.rectTransform.Place(new Vector2(0, 1), new Vector2(92, -18), new Vector2(500, 42), new Vector2(0, 1));
            mgSub = UiKit.Txt(mg, "Take your time · a miss only lowers the grade", UiKit.Body, 17, Ponyo.InkSoft, TextAnchor.MiddleLeft); mgSub.rectTransform.Place(new Vector2(0, 1), new Vector2(94, -58), new Vector2(560, 26), new Vector2(0, 1));
            mgStopLbl = UiKit.Txt(mg, "STOP", UiKit.Bold, 14, Ponyo.InkSoft, TextAnchor.MiddleRight); mgStopLbl.rectTransform.Place(new Vector2(1, 1), new Vector2(-170, -30), new Vector2(80, 30), new Vector2(1, 1));
            for (int i = 0; i < 3; i++)
            {
                var p = UiKit.Img(mg, UiKit.Circle, Ponyo.Paper2, "pip" + i); p.rectTransform.Place(new Vector2(1, 1), new Vector2(-130 + i * 42, -28), new Vector2(32, 32), new Vector2(0.5f, 1));
                var e = UiKit.Img(p.transform, UiKit.RingSprite, Ponyo.Line, "edge"); e.rectTransform.Stretch(-1);
                pipX[i] = UiKit.Txt(p.transform, "", UiKit.Black, 16, Ponyo.Paper, TextAnchor.MiddleCenter); pipX[i].rectTransform.Stretch();
                pipChk[i] = UiKit.Img(p.transform, UiKit.Check, Ponyo.Paper, "chk"); pipChk[i].rectTransform.Stretch(6); pipChk[i].enabled = false;
                pip[i] = p;
            }
            mgHeavy = UiKit.Txt(mg, "", UiKit.Black, 15, Ponyo.Poppy, TextAnchor.MiddleRight); mgHeavy.rectTransform.Place(new Vector2(1, 1), new Vector2(-32, -64), new Vector2(420, 24), new Vector2(1, 1));
            // the bar
            bar = UiKit.Card(mg, "bar", 24, Ponyo.Hex("CFF2F0"), false); bar.Place(new Vector2(0.5f, 1), new Vector2(0, -122), new Vector2(830, 46), new Vector2(0.5f, 0.5f));
            var wave = UiKit.Img(bar, null, new Color(0.27f, 0.84f, 0.78f, 0.25f), "tide"); wave.rectTransform.Stretch(6); wave.rectTransform.offsetMax = new Vector2(-6, -24);
            zone = UiKit.Rect("zone", bar); zone.anchorMin = new Vector2(0.4f, 0); zone.anchorMax = new Vector2(0.6f, 1); zone.offsetMin = new Vector2(0, 3); zone.offsetMax = new Vector2(0, -3);
            var zr = zone.gameObject.AddComponent<RawImage>(); zr.texture = UiKit.Stripes(Ponyo.Leaf, Ponyo.Hex("6AAE58"), 32); zr.raycastTarget = false;
            var zl = UiKit.Img(zone, null, Ponyo.Hex("4E8C40"), "l"); zl.rectTransform.anchorMin = new Vector2(0, 0); zl.rectTransform.anchorMax = new Vector2(0, 1); zl.rectTransform.sizeDelta = new Vector2(3, 0);
            var zr2 = UiKit.Img(zone, null, Ponyo.Hex("4E8C40"), "r"); zr2.rectTransform.anchorMin = new Vector2(1, 0); zr2.rectTransform.anchorMax = new Vector2(1, 1); zr2.rectTransform.sizeDelta = new Vector2(3, 0);
            var sweet = UiKit.Img(zone, null, new Color(1, 1, 1, 0.9f), "sweet"); sweet.rectTransform.anchorMin = new Vector2(0.5f, 0.2f); sweet.rectTransform.anchorMax = new Vector2(0.5f, 0.8f); sweet.rectTransform.sizeDelta = new Vector2(3, 0);
            ghost = UiKit.Rect("ghost", bar); ghost.anchorMin = new Vector2(0.3f, 0); ghost.anchorMax = new Vector2(0.3f, 1); ghost.sizeDelta = new Vector2(2, -10);
            var gi = ghost.gameObject.AddComponent<Image>(); gi.color = new Color(0.31f, 0.55f, 0.25f, 0.8f); gi.raycastTarget = false;
            // pointer: a little red-and-white bobber riding a stick above the bar
            pointer = UiKit.Rect("pointer", bar); pointer.anchorMin = pointer.anchorMax = new Vector2(0, 0.5f); pointer.sizeDelta = new Vector2(30, 80); pointer.anchoredPosition = Vector2.zero;
            var stick = UiKit.Img(pointer, null, Ponyo.Line, "stick"); stick.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(4, 56));
            var foot = UiKit.Img(pointer, UiKit.Circle, Ponyo.Line, "foot"); foot.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -34), new Vector2(12, 12));
            var bw = UiKit.Img(pointer, UiKit.Circle, Ponyo.Paper, "bob"); bw.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 28), new Vector2(28, 28));
            var br = UiKit.Img(bw.transform, UiKit.Circle, Ponyo.Poppy, "red"); br.type = Image.Type.Filled; br.fillMethod = Image.FillMethod.Vertical; br.fillOrigin = 1; br.fillAmount = 0.52f; br.rectTransform.Stretch(2);
            UiKit.Img(bw.transform, UiKit.RingSprite, Ponyo.Line, "edge").rectTransform.Stretch(-1);
            barNote = UiKit.Txt(mg, "", UiKit.Bold, 14, Ponyo.Hex("4E8C40"), TextAnchor.MiddleCenter); barNote.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(300, 22), new Vector2(0.5f, 0.5f));
            // grade ladder + key hint
            string[] g = Ponyo.GradeName;
            for (int i = 0; i < 4; i++)
            {
                var pill = UiKit.Card(mg, "ladder" + i, 18, Ponyo.Paper2, false, Ponyo.Paper3);
                pill.Place(new Vector2(0, 0), new Vector2(32 + i * 122, 22), new Vector2(108, 36), new Vector2(0, 0));
                var d = UiKit.Img(pill, UiKit.GradeShape(i), Ponyo.GradeCol[i], "dot"); d.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(14, 14), new Vector2(0, 0.5f));
                var t = UiKit.Txt(pill, g[i], UiKit.Bold, 16, Ponyo.InkSoft, TextAnchor.MiddleLeft); t.rectTransform.Stretch(); t.rectTransform.offsetMin = new Vector2(34, 0);
                ladder[i] = pill;
                if (i < 3) { var ch = UiKit.Txt(mg, "›", UiKit.Bold, 20, Ponyo.InkSoft, TextAnchor.MiddleCenter); ch.rectTransform.Place(new Vector2(0, 0), new Vector2(32 + i * 122 + 115, 26), new Vector2(10, 30), new Vector2(0.5f, 0)); }
            }
            var kh = UiKit.Rect("keys", mg).Place(new Vector2(1, 0), new Vector2(-30, 24), new Vector2(300, 34), new Vector2(1, 0));
            var khl = kh.gameObject.AddComponent<HorizontalLayoutGroup>(); khl.spacing = 8; khl.childAlignment = TextAnchor.MiddleRight; khl.childControlWidth = khl.childControlHeight = false; khl.childForceExpandWidth = false;
            UiKit.Key(kh, "Space", 32); UiKit.Key(kh, "LMB", 32); UiKit.Txt(kh, "Stop", UiKit.Bold, 19, Ponyo.Ink, TextAnchor.MiddleLeft).rectTransform.sizeDelta = new Vector2(52, 32);
            mg.gameObject.SetActive(false);
        }

        void BuildHeavy()
        {
            heavy = UiKit.Card(root, "heavy", 26, Ponyo.Paper, true);
            heavy.Place(new Vector2(0.5f, 0), new Vector2(0, 220), new Vector2(720, 170), new Vector2(0.5f, 0));
            var t = UiKit.Txt(heavy, "Something HEAVY!", UiKit.Display, 36, Ponyo.Poppy, TextAnchor.UpperLeft); t.rectTransform.Stretch(26);
            heavyT = UiKit.Txt(heavy, "", UiKit.Body, 19, Ponyo.Ink, TextAnchor.UpperLeft); heavyT.rectTransform.Stretch(26); heavyT.rectTransform.offsetMax = new Vector2(-26, -72);
            var kh = UiKit.Rect("keys", heavy).Place(new Vector2(1, 0), new Vector2(-26, 22), new Vector2(460, 34), new Vector2(1, 0));
            var hl = kh.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 8; hl.childAlignment = TextAnchor.MiddleRight; hl.childControlWidth = hl.childControlHeight = false; hl.childForceExpandWidth = false;
            UiKit.Key(kh, "E", 32); UiKit.Txt(kh, "Fight it", UiKit.Bold, 19, Ponyo.Ink).rectTransform.sizeDelta = new Vector2(90, 32);
            UiKit.Key(kh, "Q", 32); UiKit.Txt(kh, "Let it go", UiKit.Bold, 19, Ponyo.Ink).rectTransform.sizeDelta = new Vector2(100, 32);
            heavy.gameObject.SetActive(false);
        }

        void BuildReveal()
        {
            dim = UiKit.Img(root, null, new Color(0.17f, 0.15f, 0.3f, 0), "dim"); dim.rectTransform.Stretch();
            rays = UiKit.Rect("rays", root).Place(new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(1500, 1500));
            var ri = rays.gameObject.AddComponent<RawImage>(); ri.texture = UiKit.Rays(); ri.color = new Color(1, 0.95f, 0.8f, 0.22f); ri.raycastTarget = false;
            rv = UiKit.Card(root, "reveal", 30, Ponyo.Paper, true, null, 5);
            rv.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(860, 800));
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
            rvHits = UiKit.Txt(rv, "", UiKit.Body, 18, Ponyo.InkSoft, TextAnchor.MiddleCenter); rvHits.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -680), new Vector2(780, 26), new Vector2(0.5f, 0.5f));
            rvKeep = Button(rv, "E", "Keep", Ponyo.Sunflower, new Vector2(-120, 50));
            rvLet = Button(rv, "Q", "Let it go", Ponyo.Paper, new Vector2(120, 50));
            rvFoot = UiKit.Txt(root, "", UiKit.Bold, 18, Ponyo.Paper, TextAnchor.MiddleCenter); rvFoot.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -420), new Vector2(900, 30));
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
            var m = new Material(Util.ParticleMat); m.SetTexture("_BaseMap", t); m.renderQueue = 3200;
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
            if (fighting && a.K != Alerts.Kind.Warning && a.Sender != "NEW species") { held.Add(a); return; }
            Show(a);
        }

        void Show(Alerts.A a)
        {
            var c = UiKit.Card(root, "alert", 20, Ponyo.Paper, true);
            float h = 92 + (a.Text.Length > 44 ? 24 : 0);
            c.Place(new Vector2(0, 1), new Vector2(-480, -40), new Vector2(452, h), new Vector2(0, 1));
            var st = UiKit.Img(c, UiKit.Circle, Alerts.Col(a.K), "stamp"); st.rectTransform.Place(new Vector2(0, 1), new Vector2(18, -18), new Vector2(40, 40), new Vector2(0, 1));
            UiKit.Img(st.transform, UiKit.RingSprite, Ponyo.Line, "edge").rectTransform.Stretch(-1);
            var gl = UiKit.Txt(st.transform, Alerts.Glyph(a.K), UiKit.Black, 20, a.K == Alerts.Kind.Info ? Ponyo.Ink : Ponyo.Paper, TextAnchor.MiddleCenter); gl.rectTransform.Stretch();
            var s = UiKit.Txt(c, UiKit.Caps(a.Sender), UiKit.Bold, 14, Ponyo.InkSoft, TextAnchor.UpperLeft); s.rectTransform.Stretch(); s.rectTransform.offsetMin = new Vector2(72, 0); s.rectTransform.offsetMax = new Vector2(-16, -16);
            var t = UiKit.Txt(c, a.Text, UiKit.Body, 20, Ponyo.Ink, TextAnchor.UpperLeft); t.rectTransform.Stretch(); t.rectTransform.offsetMin = new Vector2(72, 12); t.rectTransform.offsetMax = new Vector2(-18, -38);
            var life = UiKit.Img(c, null, Alerts.Col(a.K == Alerts.Kind.Info ? Alerts.Kind.Crew : a.K), "life"); life.rectTransform.anchorMin = new Vector2(0, 0); life.rectTransform.anchorMax = new Vector2(1, 0); life.rectTransform.pivot = new Vector2(0, 0);
            life.rectTransform.offsetMin = new Vector2(16, 6); life.rectTransform.offsetMax = new Vector2(-16, 10);
            var aa = a; aa.Born = Time.unscaledTime;
            cards.Insert(0, (c, aa, h, life));
            Snd.Play2D(a.K == Alerts.Kind.Warning ? Synth.Bloop : M1Assets.I.Click, a.K == Alerts.Kind.Warning ? 0.25f : 0.2f, a.K == Alerts.Kind.Warning ? 0.7f : 1.1f);
        }

        static float Life(Alerts.A a) => a.K == Alerts.Kind.Warning ? 8f : Mathf.Min(9f, 5f + 0.04f * a.Text.Length);

        // ------------------------------------------------------------------------------------------------ per frame
        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            Fx.Tick();
            if (Ctl.Key_(UnityEngine.InputSystem.Key.F3)) ShowDebug = !ShowDebug;
            Hotbar(dt); Prompts(); Cast(); Minigame(dt); Heavy(); Reveal(dt); Fly(dt); AlertsTick(dt); Debug_();
            fade.color = new Color(fade.color.r, fade.color.g, fade.color.b, FadeA);
        }

        void Hotbar(float dt)
        {
            var b = me.Bar;
            for (int i = 0; i < 8; i++)
            {
                var s = b.S[i]; bool sel = i == b.Selected;
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
                slotFresh[i].enabled = true; // ponytail: rot arrives in M3; full freshness until then
            }
            // the name of the selected item, 2.5 s after a switch (always while it's the rod and nothing else is going on)
            var sl = b.Sel;
            nameShow -= dt;
            bool show = nameShow > 0 && (fish.State is not (Fishing.St.Minigame or Fishing.St.Reveal or Fishing.St.Heavy));
            namePill.gameObject.SetActive(show && !sl.Empty);
            if (sl.Rod) { nameT.text = fish.Rod.Name; nameSub.text = $"{BaitText()} · line {fish.Rod.LineKg:0} kg"; }
            else if (sl.Fish.Count > 0) { var f = sl.Fish[0]; nameT.text = f.Species.Name; nameSub.text = $"{Ponyo.GradeName[f.Grade]} · {sl.Fish.Count} of 5 · about ${Mathf.RoundToInt(f.Value * 0.6f)} each"; }
            namePill.sizeDelta = new Vector2(Mathf.Max(460, nameT.preferredWidth + nameSub.preferredWidth + 70), 50);
            baitTag.text = sl.Rod && !me.AtHelm ? (fish.BaitCount[fish.BaitI] > 0 ? BaitText() : "<color=#F7C548>No bait (sprats only)</color>") : "";
        }

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
                    case Fishing.St.Ready: list.Add(("LMB", "Hold to cast")); break;
                    case Fishing.St.Charging: list.Add(("LMB", "Release to cast")); list.Add(("RMB", "Cancel")); break;
                    case Fishing.St.Waiting: case Fishing.St.Settle: list.Add(("LMB", "Strike on the bite")); list.Add(("RMB", "Hold to reel in")); break;
                    case Fishing.St.Bite: list.Add(("LMB", "STRIKE!")); break;
                    case Fishing.St.Stowed: if (me.Bar.Sel.Rod && me.Swimming) list.Add(("", "Can't fish while swimming")); else if (!me.Bar.Sel.Rod && !me.Swimming) list.Add(("1", "Rod")); break;
                }
            string key = string.Join("|", list.ConvertAll(x => x.k + x.v));
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

        void Cast()
        {
            bool on = fish.State == Fishing.St.Charging;
            arc.enabled = on; distLabel.enabled = on;
            if (!on) return;
            arc.fillAmount = fish.Power * 60f / 360f;
            arc.color = Color.Lerp(Color.white, Ponyo.Sunflower, fish.InSweet ? 1 : fish.Power * 0.6f);
            var cam = canvas.worldCamera; var sp = cam.WorldToScreenPoint(fish.Marker + Vector3.up * 0.9f);
            if (sp.z < 0) { distLabel.enabled = false; return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, cam, out var lp);
            distLabel.rectTransform.anchoredPosition = lp + root.rect.size * 0.5f;
            distLabel.text = $"{fish.MarkerDist:0} m";
            distLabel.color = fish.MarkerSnapped ? Ponyo.Hex("FFB0A0") : fish.InSweet ? Ponyo.Sunflower : Color.white;
        }

        void Minigame(float dt)
        {
            bool on = fish.State == Fishing.St.Minigame;
            mgIn = Mathf.MoveTowards(mgIn, on ? 1 : 0, dt / (on ? 0.22f : 0.16f));
            mg.gameObject.SetActive(mgIn > 0.001f);
            if (!mg.gameObject.activeSelf || fish.Hooked == null) return;
            mg.anchoredPosition = new Vector2(0, 168 - 24 * (1 - Util.EaseOutBack(mgIn)));
            var cg = mg.GetComponent<CanvasGroup>() ?? mg.gameObject.AddComponent<CanvasGroup>(); cg.alpha = mgIn;
            mgTitle.text = FishDb.Hint[(int)fish.Hooked.Class];
            mgHeavy.text = fish.HeavyFight ? "HEAVY · 2 misses snap the line" : "";
            int shownStop = Mathf.Min(fish.Stop, 2);
            for (int i = 0; i < 3; i++)
            {
                int r = fish.Result[i];
                pip[i].color = r > 0 ? Ponyo.Leaf : r < 0 ? Ponyo.Hex("C9C2BA") : i == fish.Stop && !fish.Frozen ? Ponyo.Sunflower : Ponyo.Paper2;
                pipX[i].text = r < 0 ? "x" : ""; pipChk[i].enabled = r > 0;
                float pop = fish.Frozen && i == fish.Stop - 1 ? 1.08f : 1f;
                pip[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(pip[i].rectTransform.localScale.x, pop, 1 - Mathf.Exp(-dt / 0.03f));
            }
            double now = Time.timeAsDouble;
            float zc = fish.Frozen ? fish.ZoneC : fish.ZoneAt(now);
            zone.anchorMin = new Vector2(zc - fish.ZoneW / 2, 0); zone.anchorMax = new Vector2(zc + fish.ZoneW / 2, 1);
            zone.GetComponent<RawImage>().uvRect = new Rect(0, 0, fish.ZoneW * 830 / 32f, 46 / 32f);
            float p = fish.Frozen ? fish.PointerShown : fish.PointerAt(now);
            pointer.anchorMin = pointer.anchorMax = new Vector2(p, 0.5f); pointer.anchoredPosition = new Vector2(0, 18);
            var flash = fish.Frozen && fish.Stop > 0 ? (fish.Result[fish.Stop - 1] > 0 ? Ponyo.Leaf : Ponyo.Hex("C9C2BA")) : Ponyo.Paper;
            pointer.Find("bob").GetComponent<Image>().color = flash;
            bool hasGhost = fish.Stop > 0 && !fish.Frozen;
            ghost.gameObject.SetActive(hasGhost);
            if (hasGhost) { ghost.anchorMin = new Vector2(lastStopAt, 0); ghost.anchorMax = new Vector2(lastStopAt, 1); }
            if (fish.Frozen && fish.Stop > 0) { lastStopAt = fish.PointerShown; barNote.text = $"stop {fish.Stop} · {(fish.Result[fish.Stop - 1] > 0 ? "hit" : "miss: one grade lower")}"; }
            else if (fish.Stop == 0) barNote.text = "";
            barNote.rectTransform.anchoredPosition = new Vector2((lastStopAt - 0.5f) * 830, -160);
            barNote.color = fish.Stop > 0 && fish.Result[Mathf.Max(0, fish.Stop - 1)] > 0 ? Ponyo.Hex("4E8C40") : Ponyo.InkSoft;
            for (int i = 0; i < 4; i++)
            {
                bool cur = i == fish.Hits;
                UiKit.Fill(ladder[i]).color = cur ? Ponyo.Paper : Ponyo.Paper2;
                UiKit.Edge(ladder[i]).color = cur ? Ponyo.Line : Ponyo.Paper3;
            }
        }
        float lastStopAt = 0.5f;

        void Heavy()
        {
            bool on = fish.State == Fishing.St.Heavy;
            heavy.gameObject.SetActive(on);
            if (on && fish.Catch != null) heavyT.text = $"Your line is rated {fish.Rod.LineKg:0} kg; this feels like {fish.Catch.Kg:0} kg. Fight it, and 2 misses snap the line. Let it go and nothing is lost (it lets go by itself in {Mathf.Max(0, 4 - fish.StateT):0} s).";
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
                rvSub.text = $"caught on {(fish.BaitId == "None" ? "a bare hook" : FishDb.Baits[fish.BaitI].Name.ToLower())} off Walt's Landing, at golden hour · {sp.Class} species";
                rvGrade.text = new string('●', c.Grade) + new string('○', 3 - c.Grade) + "  " + Ponyo.GradeName[c.Grade].ToUpper();
                rvGrade.text = Ponyo.GradeName[c.Grade].ToUpper();
                UiKit.Fill(rvGradePill).color = Ponyo.GradeCol[c.Grade];
                rvGrade.color = c.Grade == 0 || c.Grade == 3 ? Ponyo.Ink : Ponyo.Paper;
                rvStat[0].text = c.Cm >= 100 ? $"{c.Cm / 100f:0.00} m" : $"{c.Cm:0} cm"; rvStatSub[0].text = fish.IsNew ? "first one!" : fish.IsBest ? "best so far" : "";
                rvStat[1].text = c.Kg < 1 ? $"{c.Kg * 1000:0} g" : $"{c.Kg:0.0} kg"; rvStatSub[1].text = fish.HeavyFight ? "line held!" : "";
                rvStat[2].text = $"${Mathf.Max(1, Mathf.RoundToInt(c.Value * 0.6f))}"; rvStatSub[2].text = $"at the market, ×{Ponyo.GradeMul[c.Grade]:0.0}";
                rvStat[3].text = "2 days"; rvStatSub[3].text = "then it's bait";
                rvHits.text = c.Grade == 3 ? "3 of 3 stops hit. <b>Prized!</b>" : $"{c.Grade} of 3 stops hit. <b>Prized</b> needs all three.";
                rvNew.gameObject.SetActive(fish.IsNew); rvBest.gameObject.SetActive(fish.IsBest);
            }
            if (!on && rv.gameObject.activeSelf)
            {
                SetReveal(false);
                foreach (var a in held) Show(a);
                held.Clear();
            }
            if (!on) return;
            rvIn = Mathf.MoveTowards(rvIn, 1, dt / 0.28f);
            float e = Util.EaseOutBack(rvIn);
            rv.anchoredPosition = new Vector2(0, 20 + 60 * (1 - e)); rv.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, e);
            dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, 0.38f * rvIn);
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
            for (int i = 0; i < cards.Count; i++)
            {
                var (r, a, h, life) = cards[i];
                float age = Time.unscaledTime - a.Born, L = Life(a);
                float inT = Mathf.Clamp01(age / 0.22f), outT = Mathf.Clamp01((age - L) / 0.16f);
                float x = 40 - 480 * (1 - Util.EaseOutBack(inT)) - 8 * outT;
                bool visible = i < 4;
                var cg = r.GetComponent<CanvasGroup>() ?? r.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = (1 - outT) * (visible ? 1 : 0) * (i == 3 ? 0.7f : 1);
                r.localScale = Vector3.one * (i == 3 ? 0.97f : 1);
                var cur = r.anchoredPosition; r.anchoredPosition = new Vector2(x, Mathf.Lerp(cur.y, y, 1 - Mathf.Exp(-dt / 0.06f)));
                life.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(1 - age / L), 0);
                if (visible) y -= h + 14;
                if (outT >= 1) { Destroy(r.gameObject); cards.RemoveAt(i); i--; }
            }
        }

        void Debug_()
        {
            dbg.enabled = ShowDebug;
            if (!ShowDebug) return;
            var f = fish; var sb = new System.Text.StringBuilder();
            sb.AppendLine($"F3 debug · {f.State} {f.StateT:F2}s · fps {1f / Mathf.Max(1e-4f, Time.smoothDeltaTime):0}");
            sb.AppendLine($"rod {f.RodTier} {f.Rod.Name} · bites {f.Rod.BiteLo:0}–{f.Rod.BiteHi:0} s · green ×{f.Rod.GreenMul:0.00} · speed ×{f.Rod.SpeedMul:0.00} (F5)");
            sb.AppendLine($"bait {FishDb.Baits[f.BaitI].Name} ×{f.BaitCount[f.BaitI]} (F6)");
            if (f.State == Fishing.St.Waiting) sb.AppendLine($"bite in {f.BiteAt - Time.time:F2} s of {f.BiteWait:F2} · nibbles {f.NibblesDone}/{f.Nibbles}");
            if (f.Hooked != null) sb.AppendLine($"hooked: {f.Hooked.Name} ({f.Hooked.Class})");
            if (f.State is Fishing.St.Minigame && f.Hooked != null) sb.AppendLine($"window {f.ZoneW / f.PtrSpeed * 1000:0} ms · zone {f.ZoneW:P0} · speed {f.PtrSpeed:F2}/s · passes {f.Passes:F1}");
            sb.AppendLine($"stop errors: {f.ErrMs[0]:0} / {f.ErrMs[1]:0} / {f.ErrMs[2]:0} ms (≤ 0 = in the green) · offset {Settings.TimingOffsetMs:0} ms");
            var s = Skiff.I; if (s != null) sb.AppendLine($"Biscuit {s.Speed:F1} m/s notch {s.Notch} {(s.Moored ? "moored" : s.Anchored ? "anchored" : "free")} roll {Mathf.DeltaAngle(0, s.transform.eulerAngles.z):F1}°");
            dbg.text = sb.ToString();
        }
    }

    // Item icons are rendered from the real 3D models with the same toon shader and outline (systems §2.6).
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
