using System.Collections.Generic;
using UnityEngine;

namespace PortBoss
{
    // Material + primitive helpers. Base materials are assets (so URP keeps their shader variants in builds);
    // everything else is a tinted copy.
    public static class Art
    {
        public static Material Lit, Emissive, Particle, WaterMat, Cloud, Land, Spark;
        public static Material Toon; // Toon Harbor's toon material when the skin is on: our colours share its shading ramp
        static readonly Dictionary<(Color, float), Material> cache = new();
        static readonly Dictionary<Material, Color> colorOf = new();
        static readonly Dictionary<Color, Texture2D> solids = new();
        const float ToonGain = 0.55f;

        public static Material Get(Color c, float smooth = 0.15f)
        {
            if (cache.TryGetValue((c, smooth), out var m)) return m;
            if (Toon != null) { m = new Material(Toon); m.SetTexture("_TextureSample", Solid(c)); }
            else { m = new Material(Lit); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); }
            cache[(c, smooth)] = m; colorOf[m] = c;
            return m;
        }

        // A flat colour as a texture (the toon shader colours by texture, not by a colour property).
        public static Texture2D Solid(Color c)
        {
            if (solids.TryGetValue(c, out var t)) return t;
            t = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            // The toon ramp lights flat colours ~1.4x brighter than URP Lit; pull ours down so they sit with the pack's atlas.
            var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = new Color(c.r * ToonGain, c.g * ToonGain, c.b * ToonGain, c.a);
            t.SetPixels(px); t.Apply(false, true);
            return solids[c] = t;
        }

        public static Color ColorOf(Material m) => m != null && colorOf.TryGetValue(m, out var c) ? c : m != null && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;

        // Per-renderer tint that works with either material type.
        public static void Tint(Renderer r, MaterialPropertyBlock mpb, Color c)
        {
            r.GetPropertyBlock(mpb);
            if (Toon != null) mpb.SetTexture("_TextureSample", Solid(c)); else mpb.SetColor("_BaseColor", c);
            r.SetPropertyBlock(mpb);
        }

        // A Toon Harbor prefab placed as scenery (its colliders dropped unless asked for).
        public static GameObject Place(GameObject prefab, Transform parent, Vector3 pos, float yaw, float scale = 1f, bool colliders = false)
        {
            if (prefab == null) return null;
            var g = Object.Instantiate(prefab, parent, false);
            g.transform.localPosition = pos; g.transform.localRotation = Quaternion.Euler(0, yaw, 0); g.transform.localScale = Vector3.one * scale;
            if (!colliders) foreach (var c in g.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = scale * Bounds(g).size.magnitude > 120 ? UnityEngine.Rendering.ShadowCastingMode.Off : r.shadowCastingMode;
            return g;
        }

        public static Bounds Bounds(GameObject g)
        {
            var rs = g.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        public static Material Glow(Color c, float intensity = 2f)
        {
            var m = new Material(Emissive);
            m.SetColor("_BaseColor", c);
            m.SetColor("_EmissionColor", c * intensity);
            return m;
        }

        // Primitive with collider kept or removed.
        // Bevelled boxes (art plan: no hard cubes). Off until the A/B against cp2 is approved: -pb-bevel turns it on.
        public static bool Bevel = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-pb-bevel") >= 0;

        public static GameObject Box(Transform parent, Vector3 pos, Vector3 size, Color c, bool collider = true, string name = "box", float smooth = 0.15f)
        {
            // Objects that get rescaled after creation keep the unit cube.
            if (!Bevel || name == "plank_gp" || name == "cleat") return Prim(PrimitiveType.Cube, parent, pos, Quaternion.identity, size, c, collider, name, smooth);
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.AddComponent<MeshFilter>().sharedMesh = PortBoss.Bevel.Box(size, Mathf.Clamp(Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.12f, 0.004f, 0.05f));
            g.AddComponent<MeshRenderer>().sharedMaterial = Get(c, smooth);
            if (collider) g.AddComponent<BoxCollider>().size = size;
            return g;
        }

        public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 pos, Quaternion rot, Vector3 size, Color c, bool collider = true, string name = "prim", float smooth = 0.15f)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = rot;
            g.transform.localScale = size;
            if (!collider) Object.DestroyImmediate(g.GetComponent<Collider>()); // not deferred: a parent rigidbody would see it for a frame
            g.GetComponent<Renderer>().sharedMaterial = Get(c, smooth);
            return g;
        }

        // Instantiate an imported model (Kenney/own-made) with its muted kit material, no colliders.
        public static GameObject Model(string name, Transform parent, Vector3 pos, float yaw, float scale = 1f)
        {
            var ui = GameUI.I;
            int i = System.Array.FindIndex(ui.Models, m => m != null && m.name == name);
            if (i < 0) { Debug.LogWarning("[PB] no model " + name); return null; }
            var g = Object.Instantiate(ui.Models[i], parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            g.transform.localScale = Vector3.one * scale;
            // Kits that share a colormap get the muted kit material; own-made models keep their own colours.
            var km = ui.KitMats[ui.ModelKit[i]];
            if (km.GetTexture("_BaseMap") != null)
                foreach (var r in g.GetComponentsInChildren<Renderer>()) r.sharedMaterial = km;
            foreach (var c in g.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            return g;
        }

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        // Palette: stylised realism, warm late afternoon.
        public static readonly Color Wood = Hex("#8a6a4a"), WoodDark = Hex("#5b4330"), WoodLight = Hex("#a88660"),
            Concrete = Hex("#9c968c"), Sand = Hex("#b9a582"), Grass = Hex("#6f7a4a"), Rock = Hex("#6c665e"),
            Rust = Hex("#9a4a2c"), Navy = Hex("#2e4a63"), Red = Hex("#b5402f"), Cream = Hex("#e8dcc4"),
            Steel = Hex("#7d858c"), Fish = Hex("#a9c3cf"), Rot = Hex("#7d7462");
    }
}
