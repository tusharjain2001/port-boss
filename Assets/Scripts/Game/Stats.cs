using System.Collections.Generic;
using UnityEngine;

namespace PortBoss
{
    // Career stats: this run (since the game was opened) and all time (kept on this PC). The crew's day totals are
    // added on every peer when the night card comes up; the rest (knocks, swims, throws, metres walked) are this
    // player's own.
    public static class Stats
    {
        static readonly Dictionary<string, int> run = new();
        static float walked;

        public static readonly (string key, string label, string unit)[] Rows =
        {
            ("days", "Days played", ""), ("earned", "Money earned", "$"), ("bestDay", "Best day", "$"),
            ("sold", "Crates sold", ""), ("spoiled", "Spoiled crates sold", ""), ("sea", "Crates lost to the sea", ""),
            ("left", "Crates left on the boats", ""), ("carried", "Crates picked up", ""), ("throws", "Crates thrown", ""),
            ("knocked", "Times knocked over", ""), ("swims", "Times in the water", ""), ("fishedOut", "Fished out by the harbour master", ""),
            ("honked", "Honked at by lorries", ""), ("soaked", "Soaked by Marlowe", ""), ("thrownOff", "Thrown off a boat", ""), ("walked", "Distance on foot", "km"),
        };

        public static int Run(string key) => run.TryGetValue(key, out var n) ? n : 0;
        public static int AllTime(string key) => PlayerPrefs.GetInt("pb.stat." + key, 0);

        public static void Add(string key, int n = 1)
        {
            if (n == 0) return;
            run[key] = Run(key) + n;
            PlayerPrefs.SetInt("pb.stat." + key, AllTime(key) + n);
        }

        static void Max(string key, int v)
        {
            if (v > Run(key)) run[key] = v;
            if (v > AllTime(key)) PlayerPrefs.SetInt("pb.stat." + key, v);
        }

        // Metres on foot (whole metres are banked as they add up).
        public static void Walk(float metres)
        {
            walked += metres;
            if (walked >= 1f) { int m = (int)walked; walked -= m; Add("walked", m); }
        }

        public static void DayDone(PortGame g)
        {
            Add("days"); Add("earned", g.Earned.Value); Max("bestDay", g.Earned.Value);
            Add("sold", g.Sold.Value); Add("spoiled", g.SpoiledSold.Value); Add("sea", g.InSea.Value); Add("left", g.LeftAboard.Value);
            PlayerPrefs.Save();
        }

        public static string Show(string key, string unit, int v) =>
            unit == "$" ? "$" + v.ToString("N0") : unit == "km" ? (v / 1000f).ToString("0.0") + " km" : v.ToString("N0");
    }
}
