using UnityEngine;

namespace PortBoss
{
    // Stylised wooden dock crate (art plan §4): dark corner posts + top rim, gapped warm slats, rope handles,
    // a stencil label. Fish crates are open with ice and fish on top; empty ones have a lid.
    public static class CrateLook
    {
        public static readonly Color Slat = Art.Hex("#b88a5a"), Frame = Art.Hex("#5b4330"), Rope = Art.Hex("#c9b07a"),
            Ice = Art.Hex("#e6f0f2"), Belly = Art.Hex("#a9c3cf"), Back = Art.Hex("#55707f"), Label = Art.Hex("#e8dcc4");

        // Builds under 'root' (centred, size s). Returns the renderers that spoil (fish), for tinting.
        public static Renderer[] Build(Transform root, float s, bool fish, int seed)
        {
            var rng = new System.Random(seed);
            float J() => 1f + ((float)rng.NextDouble() - 0.5f) * 0.1f;
            float h = s / 2, post = 0.055f, th = 0.03f;
            // Corner posts (the dark frame).
            foreach (var x in new[] { -1, 1 })
                foreach (var z in new[] { -1, 1 })
                    Art.Box(root, new Vector3(x * (h - post / 2), 0, z * (h - post / 2)), new Vector3(post, s, post), Frame, false, "post_c");
            // Three slats per side with dark gaps between them.
            float slatH = (s - 0.08f) / 3f, gap = 0.04f;
            for (int i = 0; i < 3; i++)
            {
                float y = -h + 0.02f + slatH / 2 + i * (slatH + gap / 2);
                foreach (var sgn in new[] { -1, 1 })
                {
                    Art.Box(root, new Vector3(0, y, sgn * (h - th / 2)), new Vector3(s - post * 2, slatH - gap / 2, th), Slat * J(), false, "slat");
                    Art.Box(root, new Vector3(sgn * (h - th / 2), y, 0), new Vector3(th, slatH - gap / 2, s - post * 2), Slat * J(), false, "slat");
                }
            }
            // Inner box (shows dark through the gaps) and the top rim.
            Art.Box(root, Vector3.zero, new Vector3(s - th * 2.2f, s - 0.02f, s - th * 2.2f), Frame * 0.7f, false, "inner");
            foreach (var sgn in new[] { -1, 1 })
            {
                Art.Box(root, new Vector3(0, h - 0.02f, sgn * (h - 0.025f)), new Vector3(s, 0.04f, 0.05f), Frame, false, "rim");
                Art.Box(root, new Vector3(sgn * (h - 0.025f), h - 0.02f, 0), new Vector3(0.05f, 0.04f, s), Frame, false, "rim");
            }
            // Rope handles on the short ends (x sides), stencil label on the front (+z).
            foreach (var sgn in new[] { -1, 1 })
            {
                Art.Box(root, new Vector3(sgn * (h + 0.025f), h * 0.45f, 0), new Vector3(0.03f, 0.03f, s * 0.38f), Rope, false, "rope_h");
                foreach (var z in new[] { -1, 1 })
                    Art.Box(root, new Vector3(sgn * (h + 0.012f), h * 0.45f - 0.03f, z * s * 0.17f), new Vector3(0.03f, 0.07f, 0.03f), Rope, false, "rope_v");
            }
            Art.Box(root, new Vector3(0.06f, 0.02f, h + 0.004f), new Vector3(s * 0.38f, s * 0.2f, 0.006f), Label, false, "label");
            Art.Box(root, new Vector3(0.06f, 0.02f, h + 0.008f), new Vector3(s * 0.3f, s * 0.05f, 0.004f), Frame, false, "label_ink");

            if (!fish)
            {
                // Lid: three boards across the top.
                for (int i = -1; i <= 1; i++)
                    Art.Box(root, new Vector3(0, h + 0.015f, i * s * 0.32f), new Vector3(s, 0.03f, s * 0.3f), Slat * J(), false, "lid");
                return new Renderer[0];
            }
            // Ice bed with fish lying across it.
            var ice = Art.Box(root, new Vector3(0, h - 0.07f, 0), new Vector3(s - th * 2.4f, 0.06f, s - th * 2.4f), Ice, false, "ice", 0.6f).GetComponent<Renderer>();
            var list = new System.Collections.Generic.List<Renderer> { ice };
            int n = 4 + rng.Next(2);
            for (int i = 0; i < n; i++)
            {
                var f = new GameObject("fish").transform;
                f.SetParent(root, false);
                f.localPosition = new Vector3(-0.15f + i * 0.3f / (n - 1) + ((float)rng.NextDouble() - 0.5f) * 0.03f, h - 0.02f + (i % 2) * 0.015f, ((float)rng.NextDouble() - 0.5f) * 0.08f);
                f.localRotation = Quaternion.Euler(0, 80 + (float)rng.NextDouble() * 20 - 10, 90);
                list.Add(Art.Prim(PrimitiveType.Capsule, f, Vector3.zero, Quaternion.identity, new Vector3(0.05f, 0.14f, 0.08f), Belly, false, "fishy", 0.7f).GetComponent<Renderer>());
                list.Add(Art.Prim(PrimitiveType.Capsule, f, new Vector3(0.016f, 0, 0), Quaternion.identity, new Vector3(0.03f, 0.13f, 0.05f), Back, false, "fishy", 0.7f).GetComponent<Renderer>());
                list.Add(Art.Box(f, new Vector3(0, -0.15f, 0), new Vector3(0.012f, 0.05f, 0.08f), Back, false, "fishy").GetComponent<Renderer>());
            }
            return list.ToArray();
        }
    }
}
