using System.Collections.Generic;
using UnityEngine;

namespace PortBoss
{
    // Chamfered box meshes (flat bevel on every edge and corner), built at their final size so thin planks
    // get the same bevel as big walls. Cached per size. Used by Art.Box when Art.Bevel is on.
    public static class Bevel
    {
        static readonly Dictionary<(Vector3, float), Mesh> cache = new();

        public static Mesh Box(Vector3 size, float r)
        {
            if (cache.TryGetValue((size, r), out var m)) return m;
            var h = size / 2;
            r = Mathf.Min(r, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.45f);
            var v = new List<Vector3>(); var tris = new List<int>();
            void Poly(params Vector3[] p)
            {
                // Fan; flip to face outward (away from the box centre).
                var n = Vector3.Cross(p[1] - p[0], p[2] - p[0]);
                var c = Vector3.zero; foreach (var q in p) c += q; c /= p.Length;
                bool flip = Vector3.Dot(n, c) < 0;
                int b = v.Count; v.AddRange(p);
                for (int i = 1; i < p.Length - 1; i++)
                {
                    tris.Add(b); tris.Add(b + (flip ? i + 1 : i)); tris.Add(b + (flip ? i : i + 1));
                }
            }
            // Corner point helper: s = signs per axis, inset on the axes listed in 'inset'.
            Vector3 P(Vector3 s, bool ix, bool iy, bool iz) => new(s.x * (h.x - (ix ? r : 0)), s.y * (h.y - (iy ? r : 0)), s.z * (h.z - (iz ? r : 0)));
            float[] S = { -1, 1 };
            // 6 faces (inset on the two in-plane axes).
            foreach (var s in S)
            {
                Poly(P(new(s, -1, -1), false, true, true), P(new(s, 1, -1), false, true, true), P(new(s, 1, 1), false, true, true), P(new(s, -1, 1), false, true, true));
                Poly(P(new(-1, s, -1), true, false, true), P(new(1, s, -1), true, false, true), P(new(1, s, 1), true, false, true), P(new(-1, s, 1), true, false, true));
                Poly(P(new(-1, -1, s), true, true, false), P(new(1, -1, s), true, true, false), P(new(1, 1, s), true, true, false), P(new(-1, 1, s), true, true, false));
            }
            // 12 edge chamfers + 8 corner triangles.
            foreach (var a in S) foreach (var b in S)
            {
                Poly(P(new(a, b, -1), false, true, true), P(new(a, b, -1), true, false, true), P(new(a, b, 1), true, false, true), P(new(a, b, 1), false, true, true)); // x-y edges along z
                Poly(P(new(a, -1, b), false, true, true), P(new(a, -1, b), true, true, false), P(new(a, 1, b), true, true, false), P(new(a, 1, b), false, true, true)); // x-z edges along y
                Poly(P(new(-1, a, b), true, false, true), P(new(-1, a, b), true, true, false), P(new(1, a, b), true, true, false), P(new(1, a, b), true, false, true)); // y-z edges along x
                foreach (var c in S) Poly(P(new(a, b, c), false, true, true), P(new(a, b, c), true, false, true), P(new(a, b, c), true, true, false));
            }
            m = new Mesh { name = "bevel_box" };
            m.SetVertices(v); m.SetTriangles(tris, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            cache[(size, r)] = m;
            return m;
        }
    }
}
