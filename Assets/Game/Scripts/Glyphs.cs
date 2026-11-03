using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    // The phone's line icons (systems §1.2, mockup 03-phone): painted in code as antialiased distance fields (white,
    // tinted by the Image), so there is no icon art to license. Each is a few strokes in [-1, 1] space, y up.
    public static class Glyphs
    {
        const int S = 96;
        static readonly Dictionary<string, Sprite> cache = new();

        // distance helpers (negative inside)
        static float Seg(Vector2 p, Vector2 a, Vector2 b) { var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude); return (p - (a + ab * t)).magnitude; }
        static float Ring(Vector2 p, Vector2 c, float r) => Mathf.Abs((p - c).magnitude - r);
        static float Disc(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;
        static float RBox(Vector2 p, Vector2 c, Vector2 h, float r) { var q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - h + Vector2.one * r; return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r; }
        static float Poly(Vector2 p, params Vector2[] v) { float d = 1e9f; for (int i = 0; i < v.Length - 1; i++) d = Mathf.Min(d, Seg(p, v[i], v[i + 1])); return d; }
        static Vector2 V(float x, float y) => new(x, y);
        // an arc of radius r round c, from angle a0 to a1 (degrees, counter-clockwise)
        static float Arc(Vector2 p, Vector2 c, float r, float a0, float a1)
        {
            var d = p - c; float a = Mathf.Repeat(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - a0, 360);
            if (a <= Mathf.Repeat(a1 - a0, 360)) return Mathf.Abs(d.magnitude - r);
            Vector2 E(float deg) => c + new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad)) * r;
            return Mathf.Min((p - E(a0)).magnitude, (p - E(a1)).magnitude);
        }

        // strokes: distance to a line (alpha where < w); fills: signed distance (alpha where < 0)
        public static Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out var s)) return s;
            const float w = 0.085f;
            System.Func<Vector2, float> f = name switch
            {
                "msg" => p => Mathf.Min(Mathf.Abs(RBox(p, V(0, 0.12f), V(0.66f, 0.46f), 0.22f)) - w, Mathf.Min(Poly(p, V(-0.32f, -0.33f), V(-0.5f, -0.66f), V(-0.06f, -0.33f)) - w,
                                Mathf.Min(Seg(p, V(-0.36f, 0.24f), V(0.36f, 0.24f)) - w, Seg(p, V(-0.36f, -0.02f), V(0.16f, -0.02f)) - w))),
                "contract" => p => Mathf.Min(Mathf.Abs(RBox(p, V(0, -0.08f), V(0.5f, 0.66f), 0.12f)) - w, Mathf.Min(RBox(p, V(0, 0.6f), V(0.22f, 0.11f), 0.05f),
                                Mathf.Min(Seg(p, V(-0.26f, 0.22f), V(0.26f, 0.22f)) - w, Mathf.Min(Seg(p, V(-0.26f, -0.06f), V(0.26f, -0.06f)) - w, Seg(p, V(-0.26f, -0.34f), V(0.08f, -0.34f)) - w)))),
                "crew" => p => Mathf.Min(Mathf.Min(Ring(p, V(-0.22f, 0.3f), 0.2f) - w, ArcUp(p, V(-0.22f, -0.42f), 0.42f) - w), Mathf.Min(Ring(p, V(0.36f, 0.36f), 0.16f) - w, ArcUp(p, V(0.42f, -0.24f), 0.3f, 0.1f) - w)),
                "map" => p => Mathf.Min(Poly(p, V(-0.66f, 0.46f), V(-0.22f, 0.62f), V(0.22f, 0.46f), V(0.66f, 0.62f), V(0.66f, -0.46f), V(0.22f, -0.62f), V(-0.22f, -0.46f), V(-0.66f, -0.62f), V(-0.66f, 0.46f)) - w,
                                Mathf.Min(Seg(p, V(-0.22f, 0.62f), V(-0.22f, -0.46f)) - w, Seg(p, V(0.22f, 0.46f), V(0.22f, -0.62f)) - w)),
                "bank" => p => Mathf.Min(Poly(p, V(-0.68f, 0.22f), V(0, 0.66f), V(0.68f, 0.22f), V(-0.68f, 0.22f)) - w, Mathf.Min(Mathf.Min(Seg(p, V(-0.42f, 0.08f), V(-0.42f, -0.42f)), Mathf.Min(Seg(p, V(0, 0.08f), V(0, -0.42f)), Seg(p, V(0.42f, 0.08f), V(0.42f, -0.42f)))) - w, Seg(p, V(-0.68f, -0.58f), V(0.68f, -0.58f)) - w)),
                "forecast" => p => Mathf.Min(Sun(p, V(-0.22f, 0.26f), 0.24f, w), Cloud(p, V(0.12f, -0.26f), w, true)),
                "journal" => p => Mathf.Min(Poly(p, V(0, -0.5f), V(-0.68f, -0.36f), V(-0.68f, 0.52f), V(0, 0.38f), V(0.68f, 0.52f), V(0.68f, -0.36f), V(0, -0.5f)) - w, Seg(p, V(0, 0.38f), V(0, -0.5f)) - w),
                "settings" => p => Gear(p, w),
                "lock" => p => Mathf.Min(RBox(p, V(0, -0.22f), V(0.46f, 0.36f), 0.08f), ArcDown(p, V(0, 0.16f), 0.3f, 0.36f) - w),
                "back" => p => Poly(p, V(0.22f, 0.6f), V(-0.32f, 0), V(0.22f, -0.6f)) - w * 1.3f,
                "anchor" => p => Mathf.Min(Mathf.Min(Ring(p, V(0, 0.56f), 0.14f) - w, Seg(p, V(0, 0.42f), V(0, -0.62f)) - w), Mathf.Min(Seg(p, V(-0.32f, 0.18f), V(0.32f, 0.18f)) - w, ArcDown(p, V(0, -0.1f), 0.52f, 0) - w)),
                "pin" => p => Mathf.Min(Disc(p, V(0, 0.18f), 0.5f), Poly(p, V(-0.36f, -0.04f), V(0, -0.72f), V(0.36f, -0.04f)) - 0.1f),
                "fish" => p => Mathf.Min(Mathf.Abs(Ellipse(p, V(-0.1f, 0), V(0.52f, 0.3f))) - w, Poly(p, V(0.38f, 0), V(0.7f, 0.3f), V(0.7f, -0.3f), V(0.38f, 0)) - w),
                "Clear" => p => Sun(p, V(0, 0), 0.32f, w),
                "Fair" => p => Mathf.Min(Sun(p, V(-0.22f, 0.24f), 0.24f, w), Cloud(p, V(0.12f, -0.24f), w, true)),
                "Breezy" => p => Mathf.Min(Mathf.Min(Poly(p, V(-0.66f, 0.3f), V(0.3f, 0.3f)) - w, WindCurl(p, V(0.3f, 0.46f), w)), Mathf.Min(Poly(p, V(-0.66f, -0.02f), V(0.5f, -0.02f)) - w, Poly(p, V(-0.5f, -0.34f), V(0.1f, -0.34f)) - w)),
                "Fog" => p => Mathf.Min(Mathf.Min(Seg(p, V(-0.6f, 0.34f), V(0.6f, 0.34f)), Seg(p, V(-0.46f, 0.04f), V(0.66f, 0.04f))), Mathf.Min(Seg(p, V(-0.66f, -0.26f), V(0.4f, -0.26f)), Seg(p, V(-0.3f, -0.56f), V(0.5f, -0.56f)))) - w,
                "Rain" => p => Mathf.Min(Cloud(p, V(0, 0.22f), w, false), Mathf.Min(Seg(p, V(-0.3f, -0.3f), V(-0.4f, -0.6f)), Mathf.Min(Seg(p, V(0.02f, -0.3f), V(-0.08f, -0.6f)), Seg(p, V(0.34f, -0.3f), V(0.24f, -0.6f)))) - w),
                "Storm" => p => Mathf.Min(Cloud(p, V(0, 0.24f), w, false), Poly(p, V(0.08f, -0.18f), V(-0.14f, -0.44f), V(0.1f, -0.44f), V(-0.1f, -0.74f)) - w),
                "check" => p => Poly(p, V(-0.5f, 0.02f), V(-0.14f, -0.36f), V(0.52f, 0.4f)) - w * 1.3f,
                "plus" => p => Mathf.Min(Seg(p, V(-0.5f, 0), V(0.5f, 0)), Seg(p, V(0, -0.5f), V(0, 0.5f))) - w * 1.2f,
                "minus" => p => Seg(p, V(-0.5f, 0), V(0.5f, 0)) - w * 1.2f,
                // the baits (June's shelf, the pouch): a curled shrimp, a slim sardine, a squid, Walt's jar of mix
                "shrimp" => p => Mathf.Min(Mathf.Min(Arc(p, V(-0.02f, 0.02f), 0.4f, 20, 250) - w * 1.6f, Disc(p, V(0.34f, 0.18f), 0.11f)),
                    Mathf.Min(Mathf.Min(Seg(p, V(0.4f, 0.26f), V(0.72f, 0.6f)), Seg(p, V(0.44f, 0.2f), V(0.76f, 0.34f))) - w * 0.6f,
                    Mathf.Min(Poly(p, V(-0.16f, -0.36f), V(0.16f, -0.18f), V(0.12f, -0.56f), V(-0.16f, -0.36f)) - w, Mathf.Min(Seg(p, V(-0.22f, 0.12f), V(-0.06f, 0.02f)), Seg(p, V(-0.22f, -0.1f), V(-0.04f, -0.06f))) - w * 0.7f))),
                "sardine" => p => Mathf.Min(Poly(p, V(-0.72f, 0), V(-0.4f, 0.2f), V(0.1f, 0.2f), V(0.44f, 0), V(0.1f, -0.2f), V(-0.4f, -0.2f), V(-0.72f, 0)) - w,
                    Mathf.Min(Poly(p, V(0.44f, 0), V(0.74f, 0.26f), V(0.74f, -0.26f), V(0.44f, 0)) - w, Disc(p, V(-0.46f, 0.04f), 0.06f))),
                "squid" => p => Mathf.Min(Poly(p, V(0, 0.72f), V(-0.3f, 0.02f), V(0.3f, 0.02f), V(0, 0.72f)) - w, Mathf.Min(Poly(p, V(-0.2f, 0.02f), V(-0.3f, -0.3f), V(-0.2f, -0.62f)) - w,
                    Mathf.Min(Poly(p, V(0, 0.02f), V(0.04f, -0.34f), V(-0.02f, -0.68f)) - w, Poly(p, V(0.2f, 0.02f), V(0.32f, -0.3f), V(0.22f, -0.62f)) - w))),
                "mix" => p => Mathf.Min(Mathf.Abs(RBox(p, V(0, -0.14f), V(0.42f, 0.46f), 0.14f)) - w, Mathf.Min(Seg(p, V(-0.5f, 0.46f), V(0.5f, 0.46f)) - w * 1.4f, Disc(p, V(0, -0.14f), 0.14f))),
                "worm" => p => Poly(p, V(-0.5f, -0.5f), V(-0.2f, -0.2f), V(-0.4f, 0.15f), V(-0.05f, 0.45f), V(0.3f, 0.2f), V(0.5f, 0.5f)) - w * 1.5f,
                "rod" => p => Mathf.Min(Seg(p, V(-0.6f, -0.6f), V(0.55f, 0.62f)) - w, Mathf.Min(Ring(p, V(-0.25f, -0.38f), 0.14f) - w, Poly(p, V(0.55f, 0.62f), V(0.6f, -0.1f)) - w * 0.6f)),
                "pouch" => p => Mathf.Min(Mathf.Abs(RBox(p, V(0, -0.1f), V(0.56f, 0.46f), 0.2f)) - w, Mathf.Min(Seg(p, V(-0.56f, 0.12f), V(0.56f, 0.12f)) - w, Disc(p, V(0, 0.12f), 0.1f))),
                _ => p => Ring(p, Vector2.zero, 0.5f) - w,
            };
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "glyph " + name };
            var px = new Color[S * S]; float aa = 2.2f / S;
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    var p = new Vector2((x + 0.5f) / S * 2 - 1, (y + 0.5f) / S * 2 - 1);
                    px[y * S + x] = new Color(1, 1, 1, Mathf.Clamp01(0.5f - f(p) / aa));
                }
            t.SetPixels(px); t.Apply(true);
            return cache[name] = Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100);
        }

        static float Ellipse(Vector2 p, Vector2 c, Vector2 r) { var q = (p - c); q.x /= r.x; q.y /= r.y; return (q.magnitude - 1) * Mathf.Min(r.x, r.y); }
        // the upper half of a ring (a shoulder line): only above the centre (+ lift)
        static float ArcUp(Vector2 p, Vector2 c, float r, float lift = 0) { if (p.y < c.y + lift) { var a = c + new Vector2(-Mathf.Sqrt(Mathf.Max(0, r * r - lift * lift)), lift); var b = c + new Vector2(Mathf.Sqrt(Mathf.Max(0, r * r - lift * lift)), lift); return Mathf.Min((p - a).magnitude, (p - b).magnitude); } return Ring(p, c, r); }
        static float ArcDown(Vector2 p, Vector2 c, float r, float lift) { if (p.y > c.y + lift) { var a = c + new Vector2(-r, lift); var b = c + new Vector2(r, lift); return Mathf.Min(Seg(p, c + new Vector2(-r, 0), a), Seg(p, c + new Vector2(r, 0), b)); } return Ring(p, c + Vector2.zero, r); }
        static float Sun(Vector2 p, Vector2 c, float r, float w)
        {
            float d = Ring(p, c, r) - w;
            for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); d = Mathf.Min(d, Seg(p, c + dir * (r + 0.13f), c + dir * (r + 0.27f)) - w * 0.85f); }
            return d;
        }
        // a cloud outline: three bumps on a flat base (cutout: the sun behind it is hidden inside)
        static float Cloud(Vector2 p, Vector2 c, float w, bool cut)
        {
            float body = Mathf.Min(Mathf.Min(Disc(p, c + V(-0.24f, -0.02f), 0.24f), Disc(p, c + V(0.08f, 0.1f), 0.32f)), Mathf.Min(Disc(p, c + V(0.36f, -0.04f), 0.22f), RBox(p, c + V(0.06f, -0.12f), V(0.5f, 0.14f), 0.12f)));
            return Mathf.Abs(body) - w;
        }
        static float WindCurl(Vector2 p, Vector2 c, float w) { var d = p - (c + V(0, -0.08f)); return d.y < 0 && d.x < 0 ? 1 : Ring(p, c + V(0, -0.08f), 0.16f) - w; }
        static float Gear(Vector2 p, float w)
        {
            float d = Mathf.Min(Ring(p, Vector2.zero, 0.34f) - w, Ring(p, Vector2.zero, 0.12f) - w);
            for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4 + Mathf.PI / 8; var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); d = Mathf.Min(d, Seg(p, dir * 0.42f, dir * 0.6f) - w * 1.35f); }
            return d;
        }
    }
}
