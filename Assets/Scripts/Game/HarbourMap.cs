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
        }

        // A pack prefab standing on y = pos.y (its lowest point there), or with its top at y (Top).
        static GameObject On(string n, Vector3 pos, float yaw, float scale = 1f, bool colliders = false)
        {
            var g = Art.Place(Skin.Get(n), R, pos, yaw, scale, colliders);
            if (g == null) return null;
            g.transform.position += Vector3.up * (pos.y - Art.Bounds(g).min.y);
            return g;
        }
        static GameObject Top(string n, Vector3 pos, float yaw, Vector3 scale)
        {
            var g = Art.Place(Skin.Get(n), R, pos, yaw, 1f);
            if (g == null) return null;
            g.transform.localScale = scale;
            g.transform.position += Vector3.up * (pos.y - Art.Bounds(g).max.y);
            return g;
        }

        // Concrete slabs (16 m) over every walkable plot.
        static void Ground()
        {
            void Tile(float x0, float x1, float z0, float z1, int seed)
            {
                var rng = new System.Random(seed);
                for (float x = x0 + 8; x < x1; x += 16)
                    for (float z = z0 + 8; z < z1; z += 16)
                        Top("slab", new Vector3(x, 0.0f, z), 90 * rng.Next(4), new Vector3(2, 1, 2));
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
                    new Vector3(1.2f + (float)rng.NextDouble() * 2.2f, 0.004f, 0.8f + (float)rng.NextDouble() * 1.4f), Art.Hex("#5c6670"), false, "puddle");
                pd.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            On("woodStack", new Vector3(-22, 0, -14), 30, 0.7f, true);
            On("cageBarrels", new Vector3(24, 0, -10), 15, 0.7f, true);
            On("forklift", new Vector3(-26, 0, -20), 140, 1f, true);
            // The back of our plot: old sheds, a broken-down truck, a rusty container pile, junk.
            int w = 0;
            foreach (var n in new[] { "whRed", "whWhite", "whGreen" }) On(n, new Vector3(-48 + w++ * 34, 0, -70), 0, 0.9f, true);
            On("trailer", new Vector3(-34, 0, -30), 70, 1f, true);
            On("truckB", new Vector3(-27, 0, -42), 200, 1f, true);
            for (int i = 0; i < 7; i++) On("cont" + (i * 3 % 6), new Vector3(-38 + (i % 3) * 3.4f, (i / 3) * 3f, -52), 0, 1f, i < 3);
            for (int i = 0; i < 5; i++) On("woodStack", new Vector3(-16 + i * 4.5f, 0, -46), i * 37, 0.6f, true);
            On("cageBarrels", new Vector3(-8, 0, -32), 60, 0.6f, true);
            On("cageBarrels", new Vector3(-5.5f, 0, -31), 20, 0.6f, true);
            // Our plot ends at a chain-link line of containers; the neighbours' yard starts behind it.
            for (int i = 0; i < 6; i++) On("cont" + (i % 6), new Vector3(46 + (i % 2) * 0.4f, 0, -50 + i * 6.4f), 0, 1f, true);
        }

        static void ContainerYard(float x0, float x1, float z0, float z1, int seed, bool colliders)
        {
            var rng = new System.Random(seed);
            for (float z = z0; z < z1; z += 18)          // bands of 4 rows, lanes between
                for (int row = 0; row < 4; row++)
                    for (float x = x0; x < x1; x += 7)
                    {
                        if (rng.Next(9) == 0) continue;  // gaps
                        int h = 1 + rng.Next(4);
                        for (int k = 0; k < h; k++)
                            On("cont" + rng.Next(6), new Vector3(x, k * 3f, z + row * 3.2f), 90, 1f, colliders && k == 0);
                    }
        }

        static void EastTerminal()
        {
            ContainerYard(92, 232, -48, 108, 21, true);
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

    // A crane swinging its boom back and forth.
    public class Slew : MonoBehaviour
    {
        public float Amp = 30f, Period = 45f;
        Quaternion b;
        void Start() => b = transform.rotation;
        void Update() => transform.rotation = b * Quaternion.Euler(0, Mathf.Sin(Time.time * 2f * Mathf.PI / Period) * Amp, 0);
    }
}
