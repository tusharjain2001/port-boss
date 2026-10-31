using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    public static class Util
    {
        public static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root) { var f = Find(c, name); if (f != null) return f; }
            return null;
        }

        public static List<Transform> FindAll(Transform root, System.Func<string, bool> pred, List<Transform> into = null)
        {
            into ??= new List<Transform>();
            if (pred(root.name)) into.Add(root);
            foreach (Transform c in root) FindAll(c, pred, into);
            return into;
        }

        // Blender collision children (gdd systems §4.8.2): *_colBox / *_colDeck -> BoxCollider from the mesh bounds,
        // *_col -> MeshCollider. Their meshes never render.
        public static void Colliders(GameObject root)
        {
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                string n = mf.name;
                bool box = n.EndsWith("_colBox") || n.EndsWith("_colDeck"), mesh = !box && (n.EndsWith("_col") || n.Contains("_col."));
                if (!box && !mesh) continue;
                if (mf.TryGetComponent<MeshRenderer>(out var r)) Object.Destroy(r);
                if (box) { var b = mf.gameObject.AddComponent<BoxCollider>(); var bb = mf.sharedMesh.bounds; b.center = bb.center; b.size = Vector3.Max(bb.size, Vector3.one * 0.02f); }
                else mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            }
        }

        // Every visible mesh gets the toon material (the FBX colours live in the vertex colours).
        public static void Toon(GameObject root, Material m)
        {
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
            }
        }

        public static Mesh Cylinder(int sides, float r0, float r1, float len, Color c0, Color c1)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var col = new List<Color>(); var t = new List<int>();
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                v.Add(d * r0); n.Add(d); col.Add(c0);
                v.Add(d * r1 + Vector3.forward * len); n.Add(d); col.Add(c1);
            }
            for (int i = 0; i < sides; i++) { int a = i * 2; t.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3 }); }
            int cap = v.Count; v.Add(Vector3.forward * len); n.Add(Vector3.forward); col.Add(c1);
            for (int i = 0; i < sides; i++) { int a = i * 2 + 1; t.AddRange(new[] { cap, a, a + 2 }); }
            var m = new Mesh(); m.SetVertices(v); m.SetNormals(n); m.SetColors(col); m.SetTriangles(t, 0); m.RecalculateBounds();
            return m;
        }

        public static Mesh Sphere(int seg, float r, System.Func<Vector3, Color> paint)
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            for (int y = 0; y <= seg; y++)
                for (int x = 0; x <= seg * 2; x++)
                {
                    float th = y * Mathf.PI / seg, ph = x * Mathf.PI / seg;
                    var p = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    v.Add(p * r); c.Add(paint(p));
                }
            int w = seg * 2 + 1;
            for (int y = 0; y < seg; y++) for (int x = 0; x < seg * 2; x++) { int a = y * w + x; t.AddRange(new[] { a, a + 1, a + w, a + 1, a + w + 1, a + w }); }
            var m = new Mesh(); m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        public static GameObject MeshObj(string name, Mesh m, Material mat, Transform parent)
        {
            var g = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            g.transform.SetParent(parent, false);
            g.GetComponent<MeshFilter>().sharedMesh = m; g.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return g;
        }

        // Soft round dot (particles) and a ring (ripples), generated once.
        static Texture2D dot, ring;
        public static Texture2D Dot { get { if (dot == null) dot = Radial(64, d => Mathf.SmoothStep(1, 0, Mathf.InverseLerp(0.4f, 1f, d))); return dot; } }
        public static Texture2D Ring { get { if (ring == null) ring = Radial(128, d => Mathf.Clamp01(1 - Mathf.Abs(d - 0.88f) / 0.035f) * 0.85f); return ring; } }
        static Texture2D Radial(int s, System.Func<float, float> a)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s / 2f, s / 2f)) / (s / 2f);
                    t.SetPixel(x, y, new Color(1, 1, 1, a(d)));
                }
            t.Apply(); return t;
        }

        static Material particle;
        public static Material ParticleMat
        {
            get
            {
                if (particle != null) return particle;
                particle = M1Assets.I.SoftParticle != null ? new Material(M1Assets.I.SoftParticle) : new Material(M1Assets.I.Particle);
                particle.SetFloat("_Surface", 1); particle.SetFloat("_Blend", 0);
                particle.SetOverrideTag("RenderType", "Transparent");
                particle.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); particle.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                particle.SetInt("_ZWrite", 0); particle.renderQueue = 3100;
                particle.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                particle.SetTexture("_BaseMap", Dot); particle.SetTexture("_MainTex", Dot); particle.SetColor("_BaseColor", Color.white);
                return particle;
            }
        }

        public static Material Soft(Color c, Texture t = null)
        {
            var m = new Material(ParticleMat); m.SetTexture("_BaseMap", t != null ? t : Dot); m.SetTexture("_MainTex", t != null ? t : Dot); m.SetColor("_BaseColor", c);
            return m;
        }

        public static float Damp(float a, float b, float rate, float dt) => Mathf.Lerp(a, b, 1 - Mathf.Exp(-rate * dt));
        public static Vector3 Damp(Vector3 a, Vector3 b, float rate, float dt) => Vector3.Lerp(a, b, 1 - Mathf.Exp(-rate * dt));
        public static float EaseOutBack(float t) { float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2); }
    }
}
