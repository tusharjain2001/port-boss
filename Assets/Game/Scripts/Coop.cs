using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace PortBoss.M1
{
    // M2 co-op (gdd systems §3, §4.6). NGO carries the connection (Unity Transport for LAN and the tests; the Steam relay
    // comes with the lobby) and the game sends its own small messages over it. The world is built in code on every
    // machine, so nothing here is an NGO NetworkObject:
    // - each player's pose, owner-authoritative, boat-relative while aboard (the receiver places a rider in its own
    //   copy of the boat, so a rolling boat never makes riders jitter), 30 a second, unreliable;
    // - the boat's pose from whoever owns it: the player at the tiller, else the host;
    // - the roster the host keeps: who is who, which crew member (one each, Manan 2 Nov), which colour.
    // Clients talk only to the host; the host relays. Without -pb-host / -pb-join the game is solo, as in M1.
    [DefaultExecutionOrder(-200)]
    public partial class Coop : MonoBehaviour
    {
        public static Coop I;
        public static bool On => I != null && I.nm != null && I.nm.IsListening;
        public static bool IsHost => On && I.nm.IsServer;
        public static ulong Me => On ? I.nm.LocalClientId : 0;
        public const ushort Port = 7777;
        public const float Rate = 30, Delay = 0.1f; // sends a second; remote things are shown this far in the past

        // player colours by join slot (systems §1.9): orange, blue, green, red
        public static readonly Color[] Colours = { Ponyo.Hex("E8752A"), Ponyo.Hex("3A86E0"), Ponyo.Hex("7DBE6A"), Ponyo.Hex("E2553F") };

        public class Entry { public ulong Id; public byte Slot, Crew; public string Name, Key; } // (Key: host only, for a rejoin)
        public readonly List<Entry> Roster = new();
        // M5: one owner per boat (Skiff.All[i]: 0 Biscuit, 1 the Kestrel): the client whose copy of that boat is the real one
        public readonly ulong[] Owner = new ulong[Skiff.MaxBoats];
        public ulong BoatOwner { get => Owner[0]; set => Owner[0] = value; }   // Biscuit's (the M2-M4 tests)
        // someone left or came back: the boats they ran are the host's again
        void Unown(ulong id)
        {
            for (int b = 0; b < Owner.Length; b++)
                if (Owner[b] == id) { Owner[b] = nm.LocalClientId; var s = Skiff.At(b); if (s != null) { s.AtHelm = false; s.TakeOver(); } }
        }
        public readonly Dictionary<ulong, Angler> Mates = new();
        public Entry Find(ulong id) => Roster.Find(e => e.Id == id);
        public static Action<string> OnLog;             // test hooks

        NetworkManager nm; UnityTransport utp;
        float sendT, clockT; string myName; bool helloSent;
        float hostShift; bool clockSet;                 // the host's clock - mine (the largest gap seen: the quickest packet)
        float simDelay, simLoss;                        // -pb-netsim <rtt ms> <loss %>: our own lag (UTP's simulator is gone)
        readonly List<(float at, Action a)> inbox = new();

        // ---------------------------------------------------------------- start / stop
        public static void Boot()
        {
            var args = Environment.GetCommandLineArgs();
            string Arg(string k) { int i = Array.IndexOf(args, k); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
            bool host = Array.IndexOf(args, "-pb-host") >= 0; string join = Arg("-pb-join");
            if (!host && join == null) return;
            Make(Arg("-pb-name") ?? (host ? "Host" : "Crew"));
            var sim = Arg("-pb-netsim");
            if (sim != null)
            {
                float.TryParse(sim, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var rtt);
                int li = Array.IndexOf(args, "-pb-netsim") + 2;
                float loss = li < args.Length && float.TryParse(args[li], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var l) ? l : 0;
                I.simDelay = rtt / 2000f; I.simLoss = loss / 100f;
            }
            ushort port = ushort.TryParse(Arg("-pb-port"), out var pp) ? pp : Port;
            I.plan = new Plan { Host = host, Join = join, Port = port };
        }

        // NGO registers its message types after the scene's Awakes: start a frame later (from Game.Awake it threw)
        void Start() => Begin(plan);

        void Begin(Plan p)
        {
            bool host = p.Host;
            nm = gameObject.AddComponent<NetworkManager>(); utp = gameObject.AddComponent<UnityTransport>();
            nm.NetworkConfig = new NetworkConfig { NetworkTransport = utp, EnableSceneManagement = false, ConnectionApproval = false, TickRate = 30 };
            nm.LogLevel = Array.IndexOf(Environment.GetCommandLineArgs(), "-pb-netlog") >= 0 ? LogLevel.Developer : LogLevel.Error; // (tests: NGO's own story)
            nm.OnClientConnectedCallback += Connected; nm.OnClientDisconnectCallback += Disconnected;
            MakeTransport(p, out bool ok);
            if (nm.LogLevel == LogLevel.Developer && nm.NetworkConfig.NetworkTransport != null)   // (tests: the transport's own events)
                nm.NetworkConfig.NetworkTransport.OnTransportEvent += (e, id, _, t) => { if (e != NetworkEvent.Data) Log($"net: transport {e}, transport id {id}, at {t:F1} s ({Time.realtimeSinceStartup:F1} s real)"); };
            string where = p.Steam ? (host ? "over Steam" : "a friend over Steam") : host ? p.Port.ToString() : p.Join;
            Log($"{(host ? "hosting" : "joining")} {where}: {(ok ? "ok" : "FAILED")}{(simDelay > 0 ? $" (lag sim {simDelay * 2000:0} ms rtt, {simLoss * 100:0}% loss)" : "")}");
            if (!ok) { Status = host ? "Couldn't host (is another copy already hosting here?)." : "Couldn't connect."; Destroy(gameObject); I = null; return; }
            var m = nm.CustomMessagingManager;
            Reg(m, "pb.hello", OnHello); Reg(m, "pb.roster", OnRoster);
            Reg(m, "pb.pose", OnPose); Reg(m, "pb.boat", OnBoat);
            Reg(m, "pb.ask", OnAsk); Reg(m, "pb.clock", OnClock);
            Reg(m, "pb.roll", OnRoll); Reg(m, "pb.rolled", OnRolled);
            Reg(m, "pb.landed", OnLanded); Reg(m, "pb.tangle", OnTangle);
            RegisterItems(m); Reg(m, "pb.voice", OnVoice); Reg(m, "pb.place", OnPlace); RegisterDay(m); RegisterSleep(m); RegisterFirm(m); RegisterM5(m); Reg(m, "pb.shove", OnShove);
            StartVoice();
            if (!host && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-pb-oldjoin") < 0) gameObject.AddComponent<JoinView>(); // M3.1: the view waits till I'm me, where I was (JoinView)
            if (host)
            {
                Roster.Add(new Entry { Id = nm.LocalClientId, Slot = 0, Crew = (byte)Angler.Body, Name = myName });
                if (Angler.I != null) Book.Bars[nm.LocalClientId] = Angler.I.Bar; // the host's own hotbar is the book's
                for (int b = 0; b < Owner.Length; b++) Owner[b] = nm.LocalClientId;
                Apply();
            }
        }

        void OnDestroy() { StopVoice(); if (nm != null && nm.IsListening) nm.Shutdown(); }

        static void Log(string s) { Debug.Log("[PB] coop: " + s); OnLog?.Invoke(s); }

        void Connected(ulong id)
        {
            if (nm.IsServer) { if (id != nm.LocalClientId) { Log($"client {id} connected"); heard[id] = Time.realtimeSinceStartup; } return; }
            if (id != nm.LocalClientId || helloSent) return;
            helloSent = true; Log($"connected as client {id}");
            // with the fish I hold (code audit 3 Nov, P0: the host's empty copy used to wipe a hotbar filled playing solo)
            var mine = new List<Fish>(); if (Angler.I != null) foreach (var sl in Angler.I.Bar.S) mine.AddRange(sl.Fish);
            Send("pb.hello", w => { w.WriteValueSafe((byte)Angler.Body); w.WriteValueSafe(myName); w.WriteValueSafe(MyKey(myName)); w.WriteValueSafe((byte)mine.Count); foreach (var f in mine) WriteFish(ref w, f); }, NetworkDelivery.ReliableSequenced);
            Log($"hello with {mine.Count} fish of my own");
        }

        void Disconnected(ulong id)
        {
            if (nm.ShutdownInProgress || nm.CustomMessagingManager == null) return; // we're the one closing
            if (nm.IsServer)
            {
                heard.Remove(id); var e = Find(id); if (e == null) return;
                KeepAway(e);
                Roster.Remove(e); Log($"{e.Name} left");
                Alerts.Push("Crew", e.Name + " left the boat.", Alerts.Kind.Crew);
                Unown(id);
                Broadcast(); Apply();
                return;
            }
            // never got in (wrong address, nobody hosting), or turned away with a reason (the crew full): the title menu and
            // a left alert say so (the menu may be closed already: a Steam join closes it before the relay connects)
            if (!helloSent || !string.IsNullOrEmpty(nm.DisconnectReason))
            {
                Log("couldn't connect" + (string.IsNullOrEmpty(nm.DisconnectReason) ? "" : ": " + nm.DisconnectReason));
                Fail(string.IsNullOrEmpty(nm.DisconnectReason) ? "Couldn't connect." : nm.DisconnectReason);
                Lobby?.Leave(); Lobby = null;
                nm.Shutdown(); Destroy(gameObject); I = null; return;
            }
            // the host is gone: back to playing alone, on our own copy of the boat
            Log("the host left");
            Alerts.Push("Crew", "The host left. You're on your own now.", Alerts.Kind.Warning);
            foreach (var a in Mates.Values) if (a != null) Destroy(a.gameObject);
            Mates.Clear(); Roster.Clear();
            foreach (var b in Skiff.All) if (b.Remote) b.TakeOver();
            Lobby?.Leave(); Lobby = null;
            nm.Shutdown(); Destroy(gameObject); I = null;
        }

        // ---------------------------------------------------------------- the roster (host)
        void OnHello(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out byte wish); r.ReadValueSafe(out string name); string key = null;
            if (r.TryBeginRead(1)) r.ReadValueSafe(out key);
            var held = new List<Fish>(); if (r.TryBeginRead(1)) { r.ReadValueSafe(out byte nf); for (int i = 0; i < nf; i++) held.Add(ReadFish(ref r)); }
            Deliver(() =>
            {
                if (!nm.IsServer || Find(from) != null) return;
                // back before their old connection timed out (Steam takes ~10 s to notice a quit): that old entry is them, so
                // retire it first and they get their own things back. The host's own account again (one PC): a stranger.
                var old = key != null ? Roster.Find(x => x.Key == key) : null;
                if (old != null && old.Id == nm.LocalClientId) key = null;
                else if (old != null)
                {
                    Log($"{old.Name} is back before their old connection closed: retiring it");
                    KeepAway(old); Roster.Remove(old);
                    Unown(old.Id);
                    nm.DisconnectClient(old.Id);
                }
                if (Roster.Count >= MaxCrew) { Log($"{name} can't join: the crew is full ({MaxCrew})"); nm.DisconnectClient(from, "The crew is full."); return; }
                var back = Back(key, from);
                byte slot = back != null && !Roster.Exists(x => x.Slot == back.Slot) ? back.Slot : (byte)0;
                if (back == null || slot != back.Slot) { slot = 0; while (Roster.Exists(x => x.Slot == slot)) slot++; }
                var e = new Entry { Id = from, Slot = slot, Crew = FreeCrew(back != null ? back.Crew : wish, from), Name = string.IsNullOrEmpty(name) ? "Crew " + (slot + 1) : name, Key = key };
                Roster.Add(e); Log($"{e.Name} {(back != null ? "is back" : "joined")}: slot {slot}, {CrewName(e.Crew)}{(back != null ? $", their hotbar back ({Book.Bar(from).FishCount} fish)" : "")}");
                Alerts.Push("Crew", back != null ? $"{e.Name} is back aboard." : $"{e.Name} came aboard as {CrewName(e.Crew)}.", Alerts.Kind.Crew);
                Broadcast(); Apply();
                if (back != null) SendPlace(from, back);
                // the boat as it is now, so the joiner doesn't wait for the next pose to see it
                foreach (var b in Skiff.All) if (Owner[b.Index] == nm.LocalClientId) SendBoat(b.NetPose(Now));
                // their hotbar (the book's) and the fish lying about. What they brought goes into the book first: all of it
                // for a newcomer; for a returning player only what they caught on their own while away (Id 0), the rest is
                // the book's already. ponytail: a fish they let go while away still comes back (the book never heard);
                // fixing that needs trusting the client's whole hotbar, or a save.
                int took = 0; var spill = new List<ulong>(); // (no room in their hotbar: on the deck at their feet, never lost)
                foreach (var f in held) if (back == null || f.Id == 0)
                {
                    var m = Book.Make(from, f.Species, f.Grade, f.Cm, f.Kg, f.CaughtAt);
                    if (Book.Keep(from, m.Id)) took++; else if (Book.Drop(from, m.Id)) spill.Add(m.Id);
                }
                if (held.Count > 0) Log($"{e.Name} brought {held.Count} fish: {took} booked{(spill.Count > 0 ? $", {spill.Count} on the deck (no room)" : "")}");
                Book.Bar(from); SendBar(from);
                foreach (var kv in Deck) if (kv.Value != null)
                {
                    var f = kv.Value.F; var s = Skiff.I; bool boat = s != null && kv.Value.transform.IsChildOf(s.transform);
                    var local = boat ? s.transform.InverseTransformPoint(kv.Value.transform.position) : kv.Value.transform.position;
                    SendTo(from, "pb.deck", w => { w.WriteValueSafe(true); WriteFish(ref w, f); w.WriteValueSafe(boat); w.WriteValueSafe(local); }, NetworkDelivery.ReliableSequenced);
                }
                if (spill.Count > 0)
                {
                    var s0 = Skiff.I; var at = back != null && back.Boat && s0 != null ? s0.transform.TransformPoint(back.At) : back != null ? back.At : Angler.I.transform.position;
                    for (int i = 0; i < spill.Count; i++) DeckAdd(spill[i], Ground(at + Vector3.up * 0.5f + Quaternion.Euler(0, i * 70, 0) * Vector3.forward * 0.35f, out var g) ? g : at);
                }
            }, true);
        }

        // One crew member per player (Manan 2 Nov): the one asked for if nobody has it, else the next free one.
        byte FreeCrew(int wish, ulong who)
        {
            int n = Mathf.Max(1, M1Assets.I.Crew != null ? M1Assets.I.Crew.Length : 1);
            for (int k = 0; k < n; k++)
            {
                byte c = (byte)((wish + k) % n);
                if (!Roster.Exists(e => e.Crew == c && e.Id != who)) return c;
            }
            return (byte)(wish % n);
        }
        static string CrewName(int c) { var n = M1Assets.I.CrewNames; return n != null && c < n.Length ? n[c] : "the worker"; }

        void Broadcast()
        {
            foreach (var id in nm.ConnectedClientsIds)
                if (id != nm.LocalClientId)
                    SendTo(id, "pb.roster", w =>
                    {
                        w.WriteValueSafe((byte)Owner.Length); foreach (var o in Owner) w.WriteValueSafe(o); w.WriteValueSafe((byte)Roster.Count);
                        foreach (var e in Roster) { w.WriteValueSafe(e.Id); w.WriteValueSafe(e.Slot); w.WriteValueSafe(e.Crew); w.WriteValueSafe(e.Name); w.WriteValueSafe(e.Key ?? MyKey(e.Name)); }   // (M5: the key, for their outfit)
                    }, NetworkDelivery.ReliableSequenced);
        }

        void OnRoster(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out byte nb); var owners = new ulong[nb]; for (int i = 0; i < nb; i++) r.ReadValueSafe(out owners[i]);
            r.ReadValueSafe(out byte n);
            var list = new List<Entry>();
            for (int i = 0; i < n; i++)
            {
                var e = new Entry(); r.ReadValueSafe(out e.Id); r.ReadValueSafe(out e.Slot); r.ReadValueSafe(out e.Crew); r.ReadValueSafe(out string nm_); e.Name = nm_; r.ReadValueSafe(out string key_); e.Key = key_;
                list.Add(e);
            }
            Deliver(() => { Roster.Clear(); Roster.AddRange(list); for (int i = 0; i < owners.Length && i < Owner.Length; i++) Owner[i] = owners[i]; Apply(); }, true);
        }

        // Make the world match the roster: my own body and colour, a body for everyone else, and who runs the boat.
        void Apply()
        {
            var me = Find(nm.LocalClientId); var a = Angler.I;
            if (me != null && a != null)
            {
                if (Angler.Body != me.Crew || a.Colour != Colours[me.Slot]) { a.Fish.Stow(); a.Wear(me.Crew, Colours[me.Slot]); }
                if (!startSpot) { startSpot = true; StartSpot(a, me.Slot); }
            }
            foreach (var e in Roster)
            {
                if (e.Id == nm.LocalClientId) continue;
                if (!Mates.TryGetValue(e.Id, out var m) || m == null)
                {
                    var at = a != null ? a.transform.position : Vector3.zero;
                    m = Angler.CreateRemote(M1Assets.I, e.Crew, Colours[e.Slot], at); m.name = "Mate " + e.Name; Mates[e.Id] = m;
                }
                else if (m.Crew != e.Crew || m.Colour != Colours[e.Slot]) m.Wear(e.Crew, Colours[e.Slot]);
                m.NetName = e.Name;
            }
            foreach (var id in new List<ulong>(Mates.Keys))
                if (Find(id) == null) { if (Mates[id] != null) Destroy(Mates[id].gameObject); Mates.Remove(id); }
            foreach (var s in Skiff.All)
            {
                ulong ow = Owner[s.Index]; bool mine = ow == nm.LocalClientId;
                if (ow != shownOwner[s.Index]) { s.Net.Clear(); shownOwner[s.Index] = ow; }
                if (mine && s.Remote) s.TakeOver(); else if (!mine) s.Remote = true;
                if (!mine && a != null && a.AtHelm && Skiff.I == s) a.LeaveHelm(); // someone else got there first
            }
        }

        // Joining from the start (still on the spawn): my own spot by slot, ~1.2 m apart on the pier, never inside whoever
        // stands on the spawn (look audit 3 Nov). The host (slot 0) keeps the spawn; a rejoin's saved place comes after this.
        bool startSpot;
        public static Vector3 SpotFor(int slot)
        {
            var sp = Landing.I != null ? Landing.I.Spawn : null; if (sp == null) return Vector3.zero;
            var r = Vector3.ProjectOnPlane(sp.right, Vector3.up).normalized; var f = Vector3.ProjectOnPlane(sp.forward, Vector3.up).normalized;
            var tries = slot switch { 1 => new[] { r, -r, f, -f }, 2 => new[] { -r, r, -f, f }, 3 => new[] { f + r, f - r, -f + r, -f - r }, _ => new[] { Vector3.zero } };
            // on the pier (not off its edge) and room for a body there: on map B's landing the first spot sat against
            // something solid, and the controller slid 0.27 m back out of it toward whoever stood on the spawn
            Vector3? grounded = null;
            foreach (var t in tries)
            {
                var p = sp.position + t * 1.2f;
                if (!Ground(p, out var at) || Mathf.Abs(at.y - sp.position.y) >= 0.35f) continue;
                grounded ??= at;
                bool clear = true;
                foreach (var c in Physics.OverlapCapsule(at + Vector3.up * 0.42f, at + Vector3.up * 1.45f, 0.3f, ~0, QueryTriggerInteraction.Ignore))
                    if (c.GetComponentInParent<Angler>() == null) { clear = false; break; }
                if (clear) return at;
            }
            Log($"no clear start spot for slot {slot}: {(grounded.HasValue ? "a grounded one" : "the spawn")} instead"); // the 0.88 m bug was silent
            return grounded ?? sp.position;
        }
        void StartSpot(Angler a, byte slot)
        {
            if (IsHost || Landing.I == null || Landing.I.Spawn == null) return;
            if (Vector3.Distance(a.transform.position, Landing.I.Spawn.position) > 1.5f) return; // joined mid-play: stay put
            var p = SpotFor(slot); a.Teleport(p + Vector3.up * 0.05f, a.transform.eulerAngles.y);
            Log($"my start spot (slot {slot}): {Vector3.Distance(p, Landing.I.Spawn.position):F2} m from the spawn");
        }

        // ---------------------------------------------------------------- asks (client -> host): the tiller, the anchor, a body
        public enum Ask : byte { Helm, LeaveHelm, Anchor, NextCrew, Scoop, Patch, Crew0 = 16 }   // (M5) Crew0 + n: the wardrobe picks crew member n
        public static void PickCrew(int c) => Request((Ask)((int)Ask.Crew0 + c));
        public static bool CrewTaken(int c) => On && I.Roster.Exists(e => e.Crew == c && e.Id != I.nm.LocalClientId);

        public static void Request(Ask what)
        {
            if (!On) return;
            byte boat = (byte)(Skiff.I != null ? Skiff.I.Index : 0);   // (M5) the boat I'm on
            if (IsHost) I.Handle(I.nm.LocalClientId, what, boat);
            else I.Send("pb.ask", w => { w.WriteValueSafe((byte)what); w.WriteValueSafe(boat); }, NetworkDelivery.ReliableSequenced);
        }

        void OnAsk(ulong from, FastBufferReader r) { r.ReadValueSafe(out byte b); r.ReadValueSafe(out byte boat); Deliver(() => { if (nm.IsServer) Handle(from, (Ask)b, boat); else Owned((Ask)b, boat); }, true); }

        // a client driving the boat: the host forwards a crewmate's anchor, scoop or patch to the real copy, here
        static void Owned(Ask what, byte boat)
        {
            var s = Skiff.At(boat); if (s == null || s.Remote) return;
            if (what == Ask.Anchor) s.ToggleAnchor(); else if (what == Ask.Scoop) s.ScoopOut(); else if (what == Ask.Patch) s.Patch();
        }

        void Handle(ulong from, Ask what, byte boat = 0)
        {
            if (!nm.IsServer) return;
            var s = Skiff.At(boat); var e = Find(from); if (e == null) return;
            ref ulong BoatOwner = ref Owner[boat < Owner.Length ? boat : 0];
            switch (what)
            {
                case Ask.Helm:
                    // the tiller is free unless someone else sits at it: the boat (and its feel) moves to the driver
                    if (s != null && (BoatOwner == from || !s.AtHelm)) { BoatOwner = from; Log($"{e.Name} has the tiller"); }
                    break;
                case Ask.LeaveHelm:
                    if (BoatOwner == from && from != nm.LocalClientId) { BoatOwner = nm.LocalClientId; Log($"{e.Name} left the tiller: the boat is the host's"); }
                    break;
                case Ask.Anchor:
                    if (BoatOwner == nm.LocalClientId) s?.ToggleAnchor();
                    else { ulong to = BoatOwner; SendTo(to, "pb.ask", w => { w.WriteValueSafe((byte)Ask.Anchor); w.WriteValueSafe(boat); }, NetworkDelivery.ReliableSequenced); }
                    break;
                case Ask.Scoop: case Ask.Patch:                       // M3 leaks: the water belongs to the real boat
                    if (BoatOwner == nm.LocalClientId) { if (what == Ask.Scoop) s?.ScoopOut(); else s?.Patch(); }
                    else { ulong to = BoatOwner; SendTo(to, "pb.ask", w => { w.WriteValueSafe((byte)what); w.WriteValueSafe(boat); }, NetworkDelivery.ReliableSequenced); }
                    break;
                case Ask.NextCrew:
                    e.Crew = FreeCrew(e.Crew + 1, from); Log($"{e.Name} is now {CrewName(e.Crew)}");
                    break;
                default:
                    if (what >= Ask.Crew0)   // (M5) the wardrobe: one crew member each (two players can't both be Pip)
                    {
                        int c = what - Ask.Crew0;
                        if (Roster.Exists(x => x.Crew == c && x.Id != from)) Log($"{e.Name} can't be {CrewName(c)}: taken");
                        else
                        {
                            e.Crew = (byte)c; Log($"{e.Name} is now {CrewName(c)} (wardrobe)");
                            var o = Firm.C?.OutfitOf(e.Key ?? MyKey(e.Name)); if (o != null && o.Preset != c) { o.Preset = c; o.Reset(); Firm.I.Dirty = true; }   // (their clothes: the new crew member's own)
                        }
                    }
                    break;
            }
            Broadcast(); Apply();
        }

        // ---------------------------------------------------------------- poses
        public static float Now => Time.realtimeSinceStartup;

        // The waves' clock (Sea.T): solo, the game's own time; in co-op the host's, on every machine, so a swimmer bobs
        // and the boat rolls on the same waves everywhere. The host sends its clock every second.
        public static float SeaTime(float solo) => On ? Now + (IsHost ? 0 : I.hostShift) : solo;

        void OnClock(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out float t);
            Deliver(() => { float d = t - Now; hostShift = clockSet ? Mathf.Max(hostShift, d) : d; clockSet = true; }, false);
        }

        // host: a crewmate whose game died (no goodbye) goes after Silence s of real time without a pose (they send ~Rate a
        // second, also alt-tabbed: runInBackground), so we stop sending to their closed port: each packet there costs the
        // transport a receive buffer (CoopSession.MakeTransport). A rejoin gets its hotbar and place back as after any leave.
        const float Silence = 8f;
        readonly Dictionary<ulong, float> heard = new();
        void DropSilent()
        {
            float now = Time.realtimeSinceStartup; ulong gone = ulong.MaxValue;
            foreach (var kv in heard) if (now - kv.Value > Silence) { gone = kv.Key; break; }
            if (gone == ulong.MaxValue) return;
            heard.Remove(gone);
            if (!nm.ConnectedClients.ContainsKey(gone)) return;   // left already
            Log($"{Find(gone)?.Name ?? "client " + gone} sent nothing for {Silence:0} s: their game is gone, dropping them");
            nm.DisconnectClient(gone);
        }

        void Update()
        {
            if (nm == null || !nm.IsListening) return;
            // the lag simulator's inbox
            for (int i = 0; i < inbox.Count; i++) if (inbox[i].at <= Now) { var a = inbox[i].a; inbox.RemoveAt(i--); a(); }
            clockT -= Time.unscaledDeltaTime;
            if (nm.IsServer && clockT <= 0)
            {
                clockT = 1; float t = Now;
                foreach (var id in nm.ConnectedClientsIds) if (id != nm.LocalClientId) SendTo(id, "pb.clock", w => w.WriteValueSafe(t), NetworkDelivery.Unreliable);
            }
            if (nm.IsServer) { Tangles(Time.deltaTime); TickAir(); TickDay(); DropSilent(); }
            TickFirm();
            TickVoice();
            sendT -= Time.unscaledDeltaTime;
            if (sendT > 0) return;
            sendT += 1f / Rate; if (sendT < 0) sendT = 0;
            var me = Angler.I;
            if (me != null && (nm.IsServer || nm.IsConnectedClient))
            {
                var p = me.NetPose(Now);
                if (nm.IsServer) Relay(nm.LocalClientId, p); else Send("pb.pose", w => p.Write(ref w), NetworkDelivery.UnreliableSequenced);
            }
            foreach (var b in Skiff.All) if (Owner[b.Index] == nm.LocalClientId && !b.Remote) SendBoat(b.NetPose(Now));
        }

        void OnPose(ulong from, FastBufferReader r)
        {
            if (nm.IsServer) heard[from] = Time.realtimeSinceStartup;
            ulong who = from;
            if (!nm.IsServer) r.ReadValueSafe(out who);
            var p = Angler.Pose.Read(ref r);
            Deliver(() =>
            {
                if (nm.IsServer) Relay(who, p);
                if (Mates.TryGetValue(who, out var m) && m != null) m.Net.Add(p.T, p);
            }, false);
        }

        // host: a player's pose to everyone but that player
        void Relay(ulong who, Angler.Pose p)
        {
            foreach (var id in nm.ConnectedClientsIds)
                if (id != nm.LocalClientId && id != who) SendTo(id, "pb.pose", w => { w.WriteValueSafe(who); p.Write(ref w); }, NetworkDelivery.UnreliableSequenced);
        }

        // The boat's pose carries who sent it: each driver stamps it with their own clock, so a copy follows only the
        // current owner's, and starts afresh when the tiller changes hands (it jumped and nudged the riders).
        void SendBoat(Skiff.Pose p)
        {
            if (nm.IsServer) RelayBoat(nm.LocalClientId, p);
            else Send("pb.boat", w => p.Write(ref w), NetworkDelivery.UnreliableSequenced);
        }
        void RelayBoat(ulong owner, Skiff.Pose p)
        {
            foreach (var id in nm.ConnectedClientsIds)
                if (id != nm.LocalClientId && id != owner) SendTo(id, "pb.boat", w => { w.WriteValueSafe(owner); p.Write(ref w); }, NetworkDelivery.UnreliableSequenced);
        }

        void OnBoat(ulong from, FastBufferReader r)
        {
            ulong owner = from;
            if (!nm.IsServer) r.ReadValueSafe(out owner);
            var p = Skiff.Pose.Read(ref r);
            Deliver(() =>
            {
                int bi = p.Flags >> 4; if (bi >= Owner.Length || owner != Owner[bi]) return;
                if (nm.IsServer) RelayBoat(owner, p);
                var s = Skiff.At(bi); if (s != null && s.Remote) s.Net.Add(p.T, p);
            }, false);
        }
        readonly ulong[] shownOwner = { ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue };

        // ---------------------------------------------------------------- fishing (gdd fishing §4.2, §4.4)
        // The host decides the fish: each cast asks it, and it rolls the species, size and weight then (the answer is back
        // long before the bite, ≥ 4 s later). The angler's own machine runs the minigame and tells the host what it landed
        // and when each stop was pressed; the host checks that against its roll (each stop ≥ 120 ms after the last, ≤ 5).
        readonly Dictionary<ulong, (int id, FishSpecies sp, float cm, float kg, ulong fish)> rolls = new();
        public int Checked, Flagged, TangleCount;       // (tests)

        public static void AskRoll(int id, int rod, string bait)
        {
            if (!On) return;
            if (IsHost) { var r = I.Roll(I.nm.LocalClientId, id, rod, bait); Angler.I?.Fish.Rolled(id, r.sp, r.cm, r.kg, r.fish); }
            else I.Send("pb.roll", w => { w.WriteValueSafe(id); w.WriteValueSafe((byte)rod); w.WriteValueSafe(bait); }, NetworkDelivery.ReliableSequenced);
        }

        (int id, FishSpecies sp, float cm, float kg, ulong fish) Roll(ulong who, int id, int rod, string bait)
        {
            var sp = FishDb.Roll(Fishing.Rng, rod, bait); FishDb.RollSize(Fishing.Rng, sp, out var cm, out var kg);
            Book.FreeHands(who); // the last cast's fish never came up
            var f = Book.Make(who, sp, 0, cm, kg, (float)Sky.Minutes); // in the book from the cast: in their hands once it's landed
            return rolls[who] = (id, sp, cm, kg, f.Id);
        }

        void OnRoll(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out int id); r.ReadValueSafe(out byte rod); r.ReadValueSafe(out string bait);
            Deliver(() =>
            {
                var x = Roll(from, id, rod, bait);
                SendTo(from, "pb.rolled", w => { w.WriteValueSafe(id); w.WriteValueSafe((short)FishDb.All.IndexOf(x.sp)); w.WriteValueSafe(x.cm); w.WriteValueSafe(x.kg); w.WriteValueSafe(x.fish); }, NetworkDelivery.ReliableSequenced);
            }, true);
        }

        void OnRolled(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out int id); r.ReadValueSafe(out short sp); r.ReadValueSafe(out float cm); r.ReadValueSafe(out float kg); r.ReadValueSafe(out ulong fish);
            Deliver(() => { if (sp >= 0 && sp < FishDb.All.Count) Angler.I?.Fish.Rolled(id, FishDb.All[sp], cm, kg, fish); }, true);
        }

        public static void Landed(int id, Fish f, List<double> stops)
        {
            if (!On) return;
            short sp = (short)FishDb.All.IndexOf(f.Species); var st = stops.ToArray();
            if (IsHost) I.Check(I.nm.LocalClientId, id, sp, f.Cm, f.Kg, f.Grade, st);
            else I.Send("pb.landed", w =>
            {
                w.WriteValueSafe(id); w.WriteValueSafe(sp); w.WriteValueSafe(f.Cm); w.WriteValueSafe(f.Kg); w.WriteValueSafe((byte)f.Grade);
                w.WriteValueSafe((byte)st.Length); foreach (var t in st) w.WriteValueSafe(t);
            }, NetworkDelivery.ReliableSequenced);
        }

        void OnLanded(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out int id); r.ReadValueSafe(out short sp); r.ReadValueSafe(out float cm); r.ReadValueSafe(out float kg); r.ReadValueSafe(out byte grade);
            r.ReadValueSafe(out byte n); var st = new double[n]; for (int i = 0; i < n; i++) r.ReadValueSafe(out st[i]);
            Deliver(() => Check(from, id, sp, cm, kg, grade, st), true);
        }

        void Check(ulong from, int id, short sp, float cm, float kg, int grade, double[] stops)
        {
            string why = null;
            if (!rolls.TryGetValue(from, out var x) || x.id != id) why = "not the cast the host rolled";
            else if (FishDb.All.IndexOf(x.sp) != sp || Mathf.Abs(x.cm - cm) > 0.01f || Mathf.Abs(x.kg - kg) > 0.001f) why = "not the fish the host rolled";
            else if (stops.Length > 5) why = stops.Length + " stops";
            else if (grade > 1 || grade == 1 && stops.Length == 0) why = "grade " + grade + " without a stop";
            else for (int i = 1; i < stops.Length; i++) if (stops[i] - stops[i - 1] < 0.12) { why = $"stops {(stops[i] - stops[i - 1]) * 1000:F0} ms apart"; break; }
            Checked++; if (why != null) Flagged++;
            if (x.fish != 0 && Book.All.TryGetValue(x.fish, out var bf)) bf.Grade = why == null ? Mathf.Clamp(grade, 0, 1) : 0; // the book's fish: as fought (a flagged one: Basic)
            var e = Find(from);
            Log($"{e?.Name ?? from.ToString()} landed a {(sp >= 0 && sp < FishDb.All.Count ? FishDb.All[sp].Name : "?")} {cm:F0} cm {kg:F2} kg, {Ponyo.GradeName[Mathf.Clamp(grade, 0, Ponyo.GradeName.Length - 1)]}, {stops.Length} stop(s): {(why == null ? "check ok" : "FLAGGED: " + why)}");
        }

        // Tangles (host): any two lines in the water that cross (Fishing.Crossed) for over a second, both reel in.
        readonly Dictionary<(ulong, ulong), float> crossT = new();
        readonly List<(ulong id, Fishing f)> lines = new();
        void Tangles(float dt)
        {
            lines.Clear();
            if (Angler.I != null && Angler.I.Fish != null && Angler.I.Fish.LineOut) lines.Add((nm.LocalClientId, Angler.I.Fish));
            foreach (var kv in Mates) if (kv.Value != null && kv.Value.Fish != null && kv.Value.Fish.LineOut) lines.Add((kv.Key, kv.Value.Fish));
            for (int i = 0; i < lines.Count; i++)
                for (int j = i + 1; j < lines.Count; j++)
                {
                    var key = lines[i].id < lines[j].id ? (lines[i].id, lines[j].id) : (lines[j].id, lines[i].id);
                    float t = Fishing.Crossed(lines[i].f, lines[j].f) ? (crossT.TryGetValue(key, out var c) ? c : 0) + dt : 0;
                    crossT[key] = t;
                    if (t <= 1f) continue;
                    crossT[key] = 0; TangleCount++;
                    Log($"tangled: {Find(key.Item1)?.Name} + {Find(key.Item2)?.Name}");
                    foreach (var id in new[] { key.Item1, key.Item2 })
                        if (id == nm.LocalClientId) Angler.I.Fish.Tangle();
                        else SendTo(id, "pb.tangle", w => { }, NetworkDelivery.ReliableSequenced);
                }
        }

        void OnTangle(ulong from, FastBufferReader r) { Deliver(() => Angler.I?.Fish.Tangle(), true); }

        // ---------------------------------------------------------------- plumbing
        // (FastBufferWriter is a handle: a copy writes into the same buffer)
        void Send(string msg, Action<FastBufferWriter> fill, NetworkDelivery d) => SendTo(NetworkManager.ServerClientId, msg, fill, d);
        void SendTo(ulong id, string msg, Action<FastBufferWriter> fill, NetworkDelivery d)
        {
            var w = new FastBufferWriter(256, Allocator.Temp, 4096);
            try { fill(w); BytesOut += w.Length; nm.CustomMessagingManager.SendNamedMessage(msg, id, w, d); } finally { w.Dispose(); }
        }

        // bytes this machine has sent and received in our messages (the soak test's bandwidth; NGO's own headers aside)
        public static long BytesOut, BytesIn;
        static void Reg(CustomMessagingManager m, string name, CustomMessagingManager.HandleNamedMessageDelegate h)
            => m.RegisterNamedMessageHandler(name, (from, r) => { BytesIn += r.Length; h(from, r); });

        // what arrives is applied now, or after the simulated lag (and an unreliable message may be lost)
        void Deliver(Action a, bool reliable)
        {
            if (simDelay <= 0 && simLoss <= 0) { a(); return; }
            if (!reliable && UnityEngine.Random.value < simLoss) return;
            inbox.Add((Now + simDelay, a));
        }
    }

    // Snapshots of something remote, shown Coop.Delay in the past: the two either side of that moment, blended. The
    // sender's clock is mapped onto ours by the smallest gap seen (the least-delayed packet).
    public class NetTrack<T> where T : struct
    {
        readonly List<(float t, T v)> buf = new(); float offset = float.MaxValue;
        public int Count => buf.Count;
        public float Age => buf.Count > 0 ? Coop.Now - (buf[^1].t + offset) : 99;
        public void Add(float t, T v)
        {
            offset = Mathf.Min(offset, Coop.Now - t);
            if (buf.Count > 0 && t <= buf[^1].t) return; // late or doubled
            buf.Add((t, v)); if (buf.Count > 40) buf.RemoveAt(0);
        }
        public void Clear() { buf.Clear(); offset = float.MaxValue; }
        // (a, b, k): blend a -> b by k; false when nothing has arrived yet
        public bool Sample(out T a, out T b, out float k)
        {
            a = b = default; k = 0;
            if (buf.Count == 0) return false;
            float t = Coop.Now - offset - Coop.Delay;
            if (t <= buf[0].t) { a = b = buf[0].v; return true; }
            for (int i = buf.Count - 1; i > 0; i--)
                if (buf[i - 1].t <= t) { a = buf[i - 1].v; b = buf[i].v; k = i == buf.Count - 1 && t > buf[i].t ? 1 : Mathf.InverseLerp(buf[i - 1].t, buf[i].t, t); if (t > buf[i].t) k = 1; return true; }
            a = b = buf[^1].v; return true;
        }
    }
}
