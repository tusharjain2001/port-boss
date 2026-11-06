using System;
using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    // M6 · workers in the world (systems "M6", §1.6.8; economy §4.5, §5.4). The host (or solo) runs every shift: at the
    // shift's start the worker walks down the pier, casts off and sails the real boat out on the sea grid's A* path at ⅔
    // throttle, fishes at the spot (anchored), sails home, moors, and carries the catch to the warehouse. Near a player the
    // boat really drives (rudder and throttle, the boat's own physics); far from everyone it is moved along the same path by
    // the clock (the phone map's dot), so both agree with the formula's times. Rosie's hiring board stands at The Mooring
    // Line (11:00 - 02:00); hiring there marks the board seen (the phone can hire after that); assigning is the phone's.
    public class Workers : MonoBehaviour
    {
        public static Workers I; public static Action<string> OnLog;
        static void Log(string s) { Debug.Log("[PB] workers: " + s); OnLog?.Invoke(s); }
        static Company C => Firm.C;
        public const float NearM = 320;                                // a player this close: the boat drives for real
        public const double PubOpen = 11 * 60, PubClose = 26 * 60;    // 11:00 - 02:00

        public Transform Board; public Vector3 BoardFront; public Npc Rosie; public bool NearBoard;
        readonly Dictionary<int, Npc> npcs = new();
        readonly Dictionary<int, (List<Vector2> path, float len)> paths = new();
        readonly Dictionary<string, Transform> berths = new();
        readonly HashSet<string> warned = new();
        public static bool Authority => !Coop.On || Coop.IsHost;
        public static readonly Dictionary<string, string> Live = new();   // boat -> "driving" | "clock" (tests)

        public static void Create() { var g = new GameObject("Workers"); I = g.AddComponent<Workers>(); I.Build(); }
        void Build()
        {
            var at = Places.Get("pub");
            if (at == null)
            {   // the M1 landing: a stand-in spot beside the old shack (map B has The Mooring Line)
                at = new GameObject("anchor_pub (temp)").transform; at.SetParent(transform, false);
                var l = Landing.I; if (l != null && l.Spawn != null) { var p = Works.Blender(new Vector3(-3.5f, -6f, 0)); var q = Works.Blender(new Vector3(-3.5f, -7f, 0)) - p; q.y = 0; at.SetPositionAndRotation(Works.Ground(p), Quaternion.LookRotation(q.sqrMagnitude > 0 ? q.normalized : Vector3.forward)); }
            }
            Board = at; BoardFront = at.position + at.forward * 1.2f;
            var root = new GameObject("Rosie's board").transform; root.SetParent(at, false);
            Color wood = Ponyo.Hex("7A5A40"), cork = Ponyo.Hex("C99A62"), paper = Ponyo.Hex("F3EBDD");
            for (int i = -1; i <= 1; i += 2) Firm.Box(root, "leg", new Vector3(0.62f * i, 0.8f, 0), new Vector3(0.08f, 1.6f, 0.08f), wood);
            Firm.Box(root, "frame", new Vector3(0, 1.45f, 0), new Vector3(1.42f, 0.92f, 0.07f), wood);
            Firm.Box(root, "cork", new Vector3(0, 1.45f, 0.03f), new Vector3(1.3f, 0.8f, 0.03f), cork);
            for (int i = 0; i < 3; i++)
            {
                float x = -0.42f + i * 0.42f; cards[i] = new GameObject("card " + i); cards[i].transform.SetParent(root, false);
                var ct = cards[i].transform; Firm.Box(ct, "card", new Vector3(x, 1.5f, 0.05f), new Vector3(0.32f, 0.42f, 0.01f), paper); Firm.Box(ct, "pin", new Vector3(x, 1.68f, 0.06f), new Vector3(0.04f, 0.04f, 0.02f), Ponyo.Poppy);
                cardText[i, 0] = Line(ct, new Vector3(x, 1.55f, 0.058f), Ponyo.Ink, 0.011f);       // initials
                cardText[i, 1] = Line(ct, new Vector3(x, 1.42f, 0.058f), Ponyo.Ink, 0.0042f);  // first name
                cardText[i, 2] = Line(ct, new Vector3(x, 1.35f, 0.058f), Ponyo.Ink, 0.0042f);      // the wage
            }
            Firm.Box(root, "header", new Vector3(0, 2.0f, 0), new Vector3(1.5f, 0.2f, 0.06f), Ponyo.Hex("2E3F5C"));
            Firm.Sign(root, "HANDS WANTED · ROSIE'S", new Vector3(0, 2.0f, 0.035f), Ponyo.Paper, 0.0062f);
            var col = root.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, 1.1f, 0); col.size = new Vector3(1.5f, 2.2f, 0.2f);
            Rosie = Npc.Make("Rosie Tull", new PortBoss.Look { Set = 1, Face = 1, Hair = 3, Beard = 0, Hat = 0, Top = 3, Gloves = 0, SkinTone = 2, HairColor = 3, TopColor = 4, AccentColor = 4, LegsColor = 1, BootsColor = 7, GloveColor = 4, HatColor = 2 }, at);
            Rosie.Hold(Works.Ground(at.position + at.right * 1.3f + at.forward * 0.4f), at.forward, false);
            SeaGrid.Build();
            Log($"Rosie's board at {(Places.Get("pub") != null ? "The Mooring Line" : "a temporary spot")} {at.position.x:F0} {at.position.z:F0}");
        }

        readonly GameObject[] cards = new GameObject[3]; readonly TextMesh[,] cardText = new TextMesh[3, 3]; string cardKey;
        static TextMesh Line(Transform root, Vector3 p, Color col, float size) { Firm.Sign(root, "", p, col, size); return root.GetChild(root.childCount - 1).GetComponent<TextMesh>(); }
        void Cards()
        {
            string key = string.Join(",", C.Board.ConvertAll(x => x.Id + ":" + x.Wage)); if (key == cardKey) return; cardKey = key;
            for (int i = 0; i < 3; i++)
            {
                var w = i < C.Board.Count ? C.Board[i] : null; if (cards[i] != null) cards[i].SetActive(w != null); if (w == null) continue;
                var parts = w.Name.Split(' '); cardText[i, 0].text = string.Concat(Array.ConvertAll(parts, x => x.Length > 0 ? x.Substring(0, 1) : ""));
                cardText[i, 1].text = parts[0].ToUpperInvariant(); cardText[i, 2].text = $"${w.Wage}/day";
            }
        }
        // a walk point on the planks: a navmesh corner, or a straight leg down to the ladder, can sit under the pier's deck
        static Vector3 Feet(Vector3 p) => Physics.Raycast(p + Vector3.up * 2.5f, Vector3.down, out var h, 4, ~0, QueryTriggerInteraction.Ignore) && !(h.collider is CharacterController) ? h.point : p;

        public static bool PubOpenAt(double m) { double h = DayTime.OfDay(m); return h >= PubOpen || h < PubClose - 24 * 60; }

        // ---------------------------------------------------------------- the prompt (Angler.Interact, after Works)
        public static string Prompt(Angler me)
        {
            if (I == null || me.Remote || me.Swimming || me.OnBoat) return null;
            var d = I.BoardFront - me.transform.position; d.y *= 0.5f; I.NearBoard = d.magnitude < 2.0f;
            if (!I.NearBoard) return null;
            if (!PubOpenAt(Sky.Minutes)) return "The Mooring Line · Rosie's back at 11 AM";
            if (C.Act < 2) return "Rosie's hiring board · opens in Act 2";
            if (Ctl.Interact) { See(); Phone.I.Show(Phone.Scr.Crew); Phone.I.CrewTab = 1; }
            return $"Rosie's hiring board · {C.Board.Count} looking for work";
        }
        public static void See() { if (C.BoardSeen) return; if (Coop.On && !Coop.IsHost) { CoopM5.Ask(10, "", "", 0, false); return; } C.BoardSeen = true; Firm.I.Dirty = true; }

        // ---------------------------------------------------------------- the actions (any machine: a client asks the host)
        public static Action<string, bool, string> Done;
        public static void Hire(int boardId, bool fromPhone) { if (Coop.On && !Coop.IsHost) { CoopM5.Ask(6, Firm.I.NewTx("hire"), "", boardId, fromPhone); return; } var why = C.Hire(Firm.I.NewTx("hire"), Firm.MyName, boardId, Sky.Minutes, fromPhone); After("hire", why); }
        public static void Fire(int id) { if (Coop.On && !Coop.IsHost) { CoopM5.Ask(8, Firm.I.NewTx("fire"), "", id, false); return; } var why = C.Fire(Firm.I.NewTx("fire"), Firm.MyName, id, Sky.Minutes); After("fire", why); }
        public static void Assign(int id, string boat, string spot, string bait, Shift sh, int backBy = 0, bool buyBait = true)
        {
            string item = $"{boat ?? "-"}|{spot}|{bait}|{(int)sh}|{backBy}|{(buyBait ? 1 : 0)}";
            if (Coop.On && !Coop.IsHost) { CoopM5.Ask(7, "", item, id, false); return; }
            After("assign", C.Assign(id, boat, spot, bait, sh, backBy, buyBait));
        }
        public static string HostAssign(int id, string item) { var p = item.Split('|'); return p.Length < 5 ? "?" : C.Assign(id, p[0] == "-" ? null : p[0], p[1], p[2], (Shift)int.Parse(p[3]), int.Parse(p[4]), p.Length < 6 || p[5] != "0"); }
        public static void LeaveBait(int i)
        {
            if (Coop.On && !Coop.IsHost)
            {   // my pouch is mine (I report it to the host): take the bait out here, the host shelves it
                var k = Firm.I.MyKit; if (i < 0 || i >= Company.CrewBaits.Length) return; int n = Math.Min(10, k.Bait[i]); if (n <= 0) return;
                k.Bait[i] -= n; CoopM5.Ask(12, "", n.ToString(), i, false); return;
            }
            After("bait", C.LeaveBait(Firm.MyKey, i));
        }
        public static void Tow(int id) { string tx = Firm.I.NewTx("tow"); if (Coop.On && !Coop.IsHost) { CoopM5.Ask(11, tx, "", id, false); return; } After("tow", C.Tow(tx, Firm.MyName, id, Sky.Minutes)); }
        public static void Recall(int id) { if (Coop.On && !Coop.IsHost) { CoopM5.Ask(9, "", "", id, false); return; } HostRecall(id); }
        public static string HostRecall(int id)
        {
            var why = C.Recall(id, Sky.Minutes); if (why == "") Firm.I.Dirty = true; return why;
        }
        static void After(string what, string why) { if (why != "") Alerts.Push("Crew", why, Alerts.Kind.Info); else Firm.I.Dirty = true; Done?.Invoke(what, why == "", why); }

        // ---------------------------------------------------------------- every frame
        void Update()
        {
            if (C == null) return;
            double m = Sky.Minutes; int today = DayTime.Day(m);
            foreach (var w in C.Staff) { NpcOf(w); }
            foreach (var k in new List<int>(npcs.Keys)) if (!C.Staff.Exists(x => x.Id == k)) { if (npcs[k] != null) Destroy(npcs[k].gameObject); npcs.Remove(k); }
            // Rosie: at her board while the pub's open
            if (Rosie != null) Rosie.Show(PubOpenAt(m));
            Cards();
            foreach (var w in C.Staff.ToArray())   // (a copy: landing can take a quitter off the crew)
            {
                var b = w.Boat != null ? Skiff.Of(w.Boat) : null;
                if (Authority) Shift(w, b, m, today);
                Pose(w, b, m);
            }
        }

        // the host: starts, sails and lands a worker's shift
        void Shift(Worker w, Skiff b, double m, int today)
        {
            var r = w.Run;
            if (b == null) { if (r != null && !r.Landed && m >= r.HomeAt) LandNow(w, null, m); return; }   // her boat's gone (sold): home on foot
            if (today < w.StartDay) return;
            double start = DayTime.At(today, Company.ShiftFrom[(int)w.Sh] - (w.Quirk == "Early riser" ? 60 : 0)), end = DayTime.At(today, Company.BackOf(w));
            if ((r == null || r.Day != today) && m >= start && m < end - 90)
            {
                string key = $"{w.Id}:{today}";
                string why = !b.Moored ? $"{b.Name} isn't at her berth." : Aboard(b) ? $"Someone's aboard {b.Name}." : null;
                if (why != null) { if (warned.Add(key)) { C.Text(w.Name, $"Ready to go, but {why} I'll wait.", m); Firm.I.Dirty = true; } return; }
                var berth = b.MoorAt != null ? b.MoorAt : b.transform;
                var spotAt = SpotPos(w.Spot, berth.position);
                var path = SeaGrid.Find(Flat(berth.position), spotAt); float len = SeaGrid.Length(path);
                double travel = Company.Travel(len, b.Top, w.Sh);
                // bait from the warehouse shelf, the rest delivered from June's; fuel to the bank (economy §5.4: ~$18 a shift)
                int need = (int)Math.Ceiling(Company.PlannedCatch(w, w.Boat) * Company.Share(w)), fuel = b.Id == "kestrel" ? 30 : 18; var bp = C.BaitPlan(w, need); int bait = bp.cost;
                if (bp.got == 0)
                { if (warned.Add(key + "b")) { C.Text(w.Name, $"No {w.Bait.ToLower()} on the warehouse shelf and auto-buy is off. Staying ashore today.", m); Firm.I.Dirty = true; } return; }
                if (C.Cash < bait + fuel)
                { if (warned.Add(key + "$")) { C.Text(w.Name, $"Can't buy bait and fuel: the bank's short (${bait + fuel}).", m); Firm.I.Dirty = true; } return; }
                bool storm = Sky.I != null && (Sky.I.WeatherNow.to == Wx.Storm || Sky.I.WeatherNow.from == Wx.Storm);
                r = C.StartShift(w, today, m, travel, storm);
                if (r.St != WSt.NoShow)   // a no-show: no shift, nothing bought, the shelf untouched
                {
                    C.TakeBait(w, need); if (bp.got < need) r.Catch = Math.Min(r.Catch, bp.got);
                    C.Spend($"shift:{w.Id}:{today}", w.Name, bp.bought > 0 ? $"{w.Name}: {bp.bought} {w.Bait.ToLower()} from June's (+${Company.BaitDelivery} delivery) + fuel ({b.Name})" : $"{w.Name}: fuel ({b.Name}); bait from the shelf", bait + fuel, m);
                }
                paths[w.Id] = (path, len);
                C.Text(w.Name, r.St == WSt.NoShow ? NoShowLine(w, r) : $"Leaving for {Company.SpotName(w.Spot)} on {b.Name} 🎣", m);
                Log($"{w.Name}: shift {w.Sh} on {b.Name} to {w.Spot}: {len:F0} m ({travel:F0} min each way), plan {r.Planned}, catch {r.Catch}{(r.Fail != null ? " (" + r.Fail + ")" : "")}");
                Firm.I.Dirty = true;
                if (r.St != WSt.NoShow) { b.CastOff(true); b.Anchored = false; }
                return;
            }
            if (r == null || r.Landed || r.Day != today && m > r.HomeAt + 120 && Company.StateAt(r, m) != WSt.Stranded) { if (r != null && !r.Landed) LandNow(w, b, m); return; }   // aground waits for the tow
            var st = Company.StateAt(r, m);
            if (st == WSt.Fishing && m > (r.FishFrom + r.FishTo) / 2 && texted.Add($"{w.Id}:{r.Day}:catch")) { C.Text(w.Name, $"Got {Company.CatchSoFar(r, m)} so far{(r.Rare > 0 ? $", {Math.Min(r.Rare, Company.CatchSoFar(r, m) / 4)} rare" : "")}. Biting well.", m); Firm.I.Dirty = true; }
            if (st == WSt.Stranded && texted.Add($"{w.Id}:{r.Day}:aground")) { C.Text(w.Name, $"Ran aground off {Company.SpotName(r.Spot)}! Never sinks, but I'm stuck. Doyle can tow me in for $200, or I wait for the 7 AM tide.", m); Firm.I.Dirty = true; }
            if (m >= r.HomeAt && st != WSt.Stranded) LandNow(w, b, m);
        }
        readonly HashSet<string> texted = new(), hooted = new();
        static string NoShowLine(Worker w, ShiftRun r) => r.Fail == "storm-refused" ? "Storm's coming. I get seasick. Not today, sorry." : new[] { "Bit hungover, boss. Not today.", "Sick kid at home. Sorry, not today.", "Overslept. Badly. Tomorrow, promise." }[w.Id % 3];
        void LandNow(Worker w, Skiff b, double m)
        {
            var r = w.Run; if (r.Landed) return;
            if (b != null) Berth(b);
            int got = r.Catch; int stored = 0;
            var rand = new System.Random(Company.CrewSeed * 13 + w.Id * 31 + r.Day);
            float mult = 1 + 0.15f * w.Fishing; int rare = r.Rare;
            for (int i = 0; i < got; i++)
            {
                var sp = FishDb.Roll(rand, 2, r.Bait ?? "Shrimp") ?? (FishDb.All.Count > 0 ? FishDb.All[0] : null); if (sp == null) break;
                FishDb.RollSize(rand, sp, out var cm, out var kg); int g = i < rare ? 1 : 0;
                var wf = new WFish { Species = sp.Id, Grade = g, Cm = cm, Kg = kg, Value = sp.Price * Ponyo.GradeMul[g] * mult * 0.75f, CaughtAt = (r.FishFrom + r.FishTo) / 2, Id = 0 };
                C.EnsureWarehouse(); if (C.Put(wf, m, C.ColdSlots > 0) < 0) break; stored++;
            }
            C.Land(w, m, got);
            string text = got == 0 ? (r.Fail == "recalled" ? "Back in. Empty hold, as you asked." : "Back in. Nothing worth keeping, sorry.") :
                $"Home: {got} fish{(rare > 0 ? $", {rare} rare" : "")}, {stored} in the warehouse{(stored < got ? $" ({got - stored} wouldn't fit)" : "")}." + (r.Fail == "engine" ? " Engine coughed, came in early." : r.Fail == "line" ? " Snapped a line out there." : r.Fail == "storm" ? " Weather turned, came home early." : "");
            C.Text(w.Name, text, m); Firm.I.Dirty = true;
            Log($"{w.Name} home: {got} fish ({rare} rare), {stored} stored, fail {r.Fail ?? "none"}");
            Live.Remove(r.Boat ?? "");
        }
        static bool Aboard(Skiff b)
        {
            if (Angler.I != null && Angler.I.Boat == b && Angler.I.OnBoat) return true;
            if (Coop.On && Coop.I != null) foreach (var m in Coop.I.Mates.Values) if (m != null && m.Boat == b && m.OnBoat) return true;
            return false;
        }
        static Vector2 Flat(Vector3 v) => new(v.x, v.z);
        public static Vector2 SpotPos(string spot, Vector3 from)
        {
            var t = Places.Get(spot); if (t != null) return Flat(t.position);
            // no map anchor (the M1 landing): out to sea in the berth's facing, at the spot's distance
            int i = Math.Max(0, Array.IndexOf(Company.SpotIds, spot)); var dir = Landing.I != null && Landing.I.Moor != null ? -Landing.I.Moor.right : Vector3.forward; dir.y = 0;
            return Flat(from + dir.normalized * (float)(Company.SpotKm[i] * 1000 * 0.25));
        }

        // ---------------------------------------------------------------- the boat and the worker: where they are now
        void Pose(Worker w, Skiff b, double m)
        {
            var r = w.Run; var n = NpcOf(w); if (n == null) return;
            var home = HomeSpot(w);
            if (r == null || r.Day != DayTime.Day(m) && r.Landed || b == null) { n.Carry(false); n.Hold(home, Vector3.forward, false); n.Show(OnDuty(w, m)); return; }
            if (!r.Landed) n.Carry(false);
            var st = Company.StateAt(r, m); var ladder = b.LadderTop != null ? b.LadderTop.position : b.transform.position;
            n.Show(true);
            if (st == WSt.NoShow) { n.Show(false); return; }
            if (r.Landed)
            {   // carrying the catch to the warehouse: 4 minutes up the pier
                double k = (m - r.HomeAt) / 4; var door = Works.I != null && Works.I.WarehouseDoor is { } d ? d : home;
                n.Carry(k < 1 && r.Catch > 0);
                if (k < 1) { var p = Works.Path(ladder, door); var at = Works.Along(p, Works.Length(p) * (float)k, out var dir); n.Hold(Feet(at), dir, true); } else n.Hold(home, Vector3.forward, false);
                return;
            }
            if (st == WSt.Leaving) { double k = (m - r.Start) / Math.Max(1, r.OutAt - r.Start); var p = Works.Path(home, ladder); var at = Works.Along(p, Works.Length(p) * (float)k, out var dir); n.Hold(Feet(at), dir, true); return; }
            // aboard: at the helm (or the stern), riding the boat
            var helm = b.Helm != null ? b.Helm.position : b.transform.TransformPoint(new Vector3(0, 0.6f, -1.6f));
            n.Hold(helm, b.transform.forward, false);
            // the horn wave (§1.6.8): out of the berth, and passing close to me, a toot and a wave (each machine its own)
            if ((st == WSt.Out || st == WSt.Coming) && Angler.I != null)
            {
                float d = Vector3.Distance(Angler.I.transform.position, b.transform.position); string key = $"{w.Id}:{r.Day}:{st}";
                if (d < 45 && d > 6 && hooted.Add(key)) { Snd.Play(Synth.Horn, b.transform.position, 0.9f); n.Wave(2.6f); Log($"{w.Name} toots and waves ({d:F0} m)"); }
            }
            if (Authority) Sail(w, b, r, st, m);
        }
        static bool OnDuty(Worker w, double m) { double h = DayTime.OfDay(m); return h >= 6 * 60 && h < 22 * 60; }
        Vector3 HomeSpot(Worker w)
        {
            var o = Works.I != null && Works.I.Office != null ? Works.I.Office : Board; int k = C.Staff.IndexOf(w);
            return Works.Ground(o.position + o.forward * (2.2f + 0.8f * (k % 3)) + o.right * (1.2f * (k / 3) - 1));
        }
        public Npc NpcOf(Worker w)
        {
            if (npcs.TryGetValue(w.Id, out var n) && n != null) return n;
            var r = new System.Random(w.Id * 7 + 3);
            var look = new PortBoss.Look { Set = 1, Face = (byte)r.Next(4), Hair = (byte)r.Next(4), Beard = (byte)r.Next(4), Hat = (byte)r.Next(4), Top = (byte)r.Next(4), Gloves = (byte)r.Next(2), SkinTone = (byte)r.Next(6), HairColor = (byte)r.Next(8), TopColor = (byte)r.Next(10), AccentColor = 4, LegsColor = (byte)r.Next(3), BootsColor = 7, GloveColor = 4, HatColor = (byte)r.Next(5) };
            n = Npc.Make(w.Name, look, Board); npcs[w.Id] = n; return n;
        }

        // the boat: berth -> spot -> berth along the A* path, by the clock (far) or for real (near a player)
        void Sail(Worker w, Skiff b, ShiftRun r, WSt st, double m)
        {
            if (!paths.TryGetValue(w.Id, out var pl)) { var berth = b.MoorAt != null ? b.MoorAt.position : b.transform.position; var p0 = SeaGrid.Find(Flat(berth), SpotPos(r.Spot, berth)); pl = (p0, SeaGrid.Length(p0)); paths[w.Id] = pl; }
            var path = pl.path; float len = pl.len;
            float d; bool fishing = false;
            if (st == WSt.Out) d = (float)((m - r.OutAt) / Math.Max(1, r.FishFrom - r.OutAt)) * len;
            else if (st == WSt.Fishing || st == WSt.Stranded) { d = len; fishing = true; }
            else d = (float)((1 - (m - r.FishTo) / Math.Max(1, r.HomeAt - r.FishTo)) * Math.Clamp(r.Reach, 0, 1)) * len;
            d = Mathf.Clamp(d, 0, len);
            bool back = st == WSt.Coming;
            var want = SeaGrid.Along(path, d, out var dir); if (back) dir = -dir;
            bool near = NearPlayer(b.transform.position) && !fishing && d > 40 && d < len - 20;
            if (b.Moored) b.CastOff(true);
            if (fishing) { if (!b.Anchored) { Glide(b, want, dir); b.AnchorNow(); } Live[b.Id] = "fishing"; return; }
            b.Anchored = false;
            if (!near) { Glide(b, want, dir); Live[b.Id] = "clock"; return; }
            // for real: steer for a point 30 m further along the path, ⅔ throttle; if she falls 60 m behind the clock, the clock wins
            var ahead = SeaGrid.Along(path, Mathf.Clamp(d + (back ? -30 : 30), 0, len), out _);
            var pos = Flat(b.transform.position);
            if ((pos - want).magnitude > 60 || !SeaGrid.Deep(pos.x, pos.y) && d > 30 && d < len - 30) { Glide(b, want, dir); return; }   // fallen behind, or drifting shallow: the path wins (never beach)
            var to = ahead - pos; float yaw = Mathf.Atan2(to.x, to.y) * Mathf.Rad2Deg, err = Mathf.DeltaAngle(b.transform.eulerAngles.y, yaw);
            b.Notch = 2; b.Steer = Mathf.Clamp(err / 35f, -1, 1); Live[b.Id] = "driving";
        }
        static bool NearPlayer(Vector3 p)
        {
            if (Angler.I != null && Vector3.Distance(Angler.I.transform.position, p) < NearM) return true;
            if (Coop.On && Coop.I != null) foreach (var m in Coop.I.Mates.Values) if (m != null && Vector3.Distance(m.transform.position, p) < NearM) return true;
            return false;
        }
        static void Glide(Skiff b, Vector2 at, Vector2 dir)
        {
            b.Notch = 0; b.Steer = 0; b.Speed = 0;
            float yaw = dir.sqrMagnitude > 1e-6f ? Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg : b.transform.eulerAngles.y;
            b.TakeOver(); b.transform.SetPositionAndRotation(new Vector3(at.x, b.transform.position.y, at.y), Quaternion.Euler(0, Mathf.MoveTowardsAngle(b.transform.eulerAngles.y, yaw, 90 * Time.deltaTime), 0)); b.TakeOver();
        }
        static void Berth(Skiff b)
        {
            var at = b.MoorAt; if (at == null) return;
            b.Anchored = false; b.TakeOver(); b.transform.SetPositionAndRotation(new Vector3(at.position.x, Sea.Level, at.position.z), at.rotation); b.TakeOver(); b.Moor(true);
        }
    }
}
