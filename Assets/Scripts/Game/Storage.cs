using UnityEngine;

namespace PortBoss
{
    // The crate store at the foot of the jetty: shelving racks, two shelves each, three crates a shelf.
    // Unload the boat into it fast while she's in, haul the fish to the market later. Stored fish spoils slower
    // (SpoilRate) and the gulls and the wind can't get at it. The host keeps the slot book;
    // the crates themselves sync as usual (Crate.Shelf says which slot, on every peer).
    // Upgrades (the shop): level 2 adds a third rack, level 3 a fourth and a chiller (the cold store).
    public static class Storage
    {
        public const int PerRack = 6, MaxLevel = 3, MaxSlots = 24;
        // Rack centres in the order they're built (the open fronts face +z); new racks go west, away from the jetty.
        static readonly Vector3[] AllRacks = { new(-10f, 0, -7.6f), new(-7f, 0, -7.6f), new(-13f, 0, -7.6f), new(-16f, 0, -7.6f) };
        public const float RackW = 2.74f, RackD = 1.0f, RackH = 3.5f;
        static readonly float[] ShelfTops = { 0.7f, 1.4f }; // TH_Shelves_03A's 2nd and 3rd shelves at 1.4x height
        static readonly Crate[] held = new Crate[MaxSlots];

        public static int Level { get; private set; } = 1;
        public static int RackCount => Level + 1;
        public static int Slots => RackCount * PerRack;
        public static bool Cold => Level >= 3;
        public static float SpoilRate => Cold ? 0.15f : 0.4f; // of the open-air rate
        public static Vector3 Rack(int i) => AllRacks[i];

        // Every peer: the store grows (racks, roof and dressing are rebuilt; crates already on the shelves stay put).
        public static void SetLevel(int level)
        {
            level = Mathf.Clamp(level, 1, MaxLevel);
            if (level == Level && World.RacksBuilt) return;
            Level = level;
            World.BuildRacks();
            HarbourMap.BuildCrateStore();
        }

        public static Vector3 SlotPos(int i)
        {
            int k = i % PerRack;
            return AllRacks[i / PerRack] + new Vector3((k % 3 - 1) * 0.86f, ShelfTops[k / 3] + Crate.Size / 2 + 0.01f, 0.02f);
        }

        public static float MinX { get { float m = float.MaxValue; for (int i = 0; i < RackCount; i++) m = Mathf.Min(m, AllRacks[i].x); return m - RackW / 2; } }
        public static float MaxX { get { float m = float.MinValue; for (int i = 0; i < RackCount; i++) m = Mathf.Max(m, AllRacks[i].x); return m + RackW / 2; } }
        public static Vector3 Center => new((MinX + MaxX) / 2, 0, AllRacks[0].z);

        // Standing in front of the racks (within reach of a shelf).
        public static bool Near(Vector3 p) => p.x > MinX - 1.6f && p.x < MaxX + 1.6f && p.z - Center.z > -1.2f && p.z - Center.z < 3.2f;

        // Every peer: how many crates are on the racks.
        public static int Count { get { int n = 0; foreach (var c in Crate.All) if (c != null && c.Stored) n++; return n; } }

        // Server: shelve a crate in the free slot nearest to `from` (the hands). False when the racks are full.
        public static bool Put(Crate c, Vector3 from)
        {
            int best = -1; float bd = float.MaxValue;
            for (int i = 0; i < Slots; i++)
            {
                if (held[i] != null && held[i].IsSpawned && held[i].Stored) continue;
                float d = (SlotPos(i) - from).sqrMagnitude;
                if (d < bd) { bd = d; best = i; }
            }
            if (best < 0) return false;
            held[best] = c;
            c.Store(best, SlotPos(best), Quaternion.Euler(0, Random.Range(-5f, 5f), 0));
            return true;
        }

        // Server: a crate leaves the racks (picked up, knocked off, despawned).
        public static void Take(Crate c) { for (int i = 0; i < MaxSlots; i++) if (held[i] == c) held[i] = null; }

        public static void Reset() => System.Array.Clear(held, 0, MaxSlots);
    }
}
