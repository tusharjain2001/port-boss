using Unity.Netcode;
using UnityEngine;

namespace PortBoss
{
    // Sine-sum sea. Height() is shared by the mesh, boat bob and crate buoyancy so they agree.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class Water : MonoBehaviour
    {
        public const float Level = -1.2f;
        public static float Storm = 1f; // wave scale knob

        public static float T
        {
            get
            {
                var nm = NetworkManager.Singleton;
                return nm != null && nm.IsListening ? nm.ServerTime.TimeAsFloat : Time.time;
            }
        }

        public static float Height(float x, float z, float t)
        {
            float h = 0.16f * Mathf.Sin(x * 0.21f + t * 1.1f)
                    + 0.11f * Mathf.Sin(z * 0.17f - t * 0.85f + x * 0.05f)
                    + 0.05f * Mathf.Sin((x + z) * 0.47f + t * 1.9f);
            return Level + h * Storm;
        }

        public static float Height(Vector3 p) => Height(p.x, p.z, T);

        Mesh mesh;
        Vector3[] verts;
        int res;
        float size;

        public static Water Create(Vector3 center, float size, int res)
        {
            var g = new GameObject("Water");
            g.transform.position = new Vector3(center.x, 0, center.z);
            var w = g.AddComponent<Water>();
            w.size = size; w.res = res;
            w.Build();
            g.GetComponent<MeshRenderer>().sharedMaterial = Art.WaterMat;
            g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return w;
        }

        void Build()
        {
            mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.MarkDynamic();
            int n = res + 1;
            verts = new Vector3[n * n];
            var tris = new int[res * res * 6];
            float step = size / res, half = size / 2;
            for (int z = 0, i = 0; z < n; z++)
                for (int x = 0; x < n; x++, i++)
                    verts[i] = new Vector3(x * step - half, Level, z * step - half);
            for (int z = 0, t = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                {
                    int i = z * n + x;
                    tris[t++] = i; tris[t++] = i + n; tris[t++] = i + 1;
                    tris[t++] = i + 1; tris[t++] = i + n; tris[t++] = i + n + 1;
                }
            mesh.vertices = verts;
            mesh.triangles = tris;
            // World-space UVs (one unit per 40 m) so a textured water shader tiles seamlessly at the pack demo's scale.
            var uv = new Vector2[verts.Length];
            for (int i = 0; i < verts.Length; i++) uv[i] = new Vector2(verts[i].x + transform.position.x, verts[i].z + transform.position.z) * 0.025f;
            mesh.uv = uv;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(size, 10, size));
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        void Update()
        {
            float t = T;
            var o = transform.position;
            for (int i = 0; i < verts.Length; i++)
                verts[i].y = Height(verts[i].x + o.x, verts[i].z + o.z, t);
            mesh.vertices = verts;
            mesh.RecalculateNormals();
        }
    }
}
