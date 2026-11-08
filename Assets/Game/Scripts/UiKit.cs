using UnityEngine;
using UnityEngine.UI;

namespace PortBoss.M1
{
    // Letter-paper UI pieces (gdd systems §2): cream cards with a 2 px warm-brown line, a printed offset shadow in the
    // line colour plus a soft blue-violet one (never black), Fraunces for things you read aloud, Zen Maru Gothic for
    // the rest. Sprites are generated once (no image files to keep in sync).
    public static class UiKit
    {
        public static Font Display, DisplayBold, Body, Bold, Black;
        static Sprite fill, edge, blur, circle, ringS, star, diamond, dotS, ringThin, ringFat;
        // The UI follows the light (systems §2.7): every HUD image and text shares this material; Sky sets its tint
        // (warmer at golden/dusk, dimmer and cooler at night) so a night HUD doesn't glare. UI/Default multiplies by _Color.
        public static Material Lit => lit ??= new Material(Shader.Find("UI/Default")) { name = "PB_UiLit" };
        static Material lit;
        // a card's printed edge (systems §2.7, Night: "a lamp-orange rim on cards"): Lit's tint, warmed toward lamp-orange
        // #FFB060 with the night (x2 on the brown line #805C52 is about the lamp)
        public static Material Rim => rim ??= new Material(Shader.Find("UI/Default")) { name = "PB_UiRim" };
        static Material rim;
        public const float BaseR = 28f;

        public static void Init(M1Assets a)
        {
            Display = a.Display; DisplayBold = a.DisplayBold ?? a.Display; Body = a.Ui; Bold = a.UiBold ?? a.Ui; Black = a.UiBlack ?? Bold;
            fill = Rounded(64, BaseR, 0, false);
            edge = Rounded(64, BaseR, 2.2f, true);
            blur = Soft(96, 30, 22);
            circle = Shape(64, (x, y) => Disc(x, y, 0.94f));
            ringS = Shape(64, (x, y) => Mathf.Clamp01(Disc(x, y, 0.94f) - Disc(x, y, 0.80f)));
            ringThin = Shape(128, (x, y) => Mathf.Clamp01(Disc(x, y, 0.96f) - Disc(x, y, 0.90f)));
            ringFat = Shape(128, (x, y) => Mathf.Clamp01(Disc(x, y, 0.94f) - Disc(x, y, 0.62f)));   // (the stamina ring)
            dotS = Shape(32, (x, y) => Disc(x, y, 0.9f));
            diamond = Shape(32, (x, y) => Mathf.Clamp01((0.92f - (Mathf.Abs(x) + Mathf.Abs(y))) * 16f));
            star = Shape(64, (x, y) =>
            {
                float a = Mathf.Atan2(y, x), r = Mathf.Sqrt(x * x + y * y);
                float k = Mathf.Cos(5 * (a - Mathf.PI / 2)) * 0.5f + 0.5f; // 5 points
                float edgeR = Mathf.Lerp(0.42f, 0.95f, Mathf.Pow(k, 2.2f));
                return Mathf.Clamp01((edgeR - r) * 20f);
            });
        }

        static float Disc(float x, float y, float r) => Mathf.Clamp01((r - Mathf.Sqrt(x * x + y * y)) * 40f);

        static Sprite Rounded(int s, float r, float line, bool outlineOnly)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float dx = Mathf.Max(r - px, px - (s - r), 0), dy = Mathf.Max(r - py, py - (s - r), 0);
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - r; // <0 inside
                    float inside = Mathf.Clamp01(0.5f - d);
                    float a = outlineOnly ? inside * Mathf.Clamp01(d + line + 0.5f) : inside;
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            t.Apply();
            float b = r + 2;
            return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        static Sprite Soft(int s, float r, float soft)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float inner = r + soft;
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float dx = Mathf.Max(inner - px, px - (s - inner), 0), dy = Mathf.Max(inner - py, py - (s - inner), 0);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1 - d / soft); a = a * a * (3 - 2 * a);
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            t.Apply();
            float b = inner + 1;
            return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        static Sprite Shape(int s, System.Func<float, float, float> a)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                    t.SetPixel(x, y, new Color(1, 1, 1, a((x + 0.5f) / s * 2 - 1, (y + 0.5f) / s * 2 - 1)));
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100);
        }

        static Sprite check;
        public static Sprite Check => check ??= Shape(48, (x, y) =>
        {
            // a tick: two strokes (-0.55,0)->(-0.15,-0.4)->(0.55,0.45)
            float Seg(float ax, float ay, float bx, float by)
            {
                float vx = bx - ax, vy = by - ay, t = Mathf.Clamp01(((x - ax) * vx + (y - ay) * vy) / (vx * vx + vy * vy));
                float dx = x - (ax + vx * t), dy = y - (ay + vy * t); return Mathf.Sqrt(dx * dx + dy * dy);
            }
            float d = Mathf.Min(Seg(-0.55f, 0f, -0.15f, -0.4f), Seg(-0.15f, -0.4f, 0.55f, 0.45f));
            return Mathf.Clamp01((0.16f - d) * 24f);
        });
        public static Sprite Circle => circle; public static Sprite RingSprite => ringS; public static Sprite RingThin => ringThin; public static Sprite RingFat => ringFat;
        public static Sprite GradeShape(int g) => g == 0 ? dotS : star;

        // ---- builders ----
        public static RectTransform Rect(string name, Transform parent)
        {
            var g = new GameObject(name, typeof(RectTransform)); g.transform.SetParent(parent, false); g.layer = parent.gameObject.layer;
            return (RectTransform)g.transform;
        }

        public static RectTransform Place(this RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            r.anchorMin = r.anchorMax = anchor; r.pivot = pivot ?? anchor; r.anchoredPosition = pos; r.sizeDelta = size; return r;
        }

        public static RectTransform Stretch(this RectTransform r, float pad = 0)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(0.5f, 0.5f); r.offsetMin = new Vector2(pad, pad); r.offsetMax = new Vector2(-pad, -pad); return r;
        }

        public static Image Img(Transform parent, Sprite s, Color c, string name = "img")
        {
            var r = Rect(name, parent); var i = r.gameObject.AddComponent<Image>(); i.sprite = s; i.color = c; i.raycastTarget = false; i.material = Lit;
            if (s != null && s.border != Vector4.zero) i.type = Image.Type.Sliced;
            return i;
        }

        // A paper card: soft violet shadow, printed line-colour offset, paper fill, 2 px line. Returns the card root.
        public static RectTransform Card(Transform parent, string name, float radius = 20, Color? paper = null, bool shadow = true, Color? line = null, float lift = 3)
        {
            var root = Rect(name, parent);
            float ppu = BaseR / Mathf.Max(4, radius);
            if (shadow)
            {
                var b = Img(root, blur, new Color(Ponyo.ShadeDeep.r, Ponyo.ShadeDeep.g, Ponyo.ShadeDeep.b, 0.32f), "blur"); b.rectTransform.Stretch(-14); b.rectTransform.anchoredPosition = new Vector2(2, -8); b.pixelsPerUnitMultiplier = 1;
                var o = Img(root, fill, line ?? Ponyo.Line, "printed"); o.rectTransform.Stretch(); o.rectTransform.anchoredPosition = new Vector2(lift * 0.4f, -lift); o.pixelsPerUnitMultiplier = ppu;
            }
            var f = Img(root, fill, paper ?? Ponyo.Paper, "fill"); f.rectTransform.Stretch(); f.pixelsPerUnitMultiplier = ppu;
            var e = Img(root, edge, line ?? Ponyo.Line, "edge"); e.rectTransform.Stretch(); e.pixelsPerUnitMultiplier = ppu; e.material = Rim;
            return root;
        }

        public static Image Fill(RectTransform card) => card.Find("fill").GetComponent<Image>();
        public static Image Edge(RectTransform card) => card.Find("edge").GetComponent<Image>();

        public static Text Txt(Transform parent, string s, Font f, int size, Color c, TextAnchor a = TextAnchor.MiddleLeft, string name = "txt")
        {
            var r = Rect(name, parent); var t = r.gameObject.AddComponent<Text>();
            t.font = f; t.fontSize = size; t.color = c; t.alignment = a; t.text = s; t.raycastTarget = false; t.material = Lit;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.supportRichText = true;
            t.lineSpacing = 1.05f;
            return t;
        }

        // Small-caps label (Zen Maru Gothic 700, caps). ponytail: legacy Text has no letter spacing; TextMeshPro if it matters.
        public static string Caps(string s) => s.ToUpperInvariant();

        public static Shadow Shade(Graphic g, Color c, Vector2 d) { var s = g.gameObject.AddComponent<Shadow>(); s.effectColor = c; s.effectDistance = d; return s; }

        // A paper key cap ("E", "LMB", "Space") as in the mockups.
        public static RectTransform Key(Transform parent, string label, float h = 30)
        {
            label = Ctl.Glyph(label);                                   // M7 step 7: the pad's button when the pad is in use
            float w = Mathf.Max(h, 14 + label.Length * (h * 0.42f));
            var k = Card(parent, "key " + label, 7, Ponyo.Paper, false);
            k.sizeDelta = new Vector2(w, h);
            var under = Img(k, fill, Ponyo.Line, "under"); under.rectTransform.Stretch(); under.rectTransform.anchoredPosition = new Vector2(0, -2.5f); under.pixelsPerUnitMultiplier = BaseR / 7; under.transform.SetAsFirstSibling();
            var t = Txt(k, label, Bold, Mathf.RoundToInt(h * 0.5f), Ponyo.Ink, TextAnchor.MiddleCenter); t.rectTransform.Stretch();
            return k;
        }

        // The hotbar's rod icon, painted (Manan 1 Nov: the baked 3D rod read as a toothpick at slot size): a slightly
        // bent, tapered bamboo blank with red thread wraps and guide rings, a speckled cork handle with a butt cap, a
        // reel with its crank and yellow knob, and a short line down to the red-and-white float. Warm brown outline
        // (the paper UI's ink), drawn with antialiased distance fields at 256 px.
        public static Texture2D RodIcon(int tier = 1)
        {
            const int S = 256; const float OL = 4.5f; // outline px
            var ink = Ponyo.Line;
            Color blank = tier switch { 1 => Ponyo.Hex("D8B070"), 2 => Ponyo.Hex("F2E2C0"), 3 => Ponyo.Hex("4A4A5C"), 4 => Ponyo.Hex("2E70C8"), _ => Ponyo.Hex("7A5638") };
            Color blankHi = Color.Lerp(blank, Color.white, 0.45f), cork = Ponyo.Hex("C9A27A"), corkDk = Ponyo.Hex("A07850"), wrap = Ponyo.Hex("D9483B");
            Color metal = Ponyo.Hex("8A8696"), metalDk = Ponyo.Hex("5E5A66"), knob = Ponyo.Hex("F7C548"), lineC = Ponyo.Hex("FFF6E2"), red = Ponyo.Hex("E2553F"), white = Ponyo.Hex("FFFBF2");
            // the rod's spine: a quadratic curve from the butt (lower left) to the tip (upper right), bowed up a little
            // (texture y goes up)
            Vector2 A = new(34, 34), C = new(150, 128), B = new(222, 226);
            Vector2 P(float t) => (1 - t) * (1 - t) * A + 2 * (1 - t) * t * C + t * t * B;
            const int N = 48; var pts = new Vector2[N + 1]; for (int i = 0; i <= N; i++) pts[i] = P(i / (float)N);
            // width along the rod: the handle, then the blank tapering to the tip
            float W(float t) => t < 0.3f ? Mathf.Lerp(13f, 11f, t / 0.3f) : Mathf.Lerp(7.5f, 3.2f, (t - 0.3f) / 0.7f);
            float RodD(Vector2 q, float t0, float t1, out float tAt)
            {
                float best = 1e9f; tAt = t0;
                int i0 = Mathf.FloorToInt(t0 * N), i1 = Mathf.Min(N, Mathf.CeilToInt(t1 * N));
                for (int i = i0; i < i1; i++)
                {
                    Vector2 a = pts[i], b = pts[i + 1], ab = b - a; float h = Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
                    float t = Mathf.Clamp((i + h) / N, t0, t1);
                    float d = (q - (a + ab * h)).magnitude - W(t);
                    if (d < best) { best = d; tAt = t; }
                }
                return best;
            }
            Vector2 Tan(float t) => (P(Mathf.Min(1, t + 0.01f)) - P(Mathf.Max(0, t - 0.01f))).normalized;
            Vector2 Nrm(float t) { var d = Tan(t); return new Vector2(d.y, -d.x); } // the rod's underside (down-right)
            float Circle(Vector2 q, Vector2 c, float r) => (q - c).magnitude - r;
            float Seg(Vector2 q, Vector2 a, Vector2 b, float r) { var ab = b - a; float h = Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude); return (q - (a + ab * h)).magnitude - r; }
            // the line: from the tip, a soft curve down to the float
            Vector2 tip = P(1), fl = new(210, 104), lc = new(236, 160);
            float LineD(Vector2 q)
            {
                float best = 1e9f; Vector2 prev = tip;
                for (int i = 1; i <= 16; i++) { float u = i / 16f; var p = (1 - u) * (1 - u) * tip + 2 * (1 - u) * u * lc + u * u * (fl + new Vector2(0, 20)); /* to the antenna */ best = Mathf.Min(best, Seg(q, prev, p, 1.2f)); prev = p; }
                return best;
            }
            var reelC = P(0.2f) + Nrm(0.2f) * 26f;
            float[] guides = { 0.46f, 0.62f, 0.77f, 0.9f };
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    var q = new Vector2(x + 0.5f, y + 0.5f);
                    var col = new Color(0, 0, 0, 0);
                    void Lay(float d, Color c, bool outline = true)
                    {
                        if (outline) col = Over(col, new Color(ink.r, ink.g, ink.b, Mathf.Clamp01(0.5f - (d - OL))));
                        col = Over(col, new Color(c.r, c.g, c.b, Mathf.Clamp01(0.5f - d) * c.a));
                    }
                    // line + float (behind the rod)
                    float ld = LineD(q);
                    col = Over(col, new Color(ink.r, ink.g, ink.b, Mathf.Clamp01(0.5f - (ld - 1.6f))));
                    col = Over(col, new Color(lineC.r, lineC.g, lineC.b, Mathf.Clamp01(0.5f - ld)));
                    Lay(Seg(q, fl + new Vector2(0, 10), fl + new Vector2(0, 20), 2.2f), red); // the antenna on top
                    float fd = Circle(q, fl, 11f);
                    if (fd < OL + 1)
                    {
                        Lay(fd, white);
                        if (q.y > fl.y && fd < 0) col = Over(col, new Color(red.r, red.g, red.b, Mathf.Clamp01(0.5f - fd) * Mathf.Clamp01(q.y - fl.y)));
                    }
                    // guide rings under the blank
                    foreach (var g in guides)
                    {
                        var gc = P(g) + Nrm(g) * (W(g) + 5f); float rr = Mathf.Lerp(6f, 4f, g);
                        float ring = Mathf.Abs(Circle(q, gc, rr)) - 1.6f;
                        Lay(Mathf.Min(ring, Seg(q, P(g), gc - Nrm(g) * rr, 1.4f)), metal);
                    }
                    // reel: foot to the rod, body, spool, crank and knob
                    Lay(Seg(q, P(0.2f), reelC, 4f), metalDk);
                    Lay(Circle(q, reelC, 20f), metal);
                    Lay(Circle(q, reelC, 11f), Ponyo.Hex("B7B3C2"), false);
                    Lay(Circle(q, reelC, 4f), metalDk, false);
                    var crank = reelC + new Vector2(-18f, -14f);
                    Lay(Seg(q, reelC, crank, 3f), metalDk);
                    Lay(Circle(q, crank, 7.5f), knob);
                    // the blank: highlight along its top edge, red wraps at the guides
                    float rd = RodD(q, 0.3f, 1f, out float tb);
                    if (rd < OL + 1)
                    {
                        Lay(rd, blank);
                        var side = Vector2.Dot(q - P(tb), Nrm(tb));
                        if (rd < -1.2f && side < -W(tb) * 0.25f) col = Over(col, new Color(blankHi.r, blankHi.g, blankHi.b, 0.8f));
                        foreach (var g in guides) if (Mathf.Abs(tb - g) < 0.012f && rd < 0) col = Over(col, new Color(wrap.r, wrap.g, wrap.b, Mathf.Clamp01(0.5f - rd)));
                    }
                    // the cork handle with a dark butt cap, speckles, a red wrap where the blank starts
                    float hd = RodD(q, 0f, 0.31f, out float th);
                    if (hd < OL + 1)
                    {
                        Lay(hd, th < 0.035f ? Ponyo.Hex("6E5040") : cork);
                        if (hd < -1f && th >= 0.035f && Hash(x / 3, y / 3) > 0.83f) col = Over(col, new Color(corkDk.r, corkDk.g, corkDk.b, 0.9f));
                        if (hd < 0 && th > 0.29f) col = Over(col, new Color(wrap.r, wrap.g, wrap.b, Mathf.Clamp01(0.5f - hd)));
                    }
                    px[y * S + x] = col;
                }
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "RodIcon" };
            tex.SetPixels(px); tex.Apply(true);
            return tex;
        }
        static Color Over(Color under, Color over)
        {
            float a = over.a + under.a * (1 - over.a);
            if (a < 1e-4f) return new Color(0, 0, 0, 0);
            return new Color((over.r * over.a + under.r * under.a * (1 - over.a)) / a, (over.g * over.a + under.g * under.a * (1 - over.a)) / a, (over.b * over.a + under.b * under.a * (1 - over.a)) / a, a);
        }
        static float Hash(int x, int y) { uint h = (uint)(x * 374761393 + y * 668265263); h = (h ^ (h >> 13)) * 1274126177; return (h & 0xffff) / 65535f; }

        public static Texture2D Stripes(Color a, Color b, int s = 32)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) t.SetPixel(x, y, ((x + y) % s) < s / 2 ? a : b);
            t.Apply(); return t;
        }

        public static Texture2D Rays(int s = 512, int n = 18)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                {
                    float fx = (x + 0.5f) / s * 2 - 1, fy = (y + 0.5f) / s * 2 - 1, r = Mathf.Sqrt(fx * fx + fy * fy);
                    float a = Mathf.Atan2(fy, fx) / (2 * Mathf.PI) * n;
                    float ray = Mathf.Clamp01((Mathf.Abs(Mathf.Repeat(a, 1f) - 0.5f) - 0.22f) * 12f);
                    t.SetPixel(x, y, new Color(1, 1, 1, ray * Mathf.Clamp01(1 - r) * Mathf.Clamp01(r * 4)));
                }
            t.Apply(); return t;
        }
    }
}
