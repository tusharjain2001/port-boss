using System;
using System.Collections.Generic;

namespace PortBoss.M1
{
    // M8 step 1, tackle (fishing §6.3–6.5, F10): reels, lines, hooks and the Sight float at Hook, Line & Sinker.
    // Reels and lines only ever get better, so the best one owned is always on every rod. The hook is the one real choice
    // (fishing §6.5: a forgiving strike or a forgiving fight), picked at June's counter. A rod comes strung with its own line
    // (FishDb.Rod.LineKg: the bamboo's frayed mono 8 kg ... Old Faithful's Walt's Line), so a bought line only helps a rod
    // strung lighter. One stop (rule 29): a hook's retries redo a missed stop (Barbed once, Treble twice); a heavy fish
    // keeps its own retry on top; never more than 5 stops (the host's check).
    // Items on June's shelf: "reel:<1..3>", "line:<1..3>", "hook:<1..3>", "float".
    public static class Tackle
    {
        public class Piece { public string Name, Short, What; public int Price; public float Strike, Kg; public int Retries; }
        public static readonly Piece[] Reels =
        {
            new() { Name = "Rusty Reel", Short = "Rusty reel", What = "Walt's. Squeaks." },
            new() { Name = "Click Reel", Short = "Click reel", Price = 120, Strike = 0.10f, What = "Strike window +0.10 s" },
            new() { Name = "Smooth Spinning Reel", Short = "Smooth reel", Price = 500, Strike = 0.15f, What = "Strike window +0.15 s" },
            new() { Name = "Twin-drag Reel", Short = "Twin-drag reel", Price = 1800, Strike = 0.20f, What = "Strike window +0.20 s" },
        };
        public static readonly Piece[] Lines =
        {
            new() { Name = "Frayed mono", Short = "Frayed mono 8 kg", Kg = 8, What = "Holds 8 kg" },
            new() { Name = "Mono 15", Short = "Mono 15 kg", Price = 40, Kg = 15, What = "Holds 15 kg: fewer HEAVY fish" },
            new() { Name = "Braid 40", Short = "Braid 40 kg", Price = 250, Kg = 40, What = "Holds 40 kg" },
            new() { Name = "Braid 120", Short = "Braid 120 kg", Price = 900, Kg = 120, What = "Holds 120 kg" },
            new() { Name = "Walt's Line", Short = "Walt's Line 300 kg", Price = -1, Kg = 300, What = "Comes with Old Faithful" },
        };
        public static readonly Piece[] Hooks =
        {
            new() { Name = "Bent hook", Short = "Bent hook", What = "It's a hook." },
            new() { Name = "Circle hook", Short = "Circle hook", Price = 40, Strike = 0.15f, What = "Strike window +0.15 s" },
            new() { Name = "Barbed hook", Short = "Barbed", Price = 250, Retries = 1, What = "A missed stop is redone once" },
            new() { Name = "Treble hook", Short = "Treble", Price = 1200, Retries = 2, What = "A missed stop is redone twice" },
        };
        public const int FloatPrice = 30, MaxStops = 5;

        public static float StrikeOf(Kit k) => Reels[Math.Clamp(k.Reel, 0, 3)].Strike + Hooks[Math.Clamp(k.Hook, 0, 3)].Strike;
        public static int RetriesOf(Kit k) => Hooks[Math.Clamp(k.Hook, 0, 3)].Retries;
        public static int LineOf(Kit k, int rodTier) => Math.Clamp(Math.Max(rodTier - 1, k.Line), 0, Lines.Length - 1);   // a rod comes strung with its own line
        public static float LineKg(Kit k, int rodTier) => Lines[LineOf(k, rodTier)].Kg;
        public static string Label(Kit k, int rodTier) => $"{Reels[Math.Clamp(k.Reel, 0, 3)].Short} · {Lines[LineOf(k, rodTier)].Short} · {Hooks[Math.Clamp(k.Hook, 0, 3)].Short}";
        public static bool Owns(Kit k, int hook) => hook == 0 || (k.Hooks >> hook & 1) != 0;

        public static bool Is(string item) => item.StartsWith("reel:") || item.StartsWith("line:") || item.StartsWith("hook:") || item == "float";
        static int N(string item) => int.Parse(item.Substring(5));
        static Piece[] Of(string item) => item.StartsWith("reel:") ? Reels : item.StartsWith("line:") ? Lines : Hooks;
        static bool Valid(string item) => item == "float" || int.TryParse(item.Substring(5), out int n) && n >= 1 && n <= 3;
        public static int Price(string item) => !Valid(item) ? -1 : item == "float" ? FloatPrice : Of(item)[N(item)].Price;
        public static string Name(string item) => !Valid(item) ? item : item == "float" ? "Sight float" : Of(item)[N(item)].Name;
        // why this can't be bought ("" = it can)
        public static string WhyNot(Kit k, string item)
        {
            if (!Valid(item)) return "Not for sale.";
            if (item == "float") return k.Sight ? "You own it." : "";
            int n = N(item);
            if (item.StartsWith("reel:")) return k.Reel == n ? "You own it." : k.Reel > n ? "You've a better one." : "";
            if (item.StartsWith("line:")) { int have = Math.Max(k.Rod - 1, k.Line); return k.Line == n ? "You own it." : have >= n ? "Your rods hold as much." : ""; }
            return Owns(k, n) ? "You own it." : "";
        }
        // the goods into a kit: a reel or a line is on every rod at once; a new hook goes on
        public static void Give(Kit k, string item)
        {
            if (!Valid(item)) return;
            if (item == "float") { k.Sight = true; return; }
            int n = N(item);
            if (item.StartsWith("reel:")) k.Reel = Math.Max(k.Reel, n);
            else if (item.StartsWith("line:")) k.Line = Math.Max(k.Line, n);
            else { k.Hooks |= 1 << n; k.Hook = n; }
        }
        // the hook on the rods (a kit choice, like the bait on the hook: the angler's own machine decides it)
        public static bool UseHook(Kit k, int hook) { if (hook < 0 || hook > 3 || !Owns(k, hook)) return false; k.Hook = hook; return true; }
    }

    public partial class Company
    {
        // companycheck: June's tackle shelf, the hook choice, the numbers the fishing reads, the save and an old save
        public static string TackleTest()
        {
            var c = new Company(); c.Earn("e", "Pip", "x", 5000, 600); var k = c.KitOf("solo");
            if (Tackle.Label(k, 1) != "Rusty reel · Frayed mono 8 kg · Bent hook") return "a new kit: " + Tackle.Label(k, 1);
            if (Tackle.StrikeOf(k) != 0 || Tackle.RetriesOf(k) != 0 || Tackle.LineKg(k, 1) != 8) return "a new kit's numbers";
            if (c.PriceOf("reel:2", 1) != 500 || c.PriceOf("hook:3", 1) != 1200 || c.PriceOf("float", 1) != 30 || c.PriceOf("reel:4", 1) != -1 || c.PriceOf("line:4", 1) != -1) return "the prices";
            if (!c.Buy("t1", "solo", "Pip", "reel:2", 1, 600, out var why)) return "the Smooth reel: " + why;
            if (k.Reel != 2 || c.Cash != 4540) return "the Smooth reel isn't on, or cost wrong";
            if (c.Buy("t2", "solo", "Pip", "reel:1", 1, 600, out why) || why != "You've a better one.") return "bought a worse reel: " + why;
            if (c.Buy("t3", "solo", "Pip", "reel:2", 1, 600, out why) || why != "You own it.") return "bought the same reel twice: " + why;
            if (!c.Buy("t4", "solo", "Pip", "line:1", 1, 600, out why) || Tackle.LineKg(k, 1) != 15) return "Mono 15 on the bamboo: " + why;
            if (Tackle.LineKg(k, 3) != 40) return "the Carbon rod is strung with Braid 40";
            if (!c.Buy("t5", "solo", "Pip", "rod:2", 1, 600, out why) || (k.Rods & 3) != 3) return "the Fibreglass rod (and the bamboo still owned): " + why;
            if (c.Buy("t6", "solo", "Pip", "line:1", 1, 600, out why) || why != "You own it.") return "Mono 15 again";
            if (!c.Buy("t7", "solo", "Pip", "hook:1", 1, 600, out why) || k.Hook != 1 || Math.Abs(Tackle.StrikeOf(k) - 0.30f) > 1e-4) return "Smooth + Circle = +0.30 s: " + why;
            if (!c.Buy("t8", "solo", "Pip", "hook:2", 1, 600, out why) || k.Hook != 2 || Tackle.RetriesOf(k) != 1 || Math.Abs(Tackle.StrikeOf(k) - 0.15f) > 1e-4) return "the Barbed hook goes on: one retry";
            if (!Tackle.UseHook(k, 1) || k.Hook != 1 || Tackle.UseHook(k, 3) || k.Hook != 1) return "the hook choice (owned only)";
            if (!c.Buy("t9", "solo", "Pip", "float", 1, 600, out _) || !k.Sight) return "the Sight float";
            if (Tackle.Label(k, 2) != "Smooth reel · Mono 15 kg · Circle hook") return "the label: " + Tackle.Label(k, 2);
            // the save, and an old (M7) kit: the rod it had and Walt's bamboo
            var w = new Json.W(); WriteKit(w, k); var back = ReadKit((Dictionary<string, object>)Json.Parse(w.ToString()));
            if (back.Reel != 2 || back.Line != 1 || back.Hooks != k.Hooks || back.Hook != 1 || !back.Sight || back.Rods != k.Rods) return "the kit through a save";
            var old = ReadKit((Dictionary<string, object>)Json.Parse("{\"rod\":3,\"baitI\":0,\"pouch5\":false,\"bait\":[1,0,0,0,0]}"));
            if (old.Rods != 5 || old.Reel != 0 || old.Hook != 0 || old.Hooks != 1) return "an M7 save's kit: rods " + old.Rods;
            if (c.Check() != "") return c.Check();
            return "";
        }
    }
}
