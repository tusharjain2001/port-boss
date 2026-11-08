using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace PortBoss.M1
{
    // M8 step 2, fishing F11 (§6.6): rod holders and trolling.
    // - Where: Biscuit has one holder once Doyle's "rod holders" upgrade is fitted (the steel tube on her port quarter,
    //   socket_rodholder_0, built here); the Kestrel's three are part of her (socket_rodholder_0..2).
    // - E at a free holder with a line out (settled, waiting) puts that rod in it, cast and all; the next rod you own comes
    //   to hand (a second rod is needed: the bamboo always counts). The holder rod waits on the same 4–10 s bite. Then a
    //   BELL, the rod bends, and 2.0 s to E it: the rod comes to your hands already struck, and it's the normal fight.
    //   Missed: "it swam off", it re-baits and waits again. E at a holder rod with no bite takes it back (bait kept).
    // - Trolling: with sardine or squid on that rod and the boat at 2–5 m/s, the line trails 14 m behind and keeps fishing.
    //   Faster, or any other bait over 2 m/s: the line drags and comes in after 1.5 s; the rod rests in the holder.
    // - A night skipped, or walking 80 m off: the lines come in and the rods rest.
    // - Co-op: the owner's machine runs its holders (the bite, the bell) and says so in "pb.hold" (host relays it). The
    //   host keeps each holder's roll aside (Coop.HoldBook), so a new cast in the hands doesn't free a fish still on a
    //   holder line, and a bite taken goes back to being the hands' roll for the landed-fish check. Only the owner takes.
    public class RodHolders : MonoBehaviour
    {
        public static RodHolders I;
        public enum HS : byte { Empty, Out, Bite, Rest }
        public const float BellWindow = 2.0f, Reach = 1.5f, BiteReach = 2.3f, TrollLo = 2f, TrollHi = 5f, DragT = 1.5f, TrollBack = 14f, Away = 80f;

        public class Holder
        {
            public Skiff Boat; public int Idx; public Transform Socket;
            public HS St; public ulong Owner; public int Tier = 1, Bait; public bool Troll;
            public Fishing.Cast Cast;                       // mine: the cast it carries (its roll)
            public Vector3 Float, FloatTo;                  // where the line meets the water (a crewmate's: eased to what they send)
            public float BiteAt, BiteSeen, Drag, SentT; public bool Taken;
            public RodView Rod; public LineView Line; public BobberView Bob; int shownTier;
            public bool Mine => St != HS.Empty && Owner == Coop.Me;
            public void Clear() { St = HS.Empty; Troll = false; Drag = 0; }
            // world: the way the rod points, up and out over the water astern
            public Vector3 Dir
            {
                get
                {
                    var p = Boat.transform.InverseTransformPoint(Socket.position);
                    var flat = new Vector3(Mathf.Abs(p.x) < 0.15f ? 0 : Mathf.Sign(p.x) * 0.6f, 0, -0.8f).normalized;
                    return Boat.transform.TransformDirection(flat * Mathf.Cos(40 * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(40 * Mathf.Deg2Rad));
                }
            }
            public void View()
            {
                if (St == HS.Empty) { if (Rod != null) { Object.Destroy(Rod.gameObject); Object.Destroy(Line.gameObject); Object.Destroy(Bob.gameObject); Rod = null; } return; }
                if (Rod == null || shownTier != Tier)
                {
                    if (Rod != null) Object.Destroy(Rod.gameObject);
                    Rod = RodView.Make(Game.ToonMat, Tier); shownTier = Tier; Rod.name = $"holder rod {Boat.Id} {Idx}";
                    if (Line == null) { Line = LineView.Make(); Bob = BobberView.Make(Game.ToonMat); }
                    Line.A = Rod.Tip; Line.Reset(Rod.Tip.position, Float);
                }
                var d = Dir; Rod.transform.SetPositionAndRotation(Socket.position - d * 0.34f, Quaternion.LookRotation(d, Vector3.up));
                bool water = St is HS.Out or HS.Bite;
                Bob.gameObject.SetActive(water && !Troll); Bob.Floating = water && !Troll; Bob.Pos = Float;
                Bob.Dip = St == HS.Bite ? 0.25f : 0;
                if (!Line.Live && water) Line.Reset(Rod.Tip.position, Float);
                Line.Live = water; Line.Slack = St == HS.Bite || Troll ? 0 : 0.05f;
                Line.B = Troll ? new Vector3(Float.x, Sea.Height(Float) - 0.1f, Float.z) : Bob.transform.position + Vector3.up * 0.06f;
                Rod.Tension = St == HS.Bite ? 0.55f + 0.2f * Mathf.Sin(Time.time * 23f) : water ? (Troll ? 0.22f : 0.05f) : 0;
                Rod.BendDir = water ? (Line.B - Rod.Tip.position).normalized : Vector3.down;
            }
        }
        public readonly List<Holder> All = new();
        public static System.Action<string> OnLog;
        static void Log(string s) { Debug.Log("[PB] holder: " + s); OnLog?.Invoke(s); }
        bool told; double lastMin = -1;

        public static void Create() { var g = new GameObject("RodHolders"); I = g.AddComponent<RodHolders>(); }

        // the holders the company has: Biscuit's once the upgrade's on, the Kestrel's once she's ours
        float scanT;
        void Scan()
        {
            foreach (var b in Skiff.All)
            {
                if (b == null) continue;
                bool usable = Firm.C != null && (b.Id == "biscuit" ? Firm.C.Has("up:biscuit:holders") : Works.CanDrive(b));
                if (!usable) continue;
                var socks = new List<Transform>();
                foreach (var t in b.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("socket_rodholder")) socks.Add(t);
                socks.Sort((x, y) => string.CompareOrdinal(x.name, y.name));
                for (int i = 0; i < socks.Count; i++) if (!All.Exists(h => h.Boat == b && h.Idx == i)) { All.Add(new Holder { Boat = b, Idx = i, Socket = socks[i] }); Log($"{b.Name} has holder {i}"); }
            }
        }
        public Holder Find(int boat, int idx) => All.Find(h => h.Boat != null && h.Boat.Index == boat && h.Idx == idx);
        public bool AnyMine => All.Exists(h => h.Mine);
        public bool MyBite => All.Exists(h => h.Mine && h.St == HS.Bite);

        void Update()
        {
            scanT -= Time.deltaTime; if (scanT <= 0) { scanT = 1; Scan(); }   // (game time: a test's frames outrun real time)
            float dt = Time.deltaTime; var me = Angler.I;
            // a night skipped (or any jump of the clock): my lines come in, the rods rest
            if (lastMin >= 0 && Sky.Minutes - lastMin > 20) foreach (var h in All) if (h.Mine && h.St is HS.Out or HS.Bite) Rest(h, null);
            lastMin = Sky.Minutes;
            foreach (var h in All)
            {
                if (h.Boat == null) continue;
                if (h.Mine && me != null) Tick(h, me, dt);
                else if (h.St != HS.Empty && Coop.On && h.Owner != Coop.Me && Coop.I != null && !Coop.I.Mates.ContainsKey(h.Owner) && !Coop.IsHostId(h.Owner)) h.Clear();   // (they left)
                else if (h.St != HS.Empty && !h.Mine) h.Float = Vector3.Lerp(h.Float, h.FloatTo, 1 - Mathf.Exp(-dt * 4));
                h.View();
            }
        }

        void Tick(Holder h, Angler me, float dt)
        {
            var b = h.Boat; float v = b.Velocity.magnitude;
            var tip = h.Rod != null ? h.Rod.Tip.position : h.Socket.position;
            if ((me.transform.position - h.Socket.position).magnitude > Away && h.St != HS.Rest) { Rest(h, "Your holder rod's reeled in: you walked off."); return; }
            if (h.St is HS.Out or HS.Bite)
            {
                bool trollBait = h.Bait == 2 || h.Bait == 3;
                bool fast = v > TrollLo;
                bool troll = fast && trollBait && v <= TrollHi;
                if (troll != h.Troll) { h.Troll = troll; Send(h); Log($"{b.Name} holder {h.Idx}: {(troll ? $"trolling at {v:F1} m/s" : "drifting")}"); }
                if (fast && !troll) { h.Drag += dt; if (h.Drag > DragT) { Rest(h, trollBait ? "Too fast for trolling: the holder rod's reeled in." : "Line dragging: the holder rod's reeled in. Sardine or squid will troll."); return; } }
                else h.Drag = 0;
                if (h.Troll)
                {
                    var back = -new Vector3(b.Velocity.x, 0, b.Velocity.z).normalized; var to = tip + back * TrollBack;
                    h.Float = Vector3.Lerp(h.Float, new Vector3(to.x, 0, to.z), 1 - Mathf.Exp(-dt * 1.5f));
                    if (Random.value < dt * 3) Fx.Splash(new Vector3(h.Float.x, Sea.Height(h.Float), h.Float.z), 0.15f, 2);
                }
                else
                {
                    h.Float += new Vector3(0.06f, 0, -0.04f) * dt;                                       // drift (as the hand's float)
                    var d = new Vector3(h.Float.x - tip.x, 0, h.Float.z - tip.z); float max = FishDb.Rods[h.Tier - 1].RangeMax + 4;
                    if (d.magnitude > max) h.Float = new Vector3(tip.x, 0, tip.z) + d.normalized * max;      // (the boat drifts off: the line pulls it along)
                }
                if (Time.unscaledTime - h.SentT > 0.5f) Send(h);
            }
            if (h.St == HS.Out && Time.time >= h.BiteAt)
            {
                if (!h.Cast.Rolled && Coop.On && !Coop.IsHost) Debug.Log("[PB] holder: the host's roll hadn't come by the bite: rolled here");
                if (!h.Cast.Rolled) { h.Cast.Sp = FishDb.Roll(Fishing.Rng, h.Tier, BaitId(h)); h.Cast.ByHost = false; h.Cast.Rolled = true; }
                h.St = HS.Bite; h.BiteSeen = Time.time; Send(h);
                Snd.Play(Synth.HolderBell, tip, 1f); Snd.Rumble(0.5f, 0.2f, 0.25f);
                Fx.Splash(new Vector3(h.Float.x, Sea.Height(h.Float), h.Float.z), 0.5f, 10);
                if (Settings.Captions) StoryUi.I?.Say(null, "[Bell] A bite on the holder rod!", 1.6f);
                if (!told) { told = true; Alerts.Push("Rod holder", "The bell! You've 2 seconds: E at the holder.", Alerts.Kind.Info); }
                Log($"{b.Name} holder {h.Idx}: bite ({h.Cast.Sp?.Id}){(h.Troll ? " trolling" : "")}");
            }
            else if (h.St == HS.Bite && Time.time - h.BiteSeen > BellWindow)
            {
                Log($"{b.Name} holder {h.Idx}: the bite swam off");
                Alerts.Push("It swam off.", "Too slow at the holder. It's baited again.", Alerts.Kind.Info);
                Rebait(h);
            }
        }

        static string BaitId(Holder h) { var k = Firm.I?.MyKit; return k != null && k.Bait[h.Bait] > 0 ? FishDb.Baits[h.Bait].Id : "None"; }

        // a fresh roll for this line (co-op: the host's, kept aside as a holder's), and the rod's wait again
        void Rebait(Holder h)
        {
            h.St = HS.Out; h.BiteAt = Time.time + Wait(h.Tier);
            h.Cast.RollId = Fishing.NewRoll(); h.Cast.Rolled = false; h.Cast.ByHost = false;
            if (Coop.On) Coop.AskRoll(h.Cast.RollId, h.Tier, BaitId(h), true);
            else { h.Cast.Sp = FishDb.Roll(Fishing.Rng, h.Tier, BaitId(h)); h.Cast.Rolled = true; }
            Send(h);
        }
        static float Wait(int tier) { var r = FishDb.Rods[tier - 1]; return r.BiteLo + (r.BiteHi - r.BiteLo - 0.02f) * (float)Fishing.Rng.NextDouble(); }

        // the host's answer to a holder's roll (Fishing passes on the ids that aren't its own)
        public void Rolled(int id, FishSpecies sp, float cm, float kg, ulong fish)
        {
            foreach (var h in All) if (h.Mine && h.Cast.RollId == id) { h.Cast.Sp = sp; h.Cast.Cm = cm; h.Cast.Kg = kg; h.Cast.Fish = fish; h.Cast.Rolled = h.Cast.ByHost = true; return; }
        }

        void Rest(Holder h, string why)
        {
            h.St = HS.Rest; h.Troll = false; h.Drag = 0; Send(h);
            if (why != null) Alerts.Push("Rod holder", why, Alerts.Kind.Info);
            Log($"{h.Boat.Name} holder {h.Idx}: resting{(why != null ? " (" + why + ")" : "")}");
        }

        // the best rod I own that's neither in my hands nor in a holder (0: none)
        public int NextRod(Kit k, int hand)
        {
            for (int t = 5; t >= 1; t--)
            {
                if ((k.Rods >> (t - 1) & 1) == 0 || t == hand) continue;
                if (All.Exists(h => h.Mine && h.Tier == t)) continue;
                return t;
            }
            return 0;
        }

        public void Put(Angler me, Holder h, int next)
        {
            var f = me.Fish; var c = f.HandOff(next);
            h.St = HS.Out; h.Owner = Coop.Me; h.Tier = c.Tier; h.Bait = c.Bait; h.Cast = c; h.Float = c.Float; h.Troll = false; h.Drag = 0;
            h.BiteAt = Mathf.Max(c.BiteAt, Time.time + 1.0f);                    // its bite keeps coming as planned
            Snd.Play(Synth.Thup, h.Socket.position, 0.5f, 1.4f); Snd.Play(Synth.Ratchet, h.Socket.position, 0.3f, 1.3f);
            Send(h);
            Log($"put the {FishDb.Rods[c.Tier - 1].Name} in {h.Boat.Name} holder {h.Idx} (bait {FishDb.Baits[c.Bait].Id}); in the hands: {FishDb.Rods[next - 1].Name}");
        }

        public void Take(Angler me, Holder h)
        {
            var f = me.Fish; bool bite = h.St == HS.Bite;
            if (bite) f.TakeBite(h.Cast); else f.TakeBack(h.Tier, h.Bait);
            Log($"took the {FishDb.Rods[h.Tier - 1].Name} from {h.Boat.Name} holder {h.Idx}{(bite ? $" on its bite ({h.Cast.Sp?.Id})" : "")}");
            h.Taken = bite; h.Clear(); Send(h); h.Taken = false;
        }

        // my holder ringing within reach of where I stand (a ringing rod reaches further: 2.3 m, the tiller to Biscuit's holder)
        public static Holder BiteNear(Angler me)
        {
            if (I == null || me.Remote || me.Boat == null) return null;
            foreach (var h in I.All)
            {
                if (h.Boat != me.Boat || !h.Mine || h.St != HS.Bite) continue;
                var d = h.Socket.position - me.transform.position; d.y *= 0.4f; if (d.magnitude < BiteReach) return h;
            }
            return null;
        }

        // ---- E: the holder within reach on my boat (Angler.Interact asks before anything else: a line out locks the rest)
        public static string Prompt(Angler me)
        {
            if (I == null || me.Remote || !me.OnBoat || me.Swimming || me.AtHelm || me.Busy || me.Boat == null) return null;
            var f = me.Fish;
            bool calm = f.State is Fishing.St.Stowed or Fishing.St.Drawing or Fishing.St.Ready or Fishing.St.Settle or Fishing.St.Waiting or Fishing.St.Recover;
            // a ringing holder wins even over a bite in the hands (both at once: E takes the holder's, the other swims off)
            var ring0 = calm || f.State == Fishing.St.Bite ? BiteNear(me) : null;
            if (ring0 != null) { if (Ctl.Interact) I.Take(me, ring0); return "Take the rod! A bite"; }
            if (!calm) return null;
            Holder near = null; float bd = Reach;
            foreach (var h in I.All)
            {
                if (h.Boat != me.Boat) continue;
                var d = h.Socket.position - me.transform.position; d.y *= 0.4f;
                if (d.magnitude < bd) { bd = d.magnitude; near = h; }
            }
            if (near == null || near.St != HS.Empty && !near.Mine) return null;
            if (near.St != HS.Bite && me.Boat.Helm != null)                                    // (by the tiller: the nearer one is what E is for; a bell always wins)
            { var dh = me.Boat.Helm.position - me.transform.position; dh.y *= 0.4f; if (dh.magnitude < bd && dh.magnitude < 1.3f) return null; }
            if (near.St == HS.Bite) { if (Ctl.Interact) I.Take(me, near); return "Take the rod! A bite"; }
            if (near.St is HS.Out or HS.Rest) { if (Ctl.Interact) I.Take(me, near); return near.St == HS.Out ? "Take the rod back" : "Take the rod"; }
            if (f.State != Fishing.St.Waiting) return null;                                 // (cast first: a line out goes in)
            int next = I.NextRod(f.Kit, f.RodTier);
            if (next == 0) { if (Ctl.Interact) Alerts.Push("Rod holder", "You'll want a second rod in your hands. June sells them.", Alerts.Kind.Info); return "Rod holder: needs a second rod"; }
            if (Ctl.Interact) I.Put(me, near, next);
            return "Put the rod in the holder";
        }

        // ---- co-op: what my holders do goes to the crew
        void Send(Holder h) { h.SentT = Time.unscaledTime; if (Coop.On) Coop.I.SendHold(h); }
        // a crewmate's holder (or the host's word on one): shown as they say
        public void Remote(ulong owner, int boat, int idx, HS st, int tier, int roll, Vector3 fl, bool troll)
        {
            Scan(); var h = Find(boat, idx); if (h == null) return;
            if (owner == Coop.Me) return;                                                    // (mine: I run it)
            if (st == HS.Bite && h.St != HS.Bite) Snd.Play(Synth.HolderBell, h.Socket.position, 0.8f);
            h.Owner = owner; h.St = st; h.Tier = Mathf.Clamp(tier, 1, 5); h.Troll = troll;
            h.FloatTo = fl; if ((h.Float - fl).sqrMagnitude > 400 || h.St == HS.Empty) h.Float = fl;
        }

        // Biscuit's holder (Doyle's upgrade): a steel tube clamped to her port quarter, leaning out and astern
        public static GameObject BuildBiscuit(Transform parent, Bounds hull)
        {
            var g = new GameObject("up:biscuit:holders"); g.transform.SetParent(parent, false);
            var at = new Vector3(hull.min.x + 0.07f, hull.max.y + 0.02f, hull.min.z + 1.1f);
            var sock = new GameObject("socket_rodholder_0").transform; sock.SetParent(g.transform, false); sock.localPosition = at + new Vector3(-0.02f, 0.2f, -0.06f);
            var flat = new Vector3(-0.6f, 0, -0.8f).normalized; var dir = flat * Mathf.Cos(40 * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(40 * Mathf.Deg2Rad);
            Color steel = Ponyo.Hex("D5DADF"), dark = Ponyo.Hex("5E5A66"), red = Ponyo.Hex("E2553F");
            var tube = Util.MeshObj("tube", Util.Cylinder(12, 0.04f, 0.034f, 0.32f, steel, steel), Game.ToonMat, g.transform);
            tube.transform.localPosition = sock.localPosition; tube.transform.localRotation = Quaternion.LookRotation(-dir, Vector3.up);   // (from the mouth, down into the tube)
            var lip = Util.MeshObj("lip", Util.Cylinder(12, 0.047f, 0.047f, 0.035f, red, red), Game.ToonMat, g.transform);
            lip.transform.localPosition = sock.localPosition - dir * 0.005f; lip.transform.localRotation = tube.transform.localRotation;
            Firm.Box(g.transform, "clamp", at + new Vector3(0.0f, 0.03f, 0), new Vector3(0.09f, 0.07f, 0.12f), dark);
            Firm.Box(g.transform, "strut", at + new Vector3(-0.01f, 0.11f, -0.03f), new Vector3(0.035f, 0.17f, 0.035f), steel, new Vector3(-20, 0, 18));
            return g;
        }
    }

    public partial class Coop
    {
        // ---- M8 step 2: rod holders. "pb.hold": a holder's state from its owner (to the host, then relayed to the rest).
        //      The host's book keeps each holder's roll aside: a new cast doesn't free it, a bite taken makes it the hands'.
        readonly Dictionary<(ulong who, int id), (int id, FishSpecies sp, float cm, float kg, ulong fish)> heldRolls = new();
        readonly Dictionary<(ulong who, int boat, int idx), int> holdRoll = new();
        public int HeldRolls => heldRolls.Count;                                    // (tests)
        public static bool IsHostId(ulong id) => On && id == NetworkManager.ServerClientId;

        void RegisterHold(CustomMessagingManager m) => Reg(m, "pb.hold", OnHold);

        public void SendHold(RodHolders.Holder h)
        {
            byte bi = (byte)h.Boat.Index, ix = (byte)h.Idx, st = (byte)h.St, tier = (byte)h.Tier; int roll = h.Cast.RollId; var fl = h.Float; bool tr = h.Troll, tk = h.Taken;
            void Body(FastBufferWriter w) { w.WriteValueSafe(bi); w.WriteValueSafe(ix); w.WriteValueSafe(st); w.WriteValueSafe(tier); w.WriteValueSafe(roll); w.WriteValueSafe(fl); w.WriteValueSafe(tr); w.WriteValueSafe(tk); }
            if (nm.IsServer) { HoldBook(nm.LocalClientId, bi, ix, (RodHolders.HS)st, roll, tk); RelayHold(nm.LocalClientId, Body); }
            else Send("pb.hold", Body, NetworkDelivery.ReliableSequenced);
        }
        void RelayHold(ulong owner, System.Action<FastBufferWriter> body)
        {
            foreach (var id in nm.ConnectedClientsIds) if (id != nm.LocalClientId && id != owner) SendTo(id, "pb.hold", w => { w.WriteValueSafe(owner); body(w); }, NetworkDelivery.ReliableSequenced);
        }
        void OnHold(ulong from, FastBufferReader r)
        {
            ulong owner = from; if (!nm.IsServer) r.ReadValueSafe(out owner);
            r.ReadValueSafe(out byte bi); r.ReadValueSafe(out byte ix); r.ReadValueSafe(out byte st); r.ReadValueSafe(out byte tier); r.ReadValueSafe(out int roll);
            r.ReadValueSafe(out Vector3 fl); r.ReadValueSafe(out bool tr); r.ReadValueSafe(out bool tk);
            Deliver(() =>
            {
                if (nm.IsServer)
                {
                    HoldBook(owner, bi, ix, (RodHolders.HS)st, roll, tk);
                    RelayHold(owner, w => { w.WriteValueSafe(bi); w.WriteValueSafe(ix); w.WriteValueSafe(st); w.WriteValueSafe(tier); w.WriteValueSafe(roll); w.WriteValueSafe(fl); w.WriteValueSafe(tr); w.WriteValueSafe(tk); });
                }
                RodHolders.I?.Remote(owner, bi, ix, (RodHolders.HS)st, tier, roll, fl, tr);
            }, true);
        }

        // (host) a holder of `who` now carries `roll` (Out / Bite), or nothing (Rest / Empty; taken: its fish is in their hands)
        void HoldBook(ulong who, int boat, int idx, RodHolders.HS st, int roll, bool taken)
        {
            var key = (who, boat, idx); holdRoll.TryGetValue(key, out int was);
            if (st is RodHolders.HS.Out or RodHolders.HS.Bite)
            {
                if (rolls.TryGetValue(who, out var x) && x.id == roll) { heldRolls[(who, roll)] = x; rolls.Remove(who); }   // put in: the hands' roll goes aside
                if (was != 0 && was != roll) FreeHeld(who, was);                                                            // re-baited: the last one swam off
                holdRoll[key] = roll;
                return;
            }
            holdRoll.Remove(key);
            if (taken && was != 0 && heldRolls.TryGetValue((who, was), out var y))
            {
                heldRolls.Remove((who, was)); FreeHandsButHeld(who);                       // whatever the hands had out is gone
                rolls[who] = y; Log($"{Find(who)?.Name ?? who.ToString()} took a holder bite: roll {was} is the hands' now");
            }
            else if (was != 0) FreeHeld(who, was);
        }
        void FreeHeld(ulong who, int id) { if (heldRolls.Remove((who, id), out var x) && x.fish != 0 && Book.Is(x.fish, Ledger.Kind.Hands, who)) Book.Free(who, x.fish); }
        // a new cast (or a bite taken): the hands' last fish never came up; the holders' fish are still out there
        void FreeHandsButHeld(ulong who)
        {
            var keep = new HashSet<ulong>(); foreach (var kv in heldRolls) if (kv.Key.who == who) keep.Add(kv.Value.fish);
            var l = new List<ulong>(); foreach (var kv in Book.Where) if (kv.Value.K == Ledger.Kind.Hands && kv.Value.Who == who && !keep.Contains(kv.Key)) l.Add(kv.Key);
            foreach (var id in l) Book.Free(who, id);
        }
    }
}
