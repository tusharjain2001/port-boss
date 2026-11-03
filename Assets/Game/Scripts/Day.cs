using System;
using System.Collections.Generic;

namespace PortBoss.M1
{
    // M3 "The day": the rules of time, light, sleep and weather (gdd systems §1.3-1.5, §4.4), as plain C# with no
    // Unity in it, so the 10,000-day weather check and the clock maths run anywhere (tools/daycheck). Sky.cs draws it,
    // Sleep.cs acts on it, CoopDay.cs keeps every machine on the host's clock and seed.
    //
    // Time is one number: in-game minutes since Day 1 00:00 (a new game starts Day 1 07:00 = 420). 1 real second = 1
    // in-game minute.
    public enum LightState { Dawn, Day, Golden, Dusk, Night, Storm }
    public enum Wx { Clear, Fair, Breezy, Fog, Rain, Storm }

    public static class DayTime
    {
        public const double Start = 420, Wake = 420, DayLen = 1440;
        public const double BedFrom = 1200, BedTo = 300;   // beds 20:00-05:00, or any time you're tired
        public const double TiredAt = 1380, HeavyAt = 90, PassOutAt = 120; // 23:00 toast; 01:30 vignette, -10% walk; 02:00

        public static int Day(double m) => (int)Math.Floor(m / DayLen) + 1;
        public static double OfDay(double m) => m - Math.Floor(m / DayLen) * DayLen;
        public static double At(int day, double ofDay) => (day - 1) * DayLen + ofDay;

        // the next 07:00 after m (a skip always lands on a wake-up, never on the same minute)
        public static double NextWake(double m)
        {
            double w = Math.Floor(m / DayLen) * DayLen + Wake;
            return w > m ? w : w + DayLen;
        }

        // awake since 07:00 and past 23:00 (until the next 07:00)
        public static bool Tired(double m) { double h = OfDay(m); return h >= TiredAt || h < Wake; }
        public static bool Heavy(double m) { double h = OfDay(m); return h >= HeavyAt && h < Wake; }  // 01:30-07:00
        public static bool PassOut(double m) { double h = OfDay(m); return h >= PassOutAt && h < Wake; } // 02:00-07:00
        public static bool BedOk(double m) { double h = OfDay(m); return Tired(m) || h >= BedFrom || h < BedTo; }

        static readonly string[] Names = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        public static string DayName(int day) => Names[(day - 1) % 7];

        // 12 h with AM/PM by default (systems §1.3), "7:05 AM"
        public static string Clock(double m, bool h24 = false)
        {
            int t = (int)Math.Floor(OfDay(m)), h = t / 60, mi = t % 60;
            if (h24) return $"{h:00}:{mi:00}";
            return $"{(h % 12 == 0 ? 12 : h % 12)}:{mi:00} {(h < 12 ? "AM" : "PM")}";
        }

        // ---- the 5 time-of-day lighting states (systems §4.4; Storm is the weather's) and the blend between neighbours:
        // each change takes 30 in-game minutes, centred on the table's boundary
        public const double Blend = 30;
        static readonly (double at, LightState to)[] Bounds =
            { (330, LightState.Dawn), (450, LightState.Day), (1050, LightState.Golden), (1170, LightState.Dusk), (1260, LightState.Night) };

        public static (LightState a, LightState b, float k) TimeState(double m)
        {
            double h = OfDay(m);
            for (int i = 0; i < Bounds.Length; i++)
            {
                var (at, to) = Bounds[i];
                var from = i == 0 ? LightState.Night : Bounds[i - 1].to;
                if (Math.Abs(h - at) < Blend / 2)
                {
                    double u = (h - at + Blend / 2) / Blend;
                    return (from, to, (float)(u * u * (3 - 2 * u)));
                }
            }
            var s = LightState.Night;
            foreach (var (at, to) in Bounds) if (h >= at) s = to;
            return (s, s, 0);
        }

        // the sun's height in degrees: up at 06:00, highest (62°) at 13:00, down at 20:00 (systems §1.3, §4.4)
        public static double SunElevation(double m)
        {
            double h = OfDay(m) / 60.0;
            return 62 * Math.Sin(Math.PI * (h - 6) / 14.0);
        }
    }

    // Rot (fishing §5.6, MUST ≥ 2 in-game days): 12 h a stage, rotten at exactly 48 h. Age = world clock minus the catch
    // time, so it runs through sleep by itself. (The warehouse's cold room ages at 25%, the Ice House 10%: rate, M5.)
    public static class Rot
    {
        public const double Full = 48 * 60;
        public static readonly string[] Names = { "Fresh", "Good", "Stale", "Old", "Rotten" };
        public static readonly float[] Price = { 1f, 0.85f, 0.6f, 0.35f, 0f };
        public static int Stage(double ageMin) => ageMin >= Full ? 4 : Math.Max(0, (int)(ageMin / 720));
    }

    // The M3 rule check (no Unity): the clock, the bed and tired hours, the six lighting states and their blend, rot.
    // Run by tools/daycheck.sh (dotnet) and in the game by -pb-weathertest. Returns the failures, empty = all held.
    public static class DayCheck
    {
        public static System.Collections.Generic.List<string> Run()
        {
            var f = new System.Collections.Generic.List<string>();
            void Eq(object a, object b, string what) { if (!Equals(a, b)) f.Add($"{what}: {a} != {b}"); }
            Eq(DayTime.Day(420), 1, "new game is Day 1"); Eq(DayTime.Clock(420), "7:00 AM", "clock"); Eq(DayTime.Clock(1440 + 780), "1:00 PM", "clock pm"); Eq(DayTime.Clock(1440 * 2 + 5), "12:05 AM", "midnight");
            Eq(DayTime.NextWake(1380), 1440 + 420.0, "sleep 23:00 -> next 07:00"); Eq(DayTime.NextWake(1440 + 60), 1440 + 420.0, "01:00 -> same morning 07:00"); Eq(DayTime.NextWake(420), 1440 + 420.0, "07:00 -> tomorrow");
            Eq(DayTime.BedOk(900), false, "15:00 too early"); Eq(DayTime.BedOk(1200), true, "20:00 bed"); Eq(DayTime.BedOk(1440 + 290), true, "04:50 bed");
            Eq(DayTime.Tired(1379), false, "22:59 not tired"); Eq(DayTime.Tired(1380), true, "23:00 tired"); Eq(DayTime.PassOut(1440 + 119), false, "01:59 up"); Eq(DayTime.PassOut(1440 + 120), true, "02:00 pass out");
            foreach (var (m, st) in new[] { (180.0, LightState.Night), (390.0, LightState.Dawn), (720.0, LightState.Day), (1110.0, LightState.Golden), (1215.0, LightState.Dusk), (1410.0, LightState.Night) })
            { var t = DayTime.TimeState(m); Eq(t.a, st, $"state at {DayTime.Clock(m)}"); Eq(t.k, 0f, $"settled at {DayTime.Clock(m)}"); }
            // smooth: the blend never jumps between half minutes (a state change only at k = 0 or 1)
            var prev = DayTime.TimeState(0);
            for (double m = 0.5; m < 1440; m += 0.5)
            {
                var t = DayTime.TimeState(m);
                bool same = t.a == prev.a && t.b == prev.b;
                if (same) { if (Math.Abs(t.k - prev.k) > 0.05f) f.Add($"blend jump at {DayTime.Clock(m)}"); }
                else if (!(t.a == prev.b && prev.k > 0.99f) && !(prev.a == prev.b && t.a == prev.a && t.k < 0.01f) && !(t.a == t.b && prev.b == t.a && prev.k > 0.99f)) f.Add($"state jump at {DayTime.Clock(m)}: {prev} -> {t}");
                prev = t;
            }
            if (!(DayTime.SunElevation(720) > 55 && DayTime.SunElevation(1215) < 5)) f.Add("sun: high at noon, down by 20:15");
            Eq(Rot.Stage(0), 0, "rot fresh"); Eq(Rot.Stage(719.9), 0, "rot < 12 h"); Eq(Rot.Stage(720), 1, "rot 12 h Good"); Eq(Rot.Stage(2879.9), 3, "rot 47.99 h Old"); Eq(Rot.Stage(2880), 4, "rot exactly 48 h Rotten");
            double caught = DayTime.At(1, 18 * 60);
            Eq(Rot.Names[Rot.Stage(DayTime.NextWake(DayTime.At(1, 22 * 60)) - caught)], "Good", "catch 18:00, sleep 22:00, wake 07:00 = Good (13 h)");
            Eq(Rot.Names[Rot.Stage(DayTime.At(3, 12 * 60) - caught)], "Old", "Day 3 noon (42 h) = Old");
            var a = new Weather(99); var b = new Weather(99);              // same seed, any roll order: the same weather
            for (int d = 60; d >= 1; d--) for (int k = 3; k >= 0; k--) Eq(a.Block(d, k), b.Block(d, k), $"weather determinism D{d}/{k}");
            for (int d = 1; d <= 60; d++) for (int k = 0; k < 4; k++) Eq(new Weather(99).Block(d, k), a.Block(d, k), $"weather fresh-roll D{d}/{k}");
            int leaks = 0;                                                    // the storm leak: only in a storm block, inside it, seeded
            for (int d = 1; d <= 400; d++) for (int k = 0; k < 4; k++)
            {
                double at = a.LeakAt(d, k); bool storm = a.Block(d, k) == Wx.Storm;
                if (storm != !double.IsNaN(at)) f.Add($"leak only in a storm D{d}/{k}");
                if (storm) { leaks++; if (Weather.BlockAt(at) != (d, k)) f.Add($"leak inside its storm D{d}/{k}"); if (at != new Weather(99).LeakAt(d, k)) f.Add($"leak seeded D{d}/{k}"); }
            }
            if (leaks == 0) f.Add("no storm leaks in 400 days");
            return f;
        }
    }

    // Weather (systems §1.5): 4 blocks a day, Morning 07-12, Afternoon 12-17, Evening 17-22, Night 22-07, each rolled
    // from the one before by a Markov table, seeded by (world seed, day) so a reload never rerolls. Any machine with the
    // seed gets the same weather, so co-op only shares the seed and the clock.
    public class Weather
    {
        public static readonly string[] BlockName = { "Morning", "Afternoon", "Evening", "Night" };
        public static readonly double[] BlockStart = { 420, 720, 1020, 1320 };

        // state -> next-block weights (Clear, Fair, Breezy, Fog, Rain, Storm). Storm only after Breezy or Rain, and a storm
        // never follows a storm (a storm night ends by 07:00).
        static readonly double[,] Table =
        {
            //  Clear Fair Breezy Fog Rain Storm
            {   50,   35,  8,    5,   2,   0 },  // Clear
            {   25,   45,  20,   2,   8,   0 },  // Fair (systems §1.5.2's example)
            {   5,    30,  35,   0,   20,  10 }, // Breezy
            {   30,   25,  0,    35,  10,  0 },  // Fog
            {   5,    20,  15,   15,  35,  10 }, // Rain
            {   0,    0,   45,   0,   55,  0 },  // Storm
        };

        // story weather (world §11.3): fog on the mornings of Day 4 and Day 13 (the goons), a storm the evening of Day 20
        // (the afternoon before it is Breezy, so it follows the table's rule). Shown in the forecast like any other day.
        static bool Story(int day, int b, out Wx w)
        {
            w = Wx.Fair;
            if ((day == 4 || day == 13) && b == 0) { w = Wx.Fog; return true; }
            if (day == 20 && b == 1) { w = Wx.Breezy; return true; }
            if (day == 20 && b == 2) { w = Wx.Storm; return true; }
            return false;
        }
        static bool StoryStorm(int day) => day == 20;
        public const int CalmDays = 2;          // no storm on Days 1-2: the first days are for learning the sea

        public readonly int Seed;
        readonly List<Wx[]> days = new();       // days[d-1][block]
        public Weather(int seed) { Seed = seed; }

        public Wx Block(int day, int b)
        {
            if (day < 1) return Wx.Fair;
            while (days.Count < day) Roll(days.Count + 1);
            return days[day - 1][b];
        }

        void Roll(int day)
        {
            var w = new Wx[4];
            Wx prev = day == 1 ? Wx.Fair : days[day - 2][3];
            for (int b = 0; b < 4; b++)
            {
                if (Story(day, b, out var s)) { w[b] = prev = s; continue; }
                if (day == 1 && b == 0) { w[b] = prev = Wx.Fair; continue; } // "First Light": a fair first morning
                bool stormOk = day > CalmDays && !StormNear(day, b, w);
                double total = 0;
                for (int k = 0; k < 6; k++) if (k != (int)Wx.Storm || stormOk) total += Table[(int)prev, k];
                double u = U(Seed, day, b) * total;
                int pick = 0;
                for (int k = 0; k < 6; k++)
                {
                    if (k == (int)Wx.Storm && !stormOk) continue;
                    u -= Table[(int)prev, k];
                    if (u < 0) { pick = k; break; }
                    pick = k;
                }
                w[b] = prev = (Wx)pick;
            }
            days.Add(w);
        }

        // the storm budget: at most 1 storm per 4 days (no other storm in the 3 days before, earlier today, or a story
        // storm in the 3 days after)
        bool StormNear(int day, int b, Wx[] today)
        {
            for (int k = 0; k < b; k++) if (today[k] == Wx.Storm) return true;
            for (int d = Math.Max(1, day - 3); d < day; d++) foreach (var x in days[d - 1]) if (x == Wx.Storm) return true;
            for (int d = day; d <= day + 3; d++) if (StoryStorm(d)) return true;
            return false;
        }

        // which block a moment is in: the night block belongs to the day it started on (22:00 Day 3 -> 07:00 Day 4 = Day 3)
        public static (int day, int b) BlockAt(double m)
        {
            int day = DayTime.Day(m); double h = DayTime.OfDay(m);
            if (h < 420) return (day - 1, 3);
            if (h < 720) return (day, 0);
            if (h < 1020) return (day, 1);
            if (h < 1320) return (day, 2);
            return (day, 3);
        }
        public static double StartOf(int day, int b) => DayTime.At(day, BlockStart[b]);
        // a storm can spring a leak in Biscuit (systems §1.6.4): at most once per storm, at a seeded moment 20-140 min in
        public double LeakAt(int day, int b) => Block(day, b) == Wx.Storm ? StartOf(day, b) + 20 + U(Seed, day, 60 + b) * 120 : double.NaN;
        static (int, int) Prev(int day, int b) => b == 0 ? (day - 1, 3) : (day, b - 1);

        // The weather at a moment: (from, to, k). A new block blends in over 6-12 in-game minutes (seeded); one with a
        // storm on either side ramps over 30.
        public (Wx from, Wx to, float k) At(double m)
        {
            var (day, b) = BlockAt(m);
            var now = Block(day, b);
            var (pd, pb) = Prev(day, b);
            var before = Block(pd, pb);
            double len = now == Wx.Storm || before == Wx.Storm ? 30 : 6 + 6 * U(Seed, day, 40 + b);
            double u = (m - StartOf(day, b)) / len;
            if (u >= 1 || before == now) return (now, now, 1);
            u = Math.Max(0, u);
            return (before, now, (float)(u * u * (3 - 2 * u)));
        }

        // ---- the forecast (systems §1.5.3). What the phone shows for a block: right with the table's chance, else one
        // step off (Fair<->Clear, Fair<->Breezy, Rain<->Fog). Storms are always shown, and nothing else ever shows as one.
        // lead 0 = today's forecast (seen at 07:00), lead 1 = tomorrow's (shown from 18:00).
        public static double Accuracy(int b, int lead) => lead == 1 ? 0.70 : b == 0 ? 0.95 : b == 1 ? 0.85 : 0.75;
        public static int Dots(int b, int lead) { double a = Accuracy(b, lead); return a >= 0.95 ? 4 : a >= 0.85 ? 3 : a >= 0.75 ? 2 : 1; }

        public Wx Shown(int day, int b, int lead)
        {
            var real = Block(day, b);
            if (real == Wx.Storm || b == 3) return real;     // (the night block: nobody plans around it, shown as is)
            if (U(Seed, day, 100 + b * 10 + lead) < Accuracy(b, lead)) return real;
            bool other = U(Seed, day, 200 + b * 10 + lead) < 0.5;
            return real switch
            {
                Wx.Clear => Wx.Fair,
                Wx.Fair => other ? Wx.Clear : Wx.Breezy,
                Wx.Breezy => Wx.Fair,
                Wx.Fog => Wx.Rain,
                Wx.Rain => Wx.Fog,
                _ => real,
            };
        }

        // Dot texts about every storm well ahead (≥ 3 in-game h, world §11.3): 4 h before an afternoon, evening or night
        // storm; the evening before (18:00, with tomorrow's forecast) for a morning one.
        public static double StormCall(int day, int b) => b == 0 ? DayTime.At(day - 1, 1080) : StartOf(day, b) - 240;

        // what the phone's Forecast screen will list at a moment (M4 draws it): today's 3 blocks, and from 18:00 tomorrow's
        public struct Line { public int Day, Block, Dots; public Wx Shown; }
        public List<Line> Forecast(double m)
        {
            var l = new List<Line>();
            int day = DayTime.Day(m); double h = DayTime.OfDay(m);
            if (h < 420) day--;                              // after midnight it's still "tonight"
            for (int b = 0; b < 3; b++) l.Add(new Line { Day = day, Block = b, Shown = Shown(day, b, 0), Dots = Dots(b, 0) });
            if (h >= 1080 || h < 420) for (int b = 0; b < 3; b++) l.Add(new Line { Day = day + 1, Block = b, Shown = Shown(day + 1, b, 1), Dots = Dots(b, 1) });
            return l;
        }

        // ---- deterministic randoms: (seed, day, slot) -> [0, 1), the same on every machine and runtime
        public static double U(int seed, int day, int slot)
        {
            ulong z = (ulong)(uint)seed * 0x9E3779B97F4A7C15UL ^ (ulong)(uint)day * 0xC2B2AE3D27D4EB4FUL ^ (ulong)(uint)slot * 0x165667B19E3779F9UL;
            z += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL; z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL; z ^= z >> 31;
            return (z >> 11) * (1.0 / 9007199254740992.0);
        }

        // ---- the M3 check (systems §5.2): 10,000 seeded days. Forecast accuracy within ±1% of 95/85/75/70, every storm
        // called ≥ 3 h ahead, never two storms in any 4 days (story storms aside), a storm only after Breezy or Rain,
        // never Storm then Storm. Returns the report; ok = all held.
        public static string SelfTest(int seed, int n, out bool ok)
        {
            var w = new Weather(seed);
            long[] right = new long[4], all = new long[4]; int storms = 0, late = 0, close = 0, badPrev = 0, falseAlarm = 0;
            var count = new int[6]; int lastStorm = -99;
            for (int d = 1; d <= n; d++)
                for (int b = 0; b < 4; b++)
                {
                    var x = w.Block(d, b); count[(int)x]++;
                    if (b < 3)
                        for (int lead = 0; lead < 2; lead++)
                        {
                            var s = w.Shown(d, b, lead);
                            if (s == Wx.Storm && x != Wx.Storm) falseAlarm++;
                            if (x == Wx.Storm) { if (s != Wx.Storm) late++; continue; } // (storms: 100%, not in the ±1% count)
                            int k = lead == 1 ? 3 : b; all[k]++; if (s == x) right[k]++;
                        }
                    if (x != Wx.Storm) continue;
                    storms++;
                    if (StartOf(d, b) - StormCall(d, b) < 180) late++;
                    var (pd, pb) = Prev(d, b); var p = w.Block(pd, pb);
                    if (p != Wx.Breezy && p != Wx.Rain) badPrev++;
                    if (d - lastStorm < 4 && !StoryStorm(d)) close++;
                    lastStorm = d;
                }
            double[] want = { 0.95, 0.85, 0.75, 0.70 };
            ok = late == 0 && close == 0 && badPrev == 0 && falseAlarm == 0 && storms > 0;
            var sb = new System.Text.StringBuilder();
            sb.Append($"{n} days, seed {seed}: ");
            for (int k = 0; k < 4; k++)
            {
                double a = all[k] > 0 ? (double)right[k] / all[k] : 0;
                bool good = Math.Abs(a - want[k]) <= 0.01; ok &= good;
                sb.Append($"{(k == 3 ? "tomorrow" : BlockName[k])} {a * 100:F2}% (want {want[k] * 100:0}±1, n={all[k]}) {(good ? "ok" : "OFF")}; ");
            }
            sb.Append($"mix Clear {count[0]} Fair {count[1]} Breezy {count[2]} Fog {count[3]} Rain {count[4]} Storm {count[5]}; ");
            sb.Append($"storms {storms}: called late {late}, two within 4 days {close}, not after Breezy/Rain {badPrev}, false alarms {falseAlarm}");
            return sb.ToString();
        }
    }
}
