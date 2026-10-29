using UnityEngine;

namespace PortBoss
{
    // The harbour (Toon Harbor skin only): our run-down berth on an open coast, and Marlowe's mega port far across the sea.
    // Layout (x east, z out to sea): our fenced yard x -60..90 z -90..0 (World.Yard*) with the concrete jetty (the old
    // pier's colliders), open water either side, a tetrapod breakwater at z ~300 with the channel gap at x 140..240,
    // and beyond it, ~600 m out, Marlowe's port: gantries, container terraces, freighters calling all day (RivalTraffic).
    // Boats come in through the gap (Boat.InPaths), so that corridor stays clear.
    // Ground colliders are World's plain boxes (hidden here); everything visible is pack prefabs.
    public static class HarbourMap
    {
        static Transform R;
        const float MarloweFront = 600f; // z of Marlowe's quay face
        public static Vector3 MarloweSign = new(30, 120, 900); // his name on the HQ roof (the intro and the fireworks aim here)

        public static void Build(Transform root)
        {
            R = new GameObject("Harbour").transform;
            R.SetParent(root, false);
            Ground();
            QuayWalls();
            Jetty();
            OurBerth();
            Coast();
            Breakwater();
            Shelter(root.Find("Shed"));
            BuildCrateStore();
            Market(root.Find("Buyer"));
            Roads();
            Hinterland();
            Check();
            MarlowePort(); // after the check: it's scenery at 600 m, not stood on our ground
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
                // Exactly the plot: n tiles of ~12 m stretched to fit, never past a quay edge (they used to overhang the water).
                var rng = new System.Random(seed);
                int nx = Mathf.CeilToInt((x1 - x0) / 12f), nz = Mathf.CeilToInt((z1 - z0) / 12f);
                float cx = (x1 - x0) / nx, cz = (z1 - z0) / nz;
                for (int i = 0; i < nx; i++)
                    for (int j = 0; j < nz; j++)
                        Top("tile1", new Vector3(x0 + (i + 0.5f) * cx, 0.0f, z0 + (j + 0.5f) * cz), 180 * rng.Next(2), new Vector3(cx / 12f, 1, cz / 12f));
            }
            Tile(World.YardX0, World.YardX1, World.YardZ0, 0, 1); // our yard
            Tile(World.YardX0, World.YardX1, World.RoadZ + 6, World.YardZ0, 5); // pavement between our wall and the road
            Tile(World.YardX0, World.YardX1, World.BackZ0, World.RoadZ - 6, 6); // the market's plaza across the road
            // Hinterland to the horizon behind the port and along the coast either side (flat concrete-coloured land, no seams).
            // (Beyond this strip the generated landscape takes over: Scenery.)
            Art.Box(R, new Vector3((Scenery.FlatX0 + 1700) / 2, -0.6f, Scenery.FlatZ0 / 2), new Vector3(1700 - Scenery.FlatX0, 1, -Scenery.FlatZ0), Art.Hex("#9a9184"), false, "hinterland");
            Scenery.Build(R, Art.Land);
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
            Edge(new Vector3(World.YardX0, 0, -6), new Vector3(-14, 0, -6), 0); // our quay front, west of the slipway
            Edge(new Vector3(-6, 0, -6), new Vector3(World.YardX1, 0, -6), 0);  // our quay front, east
        }

        // The shore either side of our yard: open coast, a tetrapod revetment at the waterline.
        static void Coast()
        {
            var rng = new System.Random(3);
            foreach (var (a, b) in new[] { (-330f, World.YardX0 - 2), (World.YardX1 + 2, 330f) })
                for (float x = a; x < b; x += 5.5f)
                {
                    var t = Art.Place(Skin.Get(rng.Next(2) == 0 ? "tetraA" : "tetraB"), R, new Vector3(x + (float)rng.NextDouble() * 2, Water.Level - 0.4f, 1.6f + (float)rng.NextDouble() * 1.6f), (float)rng.NextDouble() * 360, 1f);
                    if (t != null) t.transform.rotation *= Quaternion.Euler((float)rng.NextDouble() * 40, 0, (float)rng.NextDouble() * 40);
                }
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
            for (int i = 0; i < 14; i++)
            {
                // Puddles on the cracked concrete.
                var pd = Art.Prim(PrimitiveType.Cylinder, R, new Vector3(-50 + (float)rng.NextDouble() * 130, 0.012f, -82 + (float)rng.NextDouble() * 76), Quaternion.Euler(0, (float)rng.NextDouble() * 180, 0),
                    new Vector3(1.2f + (float)rng.NextDouble() * 2.2f, 0.004f, 0.8f + (float)rng.NextDouble() * 1.4f), Art.Hex("#93a6b2"), false, "puddle"); // light, sky-coloured: dark ones read as holes
                pd.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            On("woodStack", new Vector3(-54, 0, -30), 90, 0.7f, true);
            On("cageBarrels", new Vector3(42, 0, -3.5f), 15, 0.7f, true);
            // Shabby but charming: old tyres hung on the quay wall and the jetty as fenders, bollards with rope, junk.
            foreach (var p in new[] { new Vector3(-30, -0.55f, 0.1f), new Vector3(-21, -0.55f, 0.1f), new Vector3(13, -0.55f, 0.1f), new Vector3(25, -0.55f, 0.1f), new Vector3(38, -0.55f, 0.1f),
                                      new Vector3(2.75f, World.PierTop - 0.7f, 10), new Vector3(2.75f, World.PierTop - 0.7f, 22), new Vector3(-2.75f, World.PierTop - 0.7f, 12.5f) })
            {
                bool side = Mathf.Abs(p.x) < 4;
                var ty = Art.Prim(PrimitiveType.Cylinder, R, p + (side ? Vector3.right * Mathf.Sign(p.x) * 0.2f : Vector3.forward * 0.2f), Quaternion.Euler(side ? 0 : 90, 0, side ? 90 : 0), new Vector3(0.85f, 0.16f, 0.85f), Art.Hex("#262626"), false, "tyre");
                Art.Prim(PrimitiveType.Cylinder, R, ty.transform.position + (side ? Vector3.right * Mathf.Sign(p.x) * 0.17f : Vector3.forward * 0.17f), ty.transform.rotation, new Vector3(0.42f, 0.02f, 0.42f), Art.Hex("#3a3a3a"), false, "tyre_hole");
            }
            // World's net pile (a green blob) becomes pack rope piles.
            foreach (var r in R.parent.GetComponentsInChildren<Renderer>())
                if (r.name == "net_pile" && r.enabled)
                {
                    r.enabled = false; var p = r.transform.position;
                    On("ropeB", new Vector3(p.x - 0.4f, 0, p.z), 20, 1.2f);
                    On("rope", new Vector3(p.x + 0.7f, 0, p.z + 0.3f), 140, 1.1f);
                }
            // World's quay bollards (colliders) wear the pack bollard instead of a grey cylinder.
            foreach (var r in R.parent.GetComponentsInChildren<Renderer>())
                if (r.name == "bollard" && r.enabled && r.transform.position.y < 1f) { r.enabled = false; On("bollard", new Vector3(r.transform.position.x, 0, r.transform.position.z), 0, 1f); }
            On("rope", new Vector3(-24.6f, 0, -1.6f), 40, 1f);
            On("powerbox", new Vector3(12.6f, 0, -12.2f), 180, 1f);
            for (int i = 0; i < 6; i++)
            {
                // Rust streaks and oil on the concrete.
                var st = Art.Prim(PrimitiveType.Cylinder, R, new Vector3(-40 + i * 22, 0.014f, -26 - (i % 3) * 18), Quaternion.Euler(0, i * 50, 0), new Vector3(2.4f, 0.004f, 1.2f), i % 2 == 0 ? Art.Hex("#7a4a2c") : Art.Hex("#6e655a"), false, "stain");
                st.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Boundary();
            // Everything else lives along the walls, so the middle of the yard is open ground to build on.
            // West fence: a fisherman's clutter, the wreck, a broken-down truck and an old trailer.
            On("wreck", new Vector3(-54, 0, -12), 100, 0.8f, true);
            On("epole", new Vector3(-57, 0, -8), 0, 1f);
            On("pallet", new Vector3(-53, 0, -20), 10, 1f, true);
            On("crateBig", new Vector3(-53.2f, 0.14f, -20.1f), 25, 0.7f); // riding the pallet (a collider here reads as the pallet floating)
            On("pallet", new Vector3(-55.5f, 0, -23), -15, 1f, true);
            for (int i = 0; i < 3; i++) On(i == 1 ? "gasBarrelB" : "gasBarrel", new Vector3(-56.5f, 0, -36 + i * 0.9f), i * 40, 1f, true);
            On("barrelA", new Vector3(-55, 0, -39), 0, 1f, true);
            On("barrelB", new Vector3(-56, 0, -40.2f), 70, 1f, true);
            On("ropeB", new Vector3(-54, 0, -43), 30, 1f);
            On("cableWaste", new Vector3(-55, 0, -48), 30, 1f);
            On("truckB", new Vector3(-54, 0, -56), 180, 1f, true);
            On("trailer", new Vector3(-54, 0, -66), 0, 1f, true);
            On("plankB", new Vector3(-55, 0, -80), 0, 1f, true);
            // Back fence: our little office in the corner, wood stacks, a rusty container pile, an advert board.
            On("office2", new Vector3(-47, 0, -79), 0, 1f, true);
            On("advertBroken", new Vector3(-30, 0, -87), 90, 0.8f);
            for (int i = 0; i < 4; i++) On("woodStack", new Vector3(14 + i * 5f, 0, -85), 90 + i * 7, 0.6f, true);
            On("epole", new Vector3(-17, 0, -88), 0, 1f);
            for (int i = 0; i < 7; i++) On("cont" + (i * 3 % 6), new Vector3(66 + (i % 3) * 3.4f, (i / 3) * 3f, -84), 0, 1f, i < 3);
            On("cageBarrels", new Vector3(52, 0, -85), 60, 0.6f, true);
            On("cageBarrels", new Vector3(55, 0, -84), 20, 0.6f, true);
            On("planks", new Vector3(40, 0, -86), 90, 1f, true);
            On("cableWasteB", new Vector3(30, 0, -86), 120, 1f);
            // East fence: parked trailers, pallets, a metal crate, junk.
            for (int i = 0; i < 2; i++) On("trailer", new Vector3(84.5f - i * 4.6f, 0, -62), 0, 1f, true);
            for (int i = 0; i < 4; i++) On(i % 2 == 0 ? "pallet" : "crateBig", new Vector3(85 - (i % 2) * 1.6f, 0, -30 - i * 2.4f), i * 25, 1f, true);
            On("metalCrate", new Vector3(85, 0, -44), 10, 1f, true);
            On("planks", new Vector3(85, 0, -16), 0, 1f, true);
            On("bag", new Vector3(83, 0, -18), 0, 1f);
        }

        // Our port's boundary: a solid concrete wall round three sides (World's invisible walls do the stopping),
        // lamp posts along it, and the gate in the back wall: two pillars, a PORT BOSS board over the top, a guard booth.
        static void Boundary()
        {
            float x0 = World.YardX0 + 0.3f, x1 = World.YardX1 - 0.3f, zb = World.YardZ0 + 0.3f, g0 = World.GateX0 - 0.6f, g1 = World.GateX1 + 0.6f;
            void Wall(Vector3 a, Vector3 b) { Run("cfence", a, b); }
            Wall(new Vector3(x0, 0, zb), new Vector3(g0, 0, zb));
            Wall(new Vector3(g1, 0, zb), new Vector3(x1, 0, zb));
            Wall(new Vector3(x0, 0, -1.5f), new Vector3(x0, 0, zb));
            Wall(new Vector3(x1, 0, -1.5f), new Vector3(x1, 0, zb));
            // Lamps along the inside of the wall.
            for (float x = x0 + 12; x < x1 - 6; x += 20) if (x < g0 - 3 || x > g1 + 3) On("streetlight2", new Vector3(x, 0, zb + 1.2f), 0, 1f);
            for (float z = zb + 14; z < -10; z += 20) { On("streetlight2", new Vector3(x0 + 1.2f, 0, z), 90, 1f); On("streetlight2", new Vector3(x1 - 1.2f, 0, z), 270, 1f); }
            // The gate: pillars, the name board over it (painted both sides), a booth for the gateman, lamps.
            var concrete = Art.Hex("#a8a196");
            foreach (var x in new[] { g0, g1 }) Art.Box(R, new Vector3(x, 2.9f, zb), new Vector3(1.2f, 5.8f, 1.2f), concrete, true, "gate_pillar");
            Art.Box(R, new Vector3((g0 + g1) / 2, 6.4f, zb), new Vector3(g1 - g0 + 1.2f, 1.6f, 0.5f), Art.Hex("#1f3346"), false, "gate_board");
            var tex = SignTexture("PORT BOSS", Art.Hex("#1f3346"), Art.Hex("#f2c230"), (g1 - g0 + 1.2f) / 1.6f);
            if (tex != null)
                foreach (var inside in new[] { true, false })
                {
                    var q = Art.Prim(PrimitiveType.Quad, R, new Vector3((g0 + g1) / 2, 6.4f, zb + (inside ? 0.26f : -0.26f)), Quaternion.Euler(0, inside ? 180 : 0, 0), new Vector3(g1 - g0 + 1.1f, 1.5f, 1), Color.white, false, "gate_sign");
                    var mpb = new MaterialPropertyBlock(); mpb.SetTexture("_TextureSample", tex);
                    q.GetComponent<Renderer>().SetPropertyBlock(mpb);
                }
            On("booth", new Vector3(g1 + 4.2f, 0, zb + 4.4f), 0, 1f, true); // windows and lanterns towards the yard
            On("streetlight2", new Vector3(g0 - 1.6f, 0, zb + 1.4f), 0, 1f);
            On("streetlight2", new Vector3(g1 + 1.6f, 0, zb - 1.4f), 180, 1f);
        }

        // The crate store at the foot of the jetty: the pack's orange shelving (stretched so a crate fits between
        // shelves) under a tin roof on poles, a painted CRATE STORE board, odds and ends on the top shelves.
        // Rebuilt as it's upgraded: more racks under a longer roof, then the cold store (chiller unit, frost, mist).
        static Transform store;
        static RenderTexture storeSign;
        public static void BuildCrateStore()
        {
            if (R == null) return;
            if (store != null) Object.Destroy(store.gameObject);
            if (storeSign != null) { storeSign.Release(); Object.Destroy(storeSign); } // the old sign's texture goes with it
            var keep = R;
            store = R = new GameObject("CrateStoreDressing").transform; R.SetParent(keep, false);
            bool cold = Storage.Cold;
            for (int k = 0; k < Storage.RackCount; k++)
            {
                var c = Storage.Rack(k);
                var s = Art.Place(Skin.Get("shelves"), R, c + new Vector3(0, 0, -0.05f), 0, 1f);
                if (s != null) s.transform.localScale = new Vector3(1.15f, 1.4f, 1.15f);
                for (int i = 0; i < 3; i++) Top(i % 2 == 0 ? "box" : "boxB", c + new Vector3(-0.8f + i * 0.8f, 2.1f + 0.35f, -0.1f), i * 30 + k * 50, Vector3.one); // the 4th shelf
                if (cold) Art.Box(R, c + new Vector3(0, 3.02f, -0.05f), new Vector3(Storage.RackW - 0.1f, 0.06f, Storage.RackD - 0.1f), Art.Hex("#eef7ff"), false, "frost");
            }
            var ctr = Storage.Center; float w = Storage.MaxX - Storage.MinX + 1.2f, front = ctr.z + 1.1f, back = ctr.z - 1.2f;
            int bays = Mathf.CeilToInt(w / 6.5f);
            for (int i = 0; i <= bays; i++)
                foreach (var z in new[] { front, back })
                    Fit("mpole", new Vector3(ctr.x - w / 2 + w * i / bays, 1.95f, z), new Vector3(0.14f, 3.9f, 0.14f));
            var roof = Fit("roofSheet", new Vector3(ctr.x, 4.0f, ctr.z - 0.05f), new Vector3(w + 0.6f, 0.3f, 3f));
            if (roof != null) roof.transform.rotation = Quaternion.Euler(8, 0, 0) * roof.transform.rotation;
            Color board = cold ? Art.Hex("#3f8fb5") : Art.Hex("#2f5d6e");
            Art.Box(R, new Vector3(ctr.x, 4.55f, front + 0.25f), new Vector3(3.6f, 0.6f, 0.06f), board, false, "store_sign");
            var tex = storeSign = SignTexture(cold ? "COLD STORE" : "CRATE STORE", board, new Color(1f, 0.97f, 0.9f), 3.6f / 0.6f);
            if (tex != null)
                foreach (var fr in new[] { true, false })
                {
                    var q = Art.Prim(PrimitiveType.Quad, R, new Vector3(ctr.x, 4.55f, front + (fr ? 0.29f : 0.21f)), Quaternion.Euler(0, fr ? 180 : 0, 0), new Vector3(3.56f, 0.57f, 1), Color.white, false, "store_sign_face");
                    var mpb = new MaterialPropertyBlock(); mpb.SetTexture("_TextureSample", tex);
                    q.GetComponent<Renderer>().SetPropertyBlock(mpb);
                }
            if (cold) ColdStore(ctr, w, front);
            R = keep;
        }

        // The cold store's kit: a big white chiller unit at the west end (fan grille, blue light, a pipe up to the
        // roof), icicles along the roof edge, and a low cold mist drifting out over the floor.
        static void ColdStore(Vector3 ctr, float w, float front)
        {
            var u = new Vector3(Storage.MinX - 1.55f, 0, ctr.z - 0.1f); // clear of the roof's corner pole
            Art.Box(R, u + new Vector3(0, 0.9f, 0), new Vector3(1.3f, 1.8f, 1.5f), Art.Hex("#e9eef2"), true, "chiller");
            Art.Box(R, u + new Vector3(0, 1.82f, 0), new Vector3(1.38f, 0.08f, 1.58f), Art.Hex("#9fb3c2"), false, "chiller_lid");
            Art.Box(R, u + new Vector3(0, 0.12f, 0), new Vector3(1.4f, 0.24f, 1.6f), Art.Hex("#4b5d6b"), false, "chiller_base");
            Art.Prim(PrimitiveType.Cylinder, R, u + new Vector3(0, 1.0f, 0.76f), Quaternion.Euler(90, 0, 0), new Vector3(1.0f, 0.02f, 1.0f), Art.Hex("#3a4650"), false, "chiller_fan");
            Art.Prim(PrimitiveType.Cylinder, R, u + new Vector3(0, 1.0f, 0.78f), Quaternion.Euler(90, 0, 0), new Vector3(0.25f, 0.02f, 0.25f), Art.Hex("#c9d3da"), false, "chiller_hub");
            for (int i = 0; i < 3; i++)
                Art.Box(R, u + new Vector3(0, 1.0f, 0.79f), new Vector3(0.9f, 0.08f, 0.01f), Art.Hex("#c9d3da"), false, "chiller_blade").transform.localRotation = Quaternion.Euler(0, 0, i * 60);
            Art.Box(R, u + new Vector3(0.45f, 1.55f, 0.76f), new Vector3(0.14f, 0.14f, 0.02f), Art.Hex("#7fd4ff"), false, "chiller_light", 0.9f);
            Art.Prim(PrimitiveType.Cylinder, R, u + new Vector3(0.5f, 2.9f, -0.4f), Quaternion.identity, new Vector3(0.14f, 1.1f, 0.14f), Art.Hex("#b8c4cc"), false, "chiller_pipe");
            for (float x = ctr.x - w / 2; x <= ctr.x + w / 2; x += 0.45f)
            {
                float len = 0.12f + 0.16f * Mathf.PerlinNoise(x * 1.7f, 3.1f);
                Art.Box(R, new Vector3(x, 3.66f - len / 2, front + 0.3f), new Vector3(0.06f, len, 0.06f), Art.Hex("#e8f6ff"), false, "icicle");
            }
            // Cold mist: slow, low and faint.
            var g = new GameObject("cold_mist"); g.transform.SetParent(R, false); g.transform.position = new Vector3(ctr.x, 0.15f, ctr.z + 0.2f);
            var ps = g.AddComponent<ParticleSystem>(); ps.Stop();
            var m = ps.main; m.loop = true; m.startLifetime = 5f; m.startSpeed = 0.15f; m.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            m.startColor = new Color(0.95f, 0.98f, 1f, 0.12f); m.maxParticles = 60; m.simulationSpace = ParticleSystemSimulationSpace.World;
            var e = ps.emission; e.rateOverTime = 8f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(w - 1f, 0.1f, 1.2f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f); vel.y = new ParticleSystem.MinMaxCurve(0.02f, 0.06f); vel.z = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient(); grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(0, 1) });
            col.color = grad;
            var r = g.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Art.Particle; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
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

        // Marlowe's mega port, ~600 m out across the sea: the giant on the horizon, what our berth could become.
        // Not reachable (no colliders) and drawn as silhouettes (no shadows at this range). A row of ship-to-shore
        // gantries over the quay, freighters alongside, container terraces, warehouses, tanks, his HQ and a big sign;
        // more freighters keep calling all day (RivalTraffic), each one an "order" he brags about.
        static void MarlowePort()
        {
            var keep = R;
            R = new GameObject("MarlowePort").transform; R.SetParent(keep, false);
            const float f = MarloweFront;
            Art.Box(R, new Vector3(-20, -0.6f, f + 250), new Vector3(960, 1, 500), Art.Hex("#a39a8c"), false, "marlowe_land");
            Art.Box(R, new Vector3(-20, -1.4f, f + 0.5f), new Vector3(960, 2.2f, 1), Art.Hex("#8e887e"), false, "marlowe_quay_face");
            // Ship-to-shore gantries, booms out over the water, shuttling along the quay.
            for (int i = 0; i < 7; i++)
            {
                var g = On(i % 2 == 0 ? "gantry" : "gantryB", new Vector3(-390 + i * 120, 0, f + 22), 180, 1f);
                if (g != null) { var m = g.AddComponent<Mover>(); m.Pts = new[] { g.transform.position + Vector3.right * 18, g.transform.position - Vector3.right * 18 }; m.Speed = 1.5f + i % 3 * 0.3f; m.Face = false; }
            }
            // Two big freighters alongside for good (one loaded to the gunwales), tugs fussing round them.
            // (Berths 240 m apart: a freighter is 230 m long. The middle two are for the callers.)
            On("freighterL", new Vector3(-360, Water.Level - 7.2f, f - 22), 90, 1f);
            On("freighter09", new Vector3(360, Water.Level - 7.2f, f - 22), 270, 1f);
            foreach (var x in new[] { -250f, 250f })
            {
                var t = On("tug", new Vector3(x, Water.Level - 1.5f, f - 55), 0, 1f);
                if (t != null) { var m = t.AddComponent<Mover>(); m.Pts = new[] { new Vector3(x, 0, f - 55), new Vector3(x + 40, 0, f - 75), new Vector3(x - 20, 0, f - 85) }; m.Speed = 4f; m.FloatY = Water.Level - 1.5f; }
            }
            // Container terraces behind the quay, yard cranes working them.
            ContainerYard(-300, 260, f + 70, f + 130, 21, false);
            // Warehouses, a tank farm, light towers, and Marlowe's HQ towering over it all with his name on top.
            string[] sheds = { "whBig", "whBlue", "whRed", "whBig", "whWhite", "whGreen", "whBig", "whBlue" };
            for (int i = 0; i < sheds.Length; i++) On(sheds[i], new Vector3(-420 + i * 110, 0, f + 210), 90, 1.4f);
            for (int i = 0; i < 12; i++) On(i % 3 == 0 ? "tankB" : i % 3 == 1 ? "tank" : "tankC", new Vector3(330 + (i % 4) * 14, 0, f + 80 + (i / 4) * 16), 0, 3f);
            for (int i = 0; i < 6; i++) On("streetlight", new Vector3(-360 + i * 140, 0, f + 50), 180, 3f);
            // HQ between two gantries, taller than all of them, so his name is the first thing you see out to sea.
            const float hqX = 30;
            for (int i = 0; i < 9; i++) if (Mathf.Abs(-400 + i * 100 - hqX) > 80) On("city" + i % 6, new Vector3(-400 + i * 100, 0, f + 330 + i % 2 * 40), 180, 2.6f + i % 3 * 0.6f);
            var hq = On("city2", new Vector3(hqX, 0, f + 310), 180, 5.5f);
            if (hq != null)
            {
                // His name on the roof's front edge, facing us across the sea.
                var hb = Art.Bounds(hq);
                float top = hb.max.y, front = hb.min.z + 4;
                MarloweSign = new Vector3(hqX, top + 9.5f, front);
                var tex = SignTexture("MARLOWE PORT", Art.Hex("#b5402f"), new Color(1f, 0.95f, 0.85f), 5.5f, true);
                Art.Box(R, new Vector3(hqX, top + 9.5f, front), new Vector3(104, 19, 1.5f), Art.Hex("#b5402f"), false, "marlowe_sign");
                if (tex != null)
                {
                    var q = Art.Prim(PrimitiveType.Quad, R, new Vector3(hqX, top + 9.5f, front - 0.8f), Quaternion.identity, new Vector3(103, 18.5f, 1), Color.white, false, "marlowe_sign_face");
                    var mpb = new MaterialPropertyBlock(); mpb.SetTexture("_TextureSample", tex);
                    q.GetComponent<Renderer>().SetPropertyBlock(mpb);
                }
            }
            On("lighthouse", new Vector3(-500, 0, f - 4), 0, 1.5f);
            // Freighters calling all day: in from the east, alongside, out to the west loaded.
            var traffic = R.gameObject.AddComponent<RivalTraffic>();
            traffic.Init(R, f - 22, new[] { -120f, 120f }, new[] { ("freighter01", "freighterL"), ("freighter05", "freighter05L") });
            foreach (var r in R.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            placed.Clear();
            R = keep;
        }

        // Port stage 2 (bought in the shop): a proper metal shed and a marked forklift yard on our plot.
        static GameObject stage2;
        public static void SetStage(int s)
        {
            if (s < 2) { if (stage2 != null) Object.Destroy(stage2); stage2 = null; return; } // a new session starts at stage 1
            if (stage2 != null || R == null || !Skin.On) return;
            stage2 = new GameObject("Stage2");
            Debug.Log("[PB] stage 2 built: shed + forklift yard");
            var keep = R; R = stage2.transform; R.SetParent(keep, false);
            On("shed07", new Vector3(30, 0, -39), 180, 0.8f, true);
            for (int i = 0; i < 5; i++) // yellow bay lines
                Art.Box(R, new Vector3(18 + i * 3.2f, 0.02f, -27), new Vector3(0.15f, 0.01f, 5f), Art.Hex("#e8b030"), false, "bay_line").GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Art.Box(R, new Vector3(24.4f, 0.02f, -24.5f), new Vector3(12.95f, 0.01f, 0.15f), Art.Hex("#e8b030"), false, "bay_line");
            On("streetlight", new Vector3(36, 0, -30), 200, 1.4f);
            placed.Clear();
            R = keep;
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
            // A corrugated pack roof sheet instead of our flat slab (the slab keeps its collider).
            var roofBox = shed.Find("roof");
            if (roofBox != null)
            {
                var rb = roofBox.GetComponent<Renderer>().bounds;
                var sheet = Fit("roofSheet", rb.center + Vector3.up * 0.08f, new Vector3(rb.size.x + 0.3f, 0.3f, rb.size.z + 0.3f));
                if (sheet != null) { sheet.transform.rotation = roofBox.rotation * sheet.transform.rotation; roofBox.GetComponent<Renderer>().enabled = false; }
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

        // Paint text on a coloured board into a texture (off-stage ortho camera, rendered once).
        public static RenderTexture SignTexture(string text, Color bg, Color fg, float aspect, bool mips = false)
        {
            var font = GameUI.I != null ? GameUI.I.TitleFont : null;
            if (font == null) return null;
            const int layer = 30;
            var rig = new GameObject("signRig").transform; rig.position = new Vector3(0, -600, 0);
            var t = new GameObject("text").transform; t.SetParent(rig, false); t.gameObject.layer = layer;
            var tm = t.gameObject.AddComponent<TextMesh>();
            tm.font = font; tm.fontSize = 96; tm.characterSize = 0.1f; tm.anchor = TextAnchor.MiddleCenter; tm.text = text; tm.color = fg;
            var mr = t.GetComponent<MeshRenderer>(); mr.sharedMaterial = font.material;
            t.localScale = Vector3.one * (aspect * 0.86f / Mathf.Max(0.01f, mr.bounds.size.x));
            var cam = new GameObject("signCam").AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 0.5f; cam.aspect = aspect;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = bg; cam.cullingMask = 1 << layer;
            cam.transform.position = rig.position - Vector3.forward * 5f;
            var rt = new RenderTexture(1024, Mathf.RoundToInt(1024 / aspect), 16) { antiAliasing = 4 };
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
            // Immediately: a second sign painted this frame would otherwise get this one's lettering too.
            Object.DestroyImmediate(cam.gameObject); Object.DestroyImmediate(rig.gameObject);
            if (!mips) return rt;
            // Seen from far off: resolve into a mipmapped copy so the lettering stays legible instead of shimmering.
            var m = new RenderTexture(rt.width, rt.height, 0) { useMipMap = true, autoGenerateMips = true, anisoLevel = 8 };
            Graphics.Blit(rt, m);
            rt.Release(); Object.Destroy(rt);
            return m;
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

        // A painted board: lettering rendered once into a texture (the font shader ignores depth, so a TextMesh would
        // show through the board and walls), on a quad on each face. Facing +z unless turned.
        static void Board(string text, Vector3 c, float w, float h, Color bg, Color fg, float yaw = 0)
        {
            var rot = Quaternion.Euler(0, yaw, 0);
            Art.Box(R, c, new Vector3(w, h, 0.06f), bg, false, "board").transform.rotation = rot;
            var tex = SignTexture(text, bg, fg, w / h);
            if (tex == null) return;
            foreach (var front in new[] { true, false })
            {
                var q = Art.Prim(PrimitiveType.Quad, R, c + rot * new Vector3(0, 0, front ? 0.035f : -0.035f), rot * Quaternion.Euler(0, front ? 180 : 0, 0), new Vector3(w - 0.04f, h - 0.03f, 1), Color.white, false, "board_face");
                var mpb = new MaterialPropertyBlock(); mpb.SetTexture("_TextureSample", tex);
                q.GetComponent<Renderer>().SetPropertyBlock(mpb);
            }
        }

        // The fish market across the road: an open market hall with a red roof; the buyer behind a counter of crates
        // with fish laid out on ice; fridges and shelves at the back; his prices on a chalkboard; a FISH MARKET
        // sign over the way in from the road; a couple of customers about. (Our table keeps its colliders.)
        static void Market(Transform stall)
        {
            if (stall == null || Skin.Get("crateBig") == null) return;
            var top = stall.Find("table_top");
            if (top == null) return;
            foreach (var r in stall.GetComponentsInChildren<MeshRenderer>())
                if (r.GetComponentInParent<Animator>() == null && (r.name == "table_top" || r.name == "lip" || r.name == "leg" || r.name == "pole" || r.name == "awning")) r.enabled = false;
            var c = top.position; float y = top.GetComponent<Renderer>().bounds.max.y;
            var rng = new System.Random(19);
            // The hall: long side along the road, open front just behind the counter.
            On("marketHall", new Vector3(c.x, 0, c.z - 5.6f), 90, 0.85f, true);
            // The counter, and a display either side, each topped with fish on beds of ice.
            Fit("crateBig", new Vector3(c.x - 0.66f, y / 2, c.z), new Vector3(1.28f, y, 1.36f), 0);
            Fit("crateBig", new Vector3(c.x + 0.66f, y / 2, c.z), new Vector3(1.28f, y, 1.36f), 90);
            foreach (var dx in new[] { -2.4f, 2.4f })
            {
                Fit("crateBig", new Vector3(c.x + dx, 0.45f, c.z - 0.2f), new Vector3(1.6f, 0.9f, 1.1f), dx < 0 ? 0 : 90);
                for (int i = 0; i < 3; i++) CrateLook.Fish(Spot(new Vector3(c.x + dx - 0.5f + i * 0.5f, 0.9f, c.z - 0.2f)), 0.55f, 0.03f, 0.48f, rng);
            }
            for (int i = 0; i < 4; i++) CrateLook.Fish(Spot(new Vector3(c.x - 0.9f + i * 0.6f, y, c.z + 0.15f)), 0.55f, 0.03f, 0.5f, rng);
            Board("FRESH FISH", new Vector3(c.x, 3.05f, c.z + 0.42f), 2.6f, 0.55f, Art.Hex("#2f5d6e"), new Color(1f, 0.93f, 0.8f));
            // Back of the hall: fridges, shelves of boxes, a bin.
            for (int i = 0; i < 3; i++) On("fridge", new Vector3(c.x - 3.4f + i * 1.1f, 0, c.z - 9.4f), 0, 1f, true);
            On("shelves", new Vector3(c.x + 2.6f, 0, c.z - 9.6f), 0, 1f, true);
            for (int i = 0; i < 3; i++) Top(i % 2 == 0 ? "box" : "boxB", new Vector3(c.x + 1.9f + i * 0.7f, 1.35f, c.z - 9.6f), i * 25, Vector3.one);
            On("trashcan", new Vector3(c.x + 4.2f, 0, c.z - 0.8f), 0, 1f, true);
            // His prices, chalked up on the pavement, facing the road.
            var chalk = new Vector3(c.x + 4.4f, 0, c.z + 2.4f);
            Fit("mpole", chalk + new Vector3(-0.8f, 0.7f, 0), new Vector3(0.07f, 1.4f, 0.07f));
            Fit("mpole", chalk + new Vector3(0.8f, 0.7f, 0), new Vector3(0.07f, 1.4f, 0.07f));
            Board($"PRICES TODAY\nFresh   ${Crate.PriceFresh}\nOkay   ${Crate.PriceOkay}\nSpoiled   ${Crate.PriceSpoiled}", chalk + new Vector3(0, 1.55f, 0), 1.9f, 1.15f, Art.Hex("#1f4d36"), new Color(0.95f, 0.95f, 0.9f));
            // FISH MARKET over the way in from the road.
            var arch = new Vector3(c.x, 0, World.RoadEdgeSouth - 1.2f);
            foreach (var dx in new[] { -4.2f, 4.2f }) Fit("mpole", arch + new Vector3(dx, 2.9f, 0), new Vector3(0.18f, 5.8f, 0.18f));
            Board("FISH MARKET", arch + new Vector3(0, 5.4f, 0), 8.8f, 1.4f, Art.Hex("#b5402f"), new Color(1f, 0.95f, 0.85f));
            On("streetlight2", new Vector3(c.x - 7.5f, 0, c.z + 3.5f), 0, 1f);
            On("streetlight2", new Vector3(c.x + 7.5f, 0, c.z + 3.5f), 0, 1f);
            // Customers browsing (idle; a CharAnim starts on its idle clip).
            foreach (var (n, dx, dz, yaw) in new[] { ("npc2", -2.6f, 1.6f, 10f), ("npc5", 3.0f, 1.7f, -20f), ("npc7", -5.5f, 3.6f, 60f) })
            {
                var m = Art.Place(Skin.Get(n), R, new Vector3(c.x + dx, 0, c.z + dz), yaw, 1f);
                var an = m != null ? m.GetComponentInChildren<Animator>() : null;
                if (an != null) an.gameObject.AddComponent<CharAnim>().Init(an, Skin.I);
            }
        }

        static Transform Spot(Vector3 p) { var t = new GameObject("display").transform; t.SetParent(R, false); t.position = p; return t; }

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
            // The road past our back gate (between us and the fish market), lorries going by both ways.
            Road("road", new Vector3(-RoadTraffic.Half, 0, World.RoadZ), new Vector3(RoadTraffic.Half, 0, World.RoadZ));
            var traffic = new GameObject("RoadTraffic").AddComponent<RoadTraffic>();
            traffic.transform.SetParent(R, false);
            for (int t = 0; t < 14; t++)
            {
                // A cab pulling a trailer (some loaded with a container), built round the hitch, facing +z.
                var rig = new GameObject("lorry" + t).transform;
                rig.SetParent(traffic.transform, false);
                Art.Place(Skin.Get(t % 2 == 0 ? "truck" : "truckB"), rig, Vector3.zero, 0, 1f);
                var trailer = Art.Place(Skin.Get("trailer"), rig, Vector3.zero, 0, 1f);
                if (trailer != null && t % 3 != 2) Art.Place(Skin.Get("cont" + t % 6), rig, new Vector3(0, Art.Bounds(trailer).max.y - 0.35f, -4.6f), 0, 0.95f);
                // Seven a lane spread round the loop, the same layout on every peer; each peer drives them on its own clock until the host's first sync (TrafficRpc) snaps them in step.
                traffic.Add(rig, t % 2 == 0 ? 1 : -1, t / 2 * RoadTraffic.Half * 2 / 7f + t * 11f, t % 2 == 0 ? 10f : 9f);
            }
        }

        // Behind the port: rows of city blocks (the countryside and the mountains beyond are Scenery's).
        static void Hinterland()
        {
            var rng = new System.Random(41);
            for (int r = 0; r < 3; r++)
                for (float x = Scenery.FlatX0 + 12 + r * 14; x < 640; x += 34 + (float)rng.NextDouble() * 12)
                {
                    var b = On("city" + rng.Next(6), new Vector3(x, -0.1f, -150 - r * 46 - (float)rng.NextDouble() * 8), 180, 1.1f + r * 0.25f + (float)rng.NextDouble() * 0.4f);
                    if (b != null) foreach (var rr in b.GetComponentsInChildren<Renderer>()) rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
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

    // Moves along a loop of points (tugs, gantries, yard cranes; lorries are RoadTraffic); FloatY rides the waves instead of the ground.
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

    // Lorries on the road behind our gate. Every peer drives them (start points fixed per session) and the host
    // sends its positions twice a second, so everyone sees each one in the same place. Drive on the right; they brake
    // for anything in their lane (people, the trolley, the forklift, crates) and lean on the horn while they wait,
    // keep their distance from the lorry in front, and no more than MaxQueue line up behind a road-blocker:
    // the rest wait off the end of the road until the jam clears.
    public class RoadTraffic : MonoBehaviour
    {
        public const float Half = 600f, Lane = 3f; // road half-length; lanes either side of the centre line
        public const int MaxQueue = 5;
        const float Len = Half * 2, Gap = 3.5f, Brake = 9f, Pull = 2.6f, Width = 2.1f;
        struct Lorry { public Transform t; public float dir, s, v, cruise, front, back, hornAt, corr; public bool held; }
        static readonly System.Collections.Generic.List<Lorry> lorries = new();
        static readonly System.Collections.Generic.List<(float s, float dir, bool person)> blockers = new();
        float lastT = -1, sendAt, moanAt;

        void Awake() => lorries.Clear(); // a fresh harbour (new session)

        public void Add(Transform rig, float dir, float s, float speed)
        {
            var b = Art.Bounds(rig.gameObject);
            var local = rig.InverseTransformPoint(b.max).z; // the cab's nose (built facing +z round the hitch)
            float front = Mathf.Max(5f, local) + 0.4f, back = Mathf.Max(10f, b.size.z - local) + 0.3f;
            // Solid, so nobody walks through a waiting lorry (kinematic: we move it, not physics).
            var box = rig.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0, 1.8f, (front - back) / 2f); box.size = new Vector3(2.5f, 3.6f, front + back - 0.6f);
            rig.gameObject.AddComponent<Rigidbody>().isKinematic = true;
            lorries.Add(new Lorry { t = rig, dir = dir, s = Mathf.Repeat(s, Len), v = speed, cruise = speed, front = front, back = back });
            Place(lorries[^1]);
        }

        static float LaneZ(float dir) => World.RoadZ - dir * Lane; // eastbound keeps to the south side
        static float X(float dir, float s) => dir > 0 ? -Half + s : Half - s;
        static float S(float dir, float x) => dir > 0 ? x + Half : Half - x;

        static void Place(Lorry l)
        {
            if (l.t.gameObject.activeSelf == l.held) l.t.gameObject.SetActive(!l.held);
            l.t.SetPositionAndRotation(new Vector3(X(l.dir, l.s), 0, LaneZ(l.dir)), Quaternion.Euler(0, l.dir > 0 ? 90 : 270, 0));
        }

        static void Blocker(Vector3 p, bool person)
        {
            if (p.y > 2.5f) return;
            for (int d = -1; d <= 1; d += 2)
                if (Mathf.Abs(p.z - LaneZ(d)) < Width) blockers.Add((S(d, p.x), d, person));
        }

        void Update()
        {
            float now = Water.T, dt = lastT < 0 ? 0 : Mathf.Clamp(now - lastT, 0, 0.1f);
            lastT = now;
            blockers.Clear();
            foreach (var p in Player.All) if (p != null) Blocker(p.transform.position, true);
            if (Trolley.I != null) Blocker(Trolley.I.transform.position, false);
            if (Forklift.I != null) Blocker(Forklift.I.transform.position, false);
            foreach (var c in Crate.All)
                if (c != null && !c.Attached.Value && !c.Stored && Mathf.Abs(c.transform.position.z - World.RoadZ) < 7f) Blocker(c.transform.position, false);

            for (int i = 0; i < lorries.Count; i++)
            {
                var l = lorries[i];
                if (l.t == null) continue;
                // Room ahead: to the nearest thing in the lane in front of the nose (someone beside the trailer is not in
                // the way: it's solid, they can't step into it, and stopping would park it across their crossing),
                // and to the tail of the lorry in front (by hitch distance round the loop; ties go by index).
                float room = float.MaxValue; bool person = false;
                foreach (var b in blockers)
                {
                    if (b.dir != l.dir) continue;
                    float d = b.s - (l.s + l.front);
                    if (d > -0.6f && d < room) { room = Mathf.Max(0, d); person = b.person; }
                }
                for (int j = 0; j < lorries.Count; j++)
                {
                    var o = lorries[j];
                    if (j == i || o.dir != l.dir || o.held) continue;
                    float d = Mathf.Repeat(o.s - l.s, Len);
                    if (d == 0 && j > i) continue;
                    float r = d - o.back - l.front;
                    if (r < room) { room = r; person = false; }
                }
                if (l.held)
                {
                    // Waiting off the end of the road: come on once the jam has room and the entry is clear.
                    if (QueuedBehindBlock(l.dir) < MaxQueue && room > Gap * 4) l.held = false;
                    else { l.v = 0; lorries[i] = l; Place(l); continue; }
                }
                float want = Mathf.Min(l.cruise, Mathf.Sqrt(2f * Brake * 0.6f * Mathf.Max(0, room - Gap)));
                l.v = Mathf.MoveTowards(l.v, want, (want < l.v ? (room < 8f ? Brake * 3f : Brake) : Pull) * dt); // somebody stepped out: stand on it
                if (l.corr != 0) { float k = Mathf.Min(1f, dt * 3f); l.s += l.corr * k; l.corr -= l.corr * k; }
                l.s += l.v * dt;
                if (l.s >= Len)
                {
                    l.s -= Len;
                    if (QueuedBehindBlock(l.dir) >= MaxQueue) { l.held = true; l.s = 0; }
                }
                // Stopped for somebody: lean on the horn now and then.
                if (person && l.v < 1.5f && room < 12f && Time.time > l.hornAt)
                {
                    l.hornAt = Time.time + Random.Range(2.5f, 4.5f);
                    Sfx.Play("horn", l.t.position + Vector3.up * 2, 0.9f, Random.Range(1.55f, 1.85f));
                    var me = Player.Local;
                    if (me != null && Time.time > moanAt && Vector3.Distance(me.transform.position, l.t.position) < l.front + 8f)
                    {
                        moanAt = Time.time + 9f;
                        Stats.Add("honked");
                        GameUI.Toast(PortGame.Pick("BEEP BEEP! \"Some of us have deliveries, mate!\"", "HONK! \"Off the road, fish boy!\"",
                            "HOOONK! \"I'm on a schedule here!\"", "BEEEP! \"Lovely day for standing in the road, is it?\""));
                    }
                }
                lorries[i] = l;
                Place(l);
            }

            // Host: everyone else follows our lorries.
            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (nm != null && nm.IsServer && nm.ConnectedClientsIds.Count > 1 && PortGame.I != null && Time.time > sendAt)
            {
                sendAt = Time.time + 0.5f;
                var s = new float[lorries.Count]; var v = new float[lorries.Count];
                for (int i = 0; i < lorries.Count; i++) { s[i] = lorries[i].held ? -1 : lorries[i].s; v[i] = lorries[i].v; }
                PortGame.I.TrafficRpc(now, s, v);
            }
        }

        // How many lorries in this lane are on their way to (or stuck behind) something in the road.
        static int QueuedBehindBlock(float dir)
        {
            float far = -1;
            foreach (var b in blockers) if (b.dir == dir && b.s > far) far = b.s;
            if (far < 0) return 0;
            int n = 0;
            foreach (var l in lorries) if (l.dir == dir && !l.held && l.s < far) n++;
            return n;
        }

        // Client: the host's lorries as of server time t (catch up smoothly; jump if far out).
        static float lastSyncT = -1;
        public static void Sync(float t, float[] s, float[] v)
        {
            // Unreliable: a late, older packet would pull the lorries back (and the next one push them on again).
            if (t <= lastSyncT && lastSyncT - t < 1f) return; // (a big jump back = a new session)
            lastSyncT = t;
            float lag = Mathf.Clamp(Water.T - t, 0, 1f);
            for (int i = 0; i < lorries.Count && i < s.Length; i++)
            {
                var l = lorries[i];
                if (s[i] < 0) { l.held = true; l.s = 0; l.v = 0; l.corr = 0; }
                else
                {
                    l.held = false; l.v = v[i];
                    float d = Mathf.Repeat(s[i] + v[i] * lag - l.s + Len / 2, Len) - Len / 2; // shortest way round the loop
                    if (Mathf.Abs(d) > 8f) { l.s = Mathf.Repeat(l.s + d, Len); l.corr = 0; } else l.corr = d;
                }
                lorries[i] = l;
            }
        }

        // Test hooks: a spot in some lorry's lane this far ahead of its nose, on the stretch we can walk (null if none),
        // and how that lane is doing (stopped = standing still within 120 m behind the spot; held = waiting off the road).
        public static Vector3? DebugAhead(float metres)
        {
            foreach (var l in lorries)
            {
                if (l.held) continue;
                float x = l.t.position.x + l.dir * (l.front + metres);
                if (x > World.BackX0 + 10 && x < World.BackX1 - 10) return new Vector3(x, 0.3f, LaneZ(l.dir));
            }
            return null;
        }
        public static (int stopped, int held, float nearest) DebugLane(Vector3 spot)
        {
            int stopped = 0, held = 0; float nearest = float.MaxValue;
            foreach (var l in lorries)
            {
                if (Mathf.Abs(spot.z - LaneZ(l.dir)) > Width) continue;
                if (l.held) { held++; continue; }
                float gap = (spot.x - l.t.position.x) * l.dir - l.front;
                if (gap > -l.front && gap < 120f && l.v < 0.5f) stopped++;
                if (gap > -l.front && gap < nearest) nearest = gap;
            }
            return (stopped, held, nearest);
        }
    }

    // Freighters calling at Marlowe's port on a fixed timetable of server time, so every peer sees the same ships:
    // in from the east, alongside his quay, then away to the west with a full load. Each arrival is an "order".
    public class RivalTraffic : MonoBehaviour
    {
        public static event System.Action Docked; // fires on every peer as a caller ties up
        const float Arrive = 150f, Stay = 150f, Leave = 150f, Cycle = Arrive + Stay + Leave, Lane = 90f, Far = 1500f, Draft = 7.2f;

        struct Ship { public Transform t; public GameObject empty, loaded; public float x, phase; public bool docked; }
        Ship[] ships;
        float berthZ, lastNow;
        bool primed;

        public void Init(Transform parent, float z, float[] berths, (string empty, string loaded)[] models)
        {
            berthZ = z;
            ships = new Ship[berths.Length];
            for (int i = 0; i < berths.Length; i++)
            {
                var t = new GameObject("caller" + i).transform;
                t.SetParent(parent, false);
                ships[i] = new Ship { t = t, x = berths[i], phase = i * Cycle / berths.Length,
                    empty = Art.Place(Skin.Get(models[i].empty), t, Vector3.zero, 0, 1f), loaded = Art.Place(Skin.Get(models[i].loaded), t, Vector3.zero, 0, 1f) };
            }
        }

        void Update()
        {
            if (ships == null) return;
            float now = Water.T;
            // The clock jumps (Time.time -> server time) when a session starts or ends: a ship skipped into its stay isn't news.
            if (Mathf.Abs(now - lastNow) > 2f) primed = false;
            lastNow = now;
            for (int i = 0; i < ships.Length; i++)
            {
                ref var s = ref ships[i];
                float u = Mathf.Repeat(now + s.phase, Cycle);
                Vector3 p;
                bool docked = u >= Arrive && u < Arrive + Stay;
                if (u < Arrive)
                {
                    // Slowing down along the lane, then swinging in alongside.
                    float k = u / Arrive, e = 1 - (1 - k) * (1 - k);
                    p = new Vector3(Mathf.Lerp(Far, s.x, e), 0, berthZ - Lane * (1 - Mathf.SmoothStep(0, 1, (k - 0.6f) / 0.4f)));
                }
                else if (docked) p = new Vector3(s.x, 0, berthZ);
                else
                {
                    // Pulling out, then gathering speed away to the west.
                    float k = (u - Arrive - Stay) / Leave;
                    p = new Vector3(Mathf.Lerp(s.x, -Far, k * k), 0, berthZ - Lane * Mathf.SmoothStep(0, 1, k / 0.35f));
                }
                p.y = Water.Height(p) - Draft;
                s.t.SetPositionAndRotation(p, Quaternion.Euler(0, 270, 0)); // bow west, both ways
                // Arrives empty, loaded halfway through its stay, leaves full.
                bool full = u >= Arrive + Stay * 0.5f;
                if (s.empty != null && s.empty.activeSelf == full) s.empty.SetActive(!full);
                if (s.loaded != null && s.loaded.activeSelf != full) s.loaded.SetActive(full);
                if (primed && docked && !s.docked) { Docked?.Invoke(); Fireworks.Show(Fireworks.Over); }
                s.docked = docked;
            }
            primed = true; // ships already alongside when we start don't count as news
        }
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
