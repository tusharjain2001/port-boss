using System;
using System.Collections.Generic;

namespace PortBoss.M1
{
    // M6 · workers, the first automation (economy §4.5, §5.4; systems "M6"): pure C#, host-authoritative, in the save.
    // Rosie's board at The Mooring Line (3 seeded candidates, refreshed at 07:00), hiring (fee = 2 days' wage, starts the
    // next morning), the Crew app's assignment (boat, spot, bait, shift, back-by), the shift itself (the formula: the game
    // drives the real boat near the player and asks the same numbers), wages at 07:00 in hiring order, the IOU rule.
    public enum Shift { Early, Day, Late }                          // 05-13, 08-17, 13-22 (Late: deck lights, +50% wage)
    public enum WSt { Waiting, Leaving, Out, Fishing, Coming, Home, NoShow, Stranded }

    public sealed class Worker
    {
        public int Id; public string Name, Quirk, Line;              // Line: Rosie's one-line review
        public int Fishing, Sea, PotF, PotS;                          // stars 1..5 and the potential (hollow stars)
        public int HiredDay, StartDay, Shifts, Morale = 70, Iou = -1, Owed; public bool Quits;   // Quits: unpaid twice while at sea: they leave once she's home // Iou: the day they worked on trust (-1: none)
        public string Boat, Spot, Bait = "Shrimp"; public Shift Sh = Shift.Day; public bool Repeat = true, BuyBait = true;   // BuyBait: the shelf runs short -> June's delivers
        public int BackBy;                                            // back-by (minute of the day); 0: the shift's end
        public ShiftRun Run;                                          // today's (or the last) shift
        public int Wage => WageOf(Fishing, Sea);
        public static int WageOf(int f, int s) => (int)Math.Round(70 + 22.5 * (f + s));
        public Worker Copy() { var w = (Worker)MemberwiseClone(); w.Run = Run?.Copy(); return w; }
    }
    public sealed class ShiftRun
    {
        public int Day; public string Boat, Spot, Bait; public Shift Sh;
        public double Start, OutAt, FishFrom, FishTo, HomeAt;          // game minutes
        public int Catch, Rare, Planned; public string Fail; public WSt St; public bool Landed, Live, Paid, Towed;
        public double Reach = 1;                                        // the share of the route out she covered (a recall on the way out: < 1)
        public float Value;                                             // $ at the blended 75%, with the grade mult
        public ShiftRun Copy() => (ShiftRun)MemberwiseClone();
    }

    public partial class Company
    {
        public readonly List<Worker> Staff = new();
        public readonly List<Worker> Board = new();                   // Rosie's three today
        public readonly Dictionary<string, int> Rehire = new();       // name -> the last day a rehire costs double
        public bool BoardSeen;                                         // hiring from the phone needs one visit to the board
        public int BoardDay, WorkerNext = 1;
        public static int CrewSeed = 7;                                // the world seed (the game sets it)
        public event Action<Worker, string> WorkerNews;               // (worker, what) the game's toasts / map / boats

        // ---- the board: refreshed at 07:00, seeded by the day
        static readonly string[] First = { "Nils", "Bea", "Tomas", "Ivy", "Rory", "Ada", "Finn", "Maggie", "Otto", "Suki", "Callum", "Nell", "Bram", "Juno", "Wes", "Orla" };
        static readonly string[] Last = { "Berg", "Quill", "Marlow", "Pryce", "Teague", "Dunmore", "Salt", "Cobb", "Rourke", "Lark", "Fenn", "Haddow" };
        static readonly string[] Quirks = { "", "", "Early riser", "Chatty", "Lucky", "Seasick", "Butterfingers", "Old Salt", "Union rep" };
        public static readonly string[] SpotIds = { "bay_shallows", "gullrock", "lantern_key", "wreck" };
        public static readonly string[] SpotNames = { "Bay Shallows", "Gullrock", "Lantern Key", "Wreck of the Margaret" };
        public static readonly double[] SpotKm = { 0.6, 1.1, 2.6, 3.3 };   // from the landing (the game measures the real chart)
        public static string SpotName(string id) { int i = Array.IndexOf(SpotIds, id); return i >= 0 ? SpotNames[i] : id; }
        public void RefreshBoard(int day)
        {
            BoardDay = day; Board.Clear();
            var r = new Random(CrewSeed * 7919 + day * 104729);
            for (int i = 0; i < 3; i++)
            {
                int f = 1 + r.Next(4), s = 1 + r.Next(4); if (r.NextDouble() < 0.15) { f = Math.Min(5, f + 1); s = Math.Min(5, s + 1); }
                string name = First[r.Next(First.Length)] + " " + Last[r.Next(Last.Length)];
                if (Staff.Exists(x => x.Name == name) || Board.Exists(x => x.Name == name)) name = First[(day + i) % First.Length] + " " + Last[(day * 3 + i) % Last.Length];
                var w = new Worker { Id = WorkerNext++, Name = name, Fishing = f, Sea = s, PotF = Math.Min(5, f + r.Next(2)), PotS = Math.Min(5, s + r.Next(2)), Quirk = Quirks[r.Next(Quirks.Length)] };
                w.Line = Review(w, r); Board.Add(w);
            }
        }
        static string Review(Worker w, Random r)
        {
            if (w.Fishing >= 4) return "Can smell a shoal from the pub door. Pricey, worth it.";
            if (w.Sea >= 4) return "Never once lost a boat. Knows every rock out there.";
            if (w.Quirk == "Chatty") return "Talks the fish to sleep, mind. Lovely though.";
            if (w.Quirk == "Seasick") return "Brilliant on a calm day. Don't send them into weather.";
            if (w.Quirk == "Lucky") return "Daft luck. Swears by a sock in the bait bucket.";
            if (w.Quirk == "Early riser") return "Up before the gulls. Out an hour early.";
            return new[] { "Steady hands, turns up on time.", "Green, but keen as mustard.", "Bit of a grump before coffee. Good fisher." }[r.Next(3)];
        }
        public int HireFee(Worker w, int day) => w.Wage * 2 * (Rehire.TryGetValue(w.Name, out var until) && day <= until ? 2 : 1);

        // ---- hire / fire (host); "" or why not
        public string Hire(string tx, string who, int boardId, double at, bool fromPhone)
        {
            var w = Board.Find(x => x.Id == boardId); if (w == null) return "They've gone.";
            if (Act < 2) return "Rosie's board opens in Act 2.";
            if (fromPhone && !BoardSeen) return "Visit Rosie's board at The Mooring Line first.";
            int fee = HireFee(w, DayTime.Day(at));
            if (!Spend(tx, who, $"Hired {w.Name} (2 days' wage)", fee, at)) return Done.Contains(tx) ? "Already hired." : $"Need ${fee - Cash} more";
            Board.Remove(w); w.HiredDay = DayTime.Day(at); w.StartDay = DayTime.Day(DayTime.NextWake(at)); Staff.Add(w);
            Text("Rosie Tull", $"{w.Name} starts tomorrow at 7. Treat 'em right.", at);
            WorkerNews?.Invoke(w, "hired"); return "";
        }
        public string Fire(string tx, string who, int id, double at)
        {
            var w = Staff.Find(x => x.Id == id); if (w == null) return "Not on the crew.";
            if (w.Run != null && !w.Run.Landed && w.Run.St != WSt.NoShow && w.Run.St != WSt.Waiting) return $"{w.Name} is at sea. Recall first.";
            Accrue(w); int pay = w.Wage + w.Owed;   // a day's severance, plus anything still owed (the last shift, an IOU)
            if (!Spend(tx, who, w.Owed > 0 ? $"Severance: {w.Name} (1 day + ${w.Owed} owed)" : $"Severance: {w.Name} (1 day)", pay, at)) return Done.Contains(tx) ? "Already done." : $"Need ${pay - Cash} more";
            w.Owed = 0;
            Staff.Remove(w); Rehire[w.Name] = DayTime.Day(at) + 7; WorkerNews?.Invoke(w, "fired"); return "";
        }

        // ---- the Crew app: assign (boat, spot, bait, shift)
        public static readonly int[] ShiftFrom = { 5 * 60, 8 * 60, 13 * 60 }, ShiftTo = { 13 * 60, 17 * 60, 22 * 60 };
        public string Assign(int id, string boat, string spot, string bait, Shift sh, int backBy = 0, bool buyBait = true)
        {
            var w = Staff.Find(x => x.Id == id); if (w == null) return "Not on the crew.";
            if (w.Run != null && !w.Run.Landed && w.Run.St != WSt.NoShow) return $"{w.Name} is at sea. Recall first.";
            if (boat != null)
            {
                if (boat != "biscuit" && !Owned.Contains("boat:" + boat)) return "We don't own that boat.";
                var other = Staff.Find(x => x != w && x.Boat == boat); if (other != null) return $"{other.Name} has the {Cap(boat)}.";
                if (sh == Shift.Late && !BoatOf(boat).Ups.Contains("lights")) return "The Late shift needs deck lights.";
            }
            if (spot != null && Array.IndexOf(SpotIds, spot) < 0) return "No such spot.";
            w.Boat = boat; w.Spot = spot ?? w.Spot ?? SpotIds[1]; w.Bait = bait ?? w.Bait; w.Sh = sh; w.BackBy = backBy; w.BuyBait = buyBait;
            WorkerNews?.Invoke(w, "assigned"); return "";
        }
        // ---- the crew's bait (economy §5.4: "bait comes from the warehouse; auto-buy at June's, delivered, +$5"): the
        // warehouse's bait shelf first, the rest in whole packs from June's (the spare goes on the shelf). One bait a fish.
        public readonly int[] BaitStore = new int[5];
        public const int BaitDelivery = 5;
        public static readonly string[] CrewBaits = { "Worm", "Shrimp", "Sardine", "Squid" };   // Shelf's bait 0..3
        public static int BaitIx(string bait) => Math.Max(0, Array.IndexOf(CrewBaits, bait));
        public (int got, int bought, int cost) BaitPlan(Worker w, int need)
        {
            int i = BaitIx(w.Bait), have = Math.Min(need, BaitStore[i]), shortN = need - have;
            if (shortN <= 0 || !w.BuyBait) return (have, 0, 0);
            int packs = (shortN + Shelf.PackOf[i] - 1) / Shelf.PackOf[i];
            return (need, packs * Shelf.PackOf[i], packs * Shelf.BaitPack[i] + BaitDelivery);
        }
        public void TakeBait(Worker w, int need) { var p = BaitPlan(w, need); BaitStore[BaitIx(w.Bait)] += p.bought - p.got; }
        // a player leaves 10 of a bait from their pouch on the shelf (the warehouse screen)
        public string LeaveBait(string key, int i)
        {
            if (i < 0 || i >= CrewBaits.Length) return "?";
            var k = KitOf(key); int n = Math.Min(10, k.Bait[i]); if (n <= 0) return $"No {CrewBaits[i].ToLower()} in your pouch.";
            k.Bait[i] -= n; BaitStore[i] += n; return "";
        }
        // co-op: a client's pouch is its own (it reports it to the host), so the client takes the bait out itself and
        // the host only shelves what it was sent (at most 10 a press)
        public string ShelveBait(int i, int n)
        {
            if (i < 0 || i >= CrewBaits.Length || n <= 0) return "?";
            BaitStore[i] += Math.Min(10, n); return "";
        }
        // back-by: home at that time instead of the shift's end (at least 3 hours out); the catch is that share of a full shift
        public static int BackOf(Worker w) { int a = ShiftFrom[(int)w.Sh], b = ShiftTo[(int)w.Sh]; return w.BackBy > 0 ? Math.Clamp(w.BackBy, a + 180, b) : b; }
        public static double Share(Worker w) => (BackOf(w) - ShiftFrom[(int)w.Sh]) / (double)(ShiftTo[(int)w.Sh] - ShiftFrom[(int)w.Sh]);
        public static int Hold(string boat) => boat == "kestrel" ? 30 : boat == "puffin" ? 100 : 20;   // the table: Biscuit 20, Kestrel 30, Puffin 100 (OPEN Q3: the fish box)
        public int WageToday(Worker w) => (int)Math.Round(w.Wage * (w.Sh == Shift.Late ? 1.5 : 1));
        public static int PlannedCatch(Worker w, string boat) => Math.Min(10 + 5 * w.Fishing, Hold(boat));
        public static double FailChance(Worker w, bool engineUp) { double p = (14 - 2 * w.Sea) / 100.0 + (w.Quirk == "Butterfingers" ? 0.05 : 0); return Math.Max(0.01, p); }
        // the model's expected catch for one shift (the -pb-workertest check): no-show 30% (0), engine 30% (half), snapped
        // line 30% (-25%), aground 10% (half: the catch so far); engine upgrades halve the engine share
        public static double Expected(Worker w, string boat, bool engineUp)
        {   // the same sums as StartShift: luck uniform 0.88-1.12 on the uncapped catch, then the hold caps it
            double p = FailChance(w, engineUp), eng = engineUp ? 0.15 : 0.3, chat = (w.Quirk == "Chatty" ? 0.9 : 1) * Share(w);
            double E(double mult) { double t = 0; const int N = 240; for (int i = 0; i < N; i++) { double k = 0.88 + 0.24 * (i + 0.5) / N; t += Math.Min(Hold(boat), Math.Round((10 + 5 * w.Fishing) * k * mult * chat)); } return t / N; }
            return (1 - p) * E(1) + p * (eng * E(0.5) + (0.6 - eng) * E(0.75) + 0.1 * E(0.5));
        }
        // game minutes each way for a path of `metres` at 2/3 throttle (1 real s = 1 game min), capped at 30% of the shift so
        // the shift keeps its fishing time (ponytail: on a long run the boat then hurries; the catch formula is per shift anyway)
        public static double Travel(float metres, float top, Shift sh) => Math.Min(metres / Math.Max(0.5, top * 0.67), 0.3 * (ShiftTo[(int)sh] - ShiftFrom[(int)sh]));

        // ---- a shift: rolled at its start (seeded by worker and day), the boat out and back. The live boat may land early.
        public ShiftRun StartShift(Worker w, int day, double at, double travel, bool storm)
        {
            Accrue(w);   // the last shift's wage is owed now (an Early shift starts before the 07:00 pay run)
            var r = new Random(CrewSeed * 31 + w.Id * 977 + day * 7717);
            var run = new ShiftRun { Day = day, Boat = w.Boat, Spot = w.Spot, Bait = w.Bait, Sh = w.Sh, Start = at, St = WSt.Leaving };
            run.Planned = PlannedCatch(w, w.Boat);
            bool engineUp = BoatOf(w.Boat).Ups.Contains("engine1");
            double end = DayTime.At(day, BackOf(w));
            travel = Math.Max(0, Math.Min(travel, (end - at - 4) * 0.4));   // a late start: a shorter run out, some fishing left
            run.OutAt = at + 4; run.FishFrom = run.OutAt + travel; run.FishTo = Math.Max(run.FishFrom, end - travel); run.HomeAt = Math.Max(end, run.FishTo + travel);
            double k = 0.88 + 0.24 * r.NextDouble();   // the day's luck, mean 1
            if (w.Quirk == "Chatty") k *= 0.9;
            k *= Share(w);   // back-by early: that share of the shift's catch
            string fail = null;
            if (storm) { if (w.Quirk == "Seasick") fail = "storm-refused"; else { fail = "storm"; k *= 0.5; run.FishTo = run.FishFrom + (run.FishTo - run.FishFrom) * 0.5; run.HomeAt = run.FishTo + travel; } }
            else if (r.NextDouble() < FailChance(w, engineUp))
            {
                double q = r.NextDouble(); double eng = engineUp ? 0.15 : 0.3;
                if (q < 0.3) fail = "no-show";
                else if (q < 0.3 + eng) { fail = "engine"; k *= 0.5; run.FishTo = run.FishFrom + (run.FishTo - run.FishFrom) * 0.5; run.HomeAt = run.FishTo + travel; }
                else if (q < 0.6 + eng) { fail = "line"; k *= 0.75; }
                else if (q < 0.7 + eng) { fail = "aground"; k *= 0.5; }
                else { fail = "line"; k *= 0.75; }
            }
            run.Fail = fail;
            if (fail == "no-show" || fail == "storm-refused") { run.St = WSt.NoShow; run.Catch = 0; run.Landed = true; }
            else
            {
                run.Catch = Math.Max(0, Math.Min(Hold(w.Boat), (int)Math.Round((10 + 5 * w.Fishing) * k)));
                double rareP = 0.18 + (w.Quirk == "Lucky" ? 0.05 : 0);
                for (int i = 0; i < run.Catch; i++) if (r.NextDouble() < rareP) run.Rare++;
            }
            w.Run = run;
            WorkerNews?.Invoke(w, run.St == WSt.NoShow ? "no-show" : "leaving");
            return run;
        }
        // the formula's catch so far (the phone's live line, and a recall: what's on board now)
        public static int CatchSoFar(ShiftRun r, double now) => r.FishTo <= r.FishFrom ? 0 : (int)Math.Floor(r.Catch * Math.Clamp((now - r.FishFrom) / (r.FishTo - r.FishFrom), 0, 1));
        public static WSt StateAt(ShiftRun r, double now)
        {
            if (r.St == WSt.NoShow) return WSt.NoShow;
            if (r.Landed) return WSt.Home;
            if (r.Fail == "aground" && !r.Towed && now >= r.FishFrom) return WSt.Stranded;
            if (now < r.OutAt) return WSt.Leaving; if (now < r.FishFrom) return WSt.Out; if (now < r.FishTo) return WSt.Fishing; if (now < r.HomeAt) return WSt.Coming;
            return WSt.Home;
        }
        // home: the catch is carried to the warehouse (the game makes the fish); growth every 12 shifts
        public void Land(Worker w, double at, int catchNow)
        {
            var r = w.Run; if (r == null || r.Landed) return;
            r.Landed = true; r.St = WSt.Home; r.Catch = Math.Min(r.Catch, catchNow);
            w.Shifts++;
            if (w.Quits) { Staff.Remove(w); Rehire[w.Name] = DayTime.Day(at) + 7; Text(w.Name, "She's moored. I'm done. Good luck with the dock.", at); WorkerNews?.Invoke(w, "quit"); return; }
            if (w.Shifts % 12 == 0) { if (w.Fishing <= w.Sea && w.Fishing < w.PotF) w.Fishing++; else if (w.Sea < w.PotS) w.Sea++; else if (w.Fishing < w.PotF) w.Fishing++; }
            WorkerNews?.Invoke(w, "landed");
        }

        static void Accrue(Worker w) { var r = w.Run; if (r != null && !r.Paid && r.St != WSt.NoShow) { w.Owed += (int)Math.Round(w.Wage * (r.Sh == Shift.Late ? 1.5 : 1)); r.Paid = true; } }
        // ---- recall (the Crew app): home now, the catch lost; the way back is as long as the way she got out
        public string Recall(int id, double now)
        {
            var w = Staff.Find(x => x.Id == id); var r = w?.Run; if (r == null || r.Landed || r.St == WSt.NoShow) return "Not at sea.";
            var st = StateAt(r, now);
            if (st == WSt.Stranded) return "Aground: Doyle's tow is the way home.";
            if (st == WSt.Coming || st == WSt.Home) return $"{w.Name} is already coming home.";
            double run = Math.Max(1, r.FishFrom - r.OutAt);
            r.Reach = st == WSt.Leaving ? 0 : st == WSt.Out ? Math.Clamp((now - r.OutAt) / run, 0, 1) : 1;
            r.Catch = 0; r.Rare = 0; r.Fail = "recalled";
            r.OutAt = Math.Min(r.OutAt, now); r.FishFrom = Math.Min(r.FishFrom, now); r.FishTo = Math.Min(r.FishTo, now); r.HomeAt = now + Math.Max(2, run * r.Reach);
            Text(w.Name, "Turning round, boss. Coming in with nothing.", now); return "";
        }
        // ---- aground: Doyle tows her home for $200 (the catch so far comes with her)
        public const int TowFee = 200;
        public string Tow(string tx, string who, int id, double at)
        {
            var w = Staff.Find(x => x.Id == id); var r = w?.Run; if (r == null || StateAt(r, at) != WSt.Stranded) return "Not aground.";
            if (!Spend(tx, who, $"Doyle's tow: {w.Name} ({r.Boat})", TowFee, at)) return Done.Contains(tx) ? "Already towed." : $"Need ${TowFee - Cash} more";
            r.Towed = true; r.FishTo = Math.Min(r.FishTo, at); r.HomeAt = at + 30;   // half an hour on the tow line
            Text("Fergus Doyle", $"Got {w.Name} off the rocks. Towing her in, home by {DayTime.Clock(r.HomeAt)}.", at);
            WorkerNews?.Invoke(w, "towed"); return "";
        }
        // ---- 07:00: the board, then wages in hiring order (the bank never goes below $0: the IOU rule)
        public void MorningM6(int day, double at)
        {
            RefreshBoard(day);
            foreach (var w in Staff.ToArray())   // (hiring order)
            {
                // aground overnight: the morning tide lifts her off for free (Doyle's $200 tow is the quick way)
                if (w.Run is { } g && g.Day <= day && StateAt(g, at) == WSt.Stranded)
                { g.Towed = true; g.FishTo = Math.Min(g.FishTo, at); g.HomeAt = at + 30; Text(w.Name, $"The morning tide lifted {Cap(g.Boat)} off. Coming home, by {DayTime.Clock(g.HomeAt)}.", at); WorkerNews?.Invoke(w, "towed"); }
                // a day's wage is for a day worked: the last shift before today (a no-show: no shift, no wage)
                if (w.Run != null && w.Run.Day < day) Accrue(w);
                if (w.Owed == 0) continue;
                if (Spend($"wage:{w.Id}:{day}", w.Name, w.Iou >= 0 ? $"Wages: {w.Name} (with the IOU)" : $"Wage: {w.Name}", w.Owed, at)) { w.Owed = 0; if (w.Iou >= 0) { w.Iou = -1; w.Morale = Math.Min(100, w.Morale + 5); } }
                else if (w.Iou < 0) { w.Iou = day; w.Morale = Math.Max(0, w.Morale - 25); Text(w.Name, $"Bank's dry, boss. I'll work today on trust. Pay me tomorrow or I'm off.", at); WorkerNews?.Invoke(w, "iou"); }
                else if (w.Run != null && !w.Run.Landed && w.Run.St != WSt.NoShow) { if (!w.Quits) { w.Quits = true; Text(w.Name, "No pay two days running. I'll bring her home, then I'm done.", at); } }
                else { Staff.Remove(w); Rehire[w.Name] = day + 7; Text(w.Name, $"No pay two days running. I'm done. Good luck with the dock.", at); WorkerNews?.Invoke(w, "quit"); }
            }
        }
        // ---- save (and the co-op "pb.co" sync)
        static void WriteWorker(Json.W w, Worker x)
        {
            w.Obj().K("id").V(x.Id).K("name").V(x.Name).K("quirk").V(x.Quirk).K("line").V(x.Line).K("f").V(x.Fishing).K("s").V(x.Sea).K("pf").V(x.PotF).K("ps").V(x.PotS)
             .K("hired").V(x.HiredDay).K("start").V(x.StartDay).K("shifts").V(x.Shifts).K("morale").V(x.Morale).K("iou").V(x.Iou).K("owed").V(x.Owed).K("quits").V(x.Quits).K("buybait").V(x.BuyBait)
             .K("boat").V(x.Boat).K("spot").V(x.Spot).K("bait").V(x.Bait).K("sh").V((int)x.Sh).K("repeat").V(x.Repeat).K("back").V(x.BackBy);
            var r = x.Run;
            if (r != null) w.K("run").Obj().K("day").V(r.Day).K("boat").V(r.Boat).K("spot").V(r.Spot).K("bait").V(r.Bait).K("sh").V((int)r.Sh).K("start").V(r.Start).K("out").V(r.OutAt)
                .K("from").V(r.FishFrom).K("to").V(r.FishTo).K("home").V(r.HomeAt).K("catch").V(r.Catch).K("rare").V(r.Rare).K("planned").V(r.Planned).K("fail").V(r.Fail)
                .K("st").V((int)r.St).K("landed").V(r.Landed).K("paid").V(r.Paid).K("towed").V(r.Towed).K("reach").V(r.Reach).End();
            w.End();
        }
        static Worker ReadWorker(Dictionary<string, object> e)
        {
            var x = new Worker { Id = Json.I(e, "id"), Name = Json.S(e, "name"), Quirk = Json.S(e, "quirk"), Line = Json.S(e, "line"), Fishing = Json.I(e, "f", 1), Sea = Json.I(e, "s", 1), PotF = Json.I(e, "pf", 1), PotS = Json.I(e, "ps", 1),
                HiredDay = Json.I(e, "hired"), StartDay = Json.I(e, "start"), Shifts = Json.I(e, "shifts"), Morale = Json.I(e, "morale", 70), Iou = Json.I(e, "iou", -1), Owed = Json.I(e, "owed"), Quits = Json.B(e, "quits"), BuyBait = Json.B(e, "buybait", true),
                Boat = Json.S(e, "boat"), Spot = Json.S(e, "spot"), Bait = Json.S(e, "bait") ?? "Shrimp", Sh = (Shift)Json.I(e, "sh", 1), BackBy = Json.I(e, "back"), Repeat = !e.ContainsKey("repeat") || Json.B(e, "repeat") };
            if (e.TryGetValue("run", out var ro) && ro is Dictionary<string, object> r)
                x.Run = new ShiftRun { Day = Json.I(r, "day"), Boat = Json.S(r, "boat"), Spot = Json.S(r, "spot"), Bait = Json.S(r, "bait"), Sh = (Shift)Json.I(r, "sh"), Start = Json.D(r, "start"), OutAt = Json.D(r, "out"),
                    FishFrom = Json.D(r, "from"), FishTo = Json.D(r, "to"), HomeAt = Json.D(r, "home"), Catch = Json.I(r, "catch"), Rare = Json.I(r, "rare"), Planned = Json.I(r, "planned"), Fail = Json.S(r, "fail"),
                    St = (WSt)Json.I(r, "st"), Landed = Json.B(r, "landed"), Paid = Json.B(r, "paid"), Towed = Json.B(r, "towed"), Reach = Json.D(r, "reach", 1) };
            return x;
        }
        void WriteM6(Json.W w)
        {
            w.K("crew").Obj().K("boardDay").V(BoardDay).K("next").V(WorkerNext).K("seen").V(BoardSeen);
            w.K("staff").Arr(); foreach (var x in Staff) WriteWorker(w, x); w.End();
            w.K("board").Arr(); foreach (var x in Board) WriteWorker(w, x); w.End();
            w.K("baitStore").Arr(); foreach (var b in BaitStore) w.V(b); w.End();
            var rk = new List<string>(Rehire.Keys); rk.Sort(StringComparer.Ordinal); w.K("rehire").Obj(); foreach (var k in rk) w.K(k).V(Rehire[k]); w.End();
            w.End();
        }
        void ReadM6(Dictionary<string, object> o)
        {
            if (!o.TryGetValue("crew", out var co) || co is not Dictionary<string, object> c) return;
            BoardDay = Json.I(c, "boardDay"); WorkerNext = Json.I(c, "next", 1); BoardSeen = Json.B(c, "seen");
            if (c.TryGetValue("staff", out var so) && so is List<object> sl) foreach (Dictionary<string, object> e in sl) Staff.Add(ReadWorker(e));
            if (c.TryGetValue("board", out var bo) && bo is List<object> bl) foreach (Dictionary<string, object> e in bl) Board.Add(ReadWorker(e));
            if (c.TryGetValue("baitStore", out var bs) && bs is List<object> bsl) for (int i = 0; i < 5 && i < bsl.Count; i++) BaitStore[i] = Convert.ToInt32(bsl[i], System.Globalization.CultureInfo.InvariantCulture);
            if (c.TryGetValue("rehire", out var rh) && rh is Dictionary<string, object> rd) foreach (var kv in rd) Rehire[kv.Key] = Convert.ToInt32(kv.Value, System.Globalization.CultureInfo.InvariantCulture);
        }

        public string CheckM6()
        {
            var boats = new HashSet<string>();
            foreach (var w in Staff) { if (w.Boat != null && !boats.Add(w.Boat)) return $"two workers on the {w.Boat}"; if (w.Fishing < 1 || w.Fishing > 5 || w.Sea < 1 || w.Sea > 5) return $"{w.Name}'s stars"; }
            if (Board.Count > 3) return "Rosie's board has more than 3";
            if (Cash < 0) return "the bank below $0";
            foreach (var b in BaitStore) if (b < 0) return "the bait shelf below 0";
            return "";
        }

        // ---- the wage / IOU suite (EditMode M6Tests + tools/companycheck): "" or what broke
        public static string CrewTest()
        {
            string F(string s) => s;
            var c = new Company { Act = 2 }; double at = DayTime.At(1, 12 * 60);
            c.RefreshBoard(1); if (c.Board.Count != 3) return F("board: not 3 faces");
            var names = c.Board.ConvertAll(x => x.Name); var c2 = new Company { Act = 2 }; c2.RefreshBoard(1);
            if (string.Join(",", names) != string.Join(",", c2.Board.ConvertAll(x => x.Name))) return F("board: not seeded by the day");
            var w = c.Board[0]; c.Cash = 5000;
            if (c.Hire("h1", "Pip", w.Id, at, true) == "") return F("hired from the phone before seeing the board");
            if (c.Hire("h1", "Pip", w.Id, at, false) != "") return F("hire at the board refused");
            if (c.Cash != 5000 - 2 * w.Wage) return F($"hire fee: ${5000 - c.Cash}, want ${2 * w.Wage}");
            if (w.StartDay != 2) return F($"starts day {w.StartDay}, want 2 (the next morning)");
            if (c.Hire("h1", "Pip", c.Board[0].Id, at, false) == "") return F("the same tx hired twice");
            int fresh = new Company().Cash; _ = fresh;
            // wage: yesterday's worked shift is paid at 07:00
            w.Boat = "biscuit"; w.Spot = "gullrock"; w.Sh = Shift.Day;
            c.StartShift(w, 2, DayTime.At(2, 8 * 60), 30, false); w.Run.St = WSt.Home; w.Run.Landed = true; w.Run.Fail = null;
            int before = c.Cash; c.MorningM6(3, DayTime.At(3, 7 * 60));
            if (before - c.Cash != w.Wage) return F($"day wage: paid ${before - c.Cash}, want ${w.Wage}");
            c.MorningM6(3, DayTime.At(3, 7 * 60)); if (before - c.Cash != w.Wage) return F("the same day paid twice");
            // Late: +50%
            w.Sh = Shift.Late; c.StartShift(w, 3, DayTime.At(3, 13 * 60), 30, false); w.Run.St = WSt.Home; w.Run.Landed = true;
            before = c.Cash; c.MorningM6(4, DayTime.At(4, 7 * 60));
            if (before - c.Cash != (int)Math.Round(w.Wage * 1.5)) return F($"late wage: ${before - c.Cash}, want ${(int)Math.Round(w.Wage * 1.5)}");
            // no-show: no wage
            w.Sh = Shift.Day; c.StartShift(w, 4, DayTime.At(4, 8 * 60), 30, false); w.Run.St = WSt.NoShow; w.Run.Landed = true;
            before = c.Cash; c.MorningM6(5, DayTime.At(5, 7 * 60)); if (c.Cash != before) return F("a no-show was paid");
            // an Early shift starts at 05:00, before the 07:00 pay run: yesterday's wage is still paid
            w.Sh = Shift.Early; c.StartShift(w, 20, DayTime.At(20, 5 * 60), 30, false); w.Run.St = WSt.Home; w.Run.Landed = true;
            c.StartShift(w, 21, DayTime.At(21, 5 * 60), 30, false); w.Run.St = WSt.Home; w.Run.Landed = true;
            before = c.Cash; c.MorningM6(21, DayTime.At(21, 7 * 60));
            if (before - c.Cash != w.Wage) return F($"early shift: paid ${before - c.Cash} at 07:00 after a 05:00 start, want ${w.Wage} (yesterday's)");
            before = c.Cash; c.MorningM6(22, DayTime.At(22, 7 * 60));
            if (before - c.Cash != w.Wage) return F($"early shift day 21: paid ${before - c.Cash}, want ${w.Wage}");
            w.Sh = Shift.Day; w.Run = null;
            // aground: Stranded until Doyle's tow ($200), then home with the catch so far
            {
                var g = new ShiftRun { Day = 22, Boat = "biscuit", Spot = "gullrock", Start = DayTime.At(22, 480), OutAt = DayTime.At(22, 484), FishFrom = DayTime.At(22, 530), FishTo = DayTime.At(22, 970), HomeAt = DayTime.At(22, 1020), Catch = 9, Fail = "aground", St = WSt.Leaving, Paid = true };
                w.Run = g; double t0 = DayTime.At(22, 600);
                if (StateAt(g, t0) != WSt.Stranded) return F("aground: not stranded");
                int cash0 = c.Cash; if (c.Tow("tow1", "Pip", w.Id, t0) != "" || c.Cash != cash0 - TowFee) return F("Doyle's tow: not charged $200");
                if (c.Tow("tow1", "Pip", w.Id, t0) == "") return F("towed twice");
                if (StateAt(g, t0 + 10) != WSt.Coming || Math.Abs(g.HomeAt - (t0 + 30)) > 1e-6) return F($"towed: {StateAt(g, t0 + 10)}, home {g.HomeAt - t0} min");
                // not towed: still aground that evening, refloated by the 07:00 tide, home half an hour later, for free
                var g2 = new ShiftRun { Day = 22, Boat = "biscuit", Spot = "gullrock", Start = DayTime.At(22, 480), OutAt = DayTime.At(22, 484), FishFrom = DayTime.At(22, 530), FishTo = DayTime.At(22, 970), HomeAt = DayTime.At(22, 1020), Catch = 9, Fail = "aground", St = WSt.Leaving, Paid = true };
                w.Run = g2; if (StateAt(g2, DayTime.At(22, 23 * 60)) != WSt.Stranded) return F("aground: free by the evening");
                cash0 = c.Cash; c.MorningM6(23, DayTime.At(23, 7 * 60));
                if (StateAt(g2, DayTime.At(23, 7 * 60 + 10)) != WSt.Coming || Math.Abs(g2.HomeAt - DayTime.At(23, 7 * 60 + 30)) > 1e-6 || c.Cash != cash0) return F("the morning tide didn't refloat her (or charged)");
                w.Run = null;
            }
            // recall: on the way out at 40% of the run -> home in 40% of the run time, the catch lost; not while already coming home
            {
                var t = new Company { Act = 2 }; var x = new Worker { Id = 1, Name = "R", Fishing = 3, Sea = 3, Quirk = "", Boat = "kestrel", Spot = "gullrock" }; t.Staff.Add(x);
                var g = new ShiftRun { Day = 4, Start = DayTime.At(4, 480), OutAt = DayTime.At(4, 484), FishFrom = DayTime.At(4, 584), FishTo = DayTime.At(4, 920), HomeAt = DayTime.At(4, 1020), Catch = 18, St = WSt.Leaving };
                x.Run = g; double now = DayTime.At(4, 524);
                if (t.Recall(1, now) != "" || Math.Abs(g.Reach - 0.4) > 1e-6 || Math.Abs(g.HomeAt - (now + 40)) > 1e-6 || g.Catch != 0 || StateAt(g, now + 1) != WSt.Coming) return F($"recall on the way out: reach {g.Reach}, home in {g.HomeAt - now} min");
                var h = new ShiftRun { Day = 5, Start = DayTime.At(5, 480), OutAt = DayTime.At(5, 484), FishFrom = DayTime.At(5, 584), FishTo = DayTime.At(5, 920), HomeAt = DayTime.At(5, 1020), Catch = 18, St = WSt.Leaving };
                x.Run = h; if (t.Recall(1, DayTime.At(5, 1000)) == "" || h.Catch != 18) return F("recall while coming home: should be refused");
                if (Read((Dictionary<string, object>)Json.Parse(t.ToJson())).Staff[0].Run.Reach != 1 || Read((Dictionary<string, object>)Json.Parse(new Company { Act = 2 }.ToJson())).Staff.Count != 0) return F("save lost reach");
            }
            // the IOU: the bank can't pay -> one day on trust (never below $0), then they quit; rehire costs double for 7 days
            c.StartShift(w, 5, DayTime.At(5, 8 * 60), 30, false); w.Run.St = WSt.Home; w.Run.Landed = true; c.Cash = 10;
            c.MorningM6(6, DayTime.At(6, 7 * 60));
            if (c.Cash < 0) return F("the bank went below $0");
            if (!c.Staff.Contains(w) || w.Iou != 6) return F("no IOU on the first unpaid morning");
            if (w.Owed != w.Wage) return F($"owed ${w.Owed}, want ${w.Wage}");
            // paid in time: the IOU clears, both days paid in one line
            c.StartShift(w, 6, DayTime.At(6, 8 * 60), 30, false); w.Run.St = WSt.Home; w.Run.Landed = true; c.Cash = 1000;
            c.MorningM6(7, DayTime.At(7, 7 * 60));
            if (w.Iou != -1 || w.Owed != 0 || c.Cash != 1000 - 2 * w.Wage) return F($"IOU not cleared (iou {w.Iou}, owed {w.Owed}, cash {c.Cash})");
            // unpaid twice: quits
            c.StartShift(w, 7, DayTime.At(7, 8 * 60), 30, false); w.Run.St = WSt.Home; w.Run.Landed = true; c.Cash = 0;
            c.MorningM6(8, DayTime.At(8, 7 * 60)); if (w.Iou != 8) return F("second IOU missing");
            c.StartShift(w, 8, DayTime.At(8, 8 * 60), 30, false); w.Run.St = WSt.Home; w.Run.Landed = true;
            c.MorningM6(9, DayTime.At(9, 7 * 60));
            if (c.Staff.Contains(w)) return F("unpaid two days running but still on the crew");
            if (!c.Rehire.TryGetValue(w.Name, out var until) || until != 16) return F("no rehire penalty");
            if (c.HireFee(w, 10) != 4 * w.Wage || c.HireFee(w, 17) != 2 * w.Wage) return F("rehire fee not double for 7 days");
            // unpaid twice while still at sea: they bring the boat home first, then quit
            {
                var t = new Company { Act = 2 }; var x = new Worker { Id = 1, Name = "Q", Fishing = 3, Sea = 3, Quirk = "", Boat = "kestrel", Spot = "gullrock" }; t.Staff.Add(x); t.Cash = 0;
                t.StartShift(x, 5, DayTime.At(5, 8 * 60), 30, false); x.Run.St = WSt.Home; x.Run.Landed = true; t.MorningM6(6, DayTime.At(6, 7 * 60));
                t.StartShift(x, 6, DayTime.At(6, 8 * 60), 30, false); x.Run.St = WSt.Fishing; x.Run.Fail = null; x.Run.HomeAt = DayTime.At(7, 9 * 60);
                t.MorningM6(7, DayTime.At(7, 7 * 60));
                if (!t.Staff.Contains(x) || !x.Quits) return F("unpaid twice at sea: abandoned the boat (or never quit)");
                if (Read((Dictionary<string, object>)Json.Parse(t.ToJson())).Staff[0].Quits != true) return F("save lost 'quits'");
                t.Land(x, DayTime.At(7, 9 * 60), 5); if (t.Staff.Contains(x) || !t.Rehire.ContainsKey("Q")) return F("quitter still on the crew after mooring");
            }
            // firing pays a day's severance plus what's owed (the last shift, unpaid yet)
            {
                var t = new Company { Act = 2 }; var x = new Worker { Id = 1, Name = "S", Fishing = 3, Sea = 3, Quirk = "", Boat = "kestrel", Spot = "gullrock" }; t.Staff.Add(x); t.Cash = 1000;
                t.StartShift(x, 5, DayTime.At(5, 8 * 60), 30, false); x.Run.St = WSt.Home; x.Run.Landed = true;
                if (t.Fire("f1", "Pip", 1, DayTime.At(5, 18 * 60)) != "" || t.Cash != 1000 - 2 * x.Wage || x.Owed != 0) return F($"fire: paid ${1000 - t.Cash}, want ${2 * x.Wage} (a day + the shift)");
            }
            // a late start (16:50 for a 17:00 end) or an early back-by: never negative fishing time
            {
                var t = new Company { Act = 2 }; var x = new Worker { Id = 1, Name = "L", Fishing = 3, Sea = 3, Quirk = "", Boat = "kestrel", Spot = "gullrock" }; t.Staff.Add(x);
                foreach (var startMin in new[] { 16 * 60 + 50, 16 * 60 + 58, 12 * 60 })
                {
                    t.StartShift(x, 3, DayTime.At(3, startMin), 90, false); var r = x.Run;
                    if (r.FishTo < r.FishFrom || r.HomeAt < r.FishTo || r.FishFrom < r.OutAt || r.Catch < 0) return F($"start {startMin / 60}:{startMin % 60:00}: out {r.OutAt} fish {r.FishFrom}-{r.FishTo} home {r.HomeAt} catch {r.Catch}");
                }
            }
            // the bait shelf: 10 shrimp left by a player, a 20-fish shift takes them + one pack from June's (+$5); auto-buy off: what's there
            {
                var t = new Company { Act = 2 }; var x = new Worker { Id = 1, Name = "B", Fishing = 3, Sea = 3, Quirk = "", Boat = "biscuit", Spot = "gullrock", Bait = "Shrimp" }; t.Staff.Add(x);
                t.KitOf("solo").Bait[1] = 14;
                if (t.LeaveBait("solo", 1) != "" || t.BaitStore[1] != 10 || t.KitOf("solo").Bait[1] != 4) return F("leave bait: shelf/pouch wrong");
                if (t.LeaveBait("solo", 1) != "" || t.BaitStore[1] != 14 || t.KitOf("solo").Bait[1] != 0 || t.LeaveBait("solo", 1) == "") return F("leave bait: the last 4 / an empty pouch");
                t.BaitStore[1] = 10; var p = t.BaitPlan(x, 20);
                if (p.got != 20 || p.bought != 10 || p.cost != Shelf.BaitPack[1] + BaitDelivery) return F($"bait plan {p}");
                t.TakeBait(x, 20); if (t.BaitStore[1] != 0) return F("shelf not used up");
                t.BaitStore[1] = 3; p = t.BaitPlan(x, 20); t.TakeBait(x, 20); if (p.bought != 20 || t.BaitStore[1] != 3) return F($"spare not shelved ({p}, shelf {t.BaitStore[1]})");
                x.BuyBait = false; t.BaitStore[1] = 6; p = t.BaitPlan(x, 20); if (p.got != 6 || p.cost != 0) return F("auto-buy off still bought");
                var t2 = Read((Dictionary<string, object>)Json.Parse(t.ToJson())); if (t2.BaitStore[1] != 6 || t2.Staff[0].BuyBait) return F("save lost the shelf / auto-buy");
            }
            // two workers can't share a boat; the Late shift needs deck lights
            c.Cash = 5000; c.BoardSeen = true; var a = c.Board[0]; c.Hire("h2", "Pip", a.Id, DayTime.At(9, 12 * 60), true); var b = c.Board[0]; c.Hire("h3", "Pip", b.Id, DayTime.At(9, 12 * 60), true);
            if (c.Assign(a.Id, "biscuit", "gullrock", "Shrimp", Shift.Day) != "") return F("assign refused");
            if (c.Assign(b.Id, "biscuit", "gullrock", "Shrimp", Shift.Day) == "") return F("two workers on one boat");
            if (c.Assign(a.Id, "biscuit", "gullrock", "Shrimp", Shift.Late) == "") return F("Late shift without deck lights");
            if (c.Assign(b.Id, "kestrel", "gullrock", "Shrimp", Shift.Day) == "") return F("assigned a boat we don't own");
            // the save keeps the crew
            var back = Read((Dictionary<string, object>)Json.Parse(c.ToJson()));
            if (back.Staff.Count != c.Staff.Count || back.Board.Count != c.Board.Count || back.Staff[0].Boat != "biscuit" || !back.BoardSeen || back.Rehire.Count != c.Rehire.Count) return F("save round trip lost the crew");
            // the formula: 4000 seeded shifts per skill average within 3% of Expected
            for (int k = 1; k <= 5; k++)
            {
                var t = new Company { Act = 2 }; var x = new Worker { Id = 1, Name = "T", Fishing = k, Sea = k, Quirk = "", Boat = "kestrel", Spot = "gullrock" }; t.Staff.Add(x);
                double sum = 0; int n = 4000;
                for (int d = 1; d <= n; d++) { t.StartShift(x, d, DayTime.At(d, 8 * 60), 30, false); sum += x.Run.Catch; }
                double mean = sum / n, exp = Expected(x, "kestrel", false);
                if (Math.Abs(mean - exp) > exp * 0.03 + 0.5) return F($"skill {k}: mean {mean:F2} vs expected {exp:F2}");
            }
            // back-by: a Day shift home at 15:00 (7 of 9 hours): home then, and that share of the catch
            {
                var t = new Company { Act = 2 }; var x = new Worker { Id = 1, Name = "B", Fishing = 3, Sea = 3, Quirk = "", Boat = "kestrel", Spot = "gullrock", BackBy = 15 * 60 }; t.Staff.Add(x);
                double sum = 0; int n = 4000;
                for (int d = 1; d <= n; d++) { t.StartShift(x, d, DayTime.At(d, 8 * 60), 30, false); sum += x.Run.Catch; if (x.Run.Fail == null && Math.Abs(x.Run.HomeAt - DayTime.At(d, 15 * 60)) > 1e-6) return F("back-by 15:00: not home at 15:00"); }
                double mean = sum / n, exp = Expected(x, "kestrel", false), full = Expected(new Worker { Fishing = 3, Sea = 3, Quirk = "" }, "kestrel", false);
                if (Math.Abs(mean - exp) > exp * 0.03 + 0.5) return F($"back-by: mean {mean:F2} vs expected {exp:F2}");
                if (exp >= full * 0.85) return F($"back-by 15:00 should catch ~7/9 of a full shift ({exp:F1} vs {full:F1})");
                x.BackBy = 10 * 60; if (BackOf(x) != 11 * 60) return F("back-by under 3 hours out isn't clamped");
            }
            if (c.CheckM6() != "") return F("CheckM6: " + c.CheckM6());
            return "";
        }
    }
}
