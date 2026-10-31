using UnityEngine;

namespace PortBoss.M1
{
    // The open sea. The surface is moved by 3 Gerstner waves in the water shader (PB_Water); Height() runs the same
    // waves on the CPU (same numbers, same clock, same distance fade) so the boat, the swimmer and the bobber float on
    // exactly the water you see. The mesh is static: a grid whose spacing is 0.5 m near the middle and grows
    // geometrically to the horizon (no T-junctions, so no cracks), re-centred on the camera in whole cells.
    [DefaultExecutionOrder(-200)]
    public class Sea : MonoBehaviour
    {
        // Ponyo Coast, Clear weather (gdd systems §1.5: roll ≤ 6° in Clear). Tunable.
        public static float Amp = 0.16f, Len = 11f, Steep = 0.35f;
        public const float Level = 0f;
        static readonly Vector2 D1 = new Vector2(1, 0.6f).normalized, D2 = new Vector2(-0.4f, 1).normalized, D3 = new Vector2(0.9f, -0.7f).normalized;
        static Vector3 cam;
        public static float T;                                   // the wave clock (shader _LDTime)
        public static Material Mat;

        const float Fine = 0.5f;

        public static Sea Create(Material mat)
        {
            var g = new GameObject("Sea", typeof(MeshFilter), typeof(MeshRenderer));
            var s = g.AddComponent<Sea>();
            Mat = mat;
            var mr = g.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            g.GetComponent<MeshFilter>().sharedMesh = BuildMesh();
            return s;
        }

        static Mesh BuildMesh()
        {
            var xs = new System.Collections.Generic.List<float>();
            float x = 0, step = Fine;
            xs.Add(0);
            while (x < 4000) { x += step; xs.Add(x); if (x > 48) step *= 1.09f; }
            var axis = new float[xs.Count * 2 - 1];
            for (int i = 0; i < xs.Count; i++) { axis[xs.Count - 1 + i] = xs[i]; axis[xs.Count - 1 - i] = -xs[i]; }
            int n = axis.Length;
            var v = new Vector3[n * n];
            for (int j = 0; j < n; j++) for (int i = 0; i < n; i++) v[j * n + i] = new Vector3(axis[i], 0, axis[j]);
            var t = new int[(n - 1) * (n - 1) * 6];
            int k = 0;
            for (int j = 0; j < n - 1; j++)
                for (int i = 0; i < n - 1; i++)
                {
                    int a = j * n + i;
                    t[k++] = a; t[k++] = a + n; t[k++] = a + 1;
                    t[k++] = a + 1; t[k++] = a + n; t[k++] = a + n + 1;
                }
            var m = new Mesh { name = "SeaGrid", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.vertices = v; m.triangles = t;
            m.bounds = new Bounds(Vector3.zero, new Vector3(8000, 20, 8000));
            return m;
        }

        void LateUpdate()
        {
            var c = Camera.main;
            if (c != null)
            {
                cam = c.transform.position;
                transform.position = new Vector3(Mathf.Round(cam.x / Fine) * Fine, Level, Mathf.Round(cam.z / Fine) * Fine);
            }
        }

        void Update()
        {
            T = Time.time;
            var c = Camera.main;
            if (c != null) cam = c.transform.position;
            Shader.SetGlobalFloat("_LDTime", T);
            Shader.SetGlobalVector("_PBSeaCam", cam);
            if (Mat != null) { Mat.SetFloat("_WaveAmp", Amp); Mat.SetFloat("_WaveLen", Len); Mat.SetFloat("_WaveSteep", Steep); }
        }

        static float Fade(float x, float z) => Mathf.Clamp01(1 - (Mathf.Sqrt((x - cam.x) * (x - cam.x) + (z - cam.z) * (z - cam.z)) - 300) / 600);

        static void G(Vector2 dir, float len, float amp, float steep, float px, float pz, float t, ref Vector3 off)
        {
            float k = 2 * Mathf.PI / len, c = Mathf.Sqrt(9.8f / k), f = k * (dir.x * px + dir.y * pz - c * t);
            float q = steep / (k * amp * 3 + 1e-4f);
            float cs = Mathf.Cos(f);
            off.x += q * amp * dir.x * cs; off.y += amp * Mathf.Sin(f); off.z += q * amp * dir.y * cs;
        }

        // Displacement of the surface point that rests at (x, z).
        public static Vector3 Offset(float x, float z, float t)
        {
            var o = Vector3.zero;
            float a = Amp * Fade(x, z);
            G(D1, Len, a, Steep, x, z, t, ref o);
            G(D2, Len * 0.61f, a * 0.55f, Steep, x, z, t, ref o);
            G(D3, Len * 0.37f, a * 0.3f, Steep, x, z, t, ref o);
            return o;
        }

        // Height of the water at the fixed point (x, z): Gerstner moves points sideways, so find the resting point that
        // lands here (3 fixed-point steps).
        public static float Height(float x, float z)
        {
            float px = x, pz = z;
            Vector3 o = default;
            for (int i = 0; i < 3; i++) { o = Offset(px, pz, T); px = x - o.x; pz = z - o.z; }
            return Level + Offset(px, pz, T).y;
        }
        public static float Height(Vector3 p) => Height(p.x, p.z);

        public static Vector3 Normal(float x, float z, float e = 0.4f)
        {
            float hx = Height(x + e, z) - Height(x - e, z), hz = Height(x, z + e) - Height(x, z - e);
            return new Vector3(-hx, 2 * e, -hz).normalized;
        }
    }
}
