using System;
using System.Collections.Generic;

namespace PortBoss.M1
{
    // M7 step 3: fish orders (gdd world §7.3 "Fish order": species x count x grade x deadline; economy §2 "The Pelican
    // orders" 150-200%; MUST). Plain C# in the company, like Mari's mooring: the host's copy is the truth, the crew sees it.
    // - Amara Bell (The Pelican, Lantern Key) texts her first order on Day 2 at 11:30: 3 Rare mackerel, $30 each (175% of
    //   a Basic mackerel's $10 is $17.50; Rare is 2x, so her $30 is the gdd's number), before 8 PM tomorrow.
    // - Accept or Not now, in her thread or on the Contracts app. Not now, or too late: she asks again the next day at 11:30.
    // - Deliver at her counter: the right fish from your hotbar, not gone soft (Fresh or Good). The money is the crew's.
    //   Delivering her first order raises "amara:1": Act 1's goal (Story.cs), and Walt's "AMARA CALLED ME" that evening.
    public enum OrdSt { Offered, Active, Done, Late, Declined }

    public partial class Company
    {
        public const string AmaraName = "Amara Bell";
        public class Ord
        {
            public int Id; public string From, Species, What; public int Count, Each, Grade; public double Offered, Due, Done = -1; public OrdSt St; public string By;
            public int Total => Count * Each;
        }
        public readonly List<Ord> FishOrders = new();
        int ordNext = 1;

        public Ord OrdOf(int id) => FishOrders.Find(o => o.Id == id);
        public Ord OpenOrd(string from) => FishOrders.FindLast(o => o.From == from && (o.St == OrdSt.Offered || o.St == OrdSt.Active));

        // Amara's order: her first on Day 2 at 11:30 (Story.cs), again the next day at 11:30 after a no or a late one
        public Ord OfferAmara(double m)
        {
            var o = new Ord { Id = ordNext++, From = AmaraName, Species = "mackerel", What = "Mackerel", Count = 3, Grade = 1, Each = 30, Offered = m, Due = DayTime.At(DayTime.Day(m) + 1, 20 * 60), St = OrdSt.Offered };
            FishOrders.Add(o);
            bool first = !FishOrders.Exists(x => x.From == AmaraName && x.Id != o.Id);
            if (first) Text(AmaraName, "Walt's grandchild? He's been calling me every hour. Fine.", m);
            Text(AmaraName, $"Three mackerel, Rare, before 8 PM tomorrow. ${o.Each} each. Don't bring me anything soft.", m, offer: "ord:" + o.Id);
            return o;
        }

        // the first answer wins (co-op: "Rani accepted"); false when there's nothing to answer
        public bool AnswerOrd(int id, bool yes, string who, double m)
        {
            var o = OrdOf(id); if (o == null || o.St != OrdSt.Offered) return false;
            if (yes) { o.St = OrdSt.Active; o.By = who; Text(o.From, "Deal.", m, me: true); Text(o.From, "Good. The kitchen door's round the back of the terrace. Lantern Key, the jetty under the lamp.", m); }
            else { o.St = OrdSt.Declined; Text(o.From, "Not this time, sorry.", m, me: true); Text(o.From, "Your loss. I'll ask once more tomorrow.", m); }
            return true;
        }

        // which of these fish would fill the order (null + why when they can't)
        public static List<int> Fits(Ord o, IList<Lot> fish, out string why)
        {
            why = null; var take = new List<int>();
            for (int i = 0; i < fish.Count && take.Count < o.Count; i++)
            {
                var f = fish[i];
                if (f.Species != o.Species || f.Grade < o.Grade) continue;
                if (f.Fresh >= 2) continue;                                       // "anything soft": Stale or worse
                take.Add(i);
            }
            if (take.Count < o.Count) { why = $"She wants {o.Count} {(o.Grade > 0 ? "Rare " : "")}{o.What.ToLowerInvariant()}, not soft. You have {take.Count}."; return null; }
            return take;
        }

        // delivered at her counter: paid once per transaction id; false (why) when it doesn't fit or it's too late
        public bool Deliver(string tx, string who, int id, IList<Lot> fish, double at, out string why)
        {
            why = null; var o = OrdOf(id);
            if (o == null || o.St != OrdSt.Active) { why = "No order of hers on."; return false; }
            if (Done.Contains(tx)) { why = "Already delivered."; return false; }
            if (at > o.Due) { why = "Too late for that one."; return false; }
            if (Fits(o, fish, out why) == null) return false;
            o.St = OrdSt.Done; o.Done = at;
            Earn(tx, who, $"{o.From.Split(' ')[0]}'s order · {o.Count} {o.What.ToLowerInvariant()}", o.Total, at);
            Text(o.From, "These are good. Not great. Good. Next week: snapper.", at);
            if (o.From == AmaraName) Event("amara:1", at);
            return true;
        }

        // the clock (Tick): a deadline passed; Amara asks again the next day at 11:30
        void TickOrders(double m)
        {
            foreach (var o in FishOrders)
                if (o.St == OrdSt.Active && m > o.Due) { o.St = OrdSt.Late; Text(o.From, "The kitchen's closed, love. Too late. I'll ask again tomorrow.", m); }
            if (Seen("amara:1")) return;
            var last = FishOrders.FindLast(o => o.From == AmaraName);
            if (last == null) return;                                             // (her first comes from the story: Story.cs)
            if ((last.St == OrdSt.Declined || last.St == OrdSt.Late) && m >= DayTime.At(DayTime.Day(last.St == OrdSt.Late ? last.Due : last.Offered) + 1, 11 * 60 + 30)) OfferAmara(m);
        }

        // ---- save
        void WriteOrders(Json.W w)
        {
            w.K("ordNext").V(ordNext);
            w.K("fishOrders").Arr();
            foreach (var o in FishOrders)
                w.Obj().K("id").V(o.Id).K("from").V(o.From).K("species").V(o.Species).K("what").V(o.What).K("count").V(o.Count).K("each").V(o.Each).K("grade").V(o.Grade)
                 .K("offered").V(o.Offered).K("due").V(o.Due).K("done").V(o.Done).K("st").V((int)o.St).K("by").V(o.By).End();
            w.End();
        }
        void ReadOrders(Dictionary<string, object> o)
        {
            ordNext = Json.I(o, "ordNext", 1);
            foreach (Dictionary<string, object> e in Json.L(o, "fishOrders"))
                FishOrders.Add(new Ord { Id = Json.I(e, "id"), From = Json.S(e, "from"), Species = Json.S(e, "species"), What = Json.S(e, "what"), Count = Json.I(e, "count"), Each = Json.I(e, "each"),
                    Grade = Json.I(e, "grade"), Offered = Json.D(e, "offered"), Due = Json.D(e, "due"), Done = Json.D(e, "done", -1), St = (OrdSt)Json.I(e, "st"), By = Json.S(e, "by") });
        }
        public string CheckOrders()
        {
            var ids = new HashSet<int>(); foreach (var o in FishOrders) { if (!ids.Add(o.Id) || o.Id >= ordNext) return $"order id {o.Id}"; if (o.St == OrdSt.Done && o.Done < 0) return $"order {o.Id} done without a time"; }
            int open = FishOrders.FindAll(x => x.From == AmaraName && (x.St == OrdSt.Offered || x.St == OrdSt.Active)).Count; if (open > 1) return $"{open} open orders from Amara";
            return "";
        }

        // ---------------------------------------------------------------- the order suite (companycheck): "" or what broke
        public static string OrderTest()
        {
            var mk = new Lot { Species = "mackerel", Everyday = true, Value = 20, Fresh = 0, Grade = 1 };
            var basic = mk; basic.Grade = 0; basic.Value = 10; var soft = mk; soft.Fresh = 2;
            // 1) Day 2 11:30: her first order (from the story), once; the offer card on her text
            var c = new Company(); c.Tick(DayTime.At(2, 11 * 60 + 29)); if (c.FishOrders.Count != 0) return "Amara's order came early";
            c.Tick(DayTime.At(2, 11 * 60 + 30)); c.Tick(DayTime.At(2, 12 * 60));
            if (c.FishOrders.Count != 1 || c.FishOrders[0].St != OrdSt.Offered || c.FishOrders[0].Due != DayTime.At(3, 20 * 60)) return "her first order: Day 2 11:30, 3 Rare mackerel by Day 3 8 PM";
            if (!c.Msgs.Exists(x => x.From == AmaraName && x.Offer == "ord:1")) return "her order text carries the offer";
            var id = c.FishOrders[0].Id;
            // 2) a delivery before accepting is refused; accept (the first answer wins)
            if (c.Deliver("d0", "Pip", id, new[] { mk, mk, mk }, DayTime.At(2, 13 * 60), out _)) return "delivered an order nobody accepted";
            if (!c.AnswerOrd(id, true, "Pip", DayTime.At(2, 12 * 60)) || c.AnswerOrd(id, false, "Rani", DayTime.At(2, 12 * 60 + 1))) return "accept, then a second answer";
            // 3) the wrong fish: Basic, soft, too few
            if (c.Deliver("d1", "Pip", id, new[] { basic, basic, basic }, DayTime.At(3, 10 * 60), out _)) return "Basic mackerel filled a Rare order";
            if (c.Deliver("d2", "Pip", id, new[] { mk, mk, soft }, DayTime.At(3, 10 * 60), out _)) return "a soft (Stale) fish filled the order";
            if (c.Deliver("d3", "Pip", id, new[] { mk, mk }, DayTime.At(3, 10 * 60), out _)) return "two filled an order for three";
            // 4) the right three: $90, once; amara:1; Act 1's goal moves
            int cash = c.Cash;
            if (!c.Deliver("d4", "Pip", id, new[] { basic, mk, mk, mk }, DayTime.At(3, 19 * 60), out var why)) return "the right three were refused: " + why;
            if (c.Cash != cash + 90 || !c.Seen("amara:1") || c.FishOrders[0].St != OrdSt.Done) return $"paid $90 and amara:1 (cash {c.Cash - cash})";
            if (c.Deliver("d4", "Pip", id, new[] { mk, mk, mk }, DayTime.At(3, 19 * 60 + 5), out _) || c.Cash != cash + 90) return "a replayed delivery paid twice";
            c.Tick(DayTime.At(5, 12 * 60)); if (c.FishOrders.Count != 1) return "she asked again after the first was done";
            // 5) Not now: she asks again the next day at 11:30; late: too, after her text
            var n = new Company(); n.Tick(DayTime.At(2, 11 * 60 + 30)); n.AnswerOrd(n.FishOrders[0].Id, false, "Pip", DayTime.At(2, 12 * 60));
            n.Tick(DayTime.At(3, 11 * 60 + 29)); if (n.FishOrders.Count != 1) return "asked again too soon";
            n.Tick(DayTime.At(3, 11 * 60 + 30)); if (n.FishOrders.Count != 2 || n.FishOrders[1].St != OrdSt.Offered) return "Not now: she asks again the next day at 11:30";
            n.AnswerOrd(n.FishOrders[1].Id, true, "Pip", DayTime.At(3, 12 * 60));
            n.Tick(DayTime.At(4, 20 * 60 + 1)); if (n.FishOrders[1].St != OrdSt.Late) return "a deadline passed: late";
            if (n.Deliver("d5", "Pip", n.FishOrders[1].Id, new[] { mk, mk, mk }, DayTime.At(4, 20 * 60 + 2), out _)) return "delivered a late order";
            n.Tick(DayTime.At(5, 11 * 60 + 30)); if (n.FishOrders.Count != 3) return "late: she asks again the next day";
            // 6) the save round trip
            var j = n.ToJson(); if (Read((Dictionary<string, object>)Json.Parse(j)).ToJson() != j) return "the save round trip changed the orders";
            if (c.CheckOrders() != "" || n.CheckOrders() != "") return c.CheckOrders() + n.CheckOrders();
            return "";
        }
    }
}
