using System.Collections.Generic;
using UnityEngine;

namespace PortBoss
{
    // The far scenery, generated (same every run): a faceted toon landscape round the bay (a green headland with
    // beaches and woods wrapping round to Marlowe Port, countryside rising into snowy mountains behind the town,
    // hills behind Marlowe, a couple of islands out east), and cartoon clouds drifting round the sky.
    // The flat ground the town and the ports stand on stays where it is; the landscape starts at its edges.
    public static class Scenery
    {
        // Where the existing flat ground is: the strip along our coast, and Marlowe's port.
        public const float FlatX0 = -340f, FlatZ0 = -330f, NorthShore = 1100f;
        static readonly Rect Marlowe = new(-500, 600, 960, 500);
        const float Base = -0.1f, Under = -0.3f, Sea = -9f; // Under: where we meet the flat ground, just below it (no z-fighting)

        // The west shore of the bay: straight past our coast, then sweeping out to meet Marlowe's land.
        static float WestShore(float z) => z < 200 ? FlatX0 - 18f * Noise(z * 0.013f, 3.1f)
            : z < 600 ? Mathf.Lerp(FlatX0, Marlowe.xMin, Mathf.SmoothStep(0, 1, (z - 200) / 400f)) - 18f * Noise(z * 0.013f, 3.1f)
            : Marlowe.xMin;

        static float Noise(float x, float y) => Mathf.PerlinNoise(x + 31.7f, y + 12.3f);
        static float Fbm(float x, float z)
        {
            float a = 0, amp = 1, f = 1, sum = 0;
            for (int o = 0; o < 4; o++) { a += amp * Noise(x * f, z * f); sum += amp; amp *= 0.5f; f *= 2.03f; }
            return a / sum;
        }
        static float Ridged(float x, float z)
        {
            float a = 0, amp = 1, f = 1, sum = 0;
            for (int o = 0; o < 4; o++) { float r = 1 - Mathf.Abs(Noise(x * f, z * f) * 2 - 1); a += amp * r * r; sum += amp; amp *= 0.5f; f *= 2.1f; }
            return a / sum;
        }

        // How far inland a point is (negative = sea), and whether it's on the flat ground we leave alone.
        static float Inland(float x, float z, out bool flat)
        {
            flat = (x >= FlatX0 && z >= FlatZ0 && z <= 0) || Marlowe.Contains(new Vector2(x, z));
            if (flat) return 0;
            float d = -1;
            if (z < FlatZ0) d = Mathf.Max(d, FlatZ0 - z);
            if (x < WestShore(z)) d = Mathf.Max(d, WestShore(z) - x);
            if (z > NorthShore) d = Mathf.Max(d, z - NorthShore + (x < Marlowe.xMax ? 120 : 0));
            if (z < 0 && x < FlatX0) d = Mathf.Max(d, Mathf.Min(FlatX0 - x, -z) + 0.01f);
            // Islands out east.
            foreach (var (c, rad) in Islands) { float k = rad - Vector2.Distance(new Vector2(x, z), c); if (k > 0) d = Mathf.Max(d, k); }
            return d; // < 0: sea
        }
        static readonly (Vector2 c, float r)[] Islands = { (new Vector2(1050, 420), 170), (new Vector2(1250, 760), 120), (new Vector2(820, 980), 90) };

        public static float Height(float x, float z, out bool flat)
        {
            float d = Inland(x, z, out flat);
            if (flat) return Under;
            if (d < 0) return Sea;
            // Beach up to a few metres, rolling hills further in, mountains far inland.
            float hills = Mathf.SmoothStep(0, 1, d / 220f) * (18f + 70f * Fbm(x * 0.0035f, z * 0.0035f));
            float mount = Mathf.SmoothStep(0, 1, (d - 380f) / 420f) * 290f * Ridged(x * 0.0021f + 5f, z * 0.0021f + 9f);
            return Base + Mathf.Min(d, 14f) * 0.22f + hills + mount;
        }

        static readonly Color Sand = Art.Hex("#e0cc95"), Grass = Art.Hex("#86b45a"), Grass2 = Art.Hex("#6fa04c"), Forest = Art.Hex("#4f8a4a"),
            Rock = Art.Hex("#9a8d7c"), RockDark = Art.Hex("#7d7264"), Snow = Art.Hex("#f4f5f2"), Tree = Art.Hex("#3e7a45"), Tree2 = Art.Hex("#4c8c4e");

        static Color Paint(float h, float ny, float x, float z)
        {
            float n = Noise(x * 0.02f, z * 0.02f);
            if (h < 2.2f) return Sand;
            if (h > 170f + 40f * n && ny > 0.55f) return Snow;
            if (ny < 0.72f || h > 130f + 30f * n) return n > 0.5f ? Rock : RockDark;
            if (h > 55f) return Color.Lerp(Forest, Grass2, n);
            return Color.Lerp(Grass2, Grass, n);
        }

        public static void Build(Transform parent, Material mat)
        {
            if (mat == null) return;
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var cols = new List<Color>(); var tris = new List<int>();
            void Tri(Vector3 a, Vector3 b, Vector3 c, Color? tint = null)
            {
                var nn = Vector3.Cross(b - a, c - a).normalized;
                var col = tint ?? Paint((a.y + b.y + c.y) / 3f, nn.y, (a.x + b.x + c.x) / 3f, (a.z + b.z + c.z) / 3f);
                int i = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);
                norms.Add(nn); norms.Add(nn); norms.Add(nn);
                cols.Add(col); cols.Add(col); cols.Add(col);
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            }
            const float x0 = -1700, x1 = 1700, z0 = -1622, z1 = 1800, step = 34; // grid lines on the flat ground's edges (x -340, z -330)
            int nx = Mathf.CeilToInt((x1 - x0) / step), nz = Mathf.CeilToInt((z1 - z0) / step);
            var h = new float[nx + 1, nz + 1];
            var fl = new bool[nx + 1, nz + 1];
            var rng = new System.Random(19);
            for (int i = 0; i <= nx; i++)
                for (int j = 0; j <= nz; j++)
                    h[i, j] = Height(x0 + i * step, z0 + j * step, out fl[i, j]);
            Vector3 P(int i, int j)
            {
                // Jitter inland vertices a little so the facets don't look like a grid (never the flat-ground seams).
                float jx = fl[i, j] || h[i, j] <= Sea ? 0 : (Noise(i * 0.71f, j * 0.37f) - 0.5f) * step * 0.5f;
                float jz = fl[i, j] || h[i, j] <= Sea ? 0 : (Noise(i * 0.43f + 9, j * 0.83f) - 0.5f) * step * 0.5f;
                return new Vector3(x0 + i * step + jx, h[i, j], z0 + j * step + jz);
            }
            var treeSpots = new List<Vector3>();
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    bool allFlat = fl[i, j] && fl[i + 1, j] && fl[i, j + 1] && fl[i + 1, j + 1];
                    bool allSea = h[i, j] <= Sea && h[i + 1, j] <= Sea && h[i, j + 1] <= Sea && h[i + 1, j + 1] <= Sea;
                    if (allFlat || allSea) continue;
                    // A cell that touches the flat ground is clipped to it (no land poking up through the town).
                    if (fl[i, j] || fl[i + 1, j] || fl[i, j + 1] || fl[i + 1, j + 1])
                    {
                        var c = new Vector2(x0 + (i + 0.5f) * step, z0 + (j + 0.5f) * step);
                        Inland(c.x, c.y, out bool cf); if (cf) continue;
                    }
                    Vector3 a = P(i, j), b = P(i + 1, j), cc = P(i + 1, j + 1), d = P(i, j + 1);
                    if ((i + j) % 2 == 0) { Tri(a, d, cc); Tri(a, cc, b); } else { Tri(a, d, b); Tri(b, d, cc); }
                    // Woods on the gentle green slopes.
                    float hm = (a.y + b.y + cc.y + d.y) / 4f;
                    if (hm > 4f && hm < 120f && Mathf.Abs(a.y - cc.y) < step * 0.6f && Fbm((x0 + i * step) * 0.006f + 40, (z0 + j * step) * 0.006f) > 0.5f)
                        for (int k = 0; k < 3; k++)
                        {
                            float u = (float)rng.NextDouble(), w = (float)rng.NextDouble();
                            treeSpots.Add(Vector3.Lerp(Vector3.Lerp(a, b, u), Vector3.Lerp(d, cc, u), w));
                        }
                }
            // Trees: little faceted cones, two tiers.
            foreach (var p in treeSpots)
            {
                float s = 0.8f + (float)rng.NextDouble() * 0.7f, hh = 11f * s, r = 3.4f * s;
                var col = rng.Next(2) == 0 ? Tree : Tree2;
                for (int tier = 0; tier < 2; tier++)
                {
                    var bottom = p + Vector3.up * (1.5f + tier * hh * 0.42f);
                    var top = bottom + Vector3.up * hh * (tier == 0 ? 0.7f : 0.6f);
                    float rr = r * (tier == 0 ? 1f : 0.72f), a0 = (float)rng.NextDouble() * 6.28f;
                    for (int k = 0; k < 5; k++)
                    {
                        float aa = a0 + k * Mathf.PI * 2 / 5, ab = a0 + (k + 1) * Mathf.PI * 2 / 5;
                        Tri(bottom + new Vector3(Mathf.Cos(aa), 0, Mathf.Sin(aa)) * rr, top, bottom + new Vector3(Mathf.Cos(ab), 0, Mathf.Sin(ab)) * rr, col);
                    }
                }
            }
            // Split into meshes of < 65k vertices.
            const int chunk = 60000;
            for (int s = 0, part = 0; s < verts.Count; s += chunk, part++)
            {
                int n = Mathf.Min(chunk, verts.Count - s);
                var m = new Mesh { name = "land" + part };
                m.SetVertices(verts.GetRange(s, n)); m.SetNormals(norms.GetRange(s, n)); m.SetColors(cols.GetRange(s, n));
                var idx = new int[n]; for (int k = 0; k < n; k++) idx[k] = k;
                m.SetTriangles(idx, 0); m.RecalculateBounds();
                var g = new GameObject("land" + part);
                g.transform.SetParent(parent, false);
                g.AddComponent<MeshFilter>().sharedMesh = m;
                var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            }
            Debug.Log($"[PB] scenery: {verts.Count / 3} land triangles, {treeSpots.Count} trees");
        }

        // Cartoon clouds: flat-bottomed heaps of puffs (a wide base row, a smaller crown), drifting slowly round the sky.
        public static void Clouds(Transform parent, Material mat)
        {
            if (mat == null) return;
            var root = new GameObject("Clouds").transform;
            root.SetParent(parent, false);
            root.position = new Vector3(0, 0, 150);
            root.gameObject.AddComponent<Drift>();
            var rng = new System.Random(5);
            float R() => (float)rng.NextDouble();
            for (int i = 0; i < 30; i++)
            {
                float ang = R() * Mathf.PI * 2, dist = 300 + R() * 420, size = 0.7f + R() * 0.8f;
                var c = new GameObject("cloud").transform;
                c.SetParent(root, false);
                c.localPosition = new Vector3(Mathf.Cos(ang) * dist, 95 + R() * 90, Mathf.Sin(ang) * dist);
                c.localRotation = Quaternion.Euler(0, R() * 360, 0);
                c.localScale = Vector3.one * size;
                int nb = 4 + rng.Next(4);
                float span = nb * 15f;
                for (int k = 0; k < nb; k++)
                {
                    float w = 22 + R() * 16, x = -span / 2 + (k + 0.5f) * span / nb + (R() - 0.5f) * 6;
                    Puff(c, new Vector3(x, w * 0.18f, (R() - 0.5f) * 14), new Vector3(w, w * 0.55f, w * 0.8f), mat);
                }
                int nt = 1 + rng.Next(3);
                for (int k = 0; k < nt; k++)
                {
                    float w = 22 + R() * 14;
                    Puff(c, new Vector3((R() - 0.5f) * span * 0.5f, 9 + R() * 7, (R() - 0.5f) * 8), new Vector3(w, w * 0.8f, w * 0.8f), mat);
                }
            }
        }

        static void Puff(Transform parent, Vector3 pos, Vector3 size, Material mat)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(p.GetComponent<Collider>());
            p.name = "puff";
            p.transform.SetParent(parent, false);
            p.transform.localPosition = pos; p.transform.localScale = size;
            var r = p.GetComponent<Renderer>();
            r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        }

        // The cloud field turns slowly round the harbour (with the server clock, so everyone sees the same sky).
        class Drift : MonoBehaviour
        {
            void Update() => transform.rotation = Quaternion.Euler(0, Water.T * 0.12f, 0);
        }
    }
}
