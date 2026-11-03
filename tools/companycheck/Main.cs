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
        Console.WriteLine(fails == 0 ? "ALL PASS" : $"{fails} FAILED");
        return fails == 0 ? 0 : 1;
    }
}
