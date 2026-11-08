using UnityEngine;

namespace PortBoss.M1
{
    // M8 step 5: the surf (systems §1.7 "separate surf waves ... sets of 3-5 every 20-40 s; height from today's swell"),
    // built INTO the sea: the water shader (PB_Water.shader, SurfOne) lifts its own surface by these waves, so they have
    // the sea's colour, depth tint, reflections and crest foam, and no seams; Sea.Height adds the same bump on the CPU,
    // so boats, swimmers, bobbers and (step 6) a board ride exactly what you see.
    // Two spots (map B, sampled with the seabed on 8 Nov):
    // - Breaker Point: the south horn's outer beach, a straight beach break. The shore runs east-west at z = -1160; the
    //   floor shelves south (1 m at -1175, 3 m at -1200, 9 m at -1350). Waves roll north and break on the 2.5 m line.
    // - the Widowmaker at Gullrock: a reef ledge 130 m off the islet's east shore (11 m deep, then 5 m); waves roll west
    //   and stand up on the ledge. Only the day after a storm, and big.
    // A wave: born offshore, rolls in at 5 m/s, grows as it shoals; reaching the break line it breaks at its peak and
    // peels: 5 m/s one way, 2 m/s the other (it alternates lefts and rights). The broken stretch is white water, lower
    // than the green face, and runs on to the sand; near the curl the face steepens and wears a white lip. Sets: 3-5 waves
    // 11 s apart, a lull of 15-37 s, all from the sea clock (Sea.T: the host's in co-op), so every machine has the same
    // waves. Height: Kai's report for the block (SurfShop.Report: face feet), 0.8-1.2x per wave.
    // Shader globals (4 waves at most, the nearest live spot only): _PBSurfA (origin x, z, the way in x, z),
    // _PBSurfB (crest s, height, the peak u, broken to the left), _PBSurfC (broken to the right, half-width, the shore s,
    // the white water's trail), _PBSurfN.
    public class SurfWaves : MonoBehaviour
    {
        public static SurfWaves I;
        public class Spot { public string Id, Name; public Vector2 O, Dir; public float Half, Shore, Born; }
        public static readonly Spot Breaker = new() { Id = "breaker", Name = "Breaker Point", O = new(-240, -1192), Dir = new(0, 1), Half = 70, Shore = 32, Born = -110 };
        public static readonly Spot Widowmaker = new() { Id = "widowmaker", Name = "the Widowmaker", O = new(-335, 5), Dir = new(-1, 0), Half = 55, Shore = 120, Born = -100 };
        public const float Speed = 5f, PeelFast = 5f, PeelSlow = 2f, Spacing = 11f, SetGrid = 70f, Trail = 20f, Near = 700f;
        public const int Max = 4;

        public struct Wave { public float S, H, U0, BL, BR; public bool Broke; public int Id; }
        public static Spot Live;                                  // the spot whose waves are up (null: none near)
        public static readonly Wave[] W = new Wave[Max];
        public static int N;
        public static float TestHeight = -1;                     // (tests: the face height in m, overrides the report)
        public static System.Action<string> OnLog;
        static readonly Vector4[] a = new Vector4[Max], b = new Vector4[Max], c = new Vector4[Max];

        public static void Create() { var g = new GameObject("SurfWaves"); I = g.AddComponent<SurfWaves>(); }

        static float Hash(int n, int k) { uint h = (uint)(n * 73856093) ^ (uint)(k * 19349663) ^ 0x9E3779B9u; h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15; return (h & 0xFFFFFF) / (float)0x1000000; }

        // the face height (m) today at a spot: Kai's report for Breaker Point; the Widowmaker the day after a storm only
        public static float FaceHeight(Spot sp, double m)
        {
            if (TestHeight >= 0) return sp == Widowmaker ? TestHeight * 1.8f : TestHeight;
            if (Sky.W == null) return 0;
            var (d, blk) = Weather.BlockAt(m); blk = Mathf.Min(blk, 2);
            if (Firm.C != null && Firm.C.Seen("test:surf:" + d)) return sp == Widowmaker ? 3.2f : 1.5f;          // (the tester's Ctrl+F11)
            if (Sky.W.Block(d, blk) == Wx.Storm) return 0;                                   // closed out: nobody's out
            float ft = SurfShop.Report(d, blk).ft, h = ft / 3.28f;
            if (sp == Breaker) return h;
            bool after = false; for (int k = 0; k < 3; k++) if (Sky.W.Block(d - 1, k) == Wx.Storm) after = true;
            return after ? Mathf.Max(h * 1.8f, 3.5f) : 0;
        }

        void Update()
        {
            N = 0; Live = null;
            var cam = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            double m = Sky.Minutes; float t = Sea.T;
            foreach (var sp in new[] { Breaker, Widowmaker })
            {
                if ((new Vector2(cam.x, cam.z) - sp.O).magnitude > Near) continue;
                float H0 = FaceHeight(sp, m); if (H0 < 0.25f) continue;
                Live = sp; Waves(sp, H0, t); break;
            }
            for (int i = 0; i < Max; i++)
            {
                if (i < N) { var w = W[i]; a[i] = new Vector4(Live.O.x, Live.O.y, Live.Dir.x, Live.Dir.y); b[i] = new Vector4(w.S, w.H, w.U0, w.BL); c[i] = new Vector4(w.BR, Live.Half, Live.Shore, Trail); }
                else { a[i] = Vector4.zero; b[i] = new Vector4(-9999, 0, 0, 0); c[i] = Vector4.zero; }
            }
            Shader.SetGlobalVectorArray("_PBSurfA", a); Shader.SetGlobalVectorArray("_PBSurfB", b); Shader.SetGlobalVectorArray("_PBSurfC", c);
            Shader.SetGlobalFloat("_PBSurfN", N);
            Spray(Time.deltaTime);
        }

        // the waves alive at t: sets on a 70 s grid, 3-5 waves 11 s apart each
        static void Waves(Spot sp, float H0, float t)
        {
            float life = (sp.Shore + 6 - sp.Born) / Speed;
            int n1 = Mathf.FloorToInt(t / SetGrid);
            for (int n = n1 - 2; n <= n1 && N < Max; n++)
            {
                float t0 = n * SetGrid + Hash(n, 99) * 8; int count = 3 + (int)(Hash(n, 7) * 3);
                for (int k = 0; k < count && N < Max; k++)
                {
                    float tb = t0 + k * Spacing, age = t - tb; if (age < 0 || age > life) continue;
                    float s = sp.Born + Speed * age, hk = H0 * (0.8f + 0.4f * Hash(n, k));
                    float h = s < 0 ? hk * (0.5f + 0.5f * Smooth(sp.Born, 0, s)) : hk * (1 - 0.65f * Smooth(0, sp.Shore, s));
                    float u0 = (Hash(n, k + 31) - 0.5f) * 50f; bool right = ((n + k) & 1) == 0;
                    float since = s < 0 ? 0 : s / Speed;                                         // s since the break line, in s
                    float fast = Mathf.Min(sp.Half * 2, PeelFast * since), slow = Mathf.Min(sp.Half * 2, PeelSlow * since);
                    W[N++] = new Wave { S = s, H = h, U0 = u0, BL = s < 0 ? 0 : right ? slow : fast, BR = s < 0 ? 0 : right ? fast : slow, Broke = s >= 0, Id = n * 10 + k };
                }
            }
        }
        static float Smooth(float e0, float e1, float x) { float u = Mathf.Clamp01((x - e0) / (e1 - e0)); return u * u * (3 - 2 * u); }

        // one wave's lift at p and its white water (the shader's SurfOne, line for line)
        public static float One(int k, float px, float pz, out float wf)
        {
            wf = 0; var w = W[k]; var sp = Live; if (w.H <= 0.001f) return 0;
            float qx = px - sp.O.x, qz = pz - sp.O.y, s = qx * sp.Dir.x + qz * sp.Dir.y, u = qx * sp.Dir.y - qz * sp.Dir.x;
            float x = s - w.S, H = w.H;
            float side = 1 - Smooth(sp.Half - 12, sp.Half, Mathf.Abs(u));
            float span = w.BL + w.BR, bs = span > 0.001f ? Mathf.Clamp01(Mathf.Min(u - (w.U0 - w.BL), (w.U0 + w.BR) - u) / 3f + 0.5f) : 0;
            float dc = span > 0.001f ? Mathf.Min(Mathf.Abs(u - (w.U0 + w.BR)), Mathf.Abs(u - (w.U0 - w.BL))) : Mathf.Abs(u - w.U0);
            float curl = (1 - bs) * (1 - Smooth(0, 9, dc)) * (w.S > -6 ? 1 : 0);
            float wface = Mathf.Lerp(Mathf.Lerp(0.7f * H + 0.3f, 0.38f * H + 0.25f, curl), 0.6f * H + 0.6f, bs), wback = 3.5f * H + 2;
            float r = x > 0 ? x / wface : x / wback, prof = Mathf.Exp(-r * r);
            float shore = 1 - Smooth(sp.Shore - 8, sp.Shore, s);
            float trail = x < 2.5f && x > -Trail - 1 ? Mathf.Clamp01(1 - (-x - 1) / Trail) : 0;
            wf = Mathf.Max(bs * trail, curl * Mathf.Exp(-(x / 1.4f) * (x / 1.4f)) * 0.95f) * side * shore;
            return H * Mathf.Lerp(1, 0.55f, bs) * prof * side * shore;
        }
        // the surf's lift at (x, z) (Sea.Height adds it), and its white water
        public static float Bump(float x, float z) => Bump(x, z, out _);
        public static float Bump(float x, float z, out float foam)
        {
            foam = 0; if (N == 0 || Live == null) return 0;
            if ((new Vector2(x, z) - Live.O).sqrMagnitude > (Live.Half + 200) * (Live.Half + 200)) return 0;
            float h = 0; for (int k = 0; k < N; k++) { h += One(k, x, z, out float f); foam = Mathf.Max(foam, f); }
            return h;
        }
        // step 6's helpers: a wave by its id; a world point in the live spot's frame (u along the crest, s in from the
        // origin); the shape of wave k at (u, x from its crest): how broken, how near a curl, how wide the face
        public static int Find(int id) { for (int k = 0; k < N; k++) if (W[k].Id == id) return k; return -1; }
        public static void Frame(Vector3 p, out float u, out float s)
        {
            var sp = Live; float qx = p.x - sp.O.x, qz = p.z - sp.O.y;
            s = qx * sp.Dir.x + qz * sp.Dir.y; u = qx * sp.Dir.y - qz * sp.Dir.x;
        }
        public static void Local(int k, float u, float x, out float bs, out float curl, out float wface)
        {
            var w = W[k]; float H = w.H, span = w.BL + w.BR;
            bs = span > 0.001f ? Mathf.Clamp01(Mathf.Min(u - (w.U0 - w.BL), (w.U0 + w.BR) - u) / 3f + 0.5f) : 0;
            float dc = span > 0.001f ? Mathf.Min(Mathf.Abs(u - (w.U0 + w.BR)), Mathf.Abs(u - (w.U0 - w.BL))) : Mathf.Abs(u - w.U0);
            curl = (1 - bs) * (1 - Smooth(0, 9, dc)) * (w.S > -6 ? 1 : 0);
            wface = Mathf.Lerp(Mathf.Lerp(0.7f * H + 0.3f, 0.38f * H + 0.25f, curl), 0.6f * H + 0.6f, bs);
        }
        // where the curl is on wave k (world), and whether it's breaking yet (the board's pocket: step 6)
        public static Vector3 Curl(int k, bool rightSide)
        {
            var w = W[k]; var sp = Live; float u = rightSide ? w.U0 + w.BR : w.U0 - w.BL;
            var p = sp.O + sp.Dir * w.S + new Vector2(sp.Dir.y, -sp.Dir.x) * u;
            return new Vector3(p.x, Sea.Level + w.H * 0.6f, p.y);
        }

        // spray off the curls, and a thump when a wave first breaks
        float sprayT; readonly System.Collections.Generic.HashSet<int> broke = new();
        void Spray(float dt)
        {
            if (Live == null || N == 0) return;
            sprayT -= dt; bool puff = sprayT <= 0; if (puff) sprayT = 0.12f;
            for (int k = 0; k < N; k++)
            {
                var w = W[k]; if (!w.Broke || w.S > Live.Shore - 8) continue;
                if (broke.Add(w.Id)) { var pk = Live.O + Live.Dir * w.S + new Vector2(Live.Dir.y, -Live.Dir.x) * w.U0; Snd.Play(Synth.Splash, new Vector3(pk.x, Sea.Level + 0.5f, pk.y), Mathf.Clamp01(0.35f + w.H * 0.25f), 0.7f); OnLog?.Invoke($"wave {w.Id} breaks at {w.H:F1} m"); }
                if (!puff) continue;
                foreach (var right in new[] { true, false })
                {
                    var cp = Curl(k, right); float lip = w.H * 0.9f;
                    Fx.Splash(new Vector3(cp.x, Sea.Height(cp) + lip * 0.5f, cp.z), Mathf.Clamp(0.25f + w.H * 0.18f, 0.25f, 0.9f), 3);
                }
            }
            if (broke.Count > 64) broke.Clear();
        }
    }
}
