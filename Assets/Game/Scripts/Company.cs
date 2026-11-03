using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PortBoss.M1
{
    // M4 "the first income" (gdd economy §2, §4.1, §5.2, §8; systems §1.2): the company's money and deals as plain C#
    // with no Unity in it, so the money rules run anywhere (tools/companycheck, the EditMode suite).
    // - One shared company bank (co-op: the host's copy is the truth). Cash never goes below $0: a spend that doesn't fit
    //   changes nothing. Every dollar is a Ledger line (who, what, when, how much, the balance after), and every change
    //   carries a transaction id that is applied once only (a replay changes nothing: economy §10).
    // - The market: price = base x grade x freshness x buyer factor x saturation (-2% per same species per buyer per
    //   day, floor 60%, resets at 07:00). Pratt's Fish Market pays 60% (05:00-16:00); Mari 100% (09:00-22:00, once she
    //   is moored), 10 fish a day, Everyday species only.
    // - Mari's mooring (MUST, Manan 31 Oct): she texts on Day 3 at 07:00 or the moment TOTAL EARNED reaches $250,
    //   whichever comes first. Accept: she ties up at B1 within the in-game hour and pays $40 every 07:00 from the next
    //   morning. Decline: she asks again the next morning at $45, then $50, then gives up.
    // - Kits: each player's own gear (rod tier, the bait pouch), bought from the bank, owned by that player (§8).
    public enum Buyer { Pratt, Mari }

    public static class Market
    {
        public static readonly string[] Name = { "Pratt's Fish Market", "Mari's Tacos" };
        public static readonly string[] Short = { "Pratt", "Mari" };
        public static readonly float[] Factor = { 0.6f, 1.0f };
        public static readonly double[] Open = { 300, 540 }, Close = { 960, 1320 };   // 05:00-16:00, 09:00-22:00
        public const int MariDaily = 10;
        public const double SatStep = 0.02, SatFloor = 0.6;
        public static bool IsOpen(Buyer b, double m) { double h = DayTime.OfDay(m); return h >= Open[(int)b] && h < Close[(int)b]; }
        public static bool NoSat;                                // test only (-pb-sat off): economy §2.3's table prices without saturation
        public static double Sat(int soldToday) => NoSat ? 1 : Math.Max(SatFloor, 1 - SatStep * soldToday);
        // one fish, in whole dollars: value = base x grade; fresh = the rot stage (Rot.Price); soldToday = this buyer's
        // count of this species today before this fish
        public static int Price(float value, int fresh, Buyer b, int soldToday) =>
            (int)Math.Round(value * Rot.Price[Math.Clamp(fresh, 0, 4)] * Factor[(int)b] * Sat(soldToday), MidpointRounding.AwayFromZero);
    }

    // Hook, Line & Sinker's shelf (economy §4.1, fishing §6.1). The baits are FishDb.Baits by index: 0 Worm, 1 Shrimp,
    // 2 Sardine, 3 Squid, 4 Walt's Mix. The pouch starts with 3 bait types (Worm, Shrimp, Sardine); the $250 tackle
    // pouch makes it 5. 99 of each at most.
    public static class Shelf
    {
        public static readonly int[] BaitPack = { 5, 12, 18, 25, 40 };      // a pack of 10 ($), Walt's Mix: $40 for 1
        public static readonly int[] PackOf = { 10, 10, 10, 10, 1 };
        public const int PouchPrice = 250, BaitMax = 99, Pouch3 = 3;
        public static readonly int[] RodPrice = { 0, 0, 450, 1600, 6000, 15000 }; // by tier
        public static bool NeedsPouch(int bait) => bait >= Pouch3;
    }

    public class Kit
    {
        public int Rod = 1;                                        // the best rod tier owned (1 = Walt's bamboo)
        public int[] Bait = { 10, 0, 0, 0, 0 };                    // June's free bag of worms (10)
        public int BaitI;                                          // the bait on the hook
        public bool Pouch5;
        public Dictionary<string, float> Best = new();              // the journal: species -> the biggest landed (cm)
        public Kit Copy() => new() { Rod = Rod, Bait = (int[])Bait.Clone(), BaitI = BaitI, Pouch5 = Pouch5, Best = new(Best) };
    }

    public enum DealSt { None, Offered, Declined, Active, GaveUp }

    public class Company
    {
        public const int StartCash = 40, MariFee = 40, MariStep = 5, MariAsks = 3, MariEarned = 250, MariDay = 3;
        public const double MariComes = 60;                        // she arrives within the in-game hour

        public class Entry { public string Tx, Who, What; public double At; public int Amount, Balance; }
        public class Msg { public int Id; public string From, Text; public double At; public bool Me; public string Offer; public bool Read; }

        public int Cash = StartCash, Earned;                       // Earned: every dollar ever earned (Mari's trigger)
        public int Morning = 1;                                    // the last day whose 07:00 has been run
        public readonly List<Entry> Ledger = new();
        public readonly HashSet<string> Done = new();              // transaction ids applied
        public readonly Dictionary<string, int> Sold = new();      // "Pratt:mackerel" -> sold today (saturation)
        public int MariToday;                                      // fish Mari bought today
        public readonly List<Msg> Msgs = new();
        public readonly Dictionary<string, Kit> Kits = new();      // by player key (SteamID64, or "solo")
        int msgNext = 1;

        // Mari's mooring deal
        public DealSt Mari;
        public int MariAsk, MariPrice = MariFee, MariFirstPay, MariRetry;
        public double MariOffered = -1, MariAccepted = -1;
        public string MariBy;                                      // who accepted (co-op: "Rani accepted")
        public bool MariTracked;

        public event Action<Msg> Texted;                           // a new text (the phone, a LEFT toast)
        public event Action<Entry> Booked;                         // a new Ledger line

        public Kit KitOf(string key) { if (!Kits.TryGetValue(key, out var k)) Kits[key] = k = new Kit(); return k; }

        public bool MariHere(double m) => Mari == DealSt.Active && m >= MariAccepted + MariComes;
        public int SoldToday(Buyer b, string species) => Sold.TryGetValue(Market.Short[(int)b] + ":" + species, out var n) ? n : 0;

        // ---- money: every change goes through Book
        Entry Book(string tx, string who, string what, int amount, double at)
        {
            Cash += amount; if (amount > 0) Earned += amount;
            var e = new Entry { Tx = tx, Who = who, What = what, Amount = amount, At = at, Balance = Cash };
            Ledger.Add(e); Booked?.Invoke(e);
            return e;
        }

        // a spend: all or nothing; false when the bank can't cover it or the transaction was already applied
        public bool Spend(string tx, string who, string what, int amount, double at)
        {
            if (amount < 0 || Done.Contains(tx) || amount > Cash) return false;
            Done.Add(tx); Book(tx, who, what, -amount, at); return true;
        }
        public bool Earn(string tx, string who, string what, int amount, double at)
        {
            if (amount < 0 || !Done.Add(tx)) return false;
            Book(tx, who, what, amount, at); Tick(at); return true;
        }

        // ---- selling. fish: (species id, class Everyday?, value = base x grade, rot stage). Returns what each fish
        // fetched (null: refused, nothing sold); `why` says why for the toast.
        public struct Lot { public string Species; public bool Everyday; public float Value; public int Fresh; }
        public int[] Quote(Buyer b, IList<Lot> fish, double at, out string why)
        {
            why = null;
            if (!Market.IsOpen(b, at)) { why = b == Buyer.Pratt ? "Pratt's is shut. Back at 5 AM." : "Mari serves 9 AM to 10 PM."; return null; }
            if (b == Buyer.Mari && !MariHere(at)) { why = "Mari isn't moored here yet."; return null; }
            if (b == Buyer.Mari && MariToday + fish.Count > Market.MariDaily) { why = MariToday >= Market.MariDaily ? "Mari's had her 10 fish today." : $"Mari takes {Market.MariDaily - MariToday} more today."; return null; }
            var p = new int[fish.Count]; var seen = new Dictionary<string, int>();
            for (int i = 0; i < fish.Count; i++)
            {
                var f = fish[i];
                if (b == Buyer.Mari && !f.Everyday) { why = "Mari only takes everyday fish: sprat, mullet, mackerel."; return null; }
                if (f.Fresh >= 4) { why = "Nobody buys a rotten fish."; return null; }
                seen.TryGetValue(f.Species, out int k);
                p[i] = Market.Price(f.Value, f.Fresh, b, SoldToday(b, f.Species) + k);
                seen[f.Species] = k + 1;
            }
            return p;
        }
        public int[] Sell(string tx, string who, Buyer b, IList<Lot> fish, double at, out string why)
        {
            why = null;
            if (Done.Contains(tx)) { why = "Already sold."; return null; }
            var p = Quote(b, fish, at, out why); if (p == null) return null;
            int sum = 0; foreach (var x in p) sum += x;
            foreach (var f in fish) { string key = Market.Short[(int)b] + ":" + f.Species; Sold[key] = SoldToday(b, f.Species) + 1; }
            if (b == Buyer.Mari) MariToday += fish.Count;
            Done.Add(tx);
            string what = fish.Count == 1 ? $"Sold 1 {Pretty(fish[0].Species)} · {Market.Short[(int)b]}" : $"Sold {fish.Count} fish · {Market.Short[(int)b]}";
            Book(tx, who, what, sum, at);
            Tick(at);
            return p;
        }
        public static Func<string, string> Namer = s => s;      // species id -> name (the game sets it to FishDb's)
        static string Pretty(string sp) => Namer(sp);

        // ---- June's shelf (personal gear from the company bank). item: "bait:<i>" x packs, "rod:<tier>", "pouch"
        public int PriceOf(string item, int qty) =>
            item.StartsWith("bait:") ? Shelf.BaitPack[int.Parse(item.Substring(5))] * qty :
            item.StartsWith("rod:") ? Shelf.RodPrice[int.Parse(item.Substring(4))] :
            item == "pouch" ? Shelf.PouchPrice : -1;
        public static string ItemName(string item, int qty)
        {
            if (item.StartsWith("bait:")) { int i = int.Parse(item.Substring(5)); string[] n = { "Worms", "Shrimp", "Sardine", "Squid strip", "Walt's Mix" }; return i == 4 ? $"{qty} Walt's Mix" : $"{n[i]} ×{qty * Shelf.PackOf[i]}"; }
            if (item.StartsWith("rod:")) { string[] r = { "", "Walt's Bamboo Rod", "Fibreglass Rod", "Carbon Rod", "Deep-sea Rod", "Old Faithful" }; return r[int.Parse(item.Substring(4))]; }
            return item == "pouch" ? "Tackle pouch (all 5 baits)" : item;
        }
        // why this can't be bought now ("" = it can)
        public string CanBuy(string key, string item, int qty)
        {
            var k = KitOf(key); int price = PriceOf(item, qty);
            if (price < 0 || qty < 1) return "Not for sale.";
            if (item.StartsWith("bait:"))
            {
                int i = int.Parse(item.Substring(5));
                if (i == 4 && k.Rod < 5) return "Only for Old Faithful.";
                if (Shelf.NeedsPouch(i) && !k.Pouch5) return "Needs the tackle pouch.";
                if (k.Bait[i] + qty * Shelf.PackOf[i] > Shelf.BaitMax) return "Your pouch is full.";
            }
            else if (item.StartsWith("rod:")) { int t = int.Parse(item.Substring(4)); if (k.Rod >= t) return "You own it."; if (t > 2) return t == 3 ? "Act 2" : t == 4 ? "Act 3" : "Walt's journal"; }
            else if (item == "pouch" && k.Pouch5) return "You own it.";
            if (price > Cash) return $"Need ${price - Cash} more";
            return "";
        }
        public bool Buy(string tx, string key, string who, string item, int qty, double at, out string why)
        {
            why = Done.Contains(tx) ? "Already bought." : CanBuy(key, item, qty);
            if (why != "") return false;
            if (!Spend(tx, who, ItemName(item, qty), PriceOf(item, qty), at)) { why = "The bank can't cover it."; return false; }
            var k = KitOf(key);
            if (item.StartsWith("bait:")) { int i = int.Parse(item.Substring(5)); k.Bait[i] += qty * Shelf.PackOf[i]; }
            else if (item.StartsWith("rod:")) k.Rod = int.Parse(item.Substring(4));
            else if (item == "pouch") k.Pouch5 = true;
            return true;
        }

        // ---- texts
        public Msg Text(string from, string text, double at, bool me = false, string offer = null)
        {
            var m = new Msg { Id = msgNext++, From = from, Text = text, At = at, Me = me, Offer = offer, Read = me };
            Msgs.Add(m); Texted?.Invoke(m); return m;
        }
        public int Unread(string from = null) { int n = 0; foreach (var m in Msgs) if (!m.Read && (from == null || m.From == from)) n++; return n; }

        // ---- the clock: Mari's text when its time comes; every 07:00 crossed (a sleep skip crosses one): the morning
        public void Tick(double m)
        {
            int today = DayTime.Day(m - DayTime.Wake) ; // the "business day": 07:00 to 07:00
            while (Morning < today) { Morning++; RunMorning(Morning, DayTime.At(Morning, DayTime.Wake)); }
            if (Mari == DealSt.None && (Earned >= MariEarned || m >= DayTime.At(MariDay, DayTime.Wake))) OfferMari(m);
        }

        void RunMorning(int day, double at)
        {
            Sold.Clear(); MariToday = 0;
            if (Mari == DealSt.Active && day >= MariFirstPay) Earn($"rent:mari:{day}", "Mari Duarte", "Mari's mooring · B1", MariPrice, at);
            if (Mari == DealSt.Declined && day >= MariRetry) { MariPrice = MariFee + MariStep * MariAsk; OfferMari(at); }
        }

        public const string MariName = "Mari Duarte";
        void OfferMari(double m)
        {
            Mari = DealSt.Offered; MariOffered = m;
            if (MariAsk == 0)
            {
                Text(MariName, "Hi! Walt said the old Hale dock is yours now.", m);
                Text(MariName, $"Can I moor my taco boat at your place? ${MariPrice} a day, paid every morning. Plus tacos.", m, offer: "mari");
            }
            else if (MariAsk == 1) Text(MariName, $"Thought about it? I can do ${MariPrice} a day. And the tacos.", m, offer: "mari");
            else Text(MariName, $"Last try, landlord: ${MariPrice} a day. Tacos forever.", m, offer: "mari");
        }

        // the first answer wins (co-op: the others see "Rani accepted"); false when there's nothing to answer
        public bool AnswerMari(bool yes, string who, double m)
        {
            if (Mari != DealSt.Offered) return false;
            if (yes)
            {
                Mari = DealSt.Active; MariAccepted = m; MariBy = who; MariFirstPay = DayTime.Day(DayTime.NextWake(m));
                Text(MariName, "Sounds good!", m, me: true);
                Text(MariName, $"You're the best. On my way with the boat. First ${MariPrice} lands tomorrow at 7.", m);
            }
            else
            {
                MariAsk++;
                Text(MariName, "Not now, sorry.", m, me: true);
                if (MariAsk >= MariAsks) { Mari = DealSt.GaveUp; Text(MariName, "No worries. I'll find another spot. Still friends?", m); }
                else { Mari = DealSt.Declined; MariRetry = DayTime.Day(DayTime.NextWake(m)); Text(MariName, "Okay! I'll ask again tomorrow.", m); }
            }
            return true;
        }
        public void HaggleMari(double m)
        {
            if (Mari != DealSt.Offered) return;
            Text(MariName, "Can you pay more?", m, me: true);
            Text(MariName, $"Ha! ${MariPrice} is what the tacos earn. Free lunch every day though.", m);
        }

        // ---- the day's summary for the night card (economy §8): per person in, and what was spent
        public (int inSum, int outSum, List<(string who, int amount)> byWho) DaySummary(double from, double to)
        {
            int i = 0, o = 0; var by = new List<(string, int)>();
            foreach (var e in Ledger)
            {
                if (e.At < from || e.At >= to) continue;
                if (e.Amount >= 0) { i += e.Amount; int k = by.FindIndex(x => x.Item1 == e.Who); if (k < 0) by.Add((e.Who, e.Amount)); else by[k] = (e.Who, by[k].Item2 + e.Amount); }
                else o -= e.Amount;
            }
            return (i, o, by);
        }

        // ---- invariants: "" when they hold
        public string Check()
        {
            if (Cash < 0) return $"cash ${Cash} < 0";
            long sum = StartCash; int earned = 0;
            foreach (var e in Ledger) { sum += e.Amount; if (e.Amount > 0) earned += e.Amount; if (e.Balance != sum) return $"ledger line {e.Tx}: balance {e.Balance}, sum {sum}"; if (e.Balance < 0) return $"ledger line {e.Tx} below $0"; }
            if (sum != Cash) return $"ledger sums to ${sum}, cash ${Cash}";
            if (earned != Earned) return $"earned ${Earned}, ledger ${earned}";
            var tx = new HashSet<string>(); foreach (var e in Ledger) if (!tx.Add(e.Tx)) return $"transaction {e.Tx} booked twice";
            foreach (var k in Kits.Values) for (int b = 0; b < 5; b++) if (k.Bait[b] < 0 || k.Bait[b] > Shelf.BaitMax) return $"bait {b}: {k.Bait[b]}";
            return "";
        }

        // ---------------------------------------------------------------- save (JSON; Save.cs wraps it)
        public void Write(Json.W w)
        {
            w.Obj();
            w.K("cash").V(Cash); w.K("earned").V(Earned); w.K("morning").V(Morning); w.K("msgNext").V(msgNext);
            w.K("mariToday").V(MariToday);
            w.K("sold").Obj(); var keys = new List<string>(Sold.Keys); keys.Sort(StringComparer.Ordinal); foreach (var k in keys) w.K(k).V(Sold[k]); w.End();
            w.K("mari").Obj().K("st").V((int)Mari).K("ask").V(MariAsk).K("price").V(MariPrice).K("firstPay").V(MariFirstPay).K("retry").V(MariRetry)
             .K("offered").V(MariOffered).K("accepted").V(MariAccepted).K("by").V(MariBy).K("tracked").V(MariTracked).End();
            w.K("ledger").Arr(); foreach (var e in Ledger) w.Obj().K("tx").V(e.Tx).K("who").V(e.Who).K("what").V(e.What).K("at").V(e.At).K("amount").V(e.Amount).K("balance").V(e.Balance).End(); w.End();
            var done = new List<string>(Done); done.Sort(StringComparer.Ordinal);
            w.K("done").Arr(); foreach (var d in done) w.V(d); w.End();
            w.K("msgs").Arr(); foreach (var m in Msgs) w.Obj().K("id").V(m.Id).K("from").V(m.From).K("text").V(m.Text).K("at").V(m.At).K("me").V(m.Me).K("offer").V(m.Offer).K("read").V(m.Read).End(); w.End();
            var kk = new List<string>(Kits.Keys); kk.Sort(StringComparer.Ordinal);
            w.K("kits").Obj(); foreach (var k in kk) WriteKit(w.K(k), Kits[k]); w.End();
            w.End();
        }
        public static void WriteKit(Json.W w, Kit k)
        {
            w.Obj().K("rod").V(k.Rod).K("baitI").V(k.BaitI).K("pouch5").V(k.Pouch5).K("bait").Arr(); foreach (var b in k.Bait) w.V(b); w.End();
            w.K("best").Obj(); var ks = new List<string>(k.Best.Keys); ks.Sort(StringComparer.Ordinal); foreach (var id in ks) w.K(id).V(k.Best[id]); w.End();
            w.End();
        }
        public static Kit ReadKit(Dictionary<string, object> o)
        {
            var k = new Kit { Rod = Json.I(o, "rod", 1), BaitI = Json.I(o, "baitI"), Pouch5 = Json.B(o, "pouch5") };
            if (o.TryGetValue("bait", out var b) && b is List<object> l) for (int i = 0; i < 5 && i < l.Count; i++) k.Bait[i] = Convert.ToInt32(l[i], CultureInfo.InvariantCulture);
            if (o.TryGetValue("best", out var bo) && bo is Dictionary<string, object> bd) foreach (var kv in bd) k.Best[kv.Key] = (float)Convert.ToDouble(kv.Value, CultureInfo.InvariantCulture);
            return k;
        }
        public static Company Read(Dictionary<string, object> o)
        {
            var c = new Company { Cash = Json.I(o, "cash", StartCash), Earned = Json.I(o, "earned"), Morning = Json.I(o, "morning", 1), MariToday = Json.I(o, "mariToday") };
            c.msgNext = Json.I(o, "msgNext", 1);
            if (o.TryGetValue("sold", out var s) && s is Dictionary<string, object> sd) foreach (var kv in sd) c.Sold[kv.Key] = Convert.ToInt32(kv.Value, CultureInfo.InvariantCulture);
            if (o.TryGetValue("mari", out var mo) && mo is Dictionary<string, object> m)
            {
                c.Mari = (DealSt)Json.I(m, "st"); c.MariAsk = Json.I(m, "ask"); c.MariPrice = Json.I(m, "price", MariFee); c.MariFirstPay = Json.I(m, "firstPay");
                c.MariRetry = Json.I(m, "retry"); c.MariOffered = Json.D(m, "offered", -1); c.MariAccepted = Json.D(m, "accepted", -1); c.MariBy = Json.S(m, "by"); c.MariTracked = Json.B(m, "tracked");
            }
            if (o.TryGetValue("ledger", out var lo) && lo is List<object> ll)
                foreach (Dictionary<string, object> e in ll) c.Ledger.Add(new Entry { Tx = Json.S(e, "tx"), Who = Json.S(e, "who"), What = Json.S(e, "what"), At = Json.D(e, "at"), Amount = Json.I(e, "amount"), Balance = Json.I(e, "balance") });
            if (o.TryGetValue("done", out var dn) && dn is List<object> dl) foreach (var d in dl) c.Done.Add((string)d);
            if (o.TryGetValue("msgs", out var ms) && ms is List<object> ml)
                foreach (Dictionary<string, object> e in ml) c.Msgs.Add(new Msg { Id = Json.I(e, "id"), From = Json.S(e, "from"), Text = Json.S(e, "text"), At = Json.D(e, "at"), Me = Json.B(e, "me"), Offer = Json.S(e, "offer"), Read = Json.B(e, "read") });
            if (o.TryGetValue("kits", out var ko) && ko is Dictionary<string, object> kd) foreach (var kv in kd) c.Kits[kv.Key] = ReadKit((Dictionary<string, object>)kv.Value);
            return c;
        }
        public string ToJson() { var w = new Json.W(); Write(w); return w.ToString(); }

        // ---------------------------------------------------------------- the money suite (EditMode + companycheck)
        // n random operations by 3 players (sales at both buyers at all hours, buys of every item, earns, replays of old
        // transaction ids, Mari's answers, mornings), checked after each: never below $0, the Ledger sums to the cash,
        // nothing applied twice. Then the save round trip of the end state.
        public static string SelfTest(int n, int seed = 1)
        {
            var r = new Random(seed); var c = new Company(); double t = DayTime.Start; int ok = 0, refused = 0;
            string[] who = { "Pip", "Rani", "Bo" }; string[] sp = { "sprat", "mullet", "mackerel", "garfish" };
            var txs = new List<string>(); int txn = 0;
            string last = null; string Tx() => last = "t" + (++txn);           // a refused id was never applied: only applied ones are replayed
            for (int i = 0; i < n; i++)
            {
                t += r.NextDouble() * 50; c.Tick(t);
                string w = who[r.Next(3)]; bool applied;
                switch (r.Next(7))
                {
                    case 0: case 1:
                    {
                        var lots = new List<Lot>(); int k = 1 + r.Next(5);
                        for (int j = 0; j < k; j++) { var s = sp[r.Next(4)]; lots.Add(new Lot { Species = s, Everyday = s != "garfish", Value = 6 + r.Next(30), Fresh = r.Next(5) }); }
                        applied = c.Sell(Tx(), w, r.Next(2) == 0 ? Buyer.Pratt : Buyer.Mari, lots, t, out _) != null; break;
                    }
                    case 2: { string[] items = { "bait:0", "bait:1", "bait:2", "bait:3", "bait:4", "rod:2", "rod:3", "pouch" }; applied = c.Buy(Tx(), w, w, items[r.Next(items.Length)], 1 + r.Next(3), t, out _); break; }
                    case 3: applied = c.Spend(Tx(), w, "test spend", r.Next(200), t); break;
                    case 4: applied = txs.Count > 0 && (r.Next(2) == 0 ? c.Spend(txs[r.Next(txs.Count)], w, "replay", 1, t) : c.Earn(txs[r.Next(txs.Count)], w, "replay", 1, t)); if (applied) return $"step {i}: a replayed transaction was applied"; break;
                    case 5: applied = c.AnswerMari(r.Next(3) > 0, w, t); break;
                    default: applied = c.Earn(Tx(), w, "test earn", r.Next(60), t); break;
                }
                if (applied) { ok++; if (last != null) txs.Add(last); } else refused++;
                last = null;
                var chk = c.Check(); if (chk != "") return $"step {i}: {chk}";
            }
            var j1 = c.ToJson(); var back = Read((Dictionary<string, object>)Json.Parse(j1)); var j2 = back.ToJson();
            if (j1 != j2) return "the save round trip changed the company";
            if (back.Check() != "") return "the loaded company: " + back.Check();
            return $"PASS ({n} ops: {ok} applied, {refused} refused; cash ${c.Cash}, earned ${c.Earned}, {c.Ledger.Count} ledger lines, Mari {c.Mari}, save {j1.Length} chars round-trips)";
        }

        // The Mari rules, step by step (economy §5.2): "" or what broke
        public static string MariTest()
        {
            // 1) total earned reaches $250 on Day 2 15:00: she texts; accept; rent at Day 3 07:00, a Ledger line, not before
            var c = new Company(); double d2 = DayTime.At(2, 900);
            c.Tick(DayTime.At(1, 600)); if (c.Mari != DealSt.None) return "texted too early";
            c.Earn("a", "Pip", "sales", 249, d2 - 60); if (c.Mari != DealSt.None) return "texted at $249 earned";
            c.Spend("s", "Pip", "engine", 200, d2 - 30);
            c.Earn("b", "Pip", "sales", 1, d2); if (c.Mari != DealSt.Offered) return "no text at $250 earned (spending must not delay it)";
            if (c.Unread(MariName) != 2) return $"expected 2 unread texts, got {c.Unread(MariName)}";
            if (!c.AnswerMari(true, "Pip", d2 + 5)) return "accept refused";
            if (c.AnswerMari(false, "Rani", d2 + 6)) return "a second answer was taken";
            if (c.MariHere(d2 + 30) || !c.MariHere(d2 + 66)) return "she should tie up within the hour";
            int cash = c.Cash; c.Tick(DayTime.At(3, 419)); if (c.Cash != cash) return "rent before 07:00";
            c.Tick(DayTime.At(3, 420)); if (c.Cash != cash + 40) return $"no $40 at Day 3 07:00 (cash {c.Cash})";
            if (c.Ledger[^1].What != "Mari's mooring · B1") return "no rent line in the Ledger";
            c.Tick(DayTime.At(5, 420)); if (c.Cash != cash + 120) return "rent should land every morning (Days 3, 4, 5)";
            // 2) nobody earns: she texts on Day 3 at 07:00; decline x3 -> $45, $50, gives up
            var e = new Company();
            e.Tick(DayTime.At(3, 419)); if (e.Mari != DealSt.None) return "texted before Day 3 07:00";
            e.Tick(DayTime.At(3, 420)); if (e.Mari != DealSt.Offered || e.MariPrice != 40) return "no Day 3 07:00 text";
            e.AnswerMari(false, "Pip", DayTime.At(3, 430)); if (e.Mari != DealSt.Declined) return "decline";
            e.Tick(DayTime.At(4, 419)); if (e.Mari != DealSt.Declined) return "asked again too soon";
            e.Tick(DayTime.At(4, 420)); if (e.Mari != DealSt.Offered || e.MariPrice != 45) return $"second ask should be $45 (got {e.MariPrice})";
            e.AnswerMari(false, "Pip", DayTime.At(4, 500)); e.Tick(DayTime.At(5, 420)); if (e.MariPrice != 50) return "third ask should be $50";
            e.AnswerMari(false, "Pip", DayTime.At(5, 500)); if (e.Mari != DealSt.GaveUp) return "she should give up after 3 noes";
            e.Tick(DayTime.At(9, 420)); if (e.Mari != DealSt.GaveUp) return "she came back after giving up";
            // 3) selling: Pratt 60% / Mari 100% / saturation / limits / hours
            var s = new Company(); var mk = new Lot { Species = "mackerel", Everyday = true, Value = 10, Fresh = 0 };
            var p = s.Sell("p1", "Pip", Buyer.Pratt, new[] { mk }, DayTime.At(1, 900), out _); if (p == null || p[0] != 6) return "Pratt should pay $6 for a $10 mackerel";
            if (s.Sell("p2", "Pip", Buyer.Pratt, new[] { mk }, DayTime.At(1, 970), out var why) != null) return "Pratt open after 16:00";
            var five = new List<Lot>(); for (int i = 0; i < 25; i++) five.Add(new Lot { Species = "mullet", Everyday = true, Value = 100, Fresh = 0 });
            p = s.Sell("p3", "Pip", Buyer.Pratt, five, DayTime.At(2, 600), out _);
            if (p[0] != 60 || p[1] != 59 || p[20] != 36 || p[24] != 36) return $"saturation: {p[0]} {p[1]} {p[20]} {p[24]} (want 60 59 36 36)";
            s.Tick(DayTime.At(3, 420)); p = s.Quote(Buyer.Pratt, new[] { five[0] }, DayTime.At(3, 600), out _); if (p[0] != 60) return "saturation should reset at 07:00";
            if (s.Quote(Buyer.Mari, new[] { mk }, DayTime.At(3, 600), out _) != null) return "Mari buys before she's moored";
            s.AnswerMari(true, "Pip", DayTime.At(3, 600));
            p = s.Quote(Buyer.Mari, new[] { mk }, DayTime.At(3, 700), out _); if (p == null || p[0] != 10) return "Mari should pay 100%";
            if (s.Quote(Buyer.Mari, new[] { new Lot { Species = "garfish", Everyday = false, Value = 20 } }, DayTime.At(3, 700), out _) != null) return "Mari took a non-Everyday fish";
            var eleven = new List<Lot>(); for (int i = 0; i < 11; i++) eleven.Add(mk);
            if (s.Sell("m1", "Pip", Buyer.Mari, eleven, DayTime.At(3, 700), out _) != null) return "Mari took 11 fish";
            if (s.Sell("m2", "Pip", Buyer.Mari, eleven.GetRange(0, 10), DayTime.At(3, 700), out _) == null) return "Mari refused 10 fish";
            if (s.Sell("m3", "Pip", Buyer.Mari, new[] { mk }, DayTime.At(3, 710), out _) != null) return "Mari took an 11th fish";
            var stale = mk; stale.Fresh = 2; if (s.Quote(Buyer.Pratt, new[] { stale }, DayTime.At(4, 600), out _)[0] != 4) return "a Stale $10 mackerel at Pratt's should be $4 (10 x 0.6 x 0.6)";
            var rot = mk; rot.Fresh = 4; if (s.Quote(Buyer.Pratt, new[] { rot }, DayTime.At(4, 600), out _) != null) return "Pratt bought a rotten fish";
            // 4) June's: no negative bank, pouch rule, the rod
            var j = new Company();
            if (j.Buy("b1", "solo", "Pip", "rod:2", 1, 500, out why)) return "bought a $450 rod with $40";
            if (!j.Buy("b2", "solo", "Pip", "bait:1", 3, 500, out why) || j.Cash != 4 || j.KitOf("solo").Bait[1] != 30) return "3 packs of shrimp ($36): " + why;
            if (j.Buy("b3", "solo", "Pip", "bait:0", 1, 500, out _)) return "bought $5 worms with $4";
            j.Earn("e", "Pip", "x", 1000, 600);
            if (j.Buy("b4", "solo", "Pip", "bait:3", 1, 600, out why) || why != "Needs the tackle pouch.") return "squid without the pouch";
            if (!j.Buy("b5", "solo", "Pip", "pouch", 1, 600, out _) || !j.Buy("b6", "solo", "Pip", "bait:3", 1, 600, out _)) return "pouch then squid";
            if (!j.Buy("b7", "solo", "Pip", "rod:2", 1, 600, out _) || j.KitOf("solo").Rod != 2) return "the Fibreglass rod";
            if (j.Buy("b8", "solo", "Pip", "bait:1", 7, 600, out why)) return "bait over 99";
            if (j.Check() != "") return j.Check();
            return "";
        }
    }

    // A small JSON writer/reader (the save; Unity's JsonUtility can't do dictionaries, and plain C# keeps the checks
    // outside Unity). Numbers are written invariant and round-trip exactly ("R").
    public static class Json
    {
        public class W
        {
            readonly StringBuilder sb = new(); readonly Stack<bool> first = new(); readonly Stack<char> close = new();
            bool key;                                       // a key was just written: the value needs no comma
            void Pre() { if (!key && first.Count > 0) { if (!first.Peek()) sb.Append(','); first.Pop(); first.Push(false); } key = false; }
            public W Obj() { Pre(); sb.Append('{'); first.Push(true); close.Push('}'); return this; }
            public W Arr() { Pre(); sb.Append('['); first.Push(true); close.Push(']'); return this; }
            public W End() { first.Pop(); sb.Append(close.Pop()); return this; }
            public W K(string k) { Pre(); Str(k); sb.Append(':'); key = true; return this; }
            public W V(string s) { Pre(); if (s == null) sb.Append("null"); else Str(s); return this; }
            public W V(int v) { Pre(); sb.Append(v.ToString(CultureInfo.InvariantCulture)); return this; }
            public W V(long v) { Pre(); sb.Append(v.ToString(CultureInfo.InvariantCulture)); return this; }
            public W V(ulong v) { Pre(); sb.Append(v.ToString(CultureInfo.InvariantCulture)); return this; }
            public W V(double v) { Pre(); sb.Append(v.ToString("R", CultureInfo.InvariantCulture)); return this; }
            public W V(float v) { Pre(); sb.Append(((double)v).ToString("R", CultureInfo.InvariantCulture)); return this; }
            public W V(bool v) { Pre(); sb.Append(v ? "true" : "false"); return this; }
            // a parsed value back out (Parse's kinds)
            public W Any(object v)
            {
                switch (v)
                {
                    case Dictionary<string, object> d: Obj(); var ks = new List<string>(d.Keys); ks.Sort(StringComparer.Ordinal); foreach (var k in ks) { K(k); Any(d[k]); } return End();
                    case List<object> l: Arr(); foreach (var x in l) Any(x); return End();
                    case string str: return V(str);
                    case bool b: return V(b);
                    case double db: return V(db);
                    case null: Pre(); sb.Append("null"); return this;
                    default: return V(Convert.ToDouble(v, CultureInfo.InvariantCulture));
                }
            }
            void Str(string s)
            {
                sb.Append('"');
                foreach (char ch in s)
                    switch (ch)
                    {
                        case '"': sb.Append("\\\""); break; case '\\': sb.Append("\\\\"); break; case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break; case '\t': sb.Append("\\t"); break;
                        default: if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4")); else sb.Append(ch); break;
                    }
                sb.Append('"');
            }
            public override string ToString() => sb.ToString();
        }

        // values: Dictionary<string, object>, List<object>, string, double, bool, null
        public static string Write(object v) => new W().Any(v).ToString();
        public static object Parse(string s) { int i = 0; var v = Val(s, ref i); Ws(s, ref i); if (i != s.Length) throw new FormatException("json: trailing data at " + i); return v; }
        static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
        static object Val(string s, ref int i)
        {
            Ws(s, ref i); if (i >= s.Length) throw new FormatException("json: unexpected end");
            char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>(); i++; Ws(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true) { Ws(s, ref i); var k = StrV(s, ref i); Ws(s, ref i); if (s[i++] != ':') throw new FormatException("json: ':' expected at " + i); d[k] = Val(s, ref i); Ws(s, ref i); if (s[i] == ',') { i++; continue; } if (s[i++] != '}') throw new FormatException("json: '}' expected at " + i); return d; }
            }
            if (c == '[')
            {
                var l = new List<object>(); i++; Ws(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true) { l.Add(Val(s, ref i)); Ws(s, ref i); if (s[i] == ',') { i++; continue; } if (s[i++] != ']') throw new FormatException("json: ']' expected at " + i); return l; }
            }
            if (c == '"') return StrV(s, ref i);
            if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (s.Length - i >= 5 && string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            int st = i; while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (st == i) throw new FormatException("json: bad value at " + i);
            return double.Parse(s.Substring(st, i - st), NumberStyles.Float, CultureInfo.InvariantCulture);
        }
        static string StrV(string s, ref int i)
        {
            if (s[i] != '"') throw new FormatException("json: string expected at " + i);
            var sb = new StringBuilder(); i++;
            while (s[i] != '"')
            {
                char c = s[i++];
                if (c != '\\') { sb.Append(c); continue; }
                char e = s[i++];
                switch (e) { case 'n': sb.Append('\n'); break; case 'r': sb.Append('\r'); break; case 't': sb.Append('\t'); break; case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16)); i += 4; break; default: sb.Append(e); break; }
            }
            i++; return sb.ToString();
        }
        public static int I(Dictionary<string, object> o, string k, int def = 0) => o.TryGetValue(k, out var v) && v is double d ? (int)d : def;
        public static double D(Dictionary<string, object> o, string k, double def = 0) => o.TryGetValue(k, out var v) && v is double d ? d : def;
        public static float F(Dictionary<string, object> o, string k, float def = 0) => o.TryGetValue(k, out var v) && v is double d ? (float)d : def;
        public static bool B(Dictionary<string, object> o, string k, bool def = false) => o.TryGetValue(k, out var v) && v is bool b ? b : def;
        public static string S(Dictionary<string, object> o, string k, string def = null) => o.TryGetValue(k, out var v) && v is string s ? s : def;
        public static Dictionary<string, object> O(Dictionary<string, object> o, string k) => o.TryGetValue(k, out var v) ? v as Dictionary<string, object> : null;
        public static List<object> L(Dictionary<string, object> o, string k) => (o.TryGetValue(k, out var v) ? v as List<object> : null) ?? new List<object>();
    }
}
