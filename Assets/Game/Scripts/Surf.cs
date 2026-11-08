using System;
using System.Collections.Generic;

namespace PortBoss.M1
{
    // M8 step 4, Swell Surf Co. (economy §4.2, world §10.3, systems §1.7): Kai Rennick's shelf. Economy's names and prices
    // win over systems' (as rods do): Foam board $350 (stable), Bodyboard $200 (ridden lying down), Longboard $1,200 (slow,
    // an easy pocket), Shortboard $900 (fast and twitchy; after Kai's challenge 1), and the Wetsuit $400 (+50% swim
    // stamina). Everything opens in Act 2. Kai's first foam board is free (economy: "Dawn Patrol ... free foam board
    // rental"; he texts the morning after Act 2 begins). Surfing earns no money (world §10.3): the gear is the only spend.
    // Items: "board:<1..4>", "wetsuit". Personal gear (the buyer's kit), from the company bank like June's.
    public static class SurfGear
    {
        public class Board { public string Name, Short, What; public int Price; }
        public static readonly Board[] Boards =
        {
            null,
            new() { Name = "Foam board", Short = "Foamie", Price = 350, What = "Soft and stable: the easy one" },
            new() { Name = "Bodyboard", Short = "Bodyboard", Price = 200, What = "Ridden lying down. Quick to catch a wave" },
            new() { Name = "Longboard", Short = "Longboard", Price = 1200, What = "Slow and smooth: the pocket's easy" },
            new() { Name = "Shortboard", Short = "Shortboard", Price = 900, What = "Fast and twitchy. Kai's challenge 1 first" },
        };
        public const int WetsuitPrice = 400;
        public const float WetsuitStamina = 1.5f;

        public static bool Is(string item) => item.StartsWith("board:") || item == "wetsuit";
        static bool Valid(string item) => item == "wetsuit" || item.Length == 7 && item.StartsWith("board:") && item[6] >= '1' && item[6] <= '4';
        public static int N(string item) => item[6] - '0';
        public static string Name(string item) => !Valid(item) ? item : item == "wetsuit" ? "Wetsuit" : Boards[N(item)].Name;
        public static bool Owns(Kit k, int board) => board >= 1 && board <= 4 && (k.Boards >> board & 1) != 0;
        public static int Best(Kit k) { foreach (var b in new[] { 3, 4, 1, 2 }) if (Owns(k, b)) return b; return 0; }   // (the board you carry first)
        public static void Give(Kit k, string item) { if (item == "wetsuit") k.Wetsuit = true; else if (Valid(item)) { k.Boards |= 1 << N(item); if (k.Board == 0) k.Board = N(item); } }
    }

    public partial class Company
    {
        public const string KaiName = "Kai Rennick";
        public bool KaiFree => !Seen("kai:foamie");                 // his first foam board, on the house
        int SurfPrice(string item) => item == "board:1" && KaiFree ? 0 : item == "wetsuit" ? SurfGear.WetsuitPrice : item.StartsWith("board:") && item.Length == 7 && item[6] >= '1' && item[6] <= '4' ? SurfGear.Boards[SurfGear.N(item)].Price : -1;
        string SurfWhy(Kit k, string item)
        {
            if (Act < 2) return "Act 2";
            if (item == "wetsuit") return k.Wetsuit ? "You own it." : "";
            int b = SurfGear.N(item);
            if (SurfGear.Owns(k, b)) return "You own it.";
            if (b == 4 && !Seen("kai:ch1")) return "Kai's challenge 1";
            return "";
        }

        // companycheck: the shelf by act, the free foamie once, the shortboard after the challenge, the save
        public static string SurfTest()
        {
            var c = new Company(); c.Earn("e", "Pip", "x", 5000, 600); var k = c.KitOf("solo");
            if (c.CanBuy("solo", "board:1", 1) != "Act 2") return "a board before Act 2: " + c.CanBuy("solo", "board:1", 1);
            c.Act = 2;
            if (c.PriceOf("board:1", 1) != 0 || c.PriceOf("board:3", 1) != 1200 || c.PriceOf("wetsuit", 1) != 400 || c.PriceOf("board:5", 1) != -1) return "the prices";
            int cash = c.Cash;
            if (!c.Buy("s1", "solo", "Pip", "board:1", 1, 700, out var why) || c.Cash != cash || !SurfGear.Owns(k, 1) || k.Board != 1) return "Kai's free foamie: " + why;
            if (c.KaiFree || c.PriceOf("board:1", 1) != 350) return "the foamie is free once";
            if (c.Buy("s2", "solo", "Pip", "board:1", 1, 700, out why) || why != "You own it.") return "a second foamie";
            if (c.Buy("s3", "solo", "Pip", "board:4", 1, 700, out why) || why != "Kai's challenge 1") return "the shortboard before the challenge: " + why;
            c.Event("kai:ch1", 700);
            if (!c.Buy("s4", "solo", "Pip", "board:4", 1, 700, out why) || !SurfGear.Owns(k, 4)) return "the shortboard after the challenge: " + why;
            if (!c.Buy("s5", "solo", "Pip", "wetsuit", 1, 700, out why) || !k.Wetsuit || c.Cash != cash - 900 - 400) return "the wetsuit: " + why;
            if (!c.Buy("s6", "solo", "Pip", "board:3", 1, 700, out _) || SurfGear.Best(k) != 3) return "the longboard first";
            var w = new Json.W(); WriteKit(w, k); var back = ReadKit((Dictionary<string, object>)Json.Parse(w.ToString()));
            if (back.Boards != k.Boards || back.Board != k.Board || !back.Wetsuit) return "the surf kit through a save";
            if (c.Check() != "") return c.Check();
            return "";
        }
    }
}
