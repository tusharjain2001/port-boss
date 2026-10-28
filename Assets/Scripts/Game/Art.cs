using System.Collections.Generic;
using UnityEngine;

namespace PortBoss
{
    // Material + primitive helpers. Base materials are assets (so URP keeps their shader variants in builds);
    // everything else is a tinted copy.
    public static class Art
    {
        public static Material Lit, Emissive, Particle, WaterMat;
        static readonly Dictionary<(Color, float), Material> cache = new();

        public static Material Get(Color c, float smooth = 0.15f)
        {
            if (cache.TryGetValue((c, smooth), out var m)) return m;
            m = new Material(Lit);
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            cache[(c, smooth)] = m;
            return m;
        }

        public static Material Glow(Color c, float intensity = 2f)
        {
            var m = new Material(Emissive);
            m.SetColor("_BaseColor", c);
            m.SetColor("_EmissionColor", c * intensity);
            return m;
        }

        // Primitive with collider kept or removed.
        public static GameObject Box(Transform parent, Vector3 pos, Vector3 size, Color c, bool collider = true, string name = "box", float smooth = 0.15f)
            => Prim(PrimitiveType.Cube, parent, pos, Quaternion.identity, size, c, collider, name, smooth);

        public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 pos, Quaternion rot, Vector3 size, Color c, bool collider = true, string name = "prim", float smooth = 0.15f)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = rot;
            g.transform.localScale = size;
            if (!collider) Object.Destroy(g.GetComponent<Collider>());
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
            foreach (var r in g.GetComponentsInChildren<Renderer>()) r.sharedMaterial = ui.KitMats[ui.ModelKit[i]];
            foreach (var c in g.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            return g;
        }

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        // Palette: stylised realism, warm late afternoon.
        public static readonly Color Wood = Hex("#8a6a4a"), WoodDark = Hex("#5b4330"), WoodLight = Hex("#a88660"),
            Concrete = Hex("#9c968c"), Sand = Hex("#b9a582"), Grass = Hex("#6f7a4a"), Rock = Hex("#6c665e"),
            Rust = Hex("#9a4a2c"), Navy = Hex("#2e4a63"), Red = Hex("#b5402f"), Cream = Hex("#e8dcc4"),
            Steel = Hex("#7d858c"), Fish = Hex("#a9c3cf"), Rot = Hex("#6b7a3a");
    }
}
