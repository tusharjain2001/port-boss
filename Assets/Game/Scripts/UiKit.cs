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
        static Sprite fill, edge, blur, circle, ringS, star, diamond, dotS, ringThin;
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
        public static Sprite Circle => circle; public static Sprite RingSprite => ringS; public static Sprite RingThin => ringThin;
        public static Sprite GradeShape(int g) => g switch { 0 => dotS, 1 => ringS, 2 => diamond, _ => star };

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
            var r = Rect(name, parent); var i = r.gameObject.AddComponent<Image>(); i.sprite = s; i.color = c; i.raycastTarget = false;
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
            var e = Img(root, edge, line ?? Ponyo.Line, "edge"); e.rectTransform.Stretch(); e.pixelsPerUnitMultiplier = ppu;
            return root;
        }

        public static Image Fill(RectTransform card) => card.Find("fill").GetComponent<Image>();
        public static Image Edge(RectTransform card) => card.Find("edge").GetComponent<Image>();

        public static Text Txt(Transform parent, string s, Font f, int size, Color c, TextAnchor a = TextAnchor.MiddleLeft, string name = "txt")
        {
            var r = Rect(name, parent); var t = r.gameObject.AddComponent<Text>();
            t.font = f; t.fontSize = size; t.color = c; t.alignment = a; t.text = s; t.raycastTarget = false;
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
            float w = Mathf.Max(h, 14 + label.Length * (h * 0.42f));
            var k = Card(parent, "key " + label, 7, Ponyo.Paper, false);
            k.sizeDelta = new Vector2(w, h);
            var under = Img(k, fill, Ponyo.Line, "under"); under.rectTransform.Stretch(); under.rectTransform.anchoredPosition = new Vector2(0, -2.5f); under.pixelsPerUnitMultiplier = BaseR / 7; under.transform.SetAsFirstSibling();
            var t = Txt(k, label, Bold, Mathf.RoundToInt(h * 0.5f), Ponyo.Ink, TextAnchor.MiddleCenter); t.rectTransform.Stretch();
            return k;
        }

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
