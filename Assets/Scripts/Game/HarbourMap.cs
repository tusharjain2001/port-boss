using UnityEngine;

namespace PortBoss
{
    // The harbour (Toon Harbor skin only): our run-down corner berth inside a big working port.
    // Layout (x east, z out to sea): our yard x -42..60 z -60..0 with the concrete jetty (the old pier's colliders),
    // the West Terminal on the left landmass (x < -55), the East Terminal (x > 60), the basin between them up to the
    // rival's quay (z ~240), a tetrapod breakwater at z ~300 with the channel gap at x 140..240, open sea beyond.
    // Boats come in through the gap and down the basin (Boat.InPaths), so the basin and that corridor stay clear.
    // Ground colliders are World's plain boxes (hidden here); everything visible is pack prefabs.
    public static class HarbourMap
    {
        static Transform R;

        public static void Build(Transform root)
        {
            R = new GameObject("Harbour").transform;
            R.SetParent(root, false);
            Ground();
            QuayWalls();
            Jetty();
            OurBerth();
            EastTerminal();
            WestTerminal();
            Breakwater();
            Shelter(root.Find("Shed"));
            Stall(root.Find("Buyer"));
            Roads();
            Industry();
            Hinterland();
            Workers();
            Check();
        }

        // A pack prefab standing on y = pos.y (its lowest point there), or with its top at y (Top).
        static GameObject On(string n, Vector3 pos, float yaw, float scale = 1f, bool colliders = false)
        {
            var g = Art.Place(Skin.Get(n), R, pos, yaw, scale, colliders);
            if (g == null) return null;
            g.transform.position += Vector3.up * (pos.y - Art.Bounds(g).min.y);
            placed.Add((g, pos.y));
            return g;
        }

        // Automated check: everything stood on the ground must be upright and flush (tilt <= 2 deg, |gap| <= 0.2 m).
        static readonly System.Collections.Generic.List<(GameObject g, float y)> placed = new();
        static void Check()
        {
            int bad = 0;
            Physics.SyncTransforms();
            foreach (var (g, y) in placed)
            {
                if (g == null) continue;
                float tilt = Vector3.Angle(g.transform.up, Vector3.up), gap = Art.Bounds(g).min.y - y;
                if (tilt > 2f || Mathf.Abs(gap) > 0.2f) { bad++; Debug.Log($"[PB] map check: {g.name} at {g.transform.position:F1} tilt={tilt:F1} gap={gap:F2}"); }
                if (y < -0.5f) continue;
                foreach (var hit in Physics.RaycastAll(g.transform.position + Vector3.up * 0.5f, Vector3.down, 3f))
                    if (!hit.transform.IsChildOf(g.transform) && hit.point.y < y + 0.3f)
                    {
                        if (Mathf.Abs(hit.point.y - y) > 0.2f) { bad++; Debug.Log($"[PB] map check: {g.name} at {g.transform.position:F1} floats/sinks vs ground {hit.point.y:F2}"); }
                        break;
                    }
            }
            Debug.Log($"[PB] map check: {placed.Count} placed, {bad} flagged");
            placed.Clear();
        }

        // A line of a modular prefab (fences) from a to b, each piece butted to the last; skip = every n-th piece missing.
        static void Run(string n, Vector3 a, Vector3 b, int skip = 0, float lean = 0)
        {
            var d = b - a; float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg - 90, len = d.magnitude, at = 0; int i = 0;
            while (at < len)
            {
                var g = On(n, a + d / len * at, yaw);
                if (g == null) return;
                var sz = Art.Bounds(g).size; float step = Mathf.Max(1f, Mathf.Abs(d.x) > Mathf.Abs(d.z) ? sz.x : sz.z);
                g.transform.position += d / len * step / 2;
                i++;
                if (skip > 0 && i % skip == 0) { Object.Destroy(g); placed.RemoveAt(placed.Count - 1); }
                else if (lean != 0 && i % 4 == 2) { g.transform.rotation = Quaternion.AngleAxis(lean, d / len) * g.transform.rotation; placed.RemoveAt(placed.Count - 1); }
                at += step;
            }
        }

        static GameObject Top(string n, Vector3 pos, float yaw, Vector3 scale)
        {
            var g = Art.Place(Skin.Get(n), R, pos, yaw, 1f);
            if (g == null) return null;
            g.transform.localScale = scale;
            g.transform.position += Vector3.up * (pos.y - Art.Bounds(g).max.y);
            return g;
        }

        // Plain 12 m concrete ground tiles (stains, grit; the paved slab's crack pattern read as X marks) over every plot.
        static void Ground()
        {
            void Tile(float x0, float x1, float z0, float z1, int seed)
            {
                var rng = new System.Random(seed);
                for (float x = x0 + 6; x < x1 + 6; x += 12)
                    for (float z = z0 + 6; z < z1 + 6; z += 12)
                    {
                        int k = rng.Next(10);
                        Top(k < 6 ? "tile1" : "tile3", new Vector3(x, 0.0f, z), 90 * rng.Next(4), Vector3.one);
                    }
            }
            Tile(-70, 250, -64, 0, 1);    // our yard, the beach end and the East Terminal's back lot
            Tile(60, 250, 0, 30, 2);      // East Terminal, south strip
            Tile(60, 245, 30, 120, 3);    // East Terminal yard
            Tile(-245, -55, -10, 230, 4); // West Terminal
            // Hinterland to the horizon behind the port (flat concrete-coloured land, no seams).
            Art.Box(R, new Vector3(0, -0.6f, -700), new Vector3(3000, 1, 1270), Art.Hex("#9a9184"), false, "hinterland");
            Art.Box(R, new Vector3(-900, -0.6f, 300), new Vector3(1300, 1, 900), Art.Hex("#9a9184"), false, "hinterland_w");
        }

        // 12 m quay-wall blocks along every edge that meets the water (top flush with the slabs).
        static void QuayWalls()
        {
            void Edge(Vector3 from, Vector3 to, float yaw)
            {
                var d = to - from; int n = Mathf.Max(1, Mathf.RoundToInt(d.magnitude / 12f));
                for (int i = 0; i < n; i++)
                    Top("quayEdge", from + d * ((i + 0.5f) / n) + Vector3.down * 0.03f, yaw, Vector3.one);
            }
            Edge(new Vector3(-55, 0, -6), new Vector3(-14, 0, -6), 0);       // our quay front, west of the slipway
            Edge(new Vector3(-6, 0, -6), new Vector3(60, 0, -6), 0);         // our quay front, east
            Edge(new Vector3(66, 0, 0), new Vector3(66, 0, 114), 270);       // East Terminal, basin face
            Edge(new Vector3(60, 0, 114), new Vector3(245, 0, 114), 0);      // East Terminal, north face
            Edge(new Vector3(-61, 0, -10), new Vector3(-61, 0, 230), 90);    // West Terminal, basin face
        }

        // The old pier keeps its colliders (World hides its planks); this is what you see: concrete on piles.
        static void Jetty()
        {
            for (float z = 4; z < 32; z += 8)
            {
                Top("slab", new Vector3(0, World.PierTop + 0.02f, z), 0, new Vector3(0.78f, 1, 1));
                foreach (var x in new[] { -2.4f, 2.4f })
                    Top("pile", new Vector3(x, World.PierTop - 0.9f, z), 0, new Vector3(0.22f, 0.4f, 0.22f));
            }
        }

        // Day 1: small and shabby. An old crane, a cabin, the yellow shed behind the shelter, puddles, junk.
        static void OurBerth()
        {
            var crane = On("smallCrane", new Vector3(17, 0, -3), 200, 0.8f);
            if (crane != null) crane.AddComponent<Slew>().Amp = 12f;
            On("whYellow", new Vector3(9, 0, -18), 90, 0.62f, true);
            foreach (var (x, z) in new[] { (-15.5f, -1.5f), (3.5f, -1.5f), (30f, -2f), (-2.6f, 29.5f) }) // also the gulls' perches
                On("streetlight", new Vector3(x, x == -2.6f ? World.PierTop : 0, z), 180, 0.8f);
            var rng = new System.Random(11);
            for (int i = 0; i < 9; i++)
            {
                // Puddles on the cracked concrete.
                var pd = Art.Prim(PrimitiveType.Cylinder, R, new Vector3(-30 + (float)rng.NextDouble() * 60, 0.012f, -40 + (float)rng.NextDouble() * 36), Quaternion.Euler(0, (float)rng.NextDouble() * 180, 0),
                    new Vector3(1.2f + (float)rng.NextDouble() * 2.2f, 0.004f, 0.8f + (float)rng.NextDouble() * 1.4f), Art.Hex("#93a6b2"), false, "puddle"); // light, sky-coloured: dark ones read as holes
                pd.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            On("woodStack", new Vector3(-22, 0, -14), 30, 0.7f, true);
            On("cageBarrels", new Vector3(24, 0, -10), 15, 0.7f, true);
            On("forklift", new Vector3(-26, 0, -20), 140, 1f, true);
            // Shabby but charming: old tyres hung on the quay wall and the jetty as fenders, bollards with rope, junk.
            foreach (var p in new[] { new Vector3(-30, -0.55f, 0.1f), new Vector3(-21, -0.55f, 0.1f), new Vector3(13, -0.55f, 0.1f), new Vector3(25, -0.55f, 0.1f), new Vector3(38, -0.55f, 0.1f),
                                      new Vector3(2.75f, World.PierTop - 0.7f, 10), new Vector3(2.75f, World.PierTop - 0.7f, 22), new Vector3(-2.75f, World.PierTop - 0.7f, 16) })
            {
                bool side = Mathf.Abs(p.x) < 4;
                var ty = Art.Prim(PrimitiveType.Cylinder, R, p + (side ? Vector3.right * Mathf.Sign(p.x) * 0.2f : Vector3.forward * 0.2f), Quaternion.Euler(side ? 0 : 90, 0, side ? 90 : 0), new Vector3(0.85f, 0.16f, 0.85f), Art.Hex("#262626"), false, "tyre");
                Art.Prim(PrimitiveType.Cylinder, R, ty.transform.position + (side ? Vector3.right * Mathf.Sign(p.x) * 0.17f : Vector3.forward * 0.17f), ty.transform.rotation, new Vector3(0.42f, 0.02f, 0.42f), Art.Hex("#3a3a3a"), false, "tyre_hole");
            }
            // World's quay bollards (colliders) wear the pack bollard instead of a grey cylinder.
            foreach (var r in R.parent.GetComponentsInChildren<Renderer>())
                if (r.name == "bollard" && r.enabled && r.transform.position.y < 1f) { r.enabled = false; On("bollard", new Vector3(r.transform.position.x, 0, r.transform.position.z), 0, 1f); }
            On("rope", new Vector3(-24.6f, 0, -1.6f), 40, 1f);
            On("wreck", new Vector3(-40, 0, -14), 115, 0.8f, true);
            On("cableWaste", new Vector3(-34, 0, -22), 30, 1f);
            On("planks", new Vector3(31, 0, -21), 75, 1f, true);
            On("bag", new Vector3(28.5f, 0, -20), 0, 1f);
            On("advertBroken", new Vector3(-24, 0, -57), 90, 0.8f);
            On("powerbox", new Vector3(12.6f, 0, -12.2f), 180, 1f);
            On("epole", new Vector3(-6, 0, -58), 0, 1f);
            for (int i = 0; i < 4; i++) On(i % 2 == 0 ? "pallet" : "crateBig", new Vector3(27 + i * 2.4f, 0, -16 - (i % 2) * 1.6f), i * 25, 1f, true);
            On("metalCrate", new Vector3(36, 0, -27), 10, 1f, true);
            for (int i = 0; i < 4; i++)
            {
                // Rust streaks and oil on the concrete.
                var st = Art.Prim(PrimitiveType.Cylinder, R, new Vector3(-20 + i * 13, 0.014f, -26 + (i % 2) * 9), Quaternion.Euler(0, i * 50, 0), new Vector3(2.4f, 0.004f, 1.2f), i % 2 == 0 ? Art.Hex("#7a4a2c") : Art.Hex("#6e655a"), false, "stain");
                st.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // A sagging, gappy chain-link fence along the back and the east side of our plot.
            Run("fence", new Vector3(-54, 0, -62), new Vector3(44, 0, -62), 5, 9f);
            Run("fence", new Vector3(44, 0, -62), new Vector3(44, 0, -8), 6, -8f);
            // The back of our plot: old sheds, a broken-down truck, a rusty container pile, junk.
            // (Flat-roofed: the pack's lean-to sheds read as tilted boxes from the high cameras.)
            On("office", new Vector3(-44, 0, -72), 0, 1f, true);
            On("whBlue", new Vector3(-8, 0, -74), 0, 0.8f, true);
            On("office2", new Vector3(26, 0, -72), 0, 1f, true);
            On("trailer", new Vector3(-34, 0, -30), 70, 1f, true);
            On("truckB", new Vector3(-27, 0, -42), 200, 1f, true);
            for (int i = 0; i < 7; i++) On("cont" + (i * 3 % 6), new Vector3(-38 + (i % 3) * 3.4f, (i / 3) * 3f, -52), 0, 1f, i < 3);
            for (int i = 0; i < 5; i++) On("woodStack", new Vector3(-16 + i * 4.5f, 0, -46), i * 37, 0.6f, true);
            On("cageBarrels", new Vector3(-8, 0, -32), 60, 0.6f, true);
            On("cageBarrels", new Vector3(-5.5f, 0, -31), 20, 0.6f, true);
            // Our plot ends at a chain-link line of containers; the neighbours' yard starts behind it.
            for (int i = 0; i < 6; i++) On("cont" + (i % 6), new Vector3(47, 0, -50 + i * 6.4f), 0, 1f, true);
        }

        // Neat blocks of 8 bays x 5 rows (6 m boxes end to end), heights in steps of 1-4, straight 12 m lanes between,
        // a rubber-tyred yard crane straddling each block and working along it.
        static void ContainerYard(float x0, float x1, float z0, float z1, int seed, bool colliders)
        {
            var rng = new System.Random(seed);
            const float bay = 6.3f, row = 3.05f, lane = 12f;
            for (float bz = z0; bz + 5 * row <= z1; bz += 5 * row + lane)
                for (float bx = x0; bx + 8 * bay <= x1; bx += 8 * bay + lane)
                {
                    int h0 = 1 + rng.Next(3);
                    for (int b = 0; b < 8; b++)
                    {
                        int hb = Mathf.Clamp(h0 + (b % 3 == 1 ? 1 : 0) - (b == 7 ? 1 : 0), 1, 4);
                        for (int r = 0; r < 5; r++)
                            for (int k = 0; k < hb - (r == 4 && b % 2 == 0 ? 1 : 0); k++)
                                On("cont" + rng.Next(6), new Vector3(bx + (b + 0.5f) * bay, k * 3f, bz + (r + 0.5f) * row), 90, 1f, colliders && k == 0);
                    }
                    var c = On("harbourCrane", new Vector3(bx + 8 * bay * (0.25f + 0.5f * (float)rng.NextDouble()), 0, bz + 2.5f * row), 0, 0.55f);
                    if (c != null) { var m = c.AddComponent<Mover>(); m.Pts = new[] { new Vector3(bx + 8, 0, bz + 2.5f * row), new Vector3(bx + 8 * bay - 8, 0, bz + 2.5f * row) }; m.Speed = 1.2f; m.Face = false; }
                }
        }

        static void EastTerminal()
        {
            ContainerYard(94, 232, -30, 106, 21, true);
            // Ship-to-shore gantries on the basin face, shuttling along their rails.
            foreach (var z in new[] { 45f, 100f })
            {
                var g = On("gantry", new Vector3(80, 0, z), 270, 0.75f);
                if (g != null) { var m = g.AddComponent<Mover>(); m.Pts = new[] { g.transform.position + Vector3.forward * 14, g.transform.position - Vector3.forward * 14 }; m.Speed = 1.2f; m.Face = false; }
            }
            // A loaded freighter alongside the north face, tugs working her, warehouses at the back.
            On("freighter", new Vector3(165, Water.Level - 4f, 150), 90, 0.9f);
            foreach (var (x, z, sp) in new[] { (120f, 175f, 3f), (210f, 180f, 2.4f) })
            {
                var t = On("tug", new Vector3(x, Water.Level - 1.5f, z), 0, 0.45f);
                if (t != null) { var m = t.AddComponent<Mover>(); m.Pts = new[] { new Vector3(x, 0, z), new Vector3(x + 30, 0, z + 18), new Vector3(x + 5, 0, z + 34) }; m.Speed = sp; m.FloatY = Water.Level - 1.5f; }
            }
            int i = 0;
            foreach (var n in new[] { "whBlue", "whWhite", "whRed", "whBig" })
                On(n, new Vector3(100 + i++ * 38, 0, -58), 0, 1f, true);
            for (float z = -40; z < 120; z += 24) On("streetlight", new Vector3(70, 0, z), 90, 1f);
            // Trucks doing laps of the yard.
            for (int t = 0; t < 3; t++)
            {
                var tr = On(t % 2 == 0 ? "truck" : "truckB", new Vector3(86, 0, -42 + t * 40), 0, 1f);
                if (tr != null) { var m = tr.AddComponent<Mover>(); m.Pts = new[] { new Vector3(86, 0, -42), new Vector3(86, 0, 112), new Vector3(238, 0, 112), new Vector3(238, 0, -42) }; m.Index = t; m.Speed = 6f; }
            }
        }

        static void WestTerminal()
        {
            // Warehouses and offices along the back, a smaller container yard, two slewing harbour cranes on the quay.
            string[] sheds = { "whYellow", "whWhite", "whRed", "whGreen", "whBlue", "whYellow", "whWhite" };
            for (int i = 0; i < sheds.Length; i++) On(sheds[i], new Vector3(-210, 0, 10 + i * 30), 90, 1f, true);
            On("office", new Vector3(-170, 0, 214), 180, 1f, true);
            On("office2", new Vector3(-120, 0, 216), 180, 1f, true);
            ContainerYard(-160, -90, 40, 190, 31, true);
            foreach (var z in new[] { 60f, 150f })
            {
                var c = On("harbourCrane", new Vector3(-72, 0, z), 90, 0.8f);
                if (c != null) { var s = c.AddComponent<Slew>(); s.Amp = 35f; s.Period = 50f + z * 0.1f; }
            }
            for (float z = 0; z < 230; z += 24) On("streetlight", new Vector3(-64, 0, z), 270, 1f);
            // Tugs moored at the West Terminal, bobbing.
            foreach (var z in new[] { 20f, 36f })
            {
                var t = On("tugB", new Vector3(-50, Water.Level - 1.5f, z), 0, 0.45f);
                if (t != null) { var m = t.AddComponent<Mover>(); m.Pts = new[] { t.transform.position }; m.FloatY = Water.Level - 1.5f; }
            }
            var tr = On("truck", new Vector3(-80, 0, 10), 0, 1f);
            if (tr != null) { var m = tr.AddComponent<Mover>(); m.Pts = new[] { new Vector3(-80, 0, 10), new Vector3(-80, 0, 205), new Vector3(-180, 0, 205), new Vector3(-180, 0, 10) }; m.Speed = 7f; }
        }

        // Our shelter's walls (World keeps them as colliders) wear the pack's weathered yellow metal panels, both faces.
        static void Shelter(Transform shed)
        {
            if (shed == null || Skin.Get("wallY") == null) return;
            int i = 0;
            foreach (Transform w in shed)
            {
                if (!(w.name.StartsWith("wall") || w.name == "lintel")) continue;
                var r = w.GetComponent<Renderer>(); var b = r.bounds;
                bool alongX = b.size.x > b.size.z;
                float len = alongX ? b.size.x : b.size.z;
                // (The panel is one-sided and we don't know which way it faces: both facings on both sides; the back faces cull.)
                var n = alongX ? Vector3.forward : Vector3.right;
                float ext = alongX ? b.extents.z : b.extents.x;
                string piece = i++ % 3 == 0 ? "wallY2" : "wallY";
                foreach (var side in new[] { 1f, -1f })
                    foreach (var flip in new[] { 0f, 180f })
                    {
                        var g = Art.Place(Skin.Get(piece), R, b.center, (alongX ? 0 : 90) + flip, 1f);
                        if (g == null) return;
                        g.transform.localScale = new Vector3(len / 8f, b.size.y / 3.5f, 1f);
                        g.transform.position += b.center - Art.Bounds(g).center + n * side * (ext + (flip == 0 ? 0.01f : 0.03f));
                    }
                r.enabled = false;
            }
            var o = shed.position + new Vector3(9, 0, -9) - shed.position; // the shelter's centre (World.Shed's origin)
            Top("extinguisher", new Vector3(o.x + 2.3f, 1.7f, o.z + 2.65f), 0, Vector3.one);
            Top("powerbox2", new Vector3(o.x + 3.15f, 2.0f, o.z - 0.8f), 90, Vector3.one);
            Top("lamp", new Vector3(o.x, 3.45f, o.z), 0, Vector3.one);
            // The fish buyer's corner: his stock of empty boxes and a bin bag.
            var by = World.BuyerPos;
            On("boxB", new Vector3(by.x - 2.4f, 0, by.z - 1.2f), 20, 1f);
            On("box", new Vector3(by.x - 2.5f, 0, by.z + 0.1f), -12, 1f);
            On("bag", new Vector3(by.x + 2.1f, 0, by.z - 1.4f), 70, 1f);
            On("box", new Vector3(o.x + 2.2f, 0, o.z - 1.9f), 15, 1f);
            On("boxB", new Vector3(o.x + 2.3f, 0, o.z - 0.9f), -10, 1f);
        }

        // A pack piece stretched to fill a box (centre, size), e.g. our stall's parts.
        static GameObject Fit(string n, Vector3 center, Vector3 size, float yaw = 0)
        {
            var g = Art.Place(Skin.Get(n), R, center, yaw, 1f);
            if (g == null) return null;
            var b = Art.Bounds(g);
            var s = g.transform.localScale;
            var local = Quaternion.Inverse(g.transform.rotation) * b.size; local = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
            var want = Quaternion.Inverse(g.transform.rotation) * size; want = new Vector3(Mathf.Abs(want.x), Mathf.Abs(want.y), Mathf.Abs(want.z));
            g.transform.localScale = new Vector3(s.x * want.x / Mathf.Max(local.x, 0.01f), s.y * want.y / Mathf.Max(local.y, 0.01f), s.z * want.z / Mathf.Max(local.z, 0.01f));
            g.transform.position += center - Art.Bounds(g).center;
            return g;
        }

        // The fish buyer's stall from pack pieces: a counter of big crates, steel poles, a corrugated roof (our table keeps its colliders).
        static void Stall(Transform stall)
        {
            if (stall == null || Skin.Get("crateBig") == null) return;
            var top = stall.Find("table_top");
            if (top == null) return;
            foreach (var r in stall.GetComponentsInChildren<MeshRenderer>())
                if (r.GetComponentInParent<Animator>() == null && (r.name == "table_top" || r.name == "lip" || r.name == "leg" || r.name == "pole" || r.name == "awning")) r.enabled = false;
            var c = top.position; float y = top.GetComponent<Renderer>().bounds.max.y;
            Fit("crateBig", new Vector3(c.x - 0.66f, y / 2, c.z), new Vector3(1.28f, y, 1.36f), 0);
            Fit("crateBig", new Vector3(c.x + 0.66f, y / 2, c.z), new Vector3(1.28f, y, 1.36f), 90);
            foreach (var dx in new[] { -1.45f, 1.45f })
                Fit("mpole", new Vector3(c.x + dx, 1.35f, c.z - 1.35f), new Vector3(0.12f, 2.7f, 0.12f));
            var roof = Fit("roofSheet", new Vector3(c.x, 2.75f, c.z - 0.75f), new Vector3(3.3f, 0.35f, 2.2f));
            if (roof != null) roof.transform.rotation = Quaternion.Euler(-10, 0, 0) * roof.transform.rotation;
        }

        // Road tiles (12 m, lane markings and kerbs) along a polyline, top flush with the slabs.
        static void Road(string tile, params Vector3[] pts)
        {
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                var d = pts[i + 1] - pts[i]; int n = Mathf.Max(1, Mathf.RoundToInt(d.magnitude / 12f));
                float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                // Squashed so the kerbs stand a few cm proud and the asphalt sits just above the ground tiles (full height buried it).
                for (int k = 0; k < n; k++) Top(tile, pts[i] + d * ((k + 0.5f) / n) + Vector3.up * (tile == "rail" ? 0.12f : 0.08f), yaw, new Vector3(1, 0.35f, d.magnitude / n / 12f));
            }
        }

        static void Roads()
        {
            // The trucks' loops, the gantries' rails, the road past our gate with a boom barrier at each terminal.
            Road("road", new Vector3(86, 0, -42), new Vector3(86, 0, 112), new Vector3(238, 0, 112), new Vector3(238, 0, -42), new Vector3(86, 0, -42));
            Road("rail", new Vector3(74, 0, 2), new Vector3(74, 0, 110));
            Road("road", new Vector3(-80, 0, 10), new Vector3(-80, 0, 205), new Vector3(-180, 0, 205), new Vector3(-180, 0, 10), new Vector3(-80, 0, 10));
            Road("road", new Vector3(-180, 0, 2), new Vector3(-66, 0, 2));
            On("barrier", new Vector3(-72, 0, 2), 90, 0.7f);
            On("barrier", new Vector3(86, 0, -36), 0, 0.7f);
            // Oil stains and puddles across the terminals; a mix of concrete tones.
            var rng = new System.Random(17);
            for (int i = 0; i < 70; i++)
            {
                bool east = i % 2 == 0;
                var p = east ? new Vector3(80 + (float)rng.NextDouble() * 160, 0.015f, -50 + (float)rng.NextDouble() * 160) : new Vector3(-235 + (float)rng.NextDouble() * 170, 0.015f, (float)rng.NextDouble() * 220);
                var st = Art.Prim(PrimitiveType.Cylinder, R, p, Quaternion.Euler(0, (float)rng.NextDouble() * 180, 0), new Vector3(1 + (float)rng.NextDouble() * 3, 0.004f, 0.8f + (float)rng.NextDouble() * 2), i % 3 == 0 ? Art.Hex("#93a6b2") : Art.Hex("#6e655a"), false, "stain");
                st.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            for (int i = 0; i < 24; i++)
            {
                var p = i % 2 == 0 ? new Vector3(90 + (float)rng.NextDouble() * 150, 0.008f, -40 + (float)rng.NextDouble() * 150) : new Vector3(-230 + (float)rng.NextDouble() * 160, 0.008f, 5 + (float)rng.NextDouble() * 210);
                var pt = Art.Box(R, p, new Vector3(16, 0.004f, 16), i % 3 == 0 ? Art.Hex("#8c8a86") : Art.Hex("#a39a8c"), false, "patch");
                pt.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static void Industry()
        {
            // A tank farm on the West Terminal (fenced) with a pipe run to the quay on supports.
            for (int i = 0; i < 8; i++) On(i % 3 == 0 ? "tankB" : i % 3 == 1 ? "tank" : "tankC", new Vector3(-150 + (i % 4) * 11, 0, 19 + (i / 4) * 12), 0, 1.7f, true);
            Run("fence", new Vector3(-158, 0, 13), new Vector3(-108, 0, 13));
            Run("fence", new Vector3(-158, 0, 37), new Vector3(-108, 0, 37));
            for (int i = 0; i < 2; i++) { On("pipeSup", new Vector3(-106 + i * 12, 0, 25), 90, 1f); On("pipe", new Vector3(-100 + i * 12, 2.5f, 25), 90, 1f); }
            // A pipe run along the back of the East Terminal.
            for (int i = 0; i < 12; i++) { On("pipeSup", new Vector3(96 + i * 12, 0, -47), 90, 1f); On("pipe", new Vector3(102 + i * 12, 2.5f, -47), 90, 1f); }
            // Light towers and parked kit in the yards.
            var rng = new System.Random(23);
            foreach (var (x, z) in new[] { (98f, 22f), (158f, 22f), (218f, 22f), (98f, 76f), (158f, 76f), (218f, 76f), (-108f, 120f), (-108f, 60f), (-150f, 180f), (-150f, 110f) })
                On("streetlight", new Vector3(x, 0, z), rng.Next(4) * 90, 2f);
            foreach (var (n, x, z, yaw) in new[] { ("trailer", 92f, -36f, 0f), ("trailer", 95.5f, -36f, 0f), ("trailer", 99f, -36f, 0f), ("truck", 232f, 60f, 0f), ("forklift", 128f, -38f, 20f), ("forklift", -96f, 100f, 200f), ("trailer", -96f, 40f, 0f), ("truckB", -168f, 40f, 0f), ("forklift", 200f, 18f, 90f) })
                On(n, new Vector3(x, 0, z), yaw, 1f, true);
            Run("cfence", new Vector3(240, 0, -62), new Vector3(240, 0, 108));
            // The lot between our fence and the East Terminal: trailers parked with boxes on, a cab, a light tower.
            for (int i = 0; i < 5; i++)
            {
                var tr = On("trailer", new Vector3(62 + i * 4.6f, 0, -50), 0, 1f, true);
                if (tr != null && i != 2 && On("cont" + (i + 2) % 6, new Vector3(62 + i * 4.6f, Art.Bounds(tr).max.y - 0.35f, -49), 0, 0.95f) != null)
                    placed.RemoveAt(placed.Count - 1); // riding a trailer, not standing on the ground
            }
            On("truck", new Vector3(80, 0, -24), 200, 1f, true);
            On("streetlight", new Vector3(74, 0, -36), 45, 2f);
            On("cageBarrels", new Vector3(64, 0, -30), 30, 0.8f, true);
        }

        // Dock workers walking their beats in the terminals (pack characters, our CC0 walk cycle), so the harbour feels busy.
        static void Workers()
        {
            if (Skin.I == null || Skin.I.Walk == null) return;
            var beats = new[] {
                new[] { new Vector3(70, 0, -30), new Vector3(70, 0, 105) },            // East quay, under the gantries
                new[] { new Vector3(100, 0, -41), new Vector3(225, 0, -41) },          // East, along the pipe run
                new[] { new Vector3(150, 0, 12), new Vector3(150, 0, 100) },           // East, between the yard blocks
                new[] { new Vector3(-66, 0, 5), new Vector3(-66, 0, 215) },            // West quay
                new[] { new Vector3(-162, 0, 8), new Vector3(-104, 0, 8) },            // West, past the tank farm
                new[] { new Vector3(-95, 0, 45), new Vector3(-95, 0, 185) },           // West yard lane
                new[] { new Vector3(53, 0, -50), new Vector3(53, 0, -10) },            // the neighbours, over our fence
                new[] { new Vector3(-50, 0, -20), new Vector3(-50, 0, -55) },          // the West road gate
            };
            for (int i = 0; i < beats.Length; i++)
            {
                var w = new GameObject("worker" + i).transform;
                w.SetParent(R, false);
                w.position = Vector3.Lerp(beats[i][0], beats[i][1], (i * 0.37f) % 1f);
                var m = Art.Place(Skin.Get("npc" + i), w, Vector3.zero, 180, 1f); // pack people face -Z
                if (m == null) continue;
                var an = m.GetComponentInChildren<Animator>();
                if (an == null) continue;
                var ca = an.gameObject.AddComponent<CharAnim>(); ca.Init(an, Skin.I);
                var mv = w.gameObject.AddComponent<Mover>(); mv.Pts = beats[i]; mv.Speed = 1.3f + (i % 3) * 0.15f; mv.Index = i % 2;
                w.gameObject.AddComponent<Walker>().Set(ca, mv.Speed);
            }
        }

        // Behind the port: rows of city blocks, then hills, fading into the fog.
        static void Hinterland()
        {
            var rng = new System.Random(41);
            for (int r = 0; r < 3; r++)
                for (float x = -420 + r * 14; x < 640; x += 34 + (float)rng.NextDouble() * 12)
                {
                    var b = On("city" + rng.Next(6), new Vector3(x, -0.1f, -120 - r * 46 - (float)rng.NextDouble() * 8), 180, 1.1f + r * 0.25f + (float)rng.NextDouble() * 0.4f);
                    if (b != null) foreach (var rr in b.GetComponentsInChildren<Renderer>()) rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            void Hill(Vector3 c, Vector3 s, string col)
            {
                var h = Art.Prim(PrimitiveType.Sphere, R, c, Quaternion.identity, s, Art.Hex(col), false, "hill");
                h.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            for (int i = 0; i < 12; i++) Hill(new Vector3(-900 + i * 200, -30, -520 - (float)rng.NextDouble() * 160), new Vector3(380, 140 + (float)rng.NextDouble() * 110, 240), i % 2 == 0 ? "#7f8c66" : "#8a9572");
            for (int i = 0; i < 5; i++) Hill(new Vector3(-560 - (i % 2) * 90, -30, -40 + i * 170), new Vector3(300, 110 + i * 15, 220), "#84906a");
        }

        // Tetrapods across the harbour mouth, a gap for the channel, a lighthouse on each pier head.
        static void Breakwater()
        {
            var rng = new System.Random(5);
            void Arm(float x0, float x1, float z)
            {
                for (float x = x0; x < x1; x += 6.5f)
                    for (int k = 0; k < 2; k++)
                    {
                        var t = Art.Place(Skin.Get(k == 0 ? "tetraA" : "tetraB"), R, new Vector3(x + (float)rng.NextDouble() * 3, Water.Level - 0.8f + k * 0.6f, z + (float)rng.NextDouble() * 6 - 3), (float)rng.NextDouble() * 360, 1.4f);
                        if (t != null) t.transform.rotation *= Quaternion.Euler((float)rng.NextDouble() * 40, 0, (float)rng.NextDouble() * 40);
                    }
            }
            Arm(-300, 140, 300);
            Arm(240, 520, 300);
            On("lighthouse", new Vector3(140, Water.Level, 300), 0, 1f);
            On("lighthouse", new Vector3(240, Water.Level, 300), 0, 1f);
        }
    }

    // Moves along a loop of points (trucks, tugs, gantries); FloatY rides the waves instead of the ground.
    public class Mover : MonoBehaviour
    {
        public Vector3[] Pts;
        public float Speed = 4f, FloatY = float.NaN;
        public bool Face = true;
        public int Index;
        float y0;

        void Start() { y0 = transform.position.y; if (Pts != null && Pts.Length > 0) Index %= Pts.Length; }

        void Update()
        {
            var p = transform.position;
            if (Pts != null && Pts.Length > 1)
            {
                var d = Pts[Index] - p; d.y = 0;
                float step = Speed * Time.deltaTime;
                if (d.magnitude <= step) Index = (Index + 1) % Pts.Length;
                else
                {
                    p += d.normalized * step;
                    if (Face) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d.normalized), Time.deltaTime * 2f);
                }
            }
            p.y = float.IsNaN(FloatY) ? y0 : Water.Height(p) + (FloatY - Water.Level) ;
            transform.position = p;
        }
    }

    // Keeps a background worker's walk cycle running.
    public class Walker : MonoBehaviour
    {
        CharAnim a; float speed;
        public void Set(CharAnim anim, float s) { a = anim; speed = s; }
        void Update() { if (a != null) a.Tick(CharAnim.Slot.Walk, speed); }
    }

    // A crane swinging its boom back and forth.
    public class Slew : MonoBehaviour
    {
        public float Amp = 30f, Period = 45f;
        Quaternion b;
        void Start() => b = transform.rotation;
        void Update() => transform.rotation = b * Quaternion.Euler(0, Mathf.Sin(Time.time * 2f * Mathf.PI / Period) * Amp, 0);
    }
}
