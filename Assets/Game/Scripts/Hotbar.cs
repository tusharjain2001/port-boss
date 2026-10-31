using System.Collections.Generic;

namespace PortBoss.M1
{
    // One caught fish: its own size, weight and catch time (the rot clock, M3) even inside a stack.
    public class Fish
    {
        public FishSpecies Species; public int Grade; public float Cm, Kg, CaughtAt;
        public float Value => Species.Price * Ponyo.GradeMul[Grade];
    }

    // The 8-slot hotbar (gdd systems §1.1): the rod is a normal item; fish stack to 5 by species + grade; a new fish
    // goes onto a matching stack, else the first empty slot left to right. Pure logic: SelfTest() checks it.
    public class Hotbar
    {
        public const int Slots = 8, Stack = 5;
        public class Slot { public bool Rod; public List<Fish> Fish = new(); public bool Empty => !Rod && Fish.Count == 0; }
        public readonly Slot[] S = new Slot[Slots];
        public int Selected;

        public Hotbar() { for (int i = 0; i < Slots; i++) S[i] = new Slot(); S[0].Rod = true; }

        public Slot Sel => S[Selected];

        // Where this fish would go (-1 = full).
        public int Target(Fish f)
        {
            for (int i = 0; i < Slots; i++)
                if (S[i].Fish.Count > 0 && S[i].Fish.Count < Stack && S[i].Fish[0].Species == f.Species && S[i].Fish[0].Grade == f.Grade) return i;
            for (int i = 0; i < Slots; i++) if (S[i].Empty) return i;
            return -1;
        }

        public int Add(Fish f)
        {
            int i = Target(f);
            if (i >= 0) S[i].Fish.Add(f);
            return i;
        }

        public Fish TakeOne(int i)
        {
            var s = S[i];
            if (s.Fish.Count == 0) return null;
            var f = s.Fish[0]; s.Fish.RemoveAt(0); // oldest first
            return f;
        }

        public bool Full => System.Array.TrueForAll(S, s => !s.Empty) && !System.Array.Exists(S, s => s.Fish.Count > 0 && s.Fish.Count < Stack);

        public int FishCount { get { int n = 0; foreach (var s in S) n += s.Fish.Count; return n; } }

        // Stacks by species + grade (Basic 0 / Rare 1), 5 to a stack, rod in slot 1. Logged at every start (Game).
        public static string SelfTest()
        {
            FishSpecies Sp(string id) => new() { Id = id, Name = id, Price = 10 };
            var a = Sp("a"); var b = Sp("b"); var c = Sp("c");
            var h = new Hotbar();
            if (h.Add(new Fish { Species = a, Grade = 0 }) != 1) return "first fish not in slot 2";
            for (int i = 0; i < 4; i++) h.Add(new Fish { Species = a, Grade = 0 });
            if (h.S[1].Fish.Count != 5) return "stack of 5 not filled";
            if (h.Add(new Fish { Species = a, Grade = 0 }) != 2) return "6th same fish did not start a new stack";
            if (h.Add(new Fish { Species = a, Grade = 1 }) != 3) return "Rare stacked on Basic";
            if (h.Add(new Fish { Species = b, Grade = 0 }) != 4) return "other species stacked";
            h.Add(new Fish { Species = b, Grade = 1 }); h.Add(new Fish { Species = c, Grade = 0 }); h.Add(new Fish { Species = c, Grade = 1 });
            if (h.Add(new Fish { Species = a, Grade = 0 }) != 2) return "did not top up the partial stack";
            foreach (var (sp, g, n) in new[] { (a, 0, 3), (a, 1, 4), (b, 0, 4), (b, 1, 4), (c, 0, 4), (c, 1, 4) })
                for (int i = 0; i < n; i++) h.Add(new Fish { Species = sp, Grade = g });
            if (!h.Full) return "not full with 7 full stacks + rod";
            if (h.Add(new Fish { Species = b, Grade = 1 }) != -1) return "accepted a fish when full";
            var first = h.S[1].Fish[0];
            if (h.TakeOne(1) != first) return "TakeOne not oldest first";
            if (h.Full) return "still full after taking one";
            if (System.Math.Abs(new Fish { Species = a, Grade = 1 }.Value - 2 * new Fish { Species = a, Grade = 0 }.Value) > 1e-4) return "Rare is not x2 Basic";
            return "PASS";
        }
    }
}
