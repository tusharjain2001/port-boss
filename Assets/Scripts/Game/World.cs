using System.Collections.Generic;
using UnityEngine;

namespace PortBoss
{
    // Static scenery, built identically on every peer (no networking needed).
    // Layout: shore z<0, pier x∈[-3,3] z∈[0,30], boat docks at x≈7.2 z≈16, slipway x∈[-14,-6], rival port z≈230.
    public static class World
    {
        public static readonly Vector3 Spawn = new(0, 0.4f, -3f);
        public static readonly Vector3 BuyerPos = new(-4f, 0, -5.3f);
        public const float BuyerReach = 3.2f;
        public static readonly Bounds SellZone = new(new Vector3(-4f, 1.25f, -4f), new Vector3(2.6f, 0.9f, 1.4f));
        public static readonly Vector3 TrolleySpawn = new(5.5f, 0.6f, -7.5f);
        public const float PierTop = 0.2f;

        public static Transform Root;
        public struct Ladder { public Vector3 Bottom, Top; }
        public static readonly List<Ladder> Ladders = new();
        public static readonly List<Renderer> WaterRenderers = new();
        // Floating labels drawn by GameUI (hidden when something is in the way).
        public static readonly (Vector3 pos, string text)[] Labels =
        {
            (new Vector3(-4f, 2.9f, -5.3f), "FISH BUYER"),
            (new Vector3(9f, 4.4f, -6.5f), "SHED"),
        };

        public static void Build()
        {
            Root = new GameObject("World").transform;
            Shore();
            Pier();
            BuyerTable();
            Shed();
            Bay();
            Ladders.Clear();
            AddLadder(new Vector3(-3.15f, 0, 6f), Vector3.right);     // pier, left side
            AddLadder(new Vector3(-3.15f, 0, 21f), Vector3.right);
            AddLadder(new Vector3(3.15f, 0, 27f), Vector3.left);      // pier, right side near the end
            AddLadder(new Vector3(20f, 0, 0.15f), Vector3.back);      // quay front
            AddLadder(new Vector3(-26f, 0, 0.15f), Vector3.back);
            RivalPort();
            OpenSea();
            if (Art.Cloud != null) Clouds();
            Speedboat.Create();
            WaterRenderers.Clear();
            WaterRenderers.Add(Water.Create(new Vector3(0, 0, 80), 280, 112).GetComponent<Renderer>());
            // Far sea to the horizon (flat, slightly below the wave mean).
            var far = Art.Box(Root, new Vector3(0, Water.Level - 0.35f, 600), new Vector3(3000, 0.1f, 1600), Art.Hex("#2f5562"), false, "far_sea", 0.8f);
            far.GetComponent<Renderer>().sharedMaterial = Art.WaterMat;
            WaterRenderers.Add(far.GetComponent<Renderer>());
        }

        static void Shore()
        {
            var t = Root;
            Art.Box(t, new Vector3(-28, -3, -30), new Vector3(28, 6, 60), Art.Concrete, true, "quay_left");   // x -42..-14
            Art.Box(t, new Vector3(27, -3, -30), new Vector3(66, 6, 60), Art.Concrete, true, "quay_right");  // x -6..60
            Art.Box(t, new Vector3(-55, -2.5f, -30), new Vector3(30, 6, 60), Art.Sand, true, "beach_left");
            // Slipway ramp: from the quay edge down into the sea (the trolley rolls down this).
            float len = 17f, drop = 3.4f, ang = Mathf.Atan2(drop, len) * Mathf.Rad2Deg;
            var ramp = Art.Box(t, new Vector3(-10, -drop / 2 - 0.25f, len / 2 - 0.4f), new Vector3(7.5f, 0.5f, Mathf.Sqrt(len * len + drop * drop)), Art.Hex("#8c8374"), true, "slipway");
            ramp.transform.localRotation = Quaternion.Euler(ang, 0, 0);
            // Fill under quay at slipway gap so there's no hole at its top.
            Art.Box(t, new Vector3(-10, -3, -30.2f), new Vector3(8, 6, 59.6f), Art.Concrete, true, "quay_mid");
            // Bollards along the quay edge, kerb.
            for (int x = -40; x <= 58; x += 6)
                if (x < -14 || x > -6) Art.Prim(PrimitiveType.Cylinder, t, new Vector3(x, 0.3f, -0.6f), Quaternion.identity, new Vector3(0.35f, 0.3f, 0.35f), Art.Hex("#3b3b3b"), true, "bollard");
            // Some clutter on the quay: barrels, a lamp, a fence.
            for (int i = 0; i < 4; i++)
                Art.Prim(PrimitiveType.Cylinder, t, new Vector3(14 + i * 0.75f, 0.5f, -3.5f + (i % 2) * 0.7f), Quaternion.identity, new Vector3(0.6f, 0.5f, 0.6f), i % 2 == 0 ? Art.Navy : Art.Rust, true, "barrel");
            Lamp(t, new Vector3(3.5f, 0, -1.5f));
            // Net pile, a beached rowboat, empty crate stacks by the shed.
            Art.Prim(PrimitiveType.Sphere, t, new Vector3(5.5f, 0.1f, -3.4f), Quaternion.identity, new Vector3(2.2f, 0.5f, 1.6f), Art.Hex("#4c5a43"), true, "net_pile");
            var row = Art.Box(t, new Vector3(-20, 0.35f, -5), new Vector3(1.5f, 0.6f, 4f), Art.Hex("#c9b48a"), true, "rowboat");
            row.transform.localRotation = Quaternion.Euler(0, 25, 8);
            Art.Box(row.transform, new Vector3(0, 0.45f, 0), new Vector3(1.05f, 0.25f, 0.95f), Art.Hex("#4a6a7a"), false, "rowboat_stripe");
            for (int i = 0; i < 6; i++)
                Art.Box(t, new Vector3(12.4f + (i % 2) * 0.62f, 0.3f + (i / 2) * 0.6f, -12.8f), new Vector3(0.58f, 0.58f, 0.58f), Color.Lerp(Art.Wood, Art.WoodDark, i * 0.13f), true, "empty_crate");
            Lamp(t, new Vector3(-15.5f, 0, -1.5f));
            for (int x = -40; x < 60; x += 3)
                Art.Box(t, new Vector3(x, 0.55f, -24), new Vector3(2.9f, 1.1f, 0.08f), Art.WoodDark, true, "fence");
            // A couple of low warehouses back on land for depth.
            Art.Box(t, new Vector3(-26, 2.5f, -16), new Vector3(14, 5, 8), Art.Hex("#a1856b"), true, "warehouse");
            Art.Box(t, new Vector3(-26, 5.3f, -16), new Vector3(14.4f, 0.6f, 8.4f), Art.Hex("#5e3b30"), true, "warehouse_roof");
            Art.Box(t, new Vector3(26, 3, -17), new Vector3(16, 6, 9), Art.Hex("#8f9a9a"), true, "warehouse2");
        }

        // Steel ladder down the side; "inward" points onto the deck.
        static void AddLadder(Vector3 edge, Vector3 inward)
        {
            float top = inward == Vector3.back ? 0f : PierTop;
            var l = new GameObject("Ladder").transform;
            l.SetParent(Root, false);
            l.position = new Vector3(edge.x, 0, edge.z);
            l.rotation = Quaternion.LookRotation(-inward);
            var steel = Art.Hex("#8a9298");
            float bottom = Water.Level - 1.6f, h = top + 0.9f - bottom;
            foreach (var x in new[] { -0.28f, 0.28f })
                Art.Box(l, new Vector3(x, bottom + h / 2, 0), new Vector3(0.06f, h, 0.06f), steel, false, "rail", 0.5f);
            for (float y = bottom + 0.2f; y < top; y += 0.32f)
                Art.Box(l, new Vector3(0, y, 0), new Vector3(0.56f, 0.05f, 0.05f), steel, false, "rung", 0.5f);
            Ladders.Add(new Ladder { Bottom = edge - inward * 0.5f + Vector3.up * (Water.Level - 1.3f), Top = edge + inward * 0.9f + Vector3.up * (top + 0.05f) });
        }

        static void Lamp(Transform t, Vector3 p)
        {
            Art.Prim(PrimitiveType.Cylinder, t, p + Vector3.up * 2.2f, Quaternion.identity, new Vector3(0.12f, 2.2f, 0.12f), Art.Hex("#2b2b2b"), true, "lamp_post");
            Art.Box(t, p + new Vector3(0, 4.45f, 0), new Vector3(0.4f, 0.3f, 0.4f), Art.Cream, false, "lamp_head").GetComponent<Renderer>().sharedMaterial = Art.Glow(Art.Hex("#ffd9a0"), 1.5f);
        }

        // The pier: a solid base and an old end section (z >= 20) on a hinge that sags when overloaded.
        public static Transform PierEnd;
        public const float PierEndZ = 20f;
        static readonly List<GameObject> holePlanks = new();

        static void Pier()
        {
            var p = new GameObject("Pier").transform;
            p.SetParent(Root, false);
            PierEnd = new GameObject("PierEnd").transform;
            PierEnd.SetParent(Root, false);
            PierEnd.position = new Vector3(0, PierTop, PierEndZ);
            var rb = PierEnd.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.Interpolate;
            PierEnd.gameObject.AddComponent<PierSag>();
            // Children are placed in world coordinates on whichever section they sit on.
            Transform Sec(float z) => z >= PierEndZ ? PierEnd : p;
            Vector3 L(Transform sec, Vector3 w) => sec == p ? w : w - PierEnd.position;

            float y = PierTop - 0.06f;
            int i = 0;
            holePlanks.Clear();
            for (float z = 0.16f; z < 30f; z += 0.34f, i++)
            {
                // Missing planks: a hole on the left at z≈10 and on the right at z≈24 (away from the gangplank).
                bool holeL = z > 9.6f && z < 11.0f, holeR = z > 23.5f && z < 24.8f;
                var tint = Color.Lerp(Art.Wood, Art.WoodLight, Mathf.PerlinNoise(i * 0.37f, 0.5f));
                var sec = Sec(z);
                var l = Art.Box(sec, L(sec, new Vector3(-1.5f, y, z)), new Vector3(2.98f, 0.12f, 0.3f), holeL ? Color.Lerp(Art.WoodLight, Color.white, 0.25f) : tint, true, "plank");
                var r = Art.Box(sec, L(sec, new Vector3(1.5f, y, z)), new Vector3(2.98f, 0.12f, 0.3f), holeR ? Color.Lerp(Art.WoodLight, Color.white, 0.25f) : Color.Lerp(tint, Art.WoodDark, 0.15f), true, "plank");
                // Hole planks exist from the start (new, paler wood) but only appear once the pier is repaired.
                if (holeL) { l.SetActive(false); holePlanks.Add(l); }
                if (holeR) { r.SetActive(false); holePlanks.Add(r); }
            }
            // Stringers + kerb, split at the hinge; posts into the water (the base keeps its posts, the end hangs off the hinge).
            foreach (var (sec, z0, z1) in new[] { (p, 0f, PierEndZ), (PierEnd, PierEndZ, 30f) })
            {
                float zc = (z0 + z1) / 2, len = z1 - z0;
                foreach (var x in new[] { -2.7f, 0f, 2.7f })
                    Art.Box(sec, L(sec, new Vector3(x, y - 0.2f, zc)), new Vector3(0.25f, 0.25f, len), Art.WoodDark, false, "stringer");
                Art.Box(sec, L(sec, new Vector3(-2.85f, PierTop + 0.1f, zc)), new Vector3(0.18f, 0.2f, len), Art.WoodDark, true, "kerb");
            }
            for (float z = 1.5f; z < 30.5f; z += 3f)
                foreach (var x in new[] { -2.9f, 2.9f })
                    Art.Prim(PrimitiveType.Cylinder, p, new Vector3(x, -2.0f, z), Quaternion.identity, new Vector3(0.32f, 2.5f, 0.32f), Art.WoodDark, true, "post");
            // Rope bollards on the right (boat side).
            for (float z = 4f; z < 30f; z += 6f)
            {
                var sec = Sec(z);
                Art.Prim(PrimitiveType.Cylinder, sec, L(sec, new Vector3(2.75f, PierTop + 0.2f, z)), Quaternion.identity, new Vector3(0.3f, 0.2f, 0.3f), Art.Hex("#3b3b3b"), true, "bollard");
            }
            Lamp(PierEnd, L(PierEnd, new Vector3(-2.6f, PierTop, 29.5f)));
        }

        public static void SetRepaired(bool on) { foreach (var g in holePlanks) if (g != null) g.SetActive(on); }

        static void BuyerTable()
        {
            var b = new GameObject("Buyer").transform;
            b.SetParent(Root, false);
            var c = World.SellZone.center;
            Art.Box(b, new Vector3(c.x, 0.85f, c.z), new Vector3(2.6f, 0.1f, 1.4f), Art.WoodLight, true, "table_top");
            // Lip so crates don't slide off.
            Art.Box(b, new Vector3(c.x, 0.97f, c.z - 0.67f), new Vector3(2.6f, 0.14f, 0.06f), Art.WoodDark, true, "lip");
            foreach (var dx in new[] { -1.2f, 1.2f })
                foreach (var dz in new[] { -0.6f, 0.6f })
                    Art.Box(b, new Vector3(c.x + dx, 0.4f, c.z + dz), new Vector3(0.1f, 0.8f, 0.1f), Art.WoodDark, true, "leg");
            // Awning with stripes.
            foreach (var dx in new[] { -1.4f, 1.4f })
                Art.Box(b, new Vector3(c.x + dx, 1.3f, c.z - 1.4f), new Vector3(0.1f, 2.6f, 0.1f), Art.WoodDark, true, "pole");
            for (int i = 0; i < 6; i++)
            {
                var s = Art.Box(b, new Vector3(c.x - 1.25f + i * 0.5f, 2.55f, c.z - 0.9f), new Vector3(0.5f, 0.05f, 1.4f), i % 2 == 0 ? Art.Red : Art.Cream, false, "awning");
                s.transform.localRotation = Quaternion.Euler(-12, 0, 0);
            }
            // The buyer: a stout figure behind the table.
            var n = new Vector3(c.x, 0, c.z - 1.3f);
            Art.Prim(PrimitiveType.Capsule, b, n + Vector3.up * 0.95f, Quaternion.identity, new Vector3(0.75f, 0.95f, 0.75f), Art.Hex("#4f5d3a"), true, "buyer_body");
            Art.Prim(PrimitiveType.Sphere, b, n + Vector3.up * 2.05f, Quaternion.identity, Vector3.one * 0.42f, Art.Hex("#c99b7a"), false, "buyer_head");
            Art.Box(b, n + new Vector3(0, 2.3f, 0), new Vector3(0.5f, 0.12f, 0.5f), Art.Hex("#2c2c2c"), false, "buyer_hat");
            // Sign board.
        }

        static void Shed()
        {
            var s = new GameObject("Shed").transform;
            s.SetParent(Root, false);
            var o = new Vector3(9, 0, -9);
            Color wall = Art.Hex("#7b8f8a");
            Art.Box(s, o + new Vector3(0, 1.75f, -2.5f), new Vector3(6, 3.5f, 0.15f), wall, true, "wall_back");
            Art.Box(s, o + new Vector3(3, 1.75f, 0), new Vector3(0.15f, 3.5f, 5), wall, true, "wall_side");
            Art.Box(s, o + new Vector3(-3, 1.75f, 0), new Vector3(0.15f, 3.5f, 5), wall, true, "wall_side");
            Art.Box(s, o + new Vector3(-1.9f, 1.75f, 2.5f), new Vector3(2.2f, 3.5f, 0.15f), wall, true, "wall_front");
            Art.Box(s, o + new Vector3(1.9f, 1.75f, 2.5f), new Vector3(2.2f, 3.5f, 0.15f), wall, true, "wall_front");
            Art.Box(s, o + new Vector3(0, 3.1f, 2.5f), new Vector3(1.6f, 0.8f, 0.15f), wall, true, "lintel");
            var roof = Art.Box(s, o + new Vector3(0, 3.7f, 0), new Vector3(6.6f, 0.15f, 5.8f), Art.Rust, true, "roof");
            roof.transform.localRotation = Quaternion.Euler(-6, 0, 0);
            Art.Box(s, o + new Vector3(-2.2f, 0.5f, -1.8f), new Vector3(1.2f, 1f, 0.9f), Art.WoodDark, true, "workbench");
            // Contract board with two notices pinned on it.
            Art.Box(s, o + new Vector3(-1.9f, 1.9f, 2.6f), new Vector3(1.6f, 1.1f, 0.06f), Art.WoodDark, false, "board");
            Art.Box(s, o + new Vector3(-2.25f, 1.95f, 2.64f), new Vector3(0.55f, 0.7f, 0.02f), Art.Cream, false, "notice");
            Art.Box(s, o + new Vector3(-1.55f, 1.9f, 2.64f), new Vector3(0.55f, 0.75f, 0.02f), Art.Hex("#f0d9a8"), false, "notice");
        }

        static void Bay()
        {
            // Low land either side of the bay, rolling hills on top, rocks at the waterline.
            Art.Box(Root, new Vector3(-150, -2, 110), new Vector3(190, 4, 240), Art.Grass, true, "land_left");
            Art.Box(Root, new Vector3(155, -2, 75), new Vector3(180, 4, 90), Art.Grass, true, "land_right");
            Hills(new Vector2(-62, 15), new Vector2(-75, 210), 9, 30, 7);
            Hills(new Vector2(-110, 40), new Vector2(-120, 220), 6, 55, 11);
            Hills(new Vector2(72, 40), new Vector2(84, 112), 5, 28, 3);
            Hills(new Vector2(125, 50), new Vector2(140, 105), 3, 55, 5);
            var rng = new System.Random(42);
            for (int i = 0; i < 46; i++)
            {
                bool left = i % 2 == 0;
                float z = 4 + (i / 2) * 9f + (float)rng.NextDouble() * 4;
                float x = left ? -55 - (float)rng.NextDouble() * 3 : 64 + (float)rng.NextDouble() * 3;
                if (!left && z > 116) continue;
                var sz = 2.5f + (float)rng.NextDouble() * 3.5f;
                Art.Box(Root, new Vector3(x, Water.Level + 0.2f, z), new Vector3(sz, sz * 0.6f, sz * 1.2f), Color.Lerp(Art.Rock, Art.Hex("#8a8278"), (float)rng.NextDouble()), false, "rock")
                    .transform.localRotation = Quaternion.Euler((float)rng.NextDouble() * 30, (float)rng.NextDouble() * 360, (float)rng.NextDouble() * 30);
            }
            // Buoys marking the channel.
            for (int i = 0; i < 4; i++)
                Art.Prim(PrimitiveType.Cylinder, Root, new Vector3(i % 2 == 0 ? 18 : 34, Water.Level + 0.3f, 50 + i * 30), Quaternion.identity, new Vector3(0.7f, 0.6f, 0.7f), i % 2 == 0 ? Art.Red : Art.Hex("#3f7a3f"), false, "buoy");
        }

        static void Hills(Vector2 from, Vector2 to, int n, float r, int seed)
        {
            var rng = new System.Random(seed);
            for (int i = 0; i < n; i++)
            {
                var p = Vector2.Lerp(from, to, i / (float)(n - 1)) + new Vector2((float)rng.NextDouble() * 10 - 5, (float)rng.NextDouble() * 10 - 5);
                float k = 0.8f + (float)rng.NextDouble() * 0.6f;
                var c = Color.Lerp(Art.Grass, Art.Hex("#7d7a4c"), (float)rng.NextDouble());
                var h = Art.Prim(PrimitiveType.Sphere, Root, new Vector3(p.x, -r * 0.12f, p.y), Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0), new Vector3(r * 1.6f * k, r * (0.35f + 0.25f * (float)rng.NextDouble()), r * 1.3f * k), c, false, "hill");
                h.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static void OpenSea()
        {
            // Lighthouse on the tip of the right headland.
            var lh = new GameObject("Lighthouse").transform;
            lh.SetParent(Root, false);
            lh.position = new Vector3(84, 0, 116);
            Art.Box(lh, new Vector3(0, -0.5f, 0), new Vector3(9, 3, 9), Art.Rock, false, "lh_rock");
            for (int i = 0; i < 6; i++)
                Art.Prim(PrimitiveType.Cylinder, lh, new Vector3(0, 2 + i * 2.6f, 0), Quaternion.identity, new Vector3(3.2f - i * 0.18f, 1.3f, 3.2f - i * 0.18f), i % 2 == 0 ? Art.Cream : Art.Red, false, "lh_band");
            Art.Prim(PrimitiveType.Cylinder, lh, new Vector3(0, 17.6f, 0), Quaternion.identity, new Vector3(2.2f, 0.9f, 2.2f), Art.Cream, false, "lh_lamp").GetComponent<Renderer>().sharedMaterial = Art.Glow(Art.Hex("#ffe2a8"), 2.5f);
            Art.Prim(PrimitiveType.Sphere, lh, new Vector3(0, 19f, 0), Quaternion.identity, new Vector3(2.4f, 1.4f, 2.4f), Art.Red, false, "lh_cap");
            // Distant islands on the horizon.
            var rng = new System.Random(9);
            foreach (var (x, z, r) in new[] { (320f, 520f, 70f), (520f, 380f, 45f), (230f, 760f, 110f), (-260f, 820f, 90f) })
                for (int k = 0; k < 3; k++)
                {
                    var h = Art.Prim(PrimitiveType.Sphere, Root, new Vector3(x + k * r * 0.5f, -r * 0.25f, z + (float)rng.NextDouble() * r * 0.3f), Quaternion.identity,
                        new Vector3(r * (1.4f - k * 0.3f), r * (0.45f - k * 0.08f), r * 0.9f), Color.Lerp(Art.Hex("#5d6a55"), Art.Hex("#7d8877"), k * 0.4f), false, "island");
                    h.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            // Ships crawling along the horizon (Kenney CC0 models, scaled up).
            foreach (var (name, x, z, sc, sp) in new[] { ("ship-large", -200f, 430f, 5f, 2.2f), ("ship-cargo-a", 260f, 620f, 6f, 1.6f), ("ship-cargo-c", 60f, 900f, 7f, 1.2f) })
            {
                var s = Art.Model(name, Root, new Vector3(x, Water.Level - 0.4f, z), 90, sc);
                if (s != null) s.AddComponent<Drifter>().Speed = sp;
            }
        }

        static void Clouds()
        {
            var rng = new System.Random(5);
            for (int i = 0; i < 26; i++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2, dist = 260 + (float)rng.NextDouble() * 260;
                var c = new GameObject("cloud").transform;
                c.SetParent(Root, false);
                c.position = new Vector3(Mathf.Cos(ang) * dist, 90 + (float)rng.NextDouble() * 70, 120 + Mathf.Sin(ang) * dist);
                int puffs = 3 + rng.Next(3);
                for (int k = 0; k < puffs; k++)
                {
                    float w = 22 + (float)rng.NextDouble() * 26;
                    var puff = Art.Prim(PrimitiveType.Sphere, c, new Vector3(k * w * 0.55f - puffs * w * 0.25f, (float)rng.NextDouble() * 5, (float)rng.NextDouble() * 10), Quaternion.identity, new Vector3(w, w * 0.38f, w * 0.7f), Color.white, false, "puff");
                    var r = puff.GetComponent<Renderer>();
                    r.sharedMaterial = Art.Cloud; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        static void RivalPort()
        {
            var r = new GameObject("RivalPort").transform;
            r.SetParent(Root, false);
            r.position = new Vector3(10, 0, 240);
            Art.Box(r, new Vector3(0, -1, 10), new Vector3(170, 6, 40), Art.Concrete, true, "rival_quay");
            var cols = new[] { Art.Red, Art.Navy, Art.Hex("#c9a13a"), Art.Hex("#3f7a5a"), Art.Hex("#d0d0cc"), Art.Rust };
            var rng = new System.Random(7);
            for (int x = -70; x < 70; x += 7)
                for (int row = 0; row < 3; row++)
                {
                    int h = 1 + rng.Next(4);
                    for (int k = 0; k < h; k++)
                        Art.Box(r, new Vector3(x, 2 + 0.05f + k * 2.6f + 1.3f - 0.05f, 4 + row * 3.0f), new Vector3(6.1f, 2.6f, 2.5f), cols[rng.Next(cols.Length)], false, "container");
                }
            // Gantry cranes: two legs, a beam, a boom out over the water.
            for (int i = 0; i < 4; i++)
            {
                var cx = -54 + i * 34;
                var col = i % 2 == 0 ? Art.Red : Art.Hex("#2f6fa0");
                Art.Box(r, new Vector3(cx - 6, 16, -6), new Vector3(1.6f, 32, 1.6f), col, false, "crane_leg");
                Art.Box(r, new Vector3(cx + 6, 16, -6), new Vector3(1.6f, 32, 1.6f), col, false, "crane_leg");
                Art.Box(r, new Vector3(cx - 6, 16, 4), new Vector3(1.6f, 32, 1.6f), col, false, "crane_leg");
                Art.Box(r, new Vector3(cx + 6, 16, 4), new Vector3(1.6f, 32, 1.6f), col, false, "crane_leg");
                Art.Box(r, new Vector3(cx, 32, -6), new Vector3(14, 2, 2), col, false, "crane_beam");
                Art.Box(r, new Vector3(cx, 33, -18), new Vector3(2.4f, 2, 38), col, false, "crane_boom");
                Art.Box(r, new Vector3(cx, 35, 2), new Vector3(5, 4, 5), Art.Cream, false, "crane_cab");
                Art.Box(r, new Vector3(cx, 34.5f, -36.5f), new Vector3(0.8f, 0.8f, 0.8f), Art.Cream, false, "tip").GetComponent<Renderer>().sharedMaterial = Art.Glow(Art.Hex("#ff3b2a"), 3f);
            }
            // A big ship alongside.
            Art.Box(r, new Vector3(-10, 1, -22), new Vector3(110, 9, 18), Art.Hex("#2b3d52"), false, "ship_hull");
            Art.Box(r, new Vector3(-10, -2.6f, -22), new Vector3(110.2f, 1.8f, 18.2f), Art.Rust, false, "ship_bottom");
            Art.Box(r, new Vector3(-55, 10, -22), new Vector3(12, 12, 15), Art.Cream, false, "ship_bridge");
            for (int x = -40; x < 40; x += 7)
                for (int k = 0; k < 2; k++)
                    Art.Box(r, new Vector3(x, 6.8f + k * 2.6f, -22), new Vector3(6.1f, 2.6f, 14), cols[(x / 7 + k + 20) % cols.Length], false, "deck_container");
            // Kenney CC0 vessels moored along the rival quay.
            Art.Model("ship-large", r, new Vector3(52, Water.Level - 0.3f, -24), 90, 4f);
            Art.Model("ship-cargo-b", r, new Vector3(-62, Water.Level - 0.3f, -38), 85, 3.2f);
            Art.Model("boat-speed-a", r, new Vector3(22, Water.Level - 0.1f, -40), 200, 2.2f);
            // Lit windows on the warehouses behind.
            Art.Box(r, new Vector3(30, 8, 26), new Vector3(60, 16, 14), Art.Hex("#8a8f94"), false, "rival_warehouse");
            for (int x = 6; x < 58; x += 4)
                Art.Box(r, new Vector3(x, 10, 18.9f), new Vector3(2, 1.2f, 0.1f), Art.Cream, false, "window").GetComponent<Renderer>().sharedMaterial = Art.Glow(Art.Hex("#ffd68a"), 2f);
            Art.Box(r, new Vector3(-40, 12, 22), new Vector3(6, 24, 6), Art.Cream, false, "tower");
            Art.Box(r, new Vector3(-40, 25, 22), new Vector3(8, 3, 8), Art.Red, false, "tower_top");
        }
    }
}
