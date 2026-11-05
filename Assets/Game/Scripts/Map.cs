using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace PortBoss.M1
{
    // Map B "Crescent Bay" (art-src/world/build_island.py -- export; art-src/world/IMPORT-PLAN.md): the island in 512 m cells
    // (terrain LOD0-2 re-sampled from one heightfield, with skirts; that cell's rocks, trees and town), the props
    // (lighthouse, wreck, causeway, Voss, the P1-P3 buoys, Mari's boat), the named anchors and the seabed.
    // ponytail: every cell is loaded at start (the whole map is ~400k tris at LOD0, under the 600k per-view budget);
    // add additive cell scenes + streaming (IMPORT-PLAN §2) when the map outgrows that.
    public static class Map
    {
        public static Transform Root;
        public static bool On => Root != null;
        public static readonly List<Renderer> Windows = new(); public static Material Glow;   // the town's lit windows (Sky turns them on with the lamps)
        public static Material Ground, Town; public static float GroundSat, GroundBri, TownSat, TownBri;   // the hill, trees and gardens: Sky mutes them at night (upward faces catch the sky light)

        [System.Serializable] public class Anchor { public string id, name; public float[] pos, fwd; public bool ground; }
        [System.Serializable] class Spot { public float[] pos; public float yaw; }
        [System.Serializable] public class SeabedInfo { public float x0, z0, size, hmin, hmax; public int px; }
        [System.Serializable] class Data { public float cell; public Spot landing; public SeabedInfo seabed; public Anchor[] anchors; }
        static Data data;

        public static void Create(M1Assets a, Material toon)
        {
            if (a.WorldJson == null || a.Cells == null || a.Cells.Length == 0) { Debug.LogError("[PB] map: no Crescent Bay assets (run build_island.py -- export, then M1Build)"); return; }
            data = JsonUtility.FromJson<Data>(a.WorldJson.text);
            Seabed.Init(a.Seabed, data.seabed);
            Root = new GameObject("CrescentBay").transform;
            // M1's FBX axes put Blender (x, y, z) at Unity (-x, z, -y): turned round, north (Blender +Y) is Unity +Z
            var geo = new GameObject("geometry").transform; geo.SetParent(Root, false); geo.localRotation = Quaternion.Euler(0, 180, 0);
            int tris = 0;
            foreach (var c in a.Cells) { if (c == null) continue; var g = Object.Instantiate(c, geo); g.name = c.name; tris += Cell(g); }
            if (a.WorldProps != null)
            {
                var p = Object.Instantiate(a.WorldProps, geo); p.name = "props";
                foreach (var mf in p.GetComponentsInChildren<MeshFilter>())
                    if (mf.name != "Expansion_markers") mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                var mari = Util.Find(p.transform, "MarisTacoBoat"); Places.MariBoat = mari != null ? mari.gameObject : null;
                Transform N = Util.Find(p.transform, "socket_north"), E = Util.Find(p.transform, "socket_east");
                if (N != null && E != null) Debug.Log($"[PB] map axes: socket_north at {N.position} (want 0,0,500), socket_east at {E.position} (want 500,0,0)");
            }
            Util.Toon(Root.gameObject, toon);
            Ground = new Material(toon) { name = "PB_Toon_Ground" }; GroundSat = Ground.GetFloat("_Saturation"); GroundBri = Ground.GetFloat("_Brightness");
            foreach (var r in Root.GetComponentsInChildren<MeshRenderer>())
                if (r.name.Contains("_Terrain") || r.name.EndsWith("_Trees") || r.name.EndsWith("_Garden")) Util.Toon(r.gameObject, Ground);
            // the houses (a cell's "_Town" mesh): day-bright at night beside the dimmed hill, so Sky dims them too (M3 quality)
            Town = new Material(toon) { name = "PB_Toon_Town" }; TownSat = Town.GetFloat("_Saturation"); TownBri = Town.GetFloat("_Brightness");
            foreach (var r in Root.GetComponentsInChildren<MeshRenderer>()) if (r.name.EndsWith("_Town")) Util.Toon(r.gameObject, Town);
            // the panes that light up at dusk (a cell's "_Windows" mesh): one shared glow, no shadows, no collider
            Windows.Clear(); Glow = Util.Soft(new Color(1f, 0.74f, 0.42f, 0f), Texture2D.whiteTexture);
            foreach (var r in Root.GetComponentsInChildren<MeshRenderer>())
                if (r.name.EndsWith("_Windows")) { r.sharedMaterial = Glow; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.enabled = false; Windows.Add(r); }
            Debug.Log($"[PB] map: Crescent Bay, {a.Cells.Length} cells, LOD0 {tris} tris");
        }

        // after Walt's Landing is in (the anchors on the pier and in the yard land on its deck and ground)
        public static void BuildPlaces() { if (!On) return; Physics.SyncTransforms(); Places.Build(data.anchors); Debug.Log($"[PB] map: {Places.All.Count} anchors"); }

        // A cell: the terrain's 3 LODs in one LODGroup (LOD2 never culled: the far coast must stay on the horizon), the
        // LOD0 mesh is its collider (a boat bumps the real shore); rocks and the town collide, trees don't.
        static int Cell(GameObject g)
        {
            var lods = new Renderer[3]; int tris = 0;
            foreach (var mf in g.GetComponentsInChildren<MeshFilter>())
            {
                string n = mf.name; var r = mf.GetComponent<MeshRenderer>();
                int k = n.EndsWith("_LOD0") ? 0 : n.EndsWith("_LOD1") ? 1 : n.EndsWith("_LOD2") ? 2 : -1;
                if (k >= 0) { lods[k] = r; if (k > 0) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
                if (k == 0 || n.EndsWith("_Rocks") || n.EndsWith("_Town")) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                if (k <= 0) tris += mf.sharedMesh.triangles.Length / 3;
            }
            var lg = g.GetComponent<LODGroup>(); if (lg == null) lg = g.AddComponent<LODGroup>();
            var list = new List<LOD>();
            float[] at = { 0.55f, 0.22f, 0f };   // a 512 m cell: LOD0 to ~800 m from its centre, LOD1 to ~2 km, then LOD2
            for (int k = 0; k < 3; k++) if (lods[k] != null) list.Add(new LOD(at[k], new[] { lods[k] }));
            if (list.Count > 0) { var l = list[^1]; l.screenRelativeTransitionHeight = 0; list[^1] = l; lg.SetLODs(list.ToArray()); lg.RecalculateBounds(); }
            else Object.Destroy(lg);
            return tris;
        }

        // Walt's Landing at its spot on the town shore (the pier points west into the bay).
        public static void PlaceLanding(Transform t)
        {
            if (data?.landing?.pos == null) return;
            var p = data.landing.pos; t.SetPositionAndRotation(new Vector3(p[0], p[1], p[2]), Quaternion.Euler(0, data.landing.yaw, 0));
        }
    }

    // The named places (M4 builds on them): "anchor_<id>" on the ground in front of each door, +Z (forward) out of the
    // door, so an NPC with that rotation faces the customer. Sea spots (gullrock, voss ...) sit on the water.
    public static class Places
    {
        public static readonly Dictionary<string, Transform> All = new();
        public static GameObject MariBoat;   // Mari's taco boat (blockout); M4 shows it from the day she moors
        public static Transform Get(string id) => id != null && All.TryGetValue(id, out var t) ? t : null;

        internal static void Build(Map.Anchor[] anchors)
        {
            All.Clear();
            var root = new GameObject("Places").transform; root.SetParent(Map.Root, false);
            foreach (var o in anchors)
            {
                string id = o.id, name = o.name; var fwd = o.fwd;
                var p = new Vector3(o.pos[0], o.pos[1], o.pos[2]);
                if (o.ground)  // onto what is really there (the exported heights are the heightfield's; the meshes are what you walk on):
                {              // of the surfaces with head room (not an awning's top), the one nearest the heightfield
                    float best = 99, y0 = p.y;
                    foreach (var hit in Physics.RaycastAll(new Vector3(p.x, y0 + 6, p.z), Vector3.down, 40, ~0, QueryTriggerInteraction.Ignore))
                        if (hit.normal.y > 0.64f && Mathf.Abs(hit.point.y - y0) < best && !Physics.CheckCapsule(hit.point + Vector3.up * 0.75f, hit.point + Vector3.up * 1.45f, 0.3f, ~0, QueryTriggerInteraction.Ignore))
                        { best = Mathf.Abs(hit.point.y - y0); p.y = hit.point.y; }
                }
                var t = new GameObject("anchor_" + id).transform; t.SetParent(root, false);
                t.SetPositionAndRotation(p, Quaternion.LookRotation(new Vector3(fwd[0], 0, fwd[1])));
                All[id] = t;
                Debug.Log($"[PB] map anchor {id} \"{name}\" at {p.x:F1} {p.y:F2} {p.z:F1}");
            }
        }
    }

    // The seabed (art-src/world/seabed/seabed_2048.png, 16-bit, map-B's 5 x 5 km sheet): the island's height and the
    // sea's depth anywhere, on the CPU (anchor, fishing, AI, the phone map) and on the GPU (PB_Water's depth colours).
    public static class Seabed
    {
        static NativeArray<ushort> px; static int n; static float x0, z0, size, h0, hr;
        public static bool On { get; private set; }

        public static void Init(Texture2D t, Map.SeabedInfo s)
        {
            if (t == null || s == null) { Debug.LogError("[PB] seabed texture missing"); return; }
            px = t.GetPixelData<ushort>(0); n = t.width; x0 = s.x0; z0 = s.z0; size = s.size; h0 = s.hmin; hr = s.hmax - s.hmin;
            On = px.Length == n * t.height;
            Shader.SetGlobalTexture("_PBSeabed", t);
            Shader.SetGlobalVector("_PBSeabedRect", new Vector4(x0, z0, 1f / size, 1f / size));
            Shader.SetGlobalVector("_PBSeabedH", new Vector4(h0, hr, 0, 0));
            Debug.Log($"[PB] seabed {t.width}x{t.height} {t.format}, {On}");
        }

        // metres above sea level (land +, sea floor -), bilinear between texel centres like the GPU
        public static float Height(float x, float z)
        {
            if (!On) return -30f;
            float fx = Mathf.Clamp((x - x0) / size * n - 0.5f, 0, n - 1.001f), fz = Mathf.Clamp((z - z0) / size * n - 0.5f, 0, n - 1.001f);
            int i = (int)fx, j = (int)fz; float u = fx - i, v = fz - j;
            float a = px[j * n + i], b = px[j * n + i + 1], c = px[(j + 1) * n + i], d = px[(j + 1) * n + i + 1];
            return h0 + Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v) / 65535f * hr;
        }
        public static float Depth(float x, float z) => Sea.Level - Height(x, z);
    }
}
