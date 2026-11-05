using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace PortBoss.M1
{
    // M5 in the world (gdd systems "M5"; economy §4.3 Doyle, §4.4 Plank & Post, §4.6 Hartley's; CHANGES row 3: no placing):
    // - the three shops' counters at map B's anchors (doyle, plank, hartleys) with their keepers; E opens YShop
    // - every buyable has ONE set spot (the office's and warehouse's spot_* empties, the landing's spots in
    //   PB_M5_Spots.json, a boat's own frame): what the company owns is shown there, popping in with a squash and settle,
    //   dust and a thup (furniture, beds, boat upgrades at once); builds land at 07:00 (the morning's timelapse: scaffold up,
    //   the thing grows in, scaffold away). The office and warehouse swap their whole building per tier.
    // - paint: each boat's 4 channels from the company (Channels.Boat), Doyle's crew (two lads) walk over from the boatworks
    //   on the town's navmesh and paint her with a visible brush line; all of it from the game clock, so every machine in
    //   co-op shows the same at the same moment, with no extra messages
    // - the Kestrel: on Doyle's pontoon before she's bought (walk aboard, look round, no tiller), at the landing after
    // - boat upgrades: the props on the boat + their effects (engine rebuild / Engine I-II speed, hull patch, bilge pump,
    //   deck lights at night); the others wait for their systems (README)
    // - the town's NPC routines v1: every shopkeeper at the counter in opening hours, indoors otherwise
    // - all the M5 actions (buy, ask the crew, answer, wear, pick a crew member, store fish) for this machine: solo and the
    //   host do them here, a client asks the host (CoopM5)
    public class Works : MonoBehaviour
    {
        public static Works I;
        public static Action<string> OnLog;
        static void Log(string s) { Debug.Log("[PB] works: " + s); OnLog?.Invoke(s); }
        static Company C => Firm.C;

        // ---------------------------------------------------------------- the counters
        public class Counter5
        {
            public Store S; public string Id, Name, Owner, Line, Place; public Transform At; public Vector3 Front; public bool Real; public Npc Keeper;
            public bool IsOpen(double m) => Catalog.IsOpen(S, m);
        }
        public readonly List<Counter5> Shops = new();
        public Counter5 Near;
        public readonly Dictionary<string, Transform> Spots = new();      // set spot id -> where it goes
        readonly Dictionary<string, GameObject> shown = new();             // item -> its visual
        public Skiff Kestrel; Transform kestrelSlip, kestrelBerth;
        Transform office, warehouse, shack; int officeShown = -1, whShown = -1;
        public readonly List<Npc> Crew = new();                           // Doyle's two lads
        public readonly List<Npc> Keepers = new();
        bool started; float reconcileT;
        public static bool NavOk;
        public static (string boat, int ch, int sw, int pat)? TryPaint;   // YShop: the colour you're looking at, on her (this machine only)

        public static void Create() { var g = new GameObject("Works"); I = g.AddComponent<Works>(); I.Build(); }

        void Build()
        {
            var a = M1Assets.I;
            BuildNav();
            LoadSpots(a);
            Counter(Store.Doyle, "doyle", "Doyle Boatworks", "Fergus Doyle", "Old Saltmouth slipway", "She'll be right. They always are, with enough paint.", new Vector2(4.5f, -2f));
            Counter(Store.Plank, "plank", "Plank & Post", "Ingrid Marsh", "the timber yard", "Measure twice, nail once. Then nail again, it's the sea.", new Vector2(-6f, 2f));
            Counter(Store.Hartley, "hartleys", "Hartley's Outfitters", "Mabel Hartley", "Old Saltmouth high street", "Clothes make the captain, dear. Not the boat.", new Vector2(6f, -2f));
            Keepers.Clear(); foreach (var s in Shops) if (s.Keeper != null) Keepers.Add(s.Keeper);
            if (Firm.I != null) foreach (var st in Firm.I.Stalls) if (st.K != Firm.Kind.Mari) Keepers.Add(Npc.Make(st.Owner, st.K == Firm.Kind.June ? KeeperLook(5) : KeeperLook(6), st.At));   // June, Gideon: town NPC routines v1
            BuildDoyleCrew();
            BuildKestrel(a);
            Firm.C.Installed += OnInstalled;
            started = true;
            Log($"{Shops.Count} shops, {Spots.Count} set spots, nav {(NavOk ? "built" : "none")}, Kestrel {(Kestrel != null ? "at Doyle's" : "missing (no model)")}");
        }

        // ---- a counter (map B's building is the shop: here only the counter and its name board, at the door)
        void Counter(Store s, string id, string name, string owner, string place, string line, Vector2 fallback)
        {
            var at = Places.Get(id); bool real = at != null;
            if (!real) at = TempSpot(id, fallback);
            var c = new Counter5 { S = s, Id = id, Name = name, Owner = owner, Line = line, Place = place, At = at, Real = real };
            c.Front = at.position + at.forward * 1.1f;
            var root = new GameObject("Counter " + name).transform; root.SetParent(at, false);
            Color wood = Ponyo.Hex("9A7352"), woodDk = Ponyo.Hex("6E5038"), top = Ponyo.Hex("C9A27A");
            Color band = s == Store.Doyle ? Ponyo.Hex("C9483A") : s == Store.Plank ? Ponyo.Hex("7DBE6A") : Ponyo.Hex("B07ACB");
            Firm.Box(root, "counter", new Vector3(0, 0.47f, 0.1f), new Vector3(1.9f, 0.94f, 0.62f), wood);
            Firm.Box(root, "counter top", new Vector3(0, 0.96f, 0.14f), new Vector3(2.02f, 0.06f, 0.74f), top);
            Firm.Box(root, "kick", new Vector3(0, 0.07f, 0.42f), new Vector3(1.86f, 0.14f, 0.02f), woodDk);
            for (int i = -1; i <= 1; i += 2) Firm.Box(root, "sign post", new Vector3(0.72f * i, 1.4f, -0.15f), new Vector3(0.05f, 0.82f, 0.05f), woodDk);
            Firm.Box(root, "board", new Vector3(0, 1.6f, -0.15f), new Vector3(1.6f, 0.34f, 0.05f), Ponyo.Hex("F3EBDD"));
            Firm.Box(root, "board edge", new Vector3(0, 1.6f, -0.185f), new Vector3(1.68f, 0.42f, 0.02f), woodDk);
            Firm.Box(root, "board stripe", new Vector3(0, 1.79f, -0.15f), new Vector3(1.68f, 0.05f, 0.06f), band);
            Firm.Sign(root, name, new Vector3(0, 1.6f, -0.12f), Ponyo.Ink, 0.0098f);
            // the goods on it: a paint tin and brushes at Doyle's, a plank and a saw at Plank & Post, folded jumpers at Hartley's
            if (s == Store.Doyle) { Firm.Box(root, "tin", new Vector3(-0.55f, 1.07f, 0.12f), new Vector3(0.22f, 0.16f, 0.22f), Ponyo.Hex("3A86E0")); Firm.Box(root, "tin2", new Vector3(-0.25f, 1.05f, 0.18f), new Vector3(0.18f, 0.12f, 0.18f), Ponyo.Hex("F7C548")); Firm.Box(root, "brush", new Vector3(0.4f, 1.0f, 0.1f), new Vector3(0.5f, 0.04f, 0.08f), woodDk); }
            else if (s == Store.Plank) { Firm.Box(root, "plank", new Vector3(0, 1.02f, 0.14f), new Vector3(1.6f, 0.05f, 0.24f), Ponyo.Hex("D4B07A")); Firm.Box(root, "saw", new Vector3(0.5f, 1.06f, 0.0f), new Vector3(0.5f, 0.02f, 0.14f), Ponyo.Hex("B9B2A6")); }
            else for (int i = 0; i < 3; i++) Firm.Box(root, "jumper", new Vector3(-0.55f + i * 0.5f, 1.04f, 0.12f), new Vector3(0.38f, 0.1f, 0.3f), i == 0 ? Ponyo.Hex("E2553F") : i == 1 ? Ponyo.Hex("2E3F5C") : Ponyo.Hex("F3EBDD"));
            var col = root.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, 0.5f, 0.1f); col.size = new Vector3(2.0f, 1.0f, 0.7f);
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            c.Keeper = Npc.Make(owner, KeeperLook((int)s), at);
            Shops.Add(c);
            Log($"{name} at {(real ? "map anchor" : "temporary spot")} anchor_{id} {at.position.x:F1} {at.position.y:F2} {at.position.z:F1}");
        }
        public static PortBoss.Look KeeperLook(int i) => i switch
        {
            0 => new PortBoss.Look { Set = 1, Face = 3, Hair = 1, Beard = 1, Hat = 1, Top = 2, Gloves = 1, SkinTone = 1, HairColor = 7, TopColor = 2, AccentColor = 4, LegsColor = 2, BootsColor = 7, GloveColor = 4, HatColor = 2 },    // Fergus Doyle
            1 => new PortBoss.Look { Set = 1, Face = 1, Hair = 3, Beard = 0, Hat = 3, Top = 3, Gloves = 1, SkinTone = 3, HairColor = 6, TopColor = 6, AccentColor = 3, LegsColor = 1, BootsColor = 7, GloveColor = 3, HatColor = 4 },    // Ingrid Marsh
            2 => new PortBoss.Look { Set = 1, Face = 1, Hair = 2, Beard = 0, Hat = 0, Top = 3, Gloves = 0, SkinTone = 0, HairColor = 7, TopColor = 9, AccentColor = 4, LegsColor = 1, BootsColor = 7, GloveColor = 4, HatColor = 2 },    // Mabel Hartley
            3 => new PortBoss.Look { Set = 1, Face = 0, Hair = 1, Beard = 2, Hat = 2, Top = 2, Gloves = 1, SkinTone = 5, HairColor = 0, TopColor = 0, AccentColor = 3, LegsColor = 0, BootsColor = 7, GloveColor = 4, HatColor = 3 },    // Doyle's lad, Kit
            4 => new PortBoss.Look { Set = 1, Face = 2, Hair = 2, Beard = 3, Hat = 1, Top = 2, Gloves = 1, SkinTone = 2, HairColor = 1, TopColor = 2, AccentColor = 4, LegsColor = 2, BootsColor = 7, GloveColor = 3, HatColor = 0 },    // Doyle's lad, Arlo
            5 => new PortBoss.Look { Set = 1, Face = 1, Hair = 3, Beard = 0, Hat = 2, Top = 0, Gloves = 0, SkinTone = 1, HairColor = 4, TopColor = 3, AccentColor = 4, LegsColor = 1, BootsColor = 6, GloveColor = 4, HatColor = 3 },    // June Pike
            _ => new PortBoss.Look { Set = 1, Face = 3, Hair = 0, Beard = 1, Hat = 0, Top = 1, Gloves = 1, SkinTone = 2, HairColor = 7, TopColor = 1, AccentColor = 4, LegsColor = 1, BootsColor = 7, GloveColor = 4, HatColor = 2 },   // Gideon Pratt
        };

        // a spot on the M1 Landing in its Blender metres (the same mapping as Walt's cot); used when map B has no anchor
        Transform TempSpot(string id, Vector2 blender)
        {
            var t = new GameObject("anchor_" + id + " (temp)").transform; t.SetParent(transform, false);
            var l = Landing.I; if (l == null || l.Spawn == null || l.LadderTop == null) return t;
            var p0 = Blender(new Vector3(blender.x, blender.y, 0)); var dir = Blender(new Vector3(blender.x, blender.y - 1, 0)) - p0; dir.y = 0;
            t.SetPositionAndRotation(Ground(p0), Quaternion.LookRotation(dir.normalized));
            return t;
        }
        public static Vector3 Blender(Vector3 p)
        {
            var l = Landing.I; if (l == null || l.Spawn == null || l.LadderTop == null) return p;
            return Sky.FromBlender(l.transform, l.Spawn, new Vector3(0.15f, -19.6f, 1.3f), l.LadderTop, new Vector3(1.2f, -20.6f, 1.3f), p);
        }
        public static Vector3 Ground(Vector3 p)
        {
            float gy = p.y; if (Physics.Raycast(new Vector3(p.x, p.y + 30, p.z), Vector3.down, out var hit, 80, ~0, QueryTriggerInteraction.Ignore)) gy = hit.point.y;
            return new Vector3(p.x, gy, p.z);
        }

        // ---------------------------------------------------------------- the set spots
        // PB_M5_Spots.json: { "spot_planks": { "pos": [x, y, z], "yaw": deg }, ... } in the landing's Blender metres
        void LoadSpots(M1Assets a)
        {
            if (Landing.I != null) shack = Util.Find(Landing.I.transform, "Shack");
            if (a.M5Spots != null)
            {
                try
                {
                    var root = Json.Parse(a.M5Spots.text) as Dictionary<string, object>;
                    var list = root != null && root.TryGetValue("spots", out var sv) && sv is Dictionary<string, object> d2 ? d2 : root;
                    foreach (var kv in list)
                    {
                        if (kv.Value is not Dictionary<string, object> o) continue;
                        var pos = (o.TryGetValue("pos", out var pv) ? pv : o.TryGetValue("loc", out var lv) ? lv : null) as List<object>; if (pos == null || pos.Count < 2) continue;
                        float x = Convert.ToSingle(pos[0]), y = Convert.ToSingle(pos[1]), z = pos.Count > 2 ? Convert.ToSingle(pos[2]) : 0;
                        float yaw = Json.F(o, "yaw");
                        var p = Blender(new Vector3(x, y, z)); var ahead = Blender(new Vector3(x + Mathf.Sin(-yaw * Mathf.Deg2Rad), y - Mathf.Cos(-yaw * Mathf.Deg2Rad), z)) - p; ahead.y = 0;
                        var t = new GameObject(kv.Key).transform; t.SetParent(Landing.I != null ? Landing.I.transform : transform, true);
                        t.SetPositionAndRotation(z > 0.01f ? p : Ground(p), Quaternion.LookRotation(ahead.sqrMagnitude > 1e-6f ? ahead.normalized : Vector3.forward));
                        Spots[kv.Key] = t;
                    }
                }
                catch (Exception e) { Log("spots: " + e.Message); }
            }
            // the cot is Walt's (Sleep built it); the office's and warehouse's empties come with their buildings (ShowBuildings)
            if (Sleep.Cot != null) Spots["spot_cot"] = Sleep.Cot;
        }
        // the office's / warehouse's spot_* empties (a new tier: the furniture moves to the new building's spots)
        void TakeSpots(Transform building)
        {
            if (building == null) return;
            foreach (var t in building.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("spot_") || t.name.StartsWith("socket_")) Spots[t.name] = t;
        }

        // ---------------------------------------------------------------- the town's navmesh (NPC walks, the validator)
        void BuildNav()
        {
            try
            {
                var c = Landing.I != null ? Landing.I.transform.position : Vector3.zero;
                var b = new Bounds(c + new Vector3(80, 10, -30), new Vector3(360, 60, 300));   // the town shore: the landing, Doyle's, Plank & Post, the high street
                var src = new List<NavMeshBuildSource>();
                NavMeshBuilder.CollectSources(b, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), src);
                src.RemoveAll(s => s.component != null && (s.component.GetComponentInParent<Skiff>() != null || s.component.GetComponentInParent<Angler>() != null));
                var set = NavMesh.GetSettingsByID(0); set.agentRadius = 0.3f; set.agentHeight = 1.8f; set.agentClimb = 0.45f; set.agentSlope = 40;
                var data = NavMeshBuilder.BuildNavMeshData(set, src, b, Vector3.zero, Quaternion.identity);
                if (navInst.valid) navInst.Remove();
                if (data != null) { navInst = NavMesh.AddNavMeshData(data); NavOk = true; Log($"navmesh: {src.Count} sources over the town"); }
            }
            catch (Exception e) { Log("navmesh: " + e.Message); NavOk = false; }
        }
        NavMeshDataInstance navInst;
        public void RebuildNav() { BuildNav(); paths.Clear(); }   // (the set-spot validator: with every item installed)
        // a walk from a to b on the navmesh (straight if there's none)
        public static List<Vector3> Path(Vector3 a, Vector3 b)
        {
            var l = new List<Vector3>();
            if (NavOk && NavMesh.SamplePosition(a, out var ha, 3, NavMesh.AllAreas) && NavMesh.SamplePosition(b, out var hb, 3, NavMesh.AllAreas))
            {
                var p = new NavMeshPath();
                if (NavMesh.CalculatePath(ha.position, hb.position, NavMesh.AllAreas, p) && p.status == NavMeshPathStatus.PathComplete) { l.AddRange(p.corners); return l; }
            }
            l.Add(a); l.Add(b); return l;
        }
        public static float Length(List<Vector3> p) { float d = 0; for (int i = 1; i < p.Count; i++) d += Vector3.Distance(p[i - 1], p[i]); return d; }
        public static Vector3 Along(List<Vector3> p, float d, out Vector3 dir)
        {
            dir = Vector3.forward;
            for (int i = 1; i < p.Count; i++)
            {
                float s = Vector3.Distance(p[i - 1], p[i]);
                dir = p[i] - p[i - 1]; dir.y = 0; if (dir.sqrMagnitude > 1e-6f) dir.Normalize();
                if (d <= s) return Vector3.Lerp(p[i - 1], p[i], s > 0 ? d / s : 1);
                d -= s;
            }
            return p[p.Count - 1];
        }

        // ---------------------------------------------------------------- the Kestrel
        void BuildKestrel(M1Assets a)
        {
            if (a.Kestrel == null) return;
            // Doyle's pontoon: beside the slipway, out from the boatworks' doors into the bay
            var d = Shops.Find(s => s.S == Store.Doyle); var at = d.At;
            var root = new GameObject("Doyle's pontoon").transform; root.SetParent(transform, false);
            var side = at.right * 5.2f; var start = Ground(at.position + side + at.forward * 2f);
            float deckY = Mathf.Max(Sea.Level + 0.9f, 0.9f);
            int n = 9; float seg = 2.2f;
            Color plank = Ponyo.Hex("B48A62"), plankDk = Ponyo.Hex("8A6A4E"), post = Ponyo.Hex("6E5038");
            for (int i = 0; i < n; i++)
            {
                var p = start + at.forward * (i * seg + seg / 2); float y = Mathf.Lerp(start.y + 0.05f, deckY, Mathf.Clamp01(i / 2f));
                var g = Firm.Box(root, "deck", Vector3.zero, new Vector3(2.0f, 0.14f, seg + 0.02f), i % 2 == 0 ? plank : plankDk);
                g.transform.SetPositionAndRotation(new Vector3(p.x, y, p.z), at.rotation * Quaternion.Euler(i < 2 ? -Mathf.Atan2(deckY - start.y, seg * 2) * Mathf.Rad2Deg : 0, 0, 0));
                g.AddComponent<BoxCollider>();
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    var pp = Firm.Box(root, "post", Vector3.zero, new Vector3(0.18f, 2.4f, 0.18f), post);
                    pp.transform.SetPositionAndRotation(new Vector3(p.x, y - 1.1f, p.z) + at.right * sx * 0.95f, at.rotation);
                }
            }
            kestrelSlip = new GameObject("moor_kestrel_doyle").transform; kestrelSlip.SetParent(root, false);
            var mp = start + at.forward * (n * seg - 4.2f) + at.right * 2.35f; kestrelSlip.SetPositionAndRotation(new Vector3(mp.x, Sea.Level, mp.z), at.rotation);
            // her berth at the landing: map B's "berth_kestrel" spot, else alongside the pier ahead of Biscuit
            kestrelBerth = Spots.TryGetValue("moor_kestrel", out var mk) ? mk : null;
            if (kestrelBerth == null && Landing.I != null && Landing.I.Moor != null)
            {
                var m = Landing.I.Moor; kestrelBerth = new GameObject("moor_kestrel").transform; kestrelBerth.SetParent(Landing.I.transform, false);
                var q = m.position + m.forward * 8.6f; kestrelBerth.SetPositionAndRotation(new Vector3(q.x, Sea.Level, q.z), m.rotation);
            }
            bool owned = C.Owned.Contains("boat:kestrel");
            Kestrel = Skiff.Create(a.Kestrel, Game.ToonMat, owned ? kestrelBerth : kestrelSlip, "kestrel", "Kestrel");
            Kestrel.Top = 9f; Kestrel.Sound = true;
            Kestrel.Hull.size = Vector3.Max(Kestrel.Hull.size, new Vector3(2.4f, 1.2f, 7.2f));
        }
        public static bool CanDrive(Skiff s) => s == null || s.Id != "kestrel" || (C != null && C.Owned.Contains("boat:kestrel"));
        public static string DriveRefusal(Skiff s) => "Fergus: \"Look all you like. She's yours once she's paid for.\"";

        // ---------------------------------------------------------------- Doyle's crew
        void BuildDoyleCrew()
        {
            var d = Shops.Find(s => s.S == Store.Doyle); if (d == null) return;
            for (int i = 0; i < 2; i++) { var n = Npc.Make(i == 0 ? "Kit (Doyle's)" : "Arlo (Doyle's)", KeeperLook(3 + i), d.At); n.Roller(i == 0 ? Ponyo.Hex("3A86E0") : Ponyo.Hex("E2553F")); Crew.Add(n); }
        }
        // where Doyle's lads are at this moment (game time): idle by the slipway, walking to a boat, painting along her
        void CrewTick(double m)
        {
            var d = Shops.Find(s => s.S == Store.Doyle); if (d == null) return;
            PaintJob job = null; Skiff boat = null;
            foreach (var b in Skiff.All) { var st = C.BoatOf(b.Id); if (st.Job != null && m < st.Job.End + PaintBack(st.Job)) { job = st.Job; boat = b; break; } }
            for (int i = 0; i < Crew.Count; i++)
            {
                var n = Crew[i]; var home = d.At.position + d.At.forward * (2.2f + i * 0.9f) + d.At.right * (i == 0 ? 2.6f : -2.6f);
                if (job == null || boat == null) { n.Hold(Ground(home), -d.At.right * (i == 0 ? 1 : -1) + d.At.forward * 0.3f, false); n.Paint(false); continue; }
                // the hull side they paint from: on the pier (the berth's shore side), each lad one end
                var hullSide = PaintSpot(boat, i, job.Progress(m));
                var path = PathCache(i, home, hullSide.pos);
                float len = Length(path); double t0 = job.Start, walkEnd = job.Start + job.Walk, end = job.End, back = end + PaintBack(job);
                if (m < walkEnd) { float k = (float)((m - t0) / Math.Max(job.Walk, 0.01)); var p = Along(path, len * k, out var dir); n.Hold(p, dir, true); n.Paint(false); }
                else if (m < end) { n.Hold(hullSide.pos, hullSide.face, false); n.Paint(true); }
                else { float k = (float)((m - end) / Math.Max(back - end, 0.01)); var p = Along(path, len * (1 - k), out var dir); n.Hold(p, -dir, true); n.Paint(false); }
            }
        }
        double PaintBack(PaintJob j) => j.Walk;
        readonly Dictionary<int, (Vector3 a, Vector3 b, List<Vector3> p)> paths = new();
        List<Vector3> PathCache(int i, Vector3 a, Vector3 b)
        {
            if (paths.TryGetValue(i, out var c) && (c.a - a).sqrMagnitude < 0.25f && (c.b - b).sqrMagnitude < 4f) return c.p;
            var p = Path(a, b); paths[i] = (a, b, p); return p;
        }
        // a painting lad's place: beside the hull on the pier side, sliding stern to bow with the brush line
        (Vector3 pos, Vector3 face) PaintSpot(Skiff b, int i, double prog)
        {
            var h = b.Hull; float z = Mathf.Lerp(h.min.z + 0.6f, h.max.z - 0.6f, i == 0 ? (float)prog : Mathf.Clamp01((float)prog * 0.9f + 0.05f));
            // the pier side: the side whose point beside the hull has ground under it (else the port side)
            float side = 1; var pR = b.transform.TransformPoint(new Vector3(h.max.x + 0.7f, 0, z)); var pL = b.transform.TransformPoint(new Vector3(h.min.x - 0.7f, 0, z));
            bool gR = Physics.Raycast(pR + Vector3.up * 3, Vector3.down, out var hr, 5, ~0, QueryTriggerInteraction.Ignore) && !hr.transform.IsChildOf(b.transform);
            bool gL = Physics.Raycast(pL + Vector3.up * 3, Vector3.down, out var hl, 5, ~0, QueryTriggerInteraction.Ignore) && !hl.transform.IsChildOf(b.transform);
            if (!gR && gL) side = -1;
            var p = side > 0 ? pR : pL; var g = side > 0 ? (gR ? hr.point : p) : (gL ? hl.point : p);
            if (i == 1) g += b.transform.forward * 0.9f;
            return (g, (b.transform.position - g).normalized);
        }

        // ---------------------------------------------------------------- every frame
        void Update()
        {
            if (C == null) return;
            double m = Sky.Minutes;
            // the counter I'm at
            var me = Angler.I; Near = null;
            if (me != null && !me.Remote) foreach (var s in Shops) { var d = s.Front - me.transform.position; d.y *= 0.5f; if (d.magnitude < 2.0f) { Near = s; break; } }
            // the keepers: at the counter while open, indoors otherwise (a short walk each way)
            foreach (var s in Shops) KeeperTick(s.Keeper, s.At, Catalog.Opens[(int)s.S], Catalog.Closes[(int)s.S], Catalog.ClosedDay(s.S, DayTime.Day(m)), m);
            if (Firm.I != null) { int k = Shops.Count; foreach (var st in Firm.I.Stalls) { if (st.K == Firm.Kind.Mari) continue; if (k < Keepers.Count) KeeperTick(Keepers[k], st.At, st.Open, st.Close, false, m); k++; } }
            CrewTick(m);
            // the boats: paint, tune, their props
            foreach (var b in Skiff.All) { var st = C.BoatOf(b.Id); PaintBoat(b, st, m); Tune(b, st); }
            KestrelTick();
            // the host: asks that lapse (nobody else online), and the overnight builds land in Company.Tick
            if (Sky.Authority && (!Coop.On || Coop.IsHost)) C.TickAsks(m, key => Others(key));
            reconcileT -= Time.unscaledDeltaTime;
            if (reconcileT <= 0) { reconcileT = 0.25f; Reconcile(); }
            Tester();
        }
        static bool Others(string key)
        {
            if (!Coop.On || Coop.I == null) return false;
            foreach (var e in Coop.I.Roster) if ((e.Key ?? Coop.MyKey(e.Name)) != key) return true;
            return false;
        }
        public static bool OthersOnline => Coop.On && Coop.I != null && Coop.I.Roster.Count > 1;

        static void KeeperTick(Npc n, Transform at, double open, double close, bool closedDay, double m)
        {
            if (n == null || at == null) return;
            double h = DayTime.OfDay(m); const double walk = 0.15 * 60 / 60;   // 9 game minutes each way (9 s)
            var counter = Ground(at.position - at.forward * 0.55f); var door = Ground(at.position - at.forward * 3.2f + at.right * 0.6f);
            bool open_ = !closedDay && h >= open && h < close;
            double minutes = h * 1; // (OfDay is in minutes already)
            if (!closedDay && minutes >= open - 9 && minutes < open) { float k = (float)((minutes - (open - 9)) / 9); n.Hold(Vector3.Lerp(door, counter, k), (counter - door), true); n.Show(true); return; }
            if (!closedDay && minutes >= close && minutes < close + 9) { float k = (float)((minutes - close) / 9); n.Hold(Vector3.Lerp(counter, door, k), (door - counter), true); n.Show(true); return; }
            n.Show(open_); if (open_) n.Hold(counter, at.forward, false);
        }

        // ---- paint on a boat
        readonly Dictionary<Skiff, Renderer[]> boatR = new();
        void PaintBoat(Skiff b, BoatSt st, double m)
        {
            if (!boatR.TryGetValue(b, out var rs) || rs == null) { var l = new List<Renderer>(); foreach (var r in b.GetComponentsInChildren<Renderer>(true)) if (r is MeshRenderer && Channels.Has(r)) l.Add(r); boatR[b] = rs = l.ToArray(); Log($"{b.Name}: {rs.Length} painted parts"); }
            if (TryPaint is { } tp && tp.boat == b.Id && (st.Job == null || m >= st.Job.End))
            {   // trying a colour on in the shop: a copy of her paint with it, nothing saved
                var t = st.Copy(); t.Job = null; t.Paint[tp.ch] = tp.sw; if (tp.ch == 1) t.Pattern = tp.pat; st = t;
            }
            Channels.Boat(rs, st, m, new Vector3(0, 0, 1));
        }
        // ---- what's been fitted: speed, leaks, the pump, the deck lights
        void Tune(Skiff b, BoatSt st)
        {
            float baseTop = b.Id == "kestrel" ? 9f : st.Engine ? 6f : Skiff.MaxSpeed;
            b.Top = baseTop * (1 + (st.Ups.Contains("engine1") ? 0.15f : 0) + (st.Ups.Contains("engine2") ? 0.15f : 0));
            if (b.Id != "kestrel") b.Sound = st.Hull;
            b.Pump = st.Ups.Contains("pump");
            if (b.Sound && b.Leaking && !b.Remote) b.Patch();   // Doyle's patch holds for good
        }
        void KestrelTick()
        {
            if (Kestrel == null) return;
            bool owned = C.Owned.Contains("boat:kestrel");
            var want = owned ? kestrelBerth : kestrelSlip;
            if (Kestrel.MoorAt != want && want != null)
            {
                Kestrel.MoorAt = want;
                if (owned && Kestrel.Moored && !Kestrel.Remote) { Kestrel.TakeOver(); Kestrel.transform.position = new Vector3(want.position.x, Sea.Level, want.position.z); Kestrel.transform.rotation = want.rotation; Kestrel.TakeOver(); Kestrel.Moor(); Log("the Kestrel is delivered to the landing"); }
            }
        }

        // ---------------------------------------------------------------- what's installed
        bool Base(string item) => item == "wh:0" || item == "office:0";
        void Reconcile()
        {
            var want = new HashSet<string>();
            foreach (var o in C.Owned) want.Add(o);
            foreach (var kv in C.Boats) foreach (var u in kv.Value.Ups) want.Add($"up:{kv.Key}:{u}");
            if (C.Boats.TryGetValue("biscuit", out var bis)) { if (bis.Engine) want.Add("rep:biscuit:engine"); if (bis.Hull) want.Add("rep:biscuit:hull"); }
            ShowBuildings();
            foreach (var w in want) if (!shown.ContainsKey(w)) Show(w, popNow.Remove(w) || C.Fresh.Contains(w) && Morning());
            foreach (var k in new List<string>(shown.Keys)) if (!want.Contains(k)) { if (shown[k] != null) Destroy(shown[k]); shown.Remove(k); }
        }
        readonly HashSet<string> popNow = new();
        void OnInstalled(string item) { if (started) popNow.Add(item); }
        bool Morning() { double h = DayTime.OfDay(Sky.Minutes); return h >= DayTime.Wake && h < DayTime.Wake + 60; }

        void ShowBuildings()
        {
            int ot = C.OfficeTier, wt = C.WhTier;
            if (ot != officeShown)
            {
                var pf = M1Assets.I.M5Prefab("Bld_office" + Mathf.Min(ot, 2));
                if (pf != null)
                {
                    bool pop = officeShown >= 0;
                    if (office != null) Destroy(office.gameObject);
                    var at = shack != null ? shack : Spots.TryGetValue("office", out var so) ? so : null;
                    if (at != null)
                    {
                        office = Instantiate(pf).transform; office.name = "Office T" + ot; Util.Colliders(office.gameObject); Util.Toon(office.gameObject, Game.ToonMat);
                        office.SetPositionAndRotation(at.position, at.rotation);
                        if (shack != null) foreach (var r in shack.GetComponentsInChildren<Renderer>()) r.enabled = false;
                        if (shack != null) foreach (var c in shack.GetComponentsInChildren<Collider>()) c.enabled = false;
                        TakeSpots(office); RespotAll("furn:", "bed:");
                        if (pop) StartCoroutine(Timelapse(office.gameObject));
                        Log($"office tier {ot} shown");
                    }
                }
                officeShown = ot;
            }
            if (wt != whShown)
            {
                var pf = M1Assets.I.M5Prefab("Bld_wh" + Mathf.Min(wt, 2));
                if (pf != null && Spots.TryGetValue("spot_wh", out var at))
                {
                    bool pop = whShown >= 0;
                    if (warehouse != null) Destroy(warehouse.gameObject);
                    warehouse = Instantiate(pf).transform; warehouse.name = "Warehouse T" + wt; Util.Colliders(warehouse.gameObject); Util.Toon(warehouse.gameObject, Game.ToonMat);
                    warehouse.SetPositionAndRotation(at.position, at.rotation); TakeSpots(warehouse);
                    if (pop) StartCoroutine(Timelapse(warehouse.gameObject));
                    Log($"warehouse tier {wt} shown");
                }
                whShown = wt;
            }
        }
        void RespotAll(params string[] pre)
        {
            foreach (var k in new List<string>(shown.Keys)) foreach (var p in pre) if (k.StartsWith(p)) { if (shown[k] != null) Destroy(shown[k]); shown.Remove(k); }
        }
        public Transform Office => office; public Transform Warehouse => warehouse;
        public IReadOnlyDictionary<string, GameObject> ShownAll => shown; public GameObject ShownOf(string item) => shown.TryGetValue(item, out var g) ? g : null;
        public bool IsShown(string item) => shown.ContainsKey(item);
        public Transform KestrelBerth => kestrelBerth; public Transform KestrelSlip => kestrelSlip;
        public Vector3? WarehouseDoor => warehouse != null ? (Spots.TryGetValue("socket_door", out var d) && d.IsChildOf(warehouse) ? d.position : warehouse.position) : (Vector3?)null;

        // an item's visual in its spot
        public static string SpotOf(string item)
        {
            var w = Catalog.Get(item);
            if (item.StartsWith("rep:")) return null;
            if (item.StartsWith("up:")) return "boat:" + item.Split(':')[1];
            return w?.Spot;
        }
        void Show(string item, bool pop)
        {
            if (item.StartsWith("wh:") || item.StartsWith("office:") || item.StartsWith("boat:")) { shown[item] = null; if (pop) Fx(item); return; }
            GameObject g = null;
            if (item.StartsWith("up:")) g = BoatProp(item);
            else if (item.StartsWith("rep:")) { shown[item] = null; if (pop) Fx(item); return; }
            else if (item == "bed:cot") g = CotFixed();
            else
            {
                var spot = SpotOf(item); if (spot == null || !Spots.TryGetValue(spot, out var at)) { shown[item] = null; Log($"{item}: no spot {spot} yet"); return; }
                string id = item.Substring(item.IndexOf(':') + 1);
                var pf = M1Assets.I.M5Prefab("Prop_" + id) ?? M1Assets.I.M5Prefab("Prop_" + item.Replace(':', '_'));
                if (pf != null) { g = Instantiate(pf); Util.Colliders(g); Util.Toon(g, Game.ToonMat); }
                else g = Stand(item);
                g.name = item; g.transform.SetParent(at, false); g.transform.localPosition = Vector3.zero; g.transform.localRotation = Quaternion.identity;
                if (item.StartsWith("bed:") && item != "bed:hammock") Sleep.AddBed(g.transform, item);
                if (item.StartsWith("dock:")) Broken(item, false);
            }
            shown[item] = g;
            if (pop && g != null) StartCoroutine(item.StartsWith("dock:") ? Timelapse(g) : Pop(g));
            if (pop) Fx(item);
        }
        // the landing's broken bits (the three planks, the berths' rotten boards) hide once fixed
        void Broken(string item, bool show)
        {
            if (Landing.I == null) return;
            string n = item == "dock:planks" ? "Broken_planks" : item == "dock:b2" ? "Broken_b2" : item == "dock:b3" ? "Broken_b3" : null; if (n == null) return;
            var t = Util.Find(Landing.I.transform, n); if (t != null) t.gameObject.SetActive(show);
            if (Spots.TryGetValue("spot_" + item.Substring(5), out var s)) foreach (Transform c in s) if (c.name.StartsWith("broken")) c.gameObject.SetActive(show);
        }
        // a code-built stand-in when an item has no model (logged: the art list in the README)
        GameObject Stand(string item)
        {
            var g = new GameObject(item); Log($"{item}: no model, a stand-in box");
            Firm.Box(g.transform, "stand-in", new Vector3(0, 0.4f, 0), new Vector3(0.8f, 0.8f, 0.8f), Ponyo.Hex("C9A27A"));
            return g;
        }
        GameObject CotFixed()
        {
            if (Sleep.Cot == null) return null;
            var g = new GameObject("bed:cot fixed"); g.transform.SetParent(Sleep.Cot, false);
            Firm.Box(g.transform, "mattress", new Vector3(0, 0.46f, 0.05f), new Vector3(0.72f, 0.09f, 1.78f), Ponyo.Hex("F3EBDD"));
            Firm.Box(g.transform, "quilt", new Vector3(0, 0.52f, 0.38f), new Vector3(0.78f, 0.06f, 1.0f), Ponyo.Hex("3A86E0"));
            Firm.Box(g.transform, "quilt band", new Vector3(0, 0.555f, 0.0f), new Vector3(0.79f, 0.01f, 0.14f), Ponyo.Hex("F7C548"));
            Firm.Box(g.transform, "pillow", new Vector3(0, 0.54f, -0.7f), new Vector3(0.56f, 0.12f, 0.32f), Ponyo.Paper);
            foreach (Transform c in Sleep.Cot) if (c.name == "canvas" || c.name == "blanket" || c.name == "pillow") c.gameObject.SetActive(false);
            return g;
        }
        // boat upgrades: their props on her (a model if there is one, else a small code-built part), in her own frame
        GameObject BoatProp(string item)
        {
            var bits = item.Split(':'); var b = Skiff.Of(bits[1]); if (b == null) return null; string u = bits[2];
            if (u == "engine1" || u == "engine2" || u == "tank" || u == "autopilot") return null;   // inside the engine / tank: no new prop, the stat changes
            var pf = M1Assets.I.M5Prefab("Prop_up_" + u);
            var g = pf != null ? Instantiate(pf) : new GameObject(item);
            if (pf != null) Util.Toon(g, Game.ToonMat);
            g.name = item; g.transform.SetParent(b.transform, false);
            var h = b.Hull; float top = h.max.y + 0.02f;
            switch (u)
            {
                case "lights":
                    g.transform.localPosition = Vector3.zero;
                    if (pf == null) for (int i = 0; i < 2; i++)
                        {
                            var p = new Vector3(i == 0 ? h.min.x + 0.08f : h.max.x - 0.08f, top, h.center.z + 0.4f);
                            Firm.Box(g.transform, "lamp post", p + Vector3.up * 0.3f, new Vector3(0.05f, 0.6f, 0.05f), Ponyo.Hex("6E5038"));
                            var lamp = Firm.Box(g.transform, "lamp", p + Vector3.up * 0.66f, new Vector3(0.16f, 0.14f, 0.16f), Ponyo.Hex("FFE7A0"));
                            lamp.name = "glow";
                        }
                    foreach (var t in g.GetComponentsInChildren<Transform>()) if (t.name.Contains("glow") || t.name.Contains("lamp"))
                        {
                            var lt = new GameObject("deck light").AddComponent<Light>(); lt.transform.SetParent(t, false); lt.type = LightType.Point; lt.range = 5; lt.intensity = 0; lt.color = Ponyo.Hex("FFD9A0"); lt.shadows = LightShadows.None;
                            g.AddComponent<NightLight>().L.Add(lt); break;
                        }
                    break;
                case "pump": g.transform.localPosition = new Vector3(h.center.x + 0.35f, h.min.y + 0.45f, h.min.z + 0.7f); if (pf == null) { Firm.Box(g.transform, "pump", Vector3.zero, new Vector3(0.22f, 0.2f, 0.3f), Ponyo.Hex("E2553F")); Firm.Box(g.transform, "hose", new Vector3(0.12f, 0.15f, 0), new Vector3(0.05f, 0.05f, 0.5f), Ponyo.Hex("3F2C27")); } break;
                case "plating":
                    g.transform.localPosition = Vector3.zero;
                    if (pf == null) for (int sx = -1; sx <= 1; sx += 2) Firm.Box(g.transform, "plate", new Vector3(sx > 0 ? h.max.x + 0.015f : h.min.x - 0.015f, Sea.Level - b.transform.position.y + 0.1f + 0.15f, h.center.z), new Vector3(0.02f, 0.22f, h.size.z * 0.82f), Ponyo.Hex("A8B2BC"));
                    break;
                case "holders": g.transform.localPosition = Vector3.zero; if (pf == null) for (int i = 0; i < 3; i++) Firm.Box(g.transform, "holder", new Vector3(h.max.x - 0.06f, top + 0.12f, h.min.z + 0.8f + i * 0.5f), new Vector3(0.06f, 0.3f, 0.06f), Ponyo.Hex("C9CED3"), new Vector3(-20, 0, -10)); break;
                case "finder": g.transform.localPosition = (b.Helm != null ? b.transform.InverseTransformPoint(b.Helm.position) : new Vector3(0, top, h.min.z + 0.8f)) + new Vector3(-0.35f, 0.45f, 0.35f); if (pf == null) { Firm.Box(g.transform, "finder", Vector3.zero, new Vector3(0.22f, 0.16f, 0.06f), Ponyo.Hex("3F2C27")); var sc = Firm.Box(g.transform, "screen glow", new Vector3(0, 0, 0.032f), new Vector3(0.18f, 0.12f, 0.005f), Ponyo.Hex("46D6C8")); } break;
                case "cap": g.transform.localPosition = new Vector3(h.min.x + 0.3f, top + 0.02f, h.min.z + 0.5f); if (pf == null) { Firm.Box(g.transform, "cap", Vector3.zero, new Vector3(0.12f, 0.05f, 0.12f), Ponyo.Hex("F7C548")); Firm.Box(g.transform, "lock", new Vector3(0, 0.05f, 0), new Vector3(0.05f, 0.05f, 0.03f), Ponyo.Hex("B9B2A6")); } break;
                default: g.transform.localPosition = new Vector3(0, top, 0); break;
            }
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return g;
        }

        // ---- the buy feedback in the world: squash and settle, dust, a thup; the 07:00 builds: a scaffold timelapse
        void Fx(string item)
        {
            var at = FxPoint(item); if (!at.HasValue) return;
            Dust(at.Value, item.StartsWith("dock:") || item.StartsWith("wh:") || item.StartsWith("office:") ? 2.2f : 0.9f);
            Snd.Play(Synth.Thup, at.Value, 0.9f, 0.9f);
        }
        Vector3? FxPoint(string item)
        {
            if (item.StartsWith("office:")) return office != null ? office.position : (Vector3?)null;
            if (item.StartsWith("wh:")) return warehouse != null ? warehouse.position : (Vector3?)null;
            if (item.StartsWith("up:") || item.StartsWith("rep:")) { var b = Skiff.Of(item.Split(':')[1]); return b != null ? b.transform.position : (Vector3?)null; }
            if (item == "boat:kestrel") return Kestrel != null ? Kestrel.transform.position : (Vector3?)null;
            if (shown.TryGetValue(item, out var g) && g != null) return g.transform.position;
            var s = SpotOf(item); return s != null && Spots.TryGetValue(s, out var t) ? t.position : (Vector3?)null;
        }
        public static IEnumerator Pop(GameObject g)
        {
            if (g == null) yield break; var t = g.transform; var s0 = t.localScale;
            for (float k = 0; k < 0.6f; k += Time.deltaTime)
            {
                if (t == null) yield break;
                float u = k / 0.6f;
                float y = u < 0.45f ? Util.EaseOutBack(u / 0.45f) * 1.12f : 1.12f - 0.12f * Mathf.Sin((u - 0.45f) / 0.55f * Mathf.PI * 1.5f) * (1 - u);   // grow past, settle
                float xz = u < 0.45f ? Util.EaseOutBack(u / 0.45f) * 0.94f : Mathf.Lerp(0.94f, 1f, (u - 0.45f) / 0.55f) + 0.05f * Mathf.Sin((u - 0.45f) / 0.55f * Mathf.PI * 2) * (1 - u);   // squash
                t.localScale = new Vector3(s0.x * xz, s0.y * Mathf.Max(0.01f, y), s0.z * xz);
                yield return null;
            }
            if (t != null) t.localScale = s0;
        }
        // the morning timelapse: four scaffold poles and planks rise, the thing grows up inside them, the scaffold drops
        IEnumerator Timelapse(GameObject g)
        {
            if (g == null) yield break;
            var rs = g.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) { yield return Pop(g); yield break; }
            var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
            var sc = new GameObject("scaffold").transform; sc.position = new Vector3(bb.center.x, bb.min.y, bb.center.z);
            Color pole = Ponyo.Hex("B9B2A6"), board = Ponyo.Hex("C9A27A");
            var e = bb.extents + new Vector3(0.3f, 0, 0.3f); float h = bb.size.y + 0.4f;
            for (int i = 0; i < 4; i++) Firm.Box(sc, "pole", new Vector3((i % 2 == 0 ? -1 : 1) * e.x, h / 2, (i < 2 ? -1 : 1) * e.z), new Vector3(0.08f, h, 0.08f), pole);
            for (int k = 1; k <= 3; k++) { Firm.Box(sc, "board", new Vector3(0, h * k / 4, -e.z), new Vector3(e.x * 2, 0.06f, 0.3f), board); Firm.Box(sc, "board", new Vector3(0, h * k / 4, e.z), new Vector3(e.x * 2, 0.06f, 0.3f), board); }
            var t = g.transform; var s0 = t.localScale; t.localScale = new Vector3(s0.x, 0.01f, s0.z);
            sc.localScale = new Vector3(1, 0.01f, 1);
            for (float k = 0; k < 0.5f; k += Time.deltaTime) { sc.localScale = new Vector3(1, Util.EaseOutBack(k / 0.5f), 1); yield return null; }
            sc.localScale = Vector3.one;
            Snd.Play(Synth.Creak, bb.center, 0.6f);
            for (float k = 0; k < 1.6f; k += Time.deltaTime)
            {
                if (t == null) break;
                float u = k / 1.6f; t.localScale = new Vector3(s0.x, s0.y * Mathf.Max(0.01f, Mathf.Floor(u * 8) / 8 + 0.02f), s0.z);   // stepped: a timelapse
                if (Mathf.FloorToInt(u * 8) != Mathf.FloorToInt((k - Time.deltaTime) / 1.6f * 8)) Snd.Play(Synth.Thup, bb.center, 0.35f, 1.2f + u * 0.4f);
                yield return null;
            }
            if (t != null) t.localScale = s0;
            Dust(bb.center - Vector3.up * bb.extents.y, Mathf.Max(bb.extents.x, bb.extents.z));
            for (float k = 0; k < 0.4f; k += Time.deltaTime) { sc.localScale = new Vector3(1, 1 - k / 0.4f, 1); yield return null; }
            Destroy(sc.gameObject);
            Snd.Play(Synth.Chime, bb.center, 0.5f, 1.1f);
        }
        static void Dust(Vector3 at, float size)
        {
            var g = new GameObject("dust"); g.transform.position = at + Vector3.up * 0.1f;
            var ps = g.AddComponent<ParticleSystem>(); ps.Stop();
            var m = ps.main; m.duration = 0.4f; m.loop = false; m.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f); m.startSpeed = new ParticleSystem.MinMaxCurve(0.6f * size, 1.4f * size);
            m.startSize = new ParticleSystem.MinMaxCurve(0.25f * size, 0.5f * size); m.startColor = new Color(0.96f, 0.92f, 0.84f, 0.85f); m.gravityModifier = -0.05f; m.maxParticles = 40; m.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0, 18) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.4f * size; sh.rotation = new Vector3(-90, 0, 0);
            var co = ps.colorOverLifetime; co.enabled = true; var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0.8f, 0), new GradientAlphaKey(0, 1) }); co.color = gr;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, 0.6f), new Keyframe(1, 1.6f)));
            var r = g.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Util.Soft(Color.white, Util.Foam); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play(); Destroy(g, 2f);
        }

        // ---------------------------------------------------------------- the prompts (Angler.Interact)
        public static string Prompt(Angler me)
        {
            if (I == null || me.Remote || me.Swimming || me.OnBoat || YShop.Open || Wardrobe.Open || StoreUi.Open) return null;
            var s = I.Near;
            if (s != null)
            {
                if (!s.IsOpen(Sky.Minutes)) return $"{s.Name} · {Catalog.ClosedSign(s.S, Sky.Minutes)}";
                if (Ctl.Interact) YShop.Show(s);
                return $"{s.Name} · {(s.S == Store.Doyle ? "boats, repairs, paint" : s.S == Store.Plank ? "builds, beds, furniture" : "clothes and dyes")}";
            }
            // the wardrobe: any bed, or the office's wardrobe / locker
            var wpt = I.WardrobePoint(me.transform.position);
            if (wpt != null) { if (Ctl.Interact) Wardrobe.Show(); return "Wardrobe · change clothes"; }
            // the warehouse's door
            var door = I.WarehouseDoor;
            if (door.HasValue && Vector3.Distance(door.Value, me.transform.position) < 2.2f) { if (Ctl.Interact) StoreUi.Show(); return $"Warehouse · {C.WhFish} fish stored"; }
            return null;
        }
        Transform WardrobePoint(Vector3 p)
        {
            foreach (var id in new[] { "furn:wardrobe" }) if (shown.TryGetValue(id, out var g) && g != null && Vector3.Distance(g.transform.position, p) < 1.8f) return g.transform;
            if (Spots.TryGetValue("spot_locker", out var lk) && Vector3.Distance(lk.position, p) < 1.6f) return lk;
            return null;
        }
        // a bed you're standing at also opens the wardrobe (Sleep shows it as a second line): W at a bed
        public static bool AtBed(Angler me) => Sleep.Cot != null && Vector3.Distance(Sleep.Cot.position, me.transform.position) < 1.7f;

        // ---------------------------------------------------------------- the actions (this machine)
        public static string MyKey => Firm.MyKey;
        public static string MyName => Firm.MyName;
        public static Action<string, bool, string> Done;   // (item, ok, why): YShop's feedback
        public static void Buy(string item, int arg, bool instant = false)
        {
            var w = Catalog.Get(item); if (w == null) return;
            string pre = PreCheck(w); if (pre != null) { Done?.Invoke(item, false, pre); Alerts.Push(StoreName(w.At), pre, Alerts.Kind.Info); return; }
            if (Coop.On && !Coop.IsHost) { CoopM5.Ask(0, Firm.I.NewTx("m5"), item, arg, instant); return; }
            if (C.NeedsAsk(item, arg, MyKey, OthersOnline, instant)) { AskCrew(item, arg); return; }
            HostBuy(Firm.I.NewTx("m5"), MyKey, MyName, item, arg, instant, out var why);
            Done?.Invoke(item, why == "", why);
            if (why != "") Alerts.Push(StoreName(w.At), why, Alerts.Kind.Info);
        }
        // what only this world knows: Doyle's crew paints her at the landing (moored)
        static string PreCheck(Ware w)
        {
            if (w.Fit == Fit.Paint) { var b = Skiff.Of(w.Boat); if (b != null && !b.Moored) return $"Moor {b.Name} at the landing first: Doyle's crew paints her there."; }
            return null;
        }
        public static string StoreName(Store s) => s == Store.Doyle ? "Doyle Boatworks" : s == Store.Plank ? "Plank & Post" : "Hartley's Outfitters";
        // host (or solo): the buy itself
        public static bool HostBuy(string tx, string key, string who, string item, int arg, bool instant, out string why)
        {
            var w = Catalog.Get(item);
            if (w != null && w.Fit == Fit.Paint) { var b = Skiff.Of(w.Boat); if (b != null && I != null) Company.PaintWalk = Mathf.Clamp(I.CrewWalk(b), 15, 90); }
            bool ok = C.BuyM5(tx, key, who, item, arg, Sky.Minutes, instant, out why);
            if (ok) { Firm.I.Dirty = true; Log($"{who} bought {item}{(arg != 0 ? " (" + arg + ")" : "")}{(instant ? " (now)" : "")}: bank ${C.Cash}"); }
            return ok;
        }
        float CrewWalk(Skiff b)
        {
            var d = Shops.Find(s => s.S == Store.Doyle); if (d == null) return 20;
            var p = Path(d.At.position + d.At.forward * 2.2f, PaintSpot(b, 0, 0).pos); return Length(p) / 1.5f;   // 1.5 m/s: a game minute is a real second
        }
        public static void AskCrew(string item, int arg)
        {
            if (Coop.On && !Coop.IsHost) { CoopM5.Ask(1, "", item, arg, false); return; }
            var a = C.AskCrew(MyKey, MyName, item, arg, 1, Sky.Minutes); Firm.I.Dirty = true;
            Alerts.Push("Ask the crew", $"Asked the crew: {Catalog.Get(item)?.Name} for ${a.Price}. One OK buys it.", Alerts.Kind.Info, "ask:" + a.Id);
            Log($"asked the crew: {item} ${a.Price} (#{a.Id})");
        }
        public static void Answer(int id, bool yes)
        {
            if (Coop.On && !Coop.IsHost) { CoopM5.Ask(2, "", "", id, yes); return; }
            if (C.AnswerAsk(id, MyKey, !Coop.On || Coop.IsHost, yes, MyName, Sky.Minutes, out var why)) { Firm.I.Dirty = true; Log($"ask #{id}: {(yes ? "OK" : "no")} from {MyName}"); }
            else if (why != "") Alerts.Push("Ask the crew", why, Alerts.Kind.Info);
        }
        public static void Wear(string item, int slot)
        {
            if (Coop.On && !Coop.IsHost) { CoopM5.Ask(3, "", item ?? "", slot, false); return; }
            if (item == null) C.WearOwn(MyKey, slot); else C.WearIt(MyKey, item);
            Firm.I.Dirty = true;
        }
        public static string Pick(int preset)
        {
            if (Coop.On) { Coop.PickCrew(preset); return ""; }
            var why = C.PickPreset(MyKey, preset, new[] { MyKey }); if (why == "") { Firm.I.Dirty = true; if (Angler.I != null && Angler.Body != preset) { Angler.I.Fish.Stow(); Angler.I.Wear(preset, Coop.Colours[0]); } }
            return why;
        }
        public static void StoreFish(List<Fish> fish)
        {
            if (fish.Count == 0) return;
            if (Coop.On && !Coop.IsHost) { CoopM5.Store(fish); return; }
            int n = HostStore(Coop.On ? Coop.Me : 0, fish);
            if (n < fish.Count) Alerts.Push("Warehouse", n == 0 ? "No room in the warehouse." : $"Stored {n}. No room for the rest.", Alerts.Kind.Info);
        }
        public static int HostStore(ulong who, List<Fish> fish)
        {
            int n = 0; var me = Angler.I;
            foreach (var f in fish)
            {
                var wf = new WFish { Species = f.Species.Id, Grade = f.Grade, Cm = f.Cm, Kg = f.Kg, Value = f.Value, CaughtAt = f.CaughtAt, Id = f.Id };
                if (C.Put(wf, Sky.Minutes, C.ColdSlots > 0) < 0) break;
                if (Coop.On) { if (!Coop.I.Book.Sell(who, new[] { f.Id })) { C.Take(C.Wh.FindIndex(s => s.Fish.Contains(wf)), Sky.Minutes); break; } }
                else me.Bar.Remove(f);
                n++;
            }
            if (Coop.On && n > 0) Coop.I.Bar(who);
            if (n > 0) { Firm.I.Dirty = true; Snd.Play2D(Synth.Plunk, 0.4f, 0.9f); }
            return n;
        }
        public static void Take(int slot, bool all)
        {
            if (Coop.On && !Coop.IsHost) { CoopM5.Take(slot, all); return; }
            HostTake(Coop.On ? Coop.Me : 0, slot, all);
        }
        public static int HostTake(ulong who, int slot, bool all)
        {
            int n = 0;
            while (slot >= 0 && slot < C.Wh.Count && C.Wh[slot].Fish.Count > 0)
            {
                var top = C.Wh[slot].Fish[^1]; var sp = FishDb.Get(top.Species); if (sp == null) break;
                var bar = Coop.On ? Coop.I.Book.Bar(who) : Angler.I.Bar;
                if (!bar.CanFit(new[] { new Fish { Species = sp, Grade = top.Grade } })) { if (n == 0) Alerts.Push("Warehouse", "Hotbar full.", Alerts.Kind.Info); break; }
                var wf = C.Take(slot, Sky.Minutes); float caught = (float)(Sky.Minutes - Company.AgeOf(wf, Sky.Minutes));   // the cold room's slower rot comes with it
                if (Coop.On) { var f = Coop.I.Book.Make(who, sp, wf.Grade, wf.Cm, wf.Kg, caught); Coop.I.Book.Keep(who, f.Id); }
                else Angler.I.Bar.Add(new Fish { Species = sp, Grade = wf.Grade, Cm = wf.Cm, Kg = wf.Kg, CaughtAt = caught });
                n++; if (!all) break;
            }
            if (Coop.On && n > 0) Coop.I.Bar(who);
            if (n > 0) { Firm.I.Dirty = true; Snd.Play2D(Synth.Plip, 0.4f, 1.1f); }
            return n;
        }

        // ---------------------------------------------------------------- testers: F7 raises the story act and adds $5,000
        void Tester()
        {
            var kb = Keyboard.current; if (kb == null || !kb.f7Key.wasPressedThisFrame || Title.Shown) return;
            if (Coop.On && !Coop.IsHost) { Alerts.Push("Tester", "F7 is the host's.", Alerts.Kind.Info); return; }
            C.Act = C.Act >= 4 ? 1 : C.Act + 1; C.Earn(Firm.I.NewTx("tester"), MyName, "Tester cash (F7)", 5000, Sky.Minutes); Firm.I.Dirty = true;
            Alerts.Push("Tester", $"Act {C.Act} · +$5,000 (F7 again: next act)", Alerts.Kind.Info); Log($"F7: act {C.Act}, bank ${C.Cash}");
        }
    }

    // a deck light's lamps: on from dusk to dawn
    public class NightLight : MonoBehaviour
    {
        public readonly List<Light> L = new();
        void Update() { bool night = DayTime.SunElevation(Sky.Minutes) < 2; foreach (var l in L) if (l != null) l.intensity = Mathf.MoveTowards(l.intensity, night ? 1.6f : 0, Time.deltaTime * 2); }
    }

    // A town NPC (v1): the M1 dock worker with his palette look, walking on the town's navmesh; everything it does is a
    // pose from the game clock (Works decides where), so co-op machines agree with no messages.
    public class Npc : MonoBehaviour
    {
        public string Name; CharAnim anim; Vector3 last; float speed; Transform roller; bool painting; GameObject body;
        public static Npc Make(string name, PortBoss.Look look, Transform near)
        {
            var a = M1Assets.I; var g = new GameObject("NPC " + name); var n = g.AddComponent<Npc>(); n.Name = name;
            if (near != null) g.transform.position = near.position;
            if (a.Worker == null) return n;
            var w = PortBoss.Worker.Make(a.Worker, g.transform); w.Apply(look);
            var rs = w.GetComponentsInChildren<Renderer>(); var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
            if (bb.size.y > 0.5f) w.transform.localScale = Vector3.one * Mathf.Clamp(1.74f / bb.size.y, 0.8f, 1.25f);
            foreach (var c in w.GetComponentsInChildren<Collider>()) Destroy(c);
            var an = w.GetComponentInChildren<Animator>();
            var skin = ScriptableObject.CreateInstance<PortBoss.Skin>(); skin.Idle = a.Idle; skin.Walk = a.Walk; skin.Run = a.Run; skin.Sprint = a.Sprint; skin.Jump = a.Jump; skin.Land = a.Land;
            skin.SwimIdle = a.SwimIdle; skin.Swim = a.Swim; skin.Climb = a.Climb; skin.Drive = a.Idle; skin.Push = a.Idle;
            n.anim = an.gameObject.AddComponent<CharAnim>(); n.anim.Init(an, skin); n.anim.Facing = g.transform; n.anim.GripLean = 0;
            n.body = w.gameObject; n.last = g.transform.position;
            return n;
        }
        // a paint roller in the right hand (Doyle's lads)
        public void Roller(Color c)
        {
            var hand = body != null ? body.GetComponentInChildren<Animator>()?.GetBoneTransform(HumanBodyBones.RightHand) : null;
            roller = new GameObject("roller").transform; roller.SetParent(hand != null ? hand : transform, false);
            Firm.Box(roller, "handle", new Vector3(0, -0.02f, 0.12f), new Vector3(0.03f, 0.03f, 0.3f), Ponyo.Hex("6E5038"));
            Firm.Box(roller, "roll", new Vector3(0, -0.02f, 0.3f), new Vector3(0.22f, 0.07f, 0.07f), c);
            roller.gameObject.SetActive(false);
        }
        public void Paint(bool on) { painting = on; if (roller != null) roller.gameObject.SetActive(on); }
        public void Show(bool on) { if (body != null && body.activeSelf != on) body.SetActive(on); }
        public void Hold(Vector3 p, Vector3 face, bool walking)
        {
            face.y = 0; transform.position = p; if (face.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face.normalized), 1 - Mathf.Exp(-10 * Time.deltaTime));
            float dt = Mathf.Max(Time.deltaTime, 1e-4f); float v = new Vector3(p.x - last.x, 0, p.z - last.z).magnitude / dt; last = p;
            speed = Mathf.Lerp(speed, walking ? Mathf.Min(v, 2.5f) : 0, 1 - Mathf.Exp(-8 * dt));
        }
        void LateUpdate()
        {
            if (anim == null || body == null || !body.activeSelf) return;
            anim.Tick(speed > 0.15f ? CharAnim.Slot.Walk : CharAnim.Slot.Idle, speed);
            if (painting && roller != null) roller.parent.localRotation *= Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 7) * 25);   // rolling up and down the planks
        }
    }
}
