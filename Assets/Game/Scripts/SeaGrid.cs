using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    // M6 · the workers' sea grid (systems §1.6.8): 32 m cells over the seabed sheet; a cell is open when the sea is at
    // least 1.5 m deep at its centre and its four quarter points (land and shallows blocked), and no step may cross the
    // Voss no-dock buoy line. A* with 8 neighbours, then the path is pulled straight where the water allows.
    public static class SeaGrid
    {
        public const float Cell = 32, MinDepth = 1.5f;
        // the Voss buoy line (Unity x, z; map B: BotMap's route note), boats keep to the west of it
        public static readonly Vector2[] Voss = { new(1460, 500), new(1680, -50), new(1480, -650) };
        static bool[] open; static int nx, nz; static float x0, z0;
        public static bool Ready => open != null;

        public static void Build(float minX = -2400, float minZ = -2400, float maxX = 2400, float maxZ = 2400)
        {
            x0 = minX; z0 = minZ; nx = Mathf.CeilToInt((maxX - minX) / Cell); nz = Mathf.CeilToInt((maxZ - minZ) / Cell);
            open = new bool[nx * nz]; int n = 0;
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    var c = Centre(i, j); bool ok = Deep(c.x, c.y);
                    for (int k = 0; k < 4 && ok; k++) ok = Deep(c.x + (k % 2 == 0 ? -0.35f : 0.35f) * Cell, c.y + (k < 2 ? -0.35f : 0.35f) * Cell);
                    open[j * nx + i] = ok; if (ok) n++;
                }
            Debug.Log($"[PB] seagrid: {nx}x{nz} cells of {Cell} m, {n} open (seabed {(Seabed.On ? "on" : "off")})");
        }
        public static bool Deep(float x, float z) => !Seabed.On || Seabed.Depth(x, z) >= MinDepth;
        static Vector2 Centre(int i, int j) => new(x0 + (i + 0.5f) * Cell, z0 + (j + 0.5f) * Cell);
        static bool Open(int i, int j) => i >= 0 && j >= 0 && i < nx && j < nz && open[j * nx + i];
        static (int, int) CellOf(Vector2 p) => (Mathf.FloorToInt((p.x - x0) / Cell), Mathf.FloorToInt((p.y - z0) / Cell));

        // does a straight leg cross the Voss line?
        public static bool CrossesVoss(Vector2 a, Vector2 b)
        {
            for (int i = 1; i < Voss.Length; i++) if (Segs(a, b, Voss[i - 1], Voss[i])) return true;
            return false;
        }
        static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
        static bool Segs(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float d1 = Cross(c, d, a), d2 = Cross(c, d, b), d3 = Cross(a, b, c), d4 = Cross(a, b, d);
            return ((d1 > 0) != (d2 > 0)) && ((d3 > 0) != (d4 > 0));
        }
        // a straight leg in deep water the whole way (every 4 m)?
        public static bool Clear(Vector2 a, Vector2 b, float step = 4)
        {
            if (CrossesVoss(a, b)) return false;
            float len = Vector2.Distance(a, b); int n = Mathf.Max(1, Mathf.CeilToInt(len / step));
            for (int k = 1; k < n; k++) { var p = Vector2.Lerp(a, b, k / (float)n); if (!Deep(p.x, p.y)) return false; }
            return true;
        }
        static (int, int) Nearest(Vector2 p)
        {
            var (ci, cj) = CellOf(p);
            for (int r = 0; r < 40; r++)
            {
                float best = float.MaxValue; (int, int) got = (-1, -1);
                for (int j = cj - r; j <= cj + r; j++) for (int i = ci - r; i <= ci + r; i++)
                    {
                        if (Mathf.Max(Mathf.Abs(i - ci), Mathf.Abs(j - cj)) != r || !Open(i, j)) continue;
                        float d = (Centre(i, j) - p).sqrMagnitude; if (d < best) { best = d; got = (i, j); }
                    }
                if (got.Item1 >= 0) return got;
            }
            return (-1, -1);
        }

        // A* from a to b (x, z); the first and last legs go to the exact points (a berth, a spot)
        public static List<Vector2> Find(Vector2 a, Vector2 b)
        {
            if (open == null) Build();
            var res = new List<Vector2>();
            var s = Nearest(a); var g = Nearest(b);
            if (s.Item1 < 0 || g.Item1 < 0) { res.Add(a); res.Add(b); return res; }
            int S = s.Item2 * nx + s.Item1, G = g.Item2 * nx + g.Item1;
            var gScore = new Dictionary<int, float> { [S] = 0 }; var from = new Dictionary<int, int>();
            var heap = new SortedSet<(float f, int id)>(); heap.Add((H(S, G), S)); var closed = new HashSet<int>();
            while (heap.Count > 0)
            {
                var cur = heap.Min; heap.Remove(cur); int c = cur.id; if (c == G) break; if (!closed.Add(c)) continue;
                int ci = c % nx, cj = c / nx; var cc = Centre(ci, cj);
                for (int dj = -1; dj <= 1; dj++) for (int di = -1; di <= 1; di++)
                    {
                        if (di == 0 && dj == 0) continue; int ni = ci + di, nj = cj + dj; if (!Open(ni, nj)) continue;
                        if (di != 0 && dj != 0 && (!Open(ci + di, cj) || !Open(ci, cj + dj))) continue;   // no corner cutting past land
                        var nc = Centre(ni, nj); if (CrossesVoss(cc, nc)) continue;
                        int n = nj * nx + ni; float t = gScore[c] + (di != 0 && dj != 0 ? 1.4142f : 1f) * Cell;
                        if (gScore.TryGetValue(n, out var old) && old <= t) continue;
                        if (gScore.ContainsKey(n)) heap.Remove((old + H(n, G), n));
                        gScore[n] = t; from[n] = c; heap.Add((t + H(n, G), n));
                    }
            }
            if (!from.ContainsKey(G) && S != G) { res.Add(a); res.Add(b); Debug.Log("[PB] seagrid: no way through"); return res; }
            var cells = new List<Vector2>(); for (int c = G; ; c = from[c]) { cells.Add(Centre(c % nx, c / nx)); if (c == S) break; }
            cells.Reverse();
            // pull it straight: from each kept point, the furthest one still in clear water
            var all = new List<Vector2> { a }; all.AddRange(cells); all.Add(b);
            res.Add(a); int k = 0;
            while (k < all.Count - 1)
            {
                int far = k + 1; for (int m = all.Count - 1; m > k + 1; m--) if (Clear(all[k], all[m])) { far = m; break; }
                res.Add(all[far]); k = far;
            }
            return res;
        }
        static float H(int a, int b) => Vector2.Distance(Centre(a % nx, a / nx), Centre(b % nx, b / nx));
        public static float Length(List<Vector2> p) { float d = 0; for (int i = 1; i < p.Count; i++) d += Vector2.Distance(p[i - 1], p[i]); return d; }
        public static Vector2 Along(List<Vector2> p, float d, out Vector2 dir)
        {
            dir = Vector2.up;
            for (int i = 1; i < p.Count; i++)
            {
                float s = Vector2.Distance(p[i - 1], p[i]); dir = (p[i] - p[i - 1]).normalized;
                if (d <= s) return Vector2.Lerp(p[i - 1], p[i], s > 0 ? d / s : 1); d -= s;
            }
            return p[p.Count - 1];
        }
        // the test: the shallowest point along a path (sampled every 2 m, the end legs' first/last 12 m skipped: the berth)
        public static (float minDepth, bool voss) Check(List<Vector2> p)
        {
            float min = 999; bool v = false; float len = Length(p);
            for (float d = 12; d < len - 12; d += 2) { var q = Along(p, d, out _); if (Seabed.On) min = Mathf.Min(min, Seabed.Depth(q.x, q.y)); }
            for (int i = 1; i < p.Count; i++) v |= CrossesVoss(p[i - 1], p[i]);
            return (min, v);
        }
    }
}
