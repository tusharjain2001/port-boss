using System; using PortBoss.M1;
// The M4 money check (gdd systems §5.2 "contract and money suites") without Unity: Mari's rules step by step, then
// random operations for three seeds (never below $0, the Ledger sums, no transaction twice, the save round trip).
static class CompanyCheckMain
{
    static int Main(string[] args)
    {
        int n = args.Length > 0 ? int.Parse(args[0]) : 20000, fails = 0;
        var m = Company.MariTest(); Console.WriteLine(m == "" ? "Mari + market + June's rules PASS" : "FAIL " + m); if (m != "") fails++;
        foreach (var seed in new[] { 1, 7, 12345 }) { var r = Company.SelfTest(n, seed); Console.WriteLine($"seed {seed}: {r}"); if (!r.StartsWith("PASS")) fails++; }
        foreach (var seed in new[] { 5, 11, 2026 }) { var r = Company.ShopTest(4000, seed); Console.WriteLine($"M5 shops seed {seed}: {r}"); if (!r.StartsWith("PASS")) fails++; }
        var st = Company.StoryTest(); Console.WriteLine(st == "" ? "M7 story: beats, lapses, events, letters, calls, pages, acts, save PASS" : "FAIL M7 story: " + st); if (st != "") fails++;
        var ot = Company.OrderTest(); Console.WriteLine(ot == "" ? "M7 orders: Amara's offer, accept, the wrong fish, delivery once, late, again tomorrow, save PASS" : "FAIL M7 orders: " + ot); if (ot != "") fails++;
        var mt = Company.MoorerTest(); Console.WriteLine(mt == "" ? "M7 Days 2-4: Hugo & Fifi, Doyle's hour, June's shrimp PASS" : "FAIL M7 Days 2-4: " + mt); if (mt != "") fails++;
        var cr = Company.CrewTest(); Console.WriteLine(cr == "" ? "M6 crew: board, hiring, wages, IOU, save, 4000-shift model PASS" : "FAIL M6 crew: " + cr); if (cr != "") fails++;
        Console.WriteLine(fails == 0 ? "ALL PASS" : $"{fails} FAILED");
        return fails == 0 ? 0 : 1;
    }
}
