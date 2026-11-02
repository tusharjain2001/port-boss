using System.Collections.Generic;

namespace PortBoss.M1
{
    // Co-op: the host's book of every fish (gdd systems §1.1.7-8, economy §10 anti-dupe). Each fish has an id and is in
    // exactly one place: a player's hands (rolled at the cast, landed, not yet kept), a player's hotbar, on a deck or a
    // pier, or in the air (tossed). Every change is one call here that happens whole or not at all: a request that no
    // longer fits the book (that fish already given, the hotbar filled meanwhile, the fish caught by someone else)
    // changes nothing, and a request seen before (its transaction id) is refused. Pure logic: SelfTest runs random
    // transfers, replays and races, and checks after each one that no fish was ever made or lost.
    public class Ledger
    {
        public enum Kind : byte { Hands, Bar, Deck, Air }
        public struct Place { public Kind K; public ulong Who; }
        public readonly Dictionary<ulong, Hotbar> Bars = new();
        public readonly Dictionary<ulong, Fish> All = new();
        public readonly Dictionary<ulong, Place> Where = new();
        public int Made, Freed;                                   // fish ever made; fish gone back to the sea
        ulong next = 1;
        readonly HashSet<(ulong, uint)> done = new();             // (player, transaction id) already applied

        public Hotbar Bar(ulong who) { if (!Bars.TryGetValue(who, out var b)) Bars[who] = b = new Hotbar(); return b; }
        // a request's transaction id: true the first time only
        public bool Fresh(ulong who, uint tx) => done.Add((who, tx));

        public Fish Make(ulong who, FishSpecies sp, int grade, float cm, float kg, float t)
        {
            var f = new Fish { Id = next++, Species = sp, Grade = grade, Cm = cm, Kg = kg, CaughtAt = t };
            All[f.Id] = f; Where[f.Id] = new Place { K = Kind.Hands, Who = who }; Made++;
            return f;
        }
        public bool Is(ulong id, Kind k, ulong who) => Where.TryGetValue(id, out var p) && p.K == k && (k == Kind.Deck || p.Who == who);
        public bool Held(ulong id, ulong who) => Is(id, Kind.Hands, who) || Is(id, Kind.Bar, who);
        void Lift(ulong id) { var p = Where[id]; if (p.K == Kind.Bar) Bars[p.Who].Remove(All[id]); }
        void Gone(ulong id) { Where.Remove(id); All.Remove(id); Freed++; }
        void To(ulong id, Kind k, ulong who = 0) => Where[id] = new Place { K = k, Who = who };

        // the catch into the hotbar (false: full, so it stays in the hands)
        public bool Keep(ulong who, ulong id) { if (!Is(id, Kind.Hands, who) || Bar(who).Add(All[id]) < 0) return false; To(id, Kind.Bar, who); return true; }
        // let go, dropped over the side, or a cast's fish that never came up
        public bool Free(ulong who, ulong id) { if (!Held(id, who)) return false; Lift(id); Gone(id); return true; }
        // a player came back with a new connection: everything booked to the old one is theirs again
        public void Rekey(ulong from, ulong to)
        {
            if (from == to) return;
            if (Bars.TryGetValue(from, out var b)) { Bars.Remove(from); Bars[to] = b; }
            foreach (var id in new List<ulong>(Where.Keys)) { var p = Where[id]; if (p.K != Kind.Deck && p.Who == from) Where[id] = new Place { K = p.K, Who = to }; }
        }

        // a new cast: the last cast's fish still in the hands never came up (it swam off, snapped, was let go)
        public void FreeHands(ulong who)
        {
            var l = new List<ulong>(); foreach (var kv in Where) if (kv.Value.K == Kind.Hands && kv.Value.Who == who) l.Add(kv.Key);
            foreach (var id in l) Free(who, id);
        }
        public bool Drop(ulong who, ulong id) { if (!Held(id, who)) return false; Lift(id); To(id, Kind.Deck); return true; }
        public bool Pickup(ulong who, ulong id) { if (!Is(id, Kind.Deck, 0) || Bar(who).Add(All[id]) < 0) return false; To(id, Kind.Bar, who); return true; }
        public bool Expire(ulong id) { if (!Is(id, Kind.Deck, 0)) return false; Gone(id); return true; } // slipped off the deck
        // one fish or a stack, all or nothing (no room for them all: nothing moves)
        public bool Give(ulong from, ulong to, IList<ulong> ids)
        {
            if (from == to || ids.Count == 0) return false;
            var fs = new List<Fish>();
            foreach (var id in ids) { if (!Is(id, Kind.Bar, from) || fs.Contains(All[id])) return false; fs.Add(All[id]); }
            if (!Bar(to).CanFit(fs)) return false;
            foreach (var f in fs) { Bars[from].Remove(f); Bars[to].Add(f); To(f.Id, Kind.Bar, to); }
            return true;
        }
        public bool Toss(ulong who, ulong id) { if (!Is(id, Kind.Bar, who)) return false; Lift(id); To(id, Kind.Air, who); return true; }
        public bool InAir(ulong id) => Where.TryGetValue(id, out var p) && p.K == Kind.Air;
        public bool Catch(ulong id, ulong who) { if (!InAir(id) || Bar(who).Add(All[id]) < 0) return false; To(id, Kind.Bar, who); return true; }
        public bool Land(ulong id, bool sea) { if (!InAir(id)) return false; if (sea) Gone(id); else To(id, Kind.Deck); return true; }

        // Every fish in exactly one place, every hotbar fish booked to that hotbar, nothing made or lost: "" when so.
        public string Check()
        {
            if (All.Count != Where.Count) return $"{All.Count} fish but {Where.Count} places";
            if (All.Count != Made - Freed) return $"{All.Count} fish, but {Made} made - {Freed} freed";
            var seen = new HashSet<Fish>(); int inBars = 0;
            foreach (var kv in Bars)
                foreach (var s in kv.Value.S)
                    foreach (var f in s.Fish)
                    {
                        inBars++;
                        if (!seen.Add(f)) return $"fish {f.Id} twice in the hotbars";
                        if (!All.TryGetValue(f.Id, out var g) || g != f) return $"fish {f.Id} in {kv.Key}'s hotbar isn't in the book";
                        if (!Is(f.Id, Kind.Bar, kv.Key)) return $"fish {f.Id} in {kv.Key}'s hotbar is booked to {Where[f.Id].K} {Where[f.Id].Who}";
                    }
            int booked = 0; foreach (var p in Where.Values) if (p.K == Kind.Bar) booked++;
            if (booked != inBars) return $"{booked} fish booked to hotbars, {inBars} in them";
            return "";
        }

        // n random transfers among 4 players (catches, keeps, gives of one or a stack, tosses caught or missed, drops,
        // pickups, frees, expiries), a quarter of them replays of an old request or races for a fish someone else just
        // moved; checked after every step.
        public static string SelfTest(int n, int seed = 1)
        {
            var r = new System.Random(seed); var l = new Ledger();
            FishSpecies Sp(string id) => new() { Id = id, Name = id, Price = 10 };
            var sp = new[] { Sp("a"), Sp("b"), Sp("c") };
            ulong[] who = { 1, 2, 3, 4 }; foreach (var w in who) l.Bar(w);
            var hist = new List<(ulong w, uint tx)>(); uint tx = 0; int ok = 0, refused = 0;
            ulong P() => who[r.Next(who.Length)];
            ulong AnyFish(System.Func<KeyValuePair<ulong, Place>, bool> f) { var c = new List<ulong>(); foreach (var kv in l.Where) if (f(kv)) c.Add(kv.Key); return c.Count > 0 ? c[r.Next(c.Count)] : 0; }
            for (int i = 0; i < n; i++)
            {
                ulong w = P(); bool applied;
                if (hist.Count > 0 && r.NextDouble() < 0.1) { var h = hist[r.Next(hist.Count)]; if (l.Fresh(h.w, h.tx)) return $"step {i}: replayed transaction {h.tx} accepted"; refused++; continue; }
                tx++; l.Fresh(w, tx); hist.Add((w, tx));
                // a random fish of anyone's (races: often not the asker's, or already moved)
                ulong any = AnyFish(_ => true), mine = AnyFish(kv => kv.Value.Who == w && (kv.Value.K == Kind.Bar || kv.Value.K == Kind.Hands));
                ulong id = r.NextDouble() < 0.7 ? mine : any;
                switch (r.Next(9))
                {
                    case 0: l.Make(w, sp[r.Next(3)], r.Next(2), 30, 0.4f, i); applied = true; break;
                    case 1: applied = l.Keep(w, id); break;
                    case 2: applied = l.Free(w, id); break;
                    case 3: applied = l.Drop(w, id); break;
                    case 4: applied = l.Pickup(w, AnyFish(kv => kv.Value.K == Kind.Deck)); break;
                    case 5:
                    {
                        // one fish, or the whole stack it's in
                        var ids = new List<ulong> { id };
                        if (id != 0 && r.NextDouble() < 0.4 && l.All.TryGetValue(id, out var f)) foreach (var s in l.Bar(w).S) if (s.Fish.Contains(f)) { ids.Clear(); foreach (var g in s.Fish) ids.Add(g.Id); }
                        applied = l.Give(w, P(), ids); break;
                    }
                    case 6: applied = l.Toss(w, id); break;
                    case 7: { ulong a = AnyFish(kv => kv.Value.K == Kind.Air); applied = r.NextDouble() < 0.5 ? l.Catch(a, P()) : l.Land(a, r.NextDouble() < 0.5); break; }
                    default: applied = l.Expire(AnyFish(kv => kv.Value.K == Kind.Deck)); break;
                }
                if (applied) ok++; else refused++;
                var c = l.Check(); if (c != "") return $"step {i}: {c}";
            }
            return $"PASS ({n} transfers: {ok} applied, {refused} refused; {l.Made} fish made, {l.Freed} back to the sea, {l.All.Count} held)";
        }
    }
}
