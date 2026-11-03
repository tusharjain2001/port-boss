using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace PortBoss.M1
{
    // M2 step 4: the hotbar host-side (gdd systems §1.1.3, §1.1.7-8; economy §10 anti-dupe). The host's Ledger books every
    // fish; a client asks for every change ("pb.inv": a transaction id, what, which fish) and the host answers with that
    // player's whole hotbar ("pb.bar"). A client's own hotbar is a copy: it may change at once (a catch kept, a fish
    // thrown) for the feel, and the host's answer puts it right. Deck fish, tosses in the air and who catches them are the
    // host's too, sent to everyone. Giving: hold E 0.4 s on a crewmate within 3 m (Shift: the stack); tossing: hold Q,
    // a gentle arc up to 6 m; a crewmate there facing it (25°) catches it if they have room, else it lands on the deck or
    // swims away.
    public partial class Coop
    {
        public readonly Ledger Book = new();                     // host
        public enum Op : byte { Keep, Free, Drop, Pickup, Give, Toss }
        struct Req { public uint Tx; public Op Op; public ulong Id, To; public Vector3 A, B; public ulong[] Ids; }
        uint txNext;
        Req lastReq;                                             // (tests: replayed to prove the host refuses it)
        public const float TossTime = 0.8f, TossMax = 6.5f, GiveRange = 3.5f;
        public int Applied, Refused, Replays;                    // host (tests)
        public bool LogPickups;                                  // (tests) say who got each deck fish, and why a toss wasn't caught
        public static int Sent;                                  // (tests) requests this machine has sent
        public readonly Dictionary<ulong, ulong> LastToss = new(); // (tests) each player's last fish tossed
        readonly List<(ulong id, ulong who, Vector3 from, Vector3 to, float at)> air = new(); // host: tosses in flight
        public readonly Dictionary<ulong, FishPickup> Deck = new(); // every machine: deck fish by id

        void RegisterItems(CustomMessagingManager m)
        {
            Reg(m, "pb.inv", OnInv); Reg(m, "pb.bar", OnBar);
            Reg(m, "pb.deck", OnDeck); Reg(m, "pb.toss", OnToss);
            Reg(m, "pb.land", OnLand); Reg(m, "pb.gave", OnGave);
            Reg(m, "pb.note", OnNote); Reg(m, "pb.audit", OnAudit);
        }

        static void WriteFish(ref FastBufferWriter w, Fish f)
        { w.WriteValueSafe(f.Id); w.WriteValueSafe((short)FishDb.All.IndexOf(f.Species)); w.WriteValueSafe((byte)f.Grade); w.WriteValueSafe(f.Cm); w.WriteValueSafe(f.Kg); w.WriteValueSafe(f.CaughtAt); }
        static Fish ReadFish(ref FastBufferReader r)
        {
            var f = new Fish(); r.ReadValueSafe(out f.Id); r.ReadValueSafe(out short sp); r.ReadValueSafe(out byte g); r.ReadValueSafe(out f.Cm); r.ReadValueSafe(out f.Kg); r.ReadValueSafe(out f.CaughtAt);
            f.Species = sp >= 0 && sp < FishDb.All.Count ? FishDb.All[sp] : FishDb.All[0]; f.Grade = g; return f;
        }

        // ---------------------------------------------------------------- asking (any machine; the host asks itself)
        static void Inv(Req q)
        {
            var c = I; q.Tx = ++c.txNext; c.lastReq = q; Sent++;
            if (IsHost) { c.Apply(c.nm.LocalClientId, q); return; }
            c.Send("pb.inv", w => WriteReq(ref w, q), NetworkDelivery.ReliableSequenced);
        }
        static void WriteReq(ref FastBufferWriter w, Req q)
        {
            w.WriteValueSafe(q.Tx); w.WriteValueSafe((byte)q.Op); w.WriteValueSafe(q.Id); w.WriteValueSafe(q.To); w.WriteValueSafe(q.A); w.WriteValueSafe(q.B);
            w.WriteValueSafe((byte)(q.Ids?.Length ?? 0)); if (q.Ids != null) foreach (var id in q.Ids) w.WriteValueSafe(id);
        }
        // (tests) the last request again, same transaction id: the host must refuse it
        public static void Replay()
        {
            var c = I; if (c == null || c.lastReq.Tx == 0) return; var q = c.lastReq; Sent++;
            if (IsHost) c.Apply(c.nm.LocalClientId, q); else c.Send("pb.inv", w => WriteReq(ref w, q), NetworkDelivery.ReliableSequenced);
        }

        // the catch into my hotbar: where it goes (the copy changes now; on the host the book does it)
        public static int KeepCatch(Fish f)
        {
            if (IsHost) { int s = I.Book.Bar(Me).Target(I.Book.All.TryGetValue(f.Id, out var g) ? g : f); Inv(new Req { Op = Op.Keep, Id = f.Id }); return s; }
            int slot = Angler.I.Bar.Add(f); Inv(new Req { Op = Op.Keep, Id = f.Id }); return slot;
        }
        public static void FreeFish(Fish f) { if (!IsHost) Angler.I.Bar.Remove(f); Inv(new Req { Op = Op.Free, Id = f.Id }); }
        public static void DropFish(Fish f, Vector3 at) { if (!IsHost) Angler.I.Bar.Remove(f); Inv(new Req { Op = Op.Drop, Id = f.Id, A = at }); }
        public static void Pickup(ulong id) => Inv(new Req { Op = Op.Pickup, Id = id });
        public static void Give(ulong to, List<Fish> fs) => Inv(new Req { Op = Op.Give, To = to, Ids = fs.ConvertAll(f => f.Id).ToArray() });
        public static void Toss(Fish f, Vector3 from, Vector3 to)
        {
            if (!IsHost) Angler.I.Bar.Remove(f);
            FishFlight.Launch(f, from, to, TossTime, f.Id); // mine flies at once; the host's word on where it ends comes after
            Inv(new Req { Op = Op.Toss, Id = f.Id, A = from, B = to });
        }

        void OnInv(ulong from, FastBufferReader r)
        {
            var q = new Req(); r.ReadValueSafe(out q.Tx); r.ReadValueSafe(out byte op); q.Op = (Op)op; r.ReadValueSafe(out q.Id); r.ReadValueSafe(out q.To);
            r.ReadValueSafe(out q.A); r.ReadValueSafe(out q.B); r.ReadValueSafe(out byte n); q.Ids = new ulong[n]; for (int i = 0; i < n; i++) r.ReadValueSafe(out q.Ids[i]);
            Deliver(() => { if (nm.IsServer) Apply(from, q); }, true);
        }

        // ---------------------------------------------------------------- the host's book
        string Name(ulong id) => Find(id)?.Name ?? "a crewmate";
        Angler Body(ulong id) => id == nm.LocalClientId ? Angler.I : Mates.TryGetValue(id, out var m) ? m : null;

        void Apply(ulong from, Req q)
        {
            if (!Book.Fresh(from, q.Tx)) { Replays++; return; }
            bool ok = false; var fish = Book.All.TryGetValue(q.Id, out var ff) ? ff : null;
            switch (q.Op)
            {
                case Op.Keep:
                    ok = Book.Keep(from, q.Id);
                    if (!ok && Book.Is(q.Id, Ledger.Kind.Hands, from) && Body(from) is Angler kb) { ok = Book.Drop(from, q.Id); if (ok) DeckAdd(q.Id, kb.transform.TransformPoint(new Vector3(0, 0.15f, 0.8f))); } // (full meanwhile: onto the deck)
                    break;
                case Op.Free: ok = Book.Free(from, q.Id); break;
                case Op.Drop: if (ok = Book.Drop(from, q.Id)) DeckAdd(q.Id, q.A); break;
                case Op.Pickup:
                    ok = Book.Pickup(from, q.Id);
                    if (ok) DeckRemove(q.Id);
                    else Note(from, "Hotbar full", Book.Is(q.Id, Ledger.Kind.Deck, 0) ? "No room for it." : "Someone got there first.");
                    break;
                case Op.Give:
                {
                    var giver = Body(from); var taker = Body(q.To);
                    bool near = giver != null && taker != null && Vector3.Distance(giver.transform.position, taker.transform.position) <= GiveRange;
                    var f0 = q.Ids.Length > 0 && Book.All.TryGetValue(q.Ids[0], out var g0) ? g0 : null;
                    ok = near && Book.Give(from, q.To, q.Ids);
                    if (ok)
                    {
                        string what = q.Ids.Length == 1 ? "a " + f0.Species.Name : $"{q.Ids.Length} {f0.Species.Name}";
                        Note(from, "Crew", $"You gave {Name(q.To)} {what}."); Note(q.To, "Crew", $"{Name(from)} gave you {what}.");
                        Broadcast("pb.gave", w => { w.WriteValueSafe(from); w.WriteValueSafe(q.To); w.WriteValueSafe((short)FishDb.All.IndexOf(f0.Species)); w.WriteValueSafe((byte)q.Ids.Length); });
                        Gave(from, q.To, f0.Species, q.Ids.Length);
                        SendBar(q.To);
                    }
                    else if (near && f0 != null && Book.Is(f0.Id, Ledger.Kind.Bar, from))
                    { Note(from, "Hotbar full", $"{Name(q.To)}'s hotbar is full."); Note(q.To, "Hotbar full", $"{Name(from)} tried to give you a {f0.Species.Name}."); }
                    break;
                }
                case Op.Toss:
                    ok = Vector3.Distance(q.A, q.B) <= TossMax + 0.5f && Book.Toss(from, q.Id);
                    if (ok)
                    {
                        air.Add((q.Id, from, q.A, q.B, Now + TossTime));
                        Broadcast("pb.toss", w => { w.WriteValueSafe(q.Id); w.WriteValueSafe(from); WriteFish(ref w, fish); w.WriteValueSafe(q.A); w.WriteValueSafe(q.B); });
                        if (from != nm.LocalClientId) FishFlight.Launch(fish, q.A, q.B, TossTime, q.Id);
                    }
                    else Land(q.Id, 3, from); // refused: the thrower's flight goes away, the copy comes back
                    break;
            }
            if (ok) Applied++; else Refused++;
            if (LogPickups && q.Op == Op.Pickup) Log($"{Name(from)} picked up fish {q.Id}: {(ok ? "yes" : "no")}");
            SendBar(from);
        }

        // Tosses landing (host): a crewmate there facing it with room catches it; else the deck or the sea.
        void TickAir()
        {
            for (int i = air.Count - 1; i >= 0; i--)
            {
                var a = air[i]; if (Now < a.at) continue;
                air.RemoveAt(i);
                Angler best = null; ulong bestId = 0; float bd = 1.6f; var thrower = Body(a.who);
                foreach (var e in Roster)
                {
                    if (e.Id == a.who) continue; var b = Body(e.Id); if (b == null || b.Down || b.Swimming) continue;
                    var d = a.to - b.transform.position; float dy = Mathf.Abs(d.y - 1f); d.y = 0;
                    // facing the thrower (not their hand: on a short toss its swing out to the side is half the angle)
                    var look = (thrower != null ? thrower.transform.position : a.from) - b.transform.position; look.y = 0;
                    if (LogPickups && d.magnitude < 3) Log($"toss {a.id} lands {d.magnitude:F2} m from {Name(e.Id)} (dy {dy:F2}), who faces {Vector3.Angle(b.transform.forward, look):F0}° off it, room {Book.Bar(e.Id).CanFit(new[] { Book.All[a.id] })}");
                    if (d.magnitude < bd && dy < 1.8f && Vector3.Angle(b.transform.forward, look) <= 25f && Book.Bar(e.Id).CanFit(new[] { Book.All[a.id] })) { best = b; bestId = e.Id; bd = d.magnitude; }
                }
                var f = Book.All[a.id];
                if (best != null && Book.Catch(a.id, bestId))
                {
                    Land(a.id, 0, bestId);
                    Note(bestId, "Caught!", $"A {f.Species.Name} from {Name(a.who)}."); Note(a.who, "Crew", $"{Name(bestId)} caught it!");
                    SendBar(bestId);
                }
                else if (Ground(a.to, out var at))
                { Book.Land(a.id, false); Land(a.id, 1, 0); DeckAdd(a.id, at); }
                else { Book.Land(a.id, true); Land(a.id, 2, 0); Note(a.who, "It's free!", $"The {f.Species.Name} swam away."); }
            }
        }

        // a deck, a pier or the shore under that point (else it's the sea)
        public static bool Ground(Vector3 p, out Vector3 at)
        {
            at = p; float sea = Sea.Height(p); var boat = Skiff.I != null ? Skiff.I.transform : null;
            // Biscuit's own floor counts whatever the sea outside does: the sea is cut out inside her hull, and her low
            // floor sits near the waterline, so on a wave crest it read as "under the sea" and a fish tossed into her swam away
            foreach (var h in Physics.RaycastAll(p + Vector3.up * 2f, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore))
                if (h.collider.GetComponentInParent<Angler>() == null && h.collider.GetComponentInParent<FishPickup>() == null
                    && (h.point.y > sea - 0.2f || boat != null && h.collider.transform.IsChildOf(boat)) && (at == p || h.point.y > at.y)) at = h.point;
            return at != p;
        }

        // host: deck fish appear and go on every machine
        void DeckAdd(ulong id, Vector3 at)
        {
            var f = Book.All[id]; var s = Skiff.I; bool boat = s != null && s.Inside(at, 0.3f);
            var local = boat ? s.transform.InverseTransformPoint(at) : at;
            Broadcast("pb.deck", w => { w.WriteValueSafe(true); WriteFish(ref w, f); w.WriteValueSafe(boat); w.WriteValueSafe(local); });
            ShowDeck(f, boat, local);
        }
        void DeckRemove(ulong id)
        {
            Broadcast("pb.deck", w => { w.WriteValueSafe(false); w.WriteValueSafe(id); });
            if (Deck.TryGetValue(id, out var p) && p != null) Destroy(p.gameObject); Deck.Remove(id);
        }
        void ShowDeck(Fish f, bool boat, Vector3 local)
        {
            var s = Skiff.I; var at = boat && s != null ? s.transform.TransformPoint(local) : local;
            if (Deck.TryGetValue(f.Id, out var old) && old != null) Destroy(old.gameObject);
            Deck[f.Id] = FishPickup.Drop(f, at + Vector3.up * 0.05f, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0));
        }
        // host: a deck fish left too long slips back into the sea (FishPickup asks)
        public static void Expire(FishPickup p)
        {
            var c = I; if (!IsHost) return;
            if (c.Book.Expire(p.F.Id)) { c.DeckRemove(p.F.Id); Alerts.Push("Deck", p.F.Species.Name + " slipped back into the sea.", Alerts.Kind.Info); }
        }

        void OnDeck(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out bool add);
            if (add) { var f = ReadFish(ref r); r.ReadValueSafe(out bool boat); r.ReadValueSafe(out Vector3 local); Deliver(() => ShowDeck(f, boat, local), true); }
            else { r.ReadValueSafe(out ulong id); Deliver(() => { if (Deck.TryGetValue(id, out var p) && p != null) Destroy(p.gameObject); Deck.Remove(id); }, true); }
        }

        // ---------------------------------------------------------------- hotbars, tosses, gives, notes (clients)
        void SendBar(ulong who)
        {
            if (who == nm.LocalClientId || !Book.Bars.TryGetValue(who, out var b)) return;
            SendTo(who, "pb.bar", w =>
            {
                foreach (var s in b.S) { w.WriteValueSafe((byte)s.Fish.Count); foreach (var f in s.Fish) WriteFish(ref w, f); }
            }, NetworkDelivery.ReliableSequenced);
        }
        void OnBar(ulong from, FastBufferReader r)
        {
            var slots = new List<Fish>[Hotbar.Slots];
            for (int i = 0; i < Hotbar.Slots; i++) { r.ReadValueSafe(out byte n); slots[i] = new List<Fish>(); for (int k = 0; k < n; k++) slots[i].Add(ReadFish(ref r)); }
            Deliver(() => { var a = Angler.I; if (a == null) return; for (int i = 0; i < Hotbar.Slots; i++) { a.Bar.S[i].Fish.Clear(); a.Bar.S[i].Fish.AddRange(slots[i]); } }, true);
        }

        void OnToss(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out ulong id); r.ReadValueSafe(out ulong who); var f = ReadFish(ref r); r.ReadValueSafe(out Vector3 a); r.ReadValueSafe(out Vector3 b);
            Deliver(() => { LastToss[who] = id; if (!FishFlight.Live.ContainsKey(id)) FishFlight.Launch(f, a, b, TossTime, id); }, true);
        }
        // 0 caught (by who), 1 onto the deck, 2 into the sea, 3 refused
        void Land(ulong id, byte how, ulong who)
        {
            Broadcast("pb.land", w => { w.WriteValueSafe(id); w.WriteValueSafe(how); w.WriteValueSafe(who); });
            if (FishFlight.Live.TryGetValue(id, out var fl)) fl.End(how, Body(who));
        }
        void OnLand(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out ulong id); r.ReadValueSafe(out byte how); r.ReadValueSafe(out ulong who);
            Deliver(() => { if (FishFlight.Live.TryGetValue(id, out var fl)) fl.End(how, Body(who)); }, true);
        }

        void OnGave(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out ulong a); r.ReadValueSafe(out ulong b); r.ReadValueSafe(out short sp); r.ReadValueSafe(out byte n);
            Deliver(() => Gave(a, b, sp >= 0 && sp < FishDb.All.Count ? FishDb.All[sp] : FishDb.All[0], n), true);
        }
        // the fish handed over: a quick little arc from one to the other (a stack: the first of it)
        void Gave(ulong a, ulong b, FishSpecies sp, int n)
        {
            var ga = Body(a); var gb = Body(b); if (ga == null || gb == null) return;
            var fl = FishFlight.Launch(new Fish { Species = sp, Cm = sp.SizeMin }, ga.transform.TransformPoint(new Vector3(0, 1.1f, 0.3f) * ga.Size), gb.transform.TransformPoint(new Vector3(0, 1.1f, 0.2f) * gb.Size), 0.35f, 0, 0.35f);
            fl.End(0, gb);
        }

        void Note(ulong who, string title, string text)
        {
            if (who == nm.LocalClientId) { Alerts.Push(title, text, title == "Hotbar full" ? Alerts.Kind.Warning : Alerts.Kind.Crew); return; }
            if (Find(who) == null) return;
            SendTo(who, "pb.note", w => { w.WriteValueSafe(title); w.WriteValueSafe(text); }, NetworkDelivery.ReliableSequenced);
        }
        void OnNote(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out string t); r.ReadValueSafe(out string x);
            Deliver(() => Alerts.Push(t, x, t == "Hotbar full" ? Alerts.Kind.Warning : Alerts.Kind.Crew), true);
        }

        void Broadcast(string msg, Action<FastBufferWriter> fill)
        {
            foreach (var id in nm.ConnectedClientsIds) if (id != nm.LocalClientId) SendTo(id, msg, fill, NetworkDelivery.ReliableSequenced);
        }

        // ---------------------------------------------------------------- tests
        // (host) n fish of one of the starter rod's species into each player's hotbar (one kind each: they stack), as if
        // caught; only to those holding fewer than `below`
        public void Grant(int n, int below = int.MaxValue)
        {
            foreach (var e in Roster)
            {
                if (Book.Bar(e.Id).FishCount >= below) continue;
                var sp = FishDb.All[Fishing.Rng.Next(Mathf.Min(3, FishDb.All.Count))]; int g = Fishing.Rng.Next(2);
                for (int i = 0; i < n; i++) { var f = Book.Make(e.Id, sp, g, 30, 0.4f, (float)Sky.Minutes); if (!Book.Keep(e.Id, f.Id)) Book.Free(e.Id, f.Id); }
                SendBar(e.Id);
            }
        }
        // (host) every hotbar's fish, so each machine can check its copy is the host's
        static ulong Sum(Hotbar b) { ulong h = 0; int n = 0; foreach (var s in b.S) foreach (var f in s.Fish) { h += f.Id * 0x9E3779B97F4A7C15UL; n++; } return h ^ (ulong)n; }
        public void Audit()
        {
            Broadcast("pb.audit", w => { w.WriteValueSafe((byte)Roster.Count); foreach (var e in Roster) { var b = Book.Bar(e.Id); w.WriteValueSafe(e.Id); w.WriteValueSafe(Sum(b)); w.WriteValueSafe((byte)b.FishCount); } });
        }
        public static string AuditResult;                       // (tests) a client's verdict on its copy
        void OnAudit(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out byte n); ulong mine = 0; int count = -1;
            for (int i = 0; i < n; i++) { r.ReadValueSafe(out ulong id); r.ReadValueSafe(out ulong h); r.ReadValueSafe(out byte c); if (id == nm.LocalClientId) { mine = h; count = c; } }
            Deliver(() =>
            {
                var b = Angler.I.Bar;
                AuditResult = Sum(b) == mine && b.FishCount == count ? $"ok ({count} fish, the same ids as the host's book)" : $"MISMATCH (mine {b.FishCount} fish, the host's book {count})";
                Log("audit: my hotbar against the host's book: " + AuditResult);
            }, true);
        }
    }

    // A fish in the air: a toss (its end is the host's word: caught, the deck, the sea), or a fish handed over.
    public class FishFlight : MonoBehaviour
    {
        public static readonly Dictionary<ulong, FishFlight> Live = new();
        ulong id; Vector3 from, to; float t0, dur, arc; int how = -1; Angler catcher; float endT = -1; Vector3 endFrom;
        public System.Action Landed;                              // solo: no host to say, it decides at the end

        public static FishFlight Launch(Fish f, Vector3 from, Vector3 to, float dur, ulong id, float arc = 1.6f)
        {
            var g = FishView.Make(f, null); g.transform.position = from;
            var fl = g.AddComponent<FishFlight>(); fl.id = id; fl.from = from; fl.to = to; fl.t0 = Time.time; fl.dur = dur; fl.arc = arc;
            if (id != 0) { if (Live.TryGetValue(id, out var old) && old != null) Destroy(old.gameObject); Live[id] = fl; }
            Snd.Play(Synth.Whoosh, from, 0.35f, 1.4f);
            return fl;
        }
        public void End(int how, Angler who) { this.how = how; catcher = who; }
        void OnDestroy() { if (id != 0 && Live.TryGetValue(id, out var f) && f == this) Live.Remove(id); }

        void Update()
        {
            float u = (Time.time - t0) / dur;
            if (u < 1 || how < 0 && Landed == null)
            {
                // the arc (then, its end still unknown, a moment at the end of it)
                u = Mathf.Min(u, 1); transform.position = Vector3.Lerp(from, to, u) + Vector3.up * arc * 4 * u * (1 - u);
                var d = to - from; d.y = 0; if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d.normalized) * Quaternion.Euler(0, 0, 90 + 540 * u);
                if (u >= 1 && Time.time - t0 > dur + 2f) Destroy(gameObject); // never told: gone
                return;
            }
            if (Landed != null) { Landed(); Destroy(gameObject); return; }
            switch (how)
            {
                case 0: // into the catcher's hands
                    if (endT < 0) { endT = Time.time; endFrom = transform.position; }
                    float k = (Time.time - endT) / 0.15f;
                    var hand = catcher != null ? catcher.transform.TransformPoint(new Vector3(0, 1.1f, 0.3f) * catcher.Size) : to;
                    transform.position = Vector3.Lerp(endFrom, hand, k);
                    if (k >= 1) { Snd.Play(Synth.Ding[2], transform.position, 0.6f); Destroy(gameObject); }
                    break;
                case 2: Fx.Splash(new Vector3(to.x, Sea.Height(to), to.z), 0.6f, 10); Snd.Play(M1Assets.I.Splash, to, 0.5f); Destroy(gameObject); break;
                case 1: Snd.Play(Synth.Slap, to, 0.6f); Destroy(gameObject); break; // the deck fish takes its place
                default: Destroy(gameObject); break;
            }
        }
    }
}
