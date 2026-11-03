using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;

namespace PortBoss.M1
{
    // M2 step 6: how a crew comes together (gdd systems §3, §5.2). Steam (app 480 until Manan's own app): the host makes a
    // friends-only lobby, friends join through an invite (the Steam overlay), the friends list's "Join game", or the
    // title menu's list of friends hosting; then NGO runs over Steam's relay (PortBoss.Net.FacepunchTransport, kept from
    // the old game). LAN: Unity Transport to an address. Anyone can drop in (up to 4); whoever leaves and comes back (same
    // Steam account, or the same name on LAN) gets their hotbar, crew member, colour and place back.
    public partial class Coop
    {
        public const string LobbyKey = "portboss", LobbyVal = "pb-m2";
        public const int MaxCrew = 4;
        public static bool SteamOk;                              // Steam is running here (lobbies, invites, the mic)
        public static Lobby? Lobby;                              // the lobby we host or joined (Steam)
        public static string Status = "";                        // the last thing worth telling the title menu
        public static bool Joined => On && (IsHost || I.nm.IsConnectedClient);
        public bool ViaSteam { get; private set; }

        struct Plan { public bool Host, Steam; public string Join; public ushort Port; public ulong SteamHost; }
        Plan plan;

        // ---------------------------------------------------------------- Steam (once, for the lobby and the mic)
        public static void SteamUp()
        {
            if (SteamOk || Array.IndexOf(Environment.GetCommandLineArgs(), "-pb-voicetest") >= 0) return; // (tests: no Steam)
            try
            {
                SteamClient.Init(480, false);
                SteamOk = true;
                SteamNetworkingUtils.InitRelayNetworkAccess();
                SteamUser.SampleRate = VoiceOut.SteamRate;
                // an invite accepted (overlay) or "Join game" in the friends list, while the game runs
                SteamFriends.OnGameLobbyJoinRequested += (l, friend) => { Debug.Log($"[PB] coop: {new Friend(friend).Name} invited us"); _ = JoinLobby(l); };
                SteamFriends.OnGameOverlayActivated += on => Log("invite: Steam's overlay " + (on ? "opened" : "closed"));
                Debug.Log($"[PB] coop: Steam ok: {SteamClient.Name}");
                // launched from an invite while the game wasn't running: +connect_lobby <id>. Only once Port Boss has its own
                // Steam app ID: on test app 480 Steam starts Spacewar instead (code audit 3 Nov), so start the game first.
                var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "+connect_lobby");
                if (i >= 0 && i + 1 < a.Length && ulong.TryParse(a[i + 1], out var id)) _ = JoinLobby(new Lobby(id));
            }
            catch (Exception e) { Debug.Log("[PB] coop: Steam isn't running (" + e.Message + "): solo and LAN still work"); }
        }
        public static void SteamTick()
        {
            if (!SteamOk) return;
            SteamClient.RunCallbacks();
            // the overlay hooks the game a few seconds after Steam starts, or never (see Invite): say which, once
            if (!overlaySaid && Time.realtimeSinceStartup > 8) { overlaySaid = true; Log($"invite: Steam overlay enabled: {SteamUtils.IsOverlayEnabled} (graphics {SystemInfo.graphicsDeviceType})"); }
        }
        static bool overlaySaid;
        // a crew that didn't happen: the title menu's line, the log and a left alert (the menu may be closed by now)
        static void Fail(string why) { Status = why; Log(why); Alerts.Push("Crew", why, Alerts.Kind.Warning); }
        public static string MyName
        {
            get
            {
                var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-pb-name");
                if (i >= 0 && i + 1 < a.Length) return a[i + 1];
                return SteamOk ? SteamClient.Name : PlayerPrefs.GetString("m1.name", Environment.MachineName); // (LAN: unique per PC, so a rejoin finds its own key)
            }
        }
        // who I am across a rejoin: my Steam account, else (LAN, tests) my name
        public static string MyKey(string name) => SteamOk ? "steam:" + SteamClient.SteamId.Value : "name:" + name;
        public static string LocalKey => MyKey(MyName);   // M4: my kit and my things in a save are kept under this

        // ---------------------------------------------------------------- starting (the title menu, the pause menu)
        static Coop Make(string name)
        {
            var g = new GameObject("Coop"); DontDestroyOnLoad(g);
            I = g.AddComponent<Coop>(); I.myName = name; return I;
        }
        public static void HostLan(ushort port = Port) { if (I != null) return; Make(MyName).plan = new Plan { Host = true, Port = port }; }
        public static void JoinLan(string address) { if (I != null) return; Make(MyName).plan = new Plan { Join = address, Port = Port }; }
        public static void HostSteam() { if (I != null || !SteamOk) return; Make(MyName).plan = new Plan { Host = true, Steam = true, Port = Port }; }

        public static async Task JoinLobby(Lobby l)
        {
            if (!SteamOk) return;
            try
            {
                if (I != null) Leave(); // (an invite while in another crew: leave it for this one)
                var r = await l.Join();
                if (r != RoomEnter.Success) { Fail("Couldn't join that crew (" + r + ")."); return; }
                if (l.GetData(LobbyKey) != LobbyVal) { l.Leave(); Fail("That isn't a Port Boss crew (or another version)."); return; }
                Lobby = l; var owner = l.Owner.Id;
                Debug.Log($"[PB] coop: joined the lobby of {l.Owner.Name} ({l.MemberCount} in it)");
                // my own crew (the same Steam account twice on one PC): Steam can't connect to itself, and a Steam crew has
                // no LAN side, so say how to test on one PC instead
                if (owner == SteamClient.SteamId) { l.Leave(); Lobby = null; Fail("That's your own crew (the same Steam account). On one PC, use Host / Join on this network."); return; }
                Make(MyName).plan = new Plan { Steam = true, SteamHost = owner };
                Title.Close();
            }
            catch (Exception e) { Debug.LogException(e); Fail("Couldn't join that crew (" + e.Message + ")."); } // (a fire-and-forget Task: never lose why)
        }

        // Friends hosting a Port Boss crew right now (their game has a lobby): the title menu's "Join a friend".
        public static List<(string name, Lobby lobby)> FriendsHosting()
        {
            var list = new List<(string, Lobby)>();
            if (!SteamOk) return list;
            foreach (var f in SteamFriends.GetFriends())
            {
                var g = f.GameInfo;
                if (f.IsPlayingThisGame && g.HasValue && g.Value.Lobby is Lobby l) list.Add((f.Name, l));
            }
            return list;
        }

        // ---------------------------------------------------------------- invites (M3.1, Manan's co-op test 3 Nov)
        // I, any time in a crew (the host or a crewmate: any lobby member may invite), opens the in-game invite list
        // (Title's Invite page): Steam friends online, each invited straight through the lobby (Lobby.InviteFriend, no
        // overlay needed: it pops up in their Steam chat), Steam's own invite window when its overlay is on, and the crew
        // code a friend can type in "Join a friend on Steam". Why: PortBoss.exe started outside Steam gets no overlay
        // (Steam starts after Unity's graphics device), so OpenGameInviteOverlay alone did nothing (Manan's Player.log).
        public static List<(string name, string sub, ulong id)> TestFriends; // -pb-invitetest: made-up friends
        public static ulong TestLobby;                                          // -pb-invitetest: a made-up lobby id
        static ulong LobbyId => Lobby?.Id.Value ?? TestLobby;
        public static bool OverlayOn => SteamOk && SteamUtils.IsOverlayEnabled;
        public static string Code => LobbyId != 0 ? CodeOf(LobbyId) : null;

        // the crew code: a lobby id's low 32 bits in Crockford base 32 (every Steam lobby id has the same top 32 bits),
        // "K7Q-4MXA"; an id with other top bits is written whole (13 characters)
        const ulong LobbyHigh = 0x0186000000000000UL;
        const string B32 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        public static string CodeOf(ulong id)
        {
            bool low = (id & 0xFFFFFFFF00000000UL) == LobbyHigh;
            ulong v = low ? id & 0xFFFFFFFFUL : id; var s = "";
            do { s = B32[(int)(v % 32)] + s; v /= 32; } while (v > 0);
            s = s.PadLeft(low ? 7 : 13, '0');
            return s.Substring(0, 3) + "-" + s.Substring(3);
        }
        public static ulong IdOf(string code)
        {
            ulong v = 0; int n = 0;
            foreach (var ch in code.ToUpperInvariant())
            {
                char c = ch == 'O' ? '0' : ch == 'I' || ch == 'L' ? '1' : ch; // (misread letters, as Crockford says)
                int d = B32.IndexOf(c); if (d < 0) continue;                    // (the dash, spaces)
                v = v * 32 + (ulong)d; n++;
            }
            return n == 0 ? 0 : n <= 7 ? LobbyHigh | v : v;
        }
        public static void JoinCode(string code)
        {
            ulong id = IdOf(code); Log($"invite: joining by crew code {code} -> lobby {id}");
            if (id == 0) { Fail("Type the crew code your friend sees in their pause menu."); return; }
            _ = JoinLobby(new Lobby(id));
        }

        public static void Invite()
        {
            Log($"invite: I pressed (in a crew {On}, host {IsHost}, Steam {SteamOk}, lobby {LobbyId}, overlay enabled {(SteamOk ? SteamUtils.IsOverlayEnabled.ToString() : "n/a")})");
            if (!On) { Alerts.Push("Crew", "You're playing solo. To invite friends, host a crew from the title menu.", Alerts.Kind.Info); return; }
            if (Lobby is Lobby l && OverlayOn) { SteamFriends.OpenGameInviteOverlay(l.Id); Log($"invite: Steam's invite window asked for (lobby {l.Id.Value})"); }
            else if (LobbyId != 0) Alerts.Push("Crew", $"Steam's invite window can't open here (its overlay isn't running in this game). Invite from the list, or give friends the crew code {Code}.", Alerts.Kind.Warning);
            else Alerts.Push("Crew", $"This crew is on your home network, so no Steam invites: friends pick Join on this network and type {LanAddress}.", Alerts.Kind.Info);
            Title.InviteList();
        }
        // what a friend on this network types: my address (the host), else the one I joined
        public static string LanAddress => IsHost ? Title.LocalAddress() : I?.plan.Join ?? "";

        // Steam friends to invite: online, not in the crew already; the ones in Port Boss first
        public static List<(string name, string sub, ulong id)> FriendsToInvite()
        {
            if (TestFriends != null) return TestFriends;
            var list = new List<(string name, string sub, ulong id)>();
            if (!SteamOk || Lobby == null) return list;
            var inCrew = new HashSet<ulong>(); foreach (var m in Lobby.Value.Members) inCrew.Add(m.Id.Value);
            foreach (var f in SteamFriends.GetFriends())
                if ((f.IsOnline || f.IsPlayingThisGame) && !inCrew.Contains(f.Id.Value)) list.Add((f.Name, f.IsPlayingThisGame ? "Playing now" : "Online", f.Id.Value));
            list.Sort((a, b) => a.sub != b.sub ? (a.sub == "Online" ? 1 : -1) : string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            Log($"invite: {list.Count} friend(s) online to invite");
            return list;
        }
        public static void InviteFriend(string name, ulong id)
        {
            bool sent = Lobby is Lobby l && l.InviteFriend(id) || TestFriends != null;
            Log($"invite: {name} ({id}) invited through the lobby: {(sent ? "sent" : "FAILED")}");
            if (sent) Alerts.Push("Crew", $"Invite sent to {name}. It pops up in their Steam chat: with Port Boss open, they click Join.", Alerts.Kind.Info);
            else Alerts.Push("Crew", $"Steam wouldn't send the invite to {name}. Give them the crew code {Code} instead.", Alerts.Kind.Warning);
        }

        // Back to playing alone: the crew goes, my boat and hotbar stay mine.
        public static void Leave()
        {
            var c = I; if (c == null) return;
            Log("left the crew");
            if (IsHost) Save.Write("host left");                // the crew's world, as they leave it
            foreach (var a in c.Mates.Values) if (a != null) Destroy(a.gameObject);
            c.Mates.Clear(); c.Roster.Clear();
            if (Skiff.I != null && Skiff.I.Remote) Skiff.I.TakeOver();
            Lobby?.Leave(); Lobby = null;
            if (c.nm != null && c.nm.IsListening) c.nm.Shutdown();
            Destroy(c.gameObject); I = null;
        }

        void MakeTransport(Plan p, out bool ok)
        {
            ViaSteam = p.Steam;
            if (p.Steam)
            {
                var ft = gameObject.AddComponent<PortBoss.Net.FacepunchTransport>(); ft.TargetSteamId = p.SteamHost;
                nm.NetworkConfig.NetworkTransport = ft;
                ok = p.Host ? nm.StartHost() : nm.StartClient();
                if (ok && p.Host) _ = MakeLobby();
                return;
            }
            if (p.Host) { utp.SetConnectionData("127.0.0.1", p.Port, "0.0.0.0"); ok = nm.StartHost(); }
            else { var hp = p.Join.Split(':'); utp.SetConnectionData(hp[0], hp.Length > 1 && ushort.TryParse(hp[1], out var jp) ? jp : p.Port); ok = nm.StartClient(); }
        }

        async Task MakeLobby()
        {
            try
            {
                var l = await SteamMatchmaking.CreateLobbyAsync(MaxCrew);
                if (l == null) { Fail("Steam couldn't make a lobby, so friends can't join. Host on this network still works."); return; }
                var lobby = l.Value;
                lobby.SetFriendsOnly(); lobby.SetJoinable(true);
                lobby.SetData(LobbyKey, LobbyVal); lobby.SetData("name", myName + "'s crew");
                Lobby = lobby; Log($"lobby made: friends can join (invite them from the pause menu: I)");
            }
            catch (Exception e) { Debug.LogException(e); Fail("Steam couldn't make a lobby (" + e.Message + ")."); }
        }

        // ---------------------------------------------------------------- rejoin (host)
        class Away { public ulong Id; public byte Crew, Slot; public Vector3 At; public float Yaw; public bool Boat; }
        readonly Dictionary<string, Away> away = new();
        public static bool Placed;                               // (tests) the host put me back where I was

        // a player left: keep what they had under their key
        void KeepAway(Entry e)
        {
            if (string.IsNullOrEmpty(e.Key)) return;
            var a = new Away { Id = e.Id, Crew = e.Crew, Slot = e.Slot };
            var s = Skiff.I;
            if (Mates.TryGetValue(e.Id, out var m) && m != null)
            {
                a.Boat = m.OnBoat && s != null;
                a.At = a.Boat ? s.transform.InverseTransformPoint(m.transform.position) : m.transform.position;
                a.Yaw = m.transform.eulerAngles.y - (a.Boat ? s.transform.eulerAngles.y : 0);
            }
            Book.FreeHands(e.Id);
            away[e.Key] = a;
            Log($"{e.Name} left: their hotbar ({Book.Bar(e.Id).FishCount} fish) and place are kept for them");
        }

        // a player came (back): their old hotbar, slot, crew member and place if we kept them
        Away Back(string key, ulong id)
        {
            if (string.IsNullOrEmpty(key) || !away.TryGetValue(key, out var a)) return null;
            away.Remove(key);
            Book.Rekey(a.Id, id);
            return a;
        }

        void SendPlace(ulong to, Away a)
        {
            SendTo(to, "pb.place", w => { w.WriteValueSafe(a.Boat); w.WriteValueSafe(a.At); w.WriteValueSafe(a.Yaw); }, NetworkDelivery.ReliableSequenced);
        }
        void OnPlace(ulong from, FastBufferReader r)
        {
            r.ReadValueSafe(out bool boat); r.ReadValueSafe(out Vector3 at); r.ReadValueSafe(out float yaw);
            Deliver(() =>
            {
                var me = Angler.I; var s = Skiff.I; if (me == null) return;
                var p = boat && s != null ? s.transform.TransformPoint(at) : at;
                me.Teleport(p + Vector3.up * 0.05f, yaw + (boat && s != null ? s.transform.eulerAngles.y : 0));
                Placed = true; Log("back where I was");
                Alerts.Push("Crew", "Welcome back. Your fish are where you left them.", Alerts.Kind.Crew);
            }, true);
        }
    }
}
