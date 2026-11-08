using System;
using System.Collections.Generic;

namespace PortBoss.M1
{
    // M7 step 4: the second moorers, Hugo & Fifi (gdd world §4.3 beat 1, §6.3; §9.1 58:00 "Hugo & Fifi's moorer request
    // (Act 2 starts)"). A retired houseboat couple; they gossip, so word gets round (the rival's warnings: M9).
    // - They text when Act 2 begins (Story.cs, "act:2"): $35 a day, once there's a berth for them (B2, Plank & Post).
    // - Accept, or Not now (they ask again the next evening at 6, three times like Mari, then they find somewhere else).
    // - Accepted and B2 repaired: they come the next morning and pay $35 every 07:00 from the day after.
    // ponytail: their houseboat isn't modelled yet; they're a text, a contract card and a Ledger line until it is.
    public partial class Company
    {
        public const string HugoName = "Hugo & Fifi";
        public const int HugoFee = 35, HugoAsks = 3;
        public DealSt Hugo; public int HugoAsk, HugoFrom; public double HugoOffered = -1, HugoAccepted = -1; public string HugoBy;
        public bool HugoHere => Hugo == DealSt.Active && HugoFrom > 0;

        public void OfferHugo(double m)
        {
            Hugo = DealSt.Offered; HugoOffered = m;
            if (HugoAsk == 0)
            {
                Text(HugoName, "Hello! Hugo here (and Fifi). Mari says the Hale dock is open again?", m);
                Text(HugoName, $"We're retired, we're quiet, we have a houseboat called The Last Word. ${HugoFee} a day once you've a berth for us? Fifi makes scones.", m, offer: "hugo");
            }
            else Text(HugoName, $"Fifi says ask again. Still ${HugoFee} a day, still scones.", m, offer: "hugo");
        }

        public bool AnswerHugo(bool yes, string who, double m)
        {
            if (Hugo != DealSt.Offered) return false;
            if (yes)
            {
                Hugo = DealSt.Active; HugoAccepted = m; HugoBy = who;
                Text(HugoName, "Lovely!", m, me: true);
                Text(HugoName, Has("dock:b2") ? "Wonderful. We'll motor round in the morning." : "Wonderful! We'll come the morning after there's a berth for us. Ingrid at Plank & Post can mend B2.", m);
            }
            else
            {
                HugoAsk++; Text(HugoName, "Not now, sorry.", m, me: true);
                if (HugoAsk >= HugoAsks) { Hugo = DealSt.GaveUp; Text(HugoName, "Never mind, dear. We'll find a corner at Breaker Point.", m); }
                else { Hugo = DealSt.Declined; Text(HugoName, "Fair enough. We'll ask again tomorrow.", m); }
            }
            return true;
        }

        // the clock: asked again the next evening at 6; accepted + B2 repaired: they come the next morning
        void TickMoorers(double m)
        {
            if (Hugo == DealSt.Declined && m >= DayTime.At(DayTime.Day(HugoOffered) + 1, 18 * 60)) OfferHugo(m);
            if (Hugo == DealSt.Active && HugoFrom == 0 && Has("dock:b2"))
            {
                HugoFrom = DayTime.Day(DayTime.NextWake(m)) + 1;
                Text(HugoName, "We'll be with you at first light! Fifi's packing the scones.", m);
            }
        }
        // 07:00 (RunMorning): their rent from the second morning after B2
        void MorningM7(int day, double at)
        {
            if (Hugo == DealSt.Active && HugoFrom > 0 && day == HugoFrom - 1) Text(HugoName, "Tied up at B2. Thank you, dear. Fifi says the view's better than Voss's.", at);
            if (Hugo == DealSt.Active && HugoFrom > 0 && day >= HugoFrom) Earn($"rent:hugo:{day}", HugoName, "Hugo & Fifi's mooring · B2", HugoFee, at);
        }

        void WriteMoorers(Json.W w)
        {
            w.K("hugo").Obj().K("st").V((int)Hugo).K("ask").V(HugoAsk).K("from").V(HugoFrom).K("offered").V(HugoOffered).K("accepted").V(HugoAccepted).K("by").V(HugoBy).End();
        }
        void ReadMoorers(Dictionary<string, object> o)
        {
            var h = Json.O(o, "hugo"); if (h == null) return;
            Hugo = (DealSt)Json.I(h, "st"); HugoAsk = Json.I(h, "ask"); HugoFrom = Json.I(h, "from"); HugoOffered = Json.D(h, "offered", -1); HugoAccepted = Json.D(h, "accepted", -1); HugoBy = Json.S(h, "by");
        }

        // ---------------------------------------------------------------- the moorer + Days 2-4 suite (companycheck): "" or what broke
        public static string MoorerTest()
        {
            int Count(Company c, string from) => c.Msgs.FindAll(x => x.From == from && !x.Me).Count;
            // 1) Hugo & Fifi ask with Act 2; yes before B2: they wait; B2 built: the next morning they come, rent the day after
            var c = new Company(); c.Tick(DayTime.At(5, 18 * 60));
            if (c.Act != 2 || c.Hugo != DealSt.Offered || !c.Msgs.Exists(x => x.Offer == "hugo")) return "Hugo & Fifi should ask when Act 2 begins";
            if (!c.AnswerHugo(true, "Pip", DayTime.At(5, 18 * 60 + 5)) || c.AnswerHugo(false, "Rani", DayTime.At(5, 18 * 60 + 6))) return "accept, then a second answer";
            c.Tick(DayTime.At(7, 7 * 60)); if (c.HugoFrom != 0) return "they came without a berth";
            c.Owned.Add("dock:b2"); c.Tick(DayTime.At(7, 9 * 60));
            if (c.HugoFrom != 9) return $"B2 on Day 7: they tie up Day 8, rent from Day 9 (from {c.HugoFrom})";
            int cash = c.Cash; c.Tick(DayTime.At(8, 7 * 60)); if (c.Cash != cash) return "rent on the morning they arrive";
            c.Tick(DayTime.At(9, 7 * 60)); if (c.Cash != cash + HugoFee || !c.Ledger.Exists(e => e.Tx == "rent:hugo:9")) return "no $35 on Day 9";
            // 2) no x3: they give up
            var n = new Company(); n.Tick(DayTime.At(5, 18 * 60)); n.AnswerHugo(false, "Pip", DayTime.At(5, 18 * 60 + 1));
            n.Tick(DayTime.At(6, 17 * 60 + 59)); if (n.Hugo != DealSt.Declined) return "asked again too soon";
            n.Tick(DayTime.At(6, 18 * 60)); if (n.Hugo != DealSt.Offered) return "Not now: they ask again the next evening";
            n.AnswerHugo(false, "Pip", DayTime.At(6, 18 * 60 + 1)); n.Tick(DayTime.At(7, 18 * 60)); n.AnswerHugo(false, "Pip", DayTime.At(7, 18 * 60 + 1));
            if (n.Hugo != DealSt.GaveUp) return "three noes: they find somewhere else";
            // 3) Doyle's engine rebuild: done within the in-game hour (Fergus texts), not overnight
            var d = new Company(); d.Earn("e", "Pip", "x", 500, DayTime.At(1, 15 * 60)); d.Tick(DayTime.At(1, 16 * 60));
            if (!d.BuyM5("r1", "solo", "Pip", "rep:biscuit:engine", 0, DayTime.At(1, 16 * 60), false, out var why)) return "the engine rebuild: " + why;
            d.Tick(DayTime.At(1, 16 * 60 + 59)); if (d.EngineFixed) return "the engine was done early";
            d.Tick(DayTime.At(1, 17 * 60)); if (!d.EngineFixed || !d.Msgs.Exists(x => x.From == FergusName && x.Text.StartsWith("She's done"))) return "the engine should be done in the hour (Fergus: She's done)";
            // 4) shrimp: not on Day 1 (June: "in tomorrow"), yes on Day 2
            var j = new Company(); j.Earn("e", "Pip", "x", 100, DayTime.At(1, 9 * 60)); j.Tick(DayTime.At(1, 9 * 60));
            if (j.CanBuy("solo", "bait:1", 1) != "In tomorrow") return $"shrimp on Day 1: {j.CanBuy("solo", "bait:1", 1)}";
            j.Tick(DayTime.At(1, 18 * 60)); if (!j.Msgs.Exists(x => x.From == JuneName && x.Text.StartsWith("Shrimp in stock tomorrow"))) return "June's 6 PM text";
            j.Tick(DayTime.At(2, 7 * 60)); if (j.CanBuy("solo", "bait:1", 1) != "") return "shrimp on Day 2: " + j.CanBuy("solo", "bait:1", 1);
            // 5) the save round trip
            var s = c.ToJson(); if (Read((Dictionary<string, object>)Json.Parse(s)).ToJson() != s) return "the save round trip changed the moorers";
            if (Count(c, HugoName) < 3) return "Hugo & Fifi's texts";
            return "";
        }
    }
}
