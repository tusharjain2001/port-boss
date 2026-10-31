using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PortBoss.M1
{
    // Pure data. The roster is plan/gdd/fishing/fish-roster.csv (Manan's final 15 fish, 3 per rod, 31 Oct 2026),
    // copied to Assets/Game/Resources/Data/fish-roster.csv by the build: change the sheet, rebuild, done.
    // Rods (gdd fishing §6.2) and baits (DISCUSS §B) are tables below, same idea.
    public enum FishClass { Everyday, Uncommon, Scarce, Trophy, Legend }

    public class FishSpecies
    {
        public string Id, Name, Notes, FavTime, FavWeather;
        public FishClass Class;
        public int RodTier;
        public string[] Baits;          // first = favourite
        public float Price, SizeMin, SizeMax, WeightMin, WeightMax;
    }

    public class Rod
    {
        public string Name; public int Tier; public float Price;
        public float GreenMul, SpeedMul, BiteLo, BiteHi, RangeMin, RangeMax, LineKg;
    }

    public class Bait { public string Id, Name; public float PricePer10; }

    public static class FishDb
    {
        public static readonly List<FishSpecies> All = new();

        // gdd fishing §6.2 (Kufu review 31 Oct). LineKg pairs a line with each rod for the debug rod key (M1 has no
        // tackle shop yet; §6.4 starter line is 8 kg).
        public static readonly Rod[] Rods =
        {
            new() { Name = "Walt's Bamboo Rod", Tier = 1, Price = 0,     GreenMul = 1.00f, SpeedMul = 1.00f, BiteLo = 4, BiteHi = 10, RangeMin = 6,  RangeMax = 16, LineKg = 8 },
            new() { Name = "Fibreglass Rod",    Tier = 2, Price = 450,   GreenMul = 1.15f, SpeedMul = 0.95f, BiteLo = 4, BiteHi = 9,  RangeMin = 7,  RangeMax = 20, LineKg = 15 },
            new() { Name = "Carbon Rod",        Tier = 3, Price = 1600,  GreenMul = 1.30f, SpeedMul = 0.90f, BiteLo = 4, BiteHi = 8,  RangeMin = 8,  RangeMax = 24, LineKg = 40 },
            new() { Name = "Deep-sea Rod",      Tier = 4, Price = 6000,  GreenMul = 1.40f, SpeedMul = 0.85f, BiteLo = 4, BiteHi = 7,  RangeMin = 10, RangeMax = 28, LineKg = 120 },
            new() { Name = "Old Faithful",      Tier = 5, Price = 15000, GreenMul = 1.50f, SpeedMul = 0.80f, BiteLo = 4, BiteHi = 6,  RangeMin = 8,  RangeMax = 30, LineKg = 300 },
        };

        // DISCUSS §B: one new bait per rod tier; Walt's Mix is the best (and the only bait Old Brass takes).
        public static readonly Bait[] Baits =
        {
            new() { Id = "Worm", Name = "Worms", PricePer10 = 5 }, new() { Id = "Shrimp", Name = "Shrimp", PricePer10 = 12 },
            new() { Id = "Sardine", Name = "Sardine", PricePer10 = 18 }, new() { Id = "Squid", Name = "Squid strip", PricePer10 = 25 },
            new() { Id = "WaltsMix", Name = "Walt's Mix", PricePer10 = 0 },
        };

        // Minigame by class (gdd fishing §3.7, starter gear): green zone fraction, pointer speed (bar widths / s)
        public static readonly float[] Green = { 0.19f, 0.18f, 0.17f, 0.15f, 0.13f };
        public static readonly float[] Speed = { 0.90f, 1.00f, 1.10f, 1.25f, 1.40f };
        public static readonly int[] NibbleLo = { 0, 1, 1, 2, 3 }, NibbleHi = { 1, 2, 3, 3, 4 };
        public static readonly string[] Hint = { "Something small…", "Fighting!", "Strong one!", "Something heavy…", "…!" };

        public static void Load(string csv)
        {
            All.Clear();
            // '#' lines are notes in the sheet (Manan's rules), not data
            var lines = Array.FindAll(csv.TrimStart('﻿').Replace("\r", "").Split('\n'), l => !l.TrimStart().StartsWith("#") && l.Trim().Length > 0);
            var head = System.Array.ConvertAll(Split(lines[0]), h => h.Trim());
            int C(string n) => Array.IndexOf(head, n);
            int iId = C("id"), iName = C("name"), iClass = C("class"), iRod = C("rod_tier"), iT = C("fav_time"), iW = C("fav_weather"), iB = C("baits"),
                iP = C("base_price"), iS0 = C("size_min_cm"), iS1 = C("size_max_cm"), iW0 = C("weight_min_kg"), iW1 = C("weight_max_kg"), iN = C("notes");
            for (int l = 1; l < lines.Length; l++)
            {
                if (string.IsNullOrWhiteSpace(lines[l])) continue;
                var f = Split(lines[l]);
                float F(int i) => float.Parse(f[i], CultureInfo.InvariantCulture);
                All.Add(new FishSpecies
                {
                    Id = f[iId], Name = f[iName], Class = (FishClass)Enum.Parse(typeof(FishClass), f[iClass]), RodTier = int.Parse(f[iRod]),
                    FavTime = f[iT], FavWeather = f[iW], Baits = f[iB].Split('|'), Price = F(iP), SizeMin = F(iS0), SizeMax = F(iS1),
                    WeightMin = F(iW0), WeightMax = F(iW1), Notes = iN >= 0 && iN < f.Length ? f[iN] : "",
                });
            }
        }

        static string[] Split(string line)
        {
            var r = new List<string>(); var cur = new System.Text.StringBuilder(); bool q = false;
            foreach (var ch in line)
            {
                if (ch == '"') q = !q;
                else if (ch == ',' && !q) { r.Add(cur.ToString()); cur.Clear(); }
                else cur.Append(ch);
            }
            r.Add(cur.ToString());
            return r.ToArray();
        }

        public static FishSpecies Get(string id) => All.Find(s => s.Id == id);

        // Which fish bites (DISCUSS §B, final): the rod decides who CAN bite (its tier and below); the bait who usually
        // does (favourite x4, liked x2, other x0.5; "None" = a bare hook); time/weather nudge x1.5, never a lock. Old Brass
        // (Legend) only ever bites on Old Faithful (tier 5) with Walt's Mix.
        public static float Weight(FishSpecies s, int rodTier, string bait, string time = "Day", string weather = "Clear")
        {
            if (s.RodTier > rodTier) return 0;
            if (s.Class == FishClass.Legend && (rodTier < 5 || bait != "WaltsMix")) return 0;
            int k = Array.IndexOf(s.Baits, bait);
            float w = k == 0 ? 4f : k > 0 ? 2f : 0.5f;
            if (bait == "WaltsMix" && k < 0 && s.Class <= FishClass.Scarce) w = 0.15f; // small fish ignore Walt's Mix
            if (s.FavTime == time) w *= 1.5f;
            if (s.FavWeather == weather) w *= 1.5f;
            return w;
        }

        public static FishSpecies Roll(System.Random r, int rodTier, string bait, string time = "Day", string weather = "Clear")
        {
            float sum = 0;
            foreach (var s in All) sum += Weight(s, rodTier, bait, time, weather);
            double u = r.NextDouble() * sum;
            foreach (var s in All) { u -= Weight(s, rodTier, bait, time, weather); if (u <= 0) return s; }
            return All[0];
        }

        // Size: skewed toward the small end (big ones are the story); weight follows length cubed between the bounds.
        public static void RollSize(System.Random r, FishSpecies s, out float cm, out float kg)
        {
            float u = (float)Math.Pow(r.NextDouble(), 1.6);
            cm = Mathf.Lerp(s.SizeMin, s.SizeMax, u);
            float lu = Mathf.InverseLerp(Mathf.Pow(s.SizeMin, 3), Mathf.Pow(s.SizeMax, 3), Mathf.Pow(cm, 3));
            kg = Mathf.Lerp(s.WeightMin, s.WeightMax, lu) * (0.92f + 0.16f * (float)r.NextDouble());
            kg = Mathf.Clamp(kg, s.WeightMin, s.WeightMax);
        }
    }
}
