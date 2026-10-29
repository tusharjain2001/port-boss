using UnityEngine;

namespace PortBoss
{
    // The crate store at the foot of the jetty: two shelving racks, two shelves each, three crates a shelf.
    // Unload the boat into it fast while she's in, haul the fish to the market later. Stored fish spoils slower
    // (Crate.StoredSpoilRate) and the gulls and the wind can't get at it. The host keeps the slot book;
    // the crates themselves sync as usual (Crate.Shelf says which slot, on every peer).
    public static class Storage
    {
        public const int Slots = 12, PerRack = 6;
        public static readonly Vector3[] Racks = { new(-10f, 0, -7.6f), new(-7f, 0, -7.6f) }; // centres; the open fronts face +z
        public const float RackW = 2.74f, RackD = 1.0f, RackH = 3.5f;
        static readonly float[] ShelfTops = { 0.7f, 1.4f }; // TH_Shelves_03A's 2nd and 3rd shelves at 1.4x height
        static readonly Crate[] held = new Crate[Slots];

        public static Vector3 SlotPos(int i)
        {
            int k = i % PerRack;
            return Racks[i / PerRack] + new Vector3((k % 3 - 1) * 0.86f, ShelfTops[k / 3] + Crate.Size / 2 + 0.01f, 0.02f);
        }

        public static Vector3 Center => (Racks[0] + Racks[^1]) / 2;

        // Standing in front of the racks (within reach of a shelf).
        public static bool Near(Vector3 p)
        {
            var d = p - Center;
            return Mathf.Abs(d.x) < 4.3f && d.z > -1.2f && d.z < 3.2f;
        }

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
        public static void Take(Crate c) { for (int i = 0; i < Slots; i++) if (held[i] == c) held[i] = null; }

        public static void Reset() => System.Array.Clear(held, 0, Slots);
    }
}
