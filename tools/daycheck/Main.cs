using System; using PortBoss.M1;
// The M3 check (gdd systems §5.2) without Unity: the day rules, then the weather over 10,000 seeded days for two seeds.
static class DayCheckMain
{
    static int Main(string[] args)
    {
        int n = args.Length > 0 ? int.Parse(args[0]) : 10000, fails = 0;
        foreach (var f in DayCheck.Run()) { fails++; Console.WriteLine("FAIL " + f); }
        foreach (var seed in new[] { 7, 12345 }) { Console.WriteLine(Weather.SelfTest(seed, n, out bool ok) + (ok ? " PASS" : " FAIL")); if (!ok) fails++; }
        Console.WriteLine(fails == 0 ? "ALL PASS" : $"{fails} FAILED");
        return fails == 0 ? 0 : 1;
    }
}
