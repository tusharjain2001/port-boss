using System;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace PortBoss.Net
{
    // Session start/stop + Steam lobby on app 480.
    // Solo = host bound to loopback. Co-op = Steam lobby + NGO over Steam relay (FacepunchTransport).
    // LAN mode (UnityTransport) exists for same-PC testing: one Steam account can't P2P-connect to itself.
    public static class Session
    {
        public const string LobbyKey = "portboss", LobbyVal = "pb-0.1";
        public const ushort Port = 7777;
        public static bool SteamOk;
        public static Lobby? Lobby;
        public static string Status = "";
        public static bool Solo;

        static NetworkManager NM => NetworkManager.Singleton;
        static UnityTransport Utp => NM.GetComponent<UnityTransport>();
        static FacepunchTransport Steam => NM.GetComponent<FacepunchTransport>();

        public static void InitSteam()
        {
            try
            {
                SteamClient.Init(480, false);
                SteamOk = true;
                SteamNetworkingUtils.InitRelayNetworkAccess();
                SteamFriends.OnGameLobbyJoinRequested += (l, friend) => _ = JoinLobby(l);
                Debug.Log($"[PB] Steam ok: {SteamClient.Name} {SteamClient.SteamId}");
            }
            catch (Exception e)
            {
                Status = "Steam isn't running, co-op over Steam is off (solo and LAN still work).";
                Debug.LogWarning("[PB] Steam init failed: " + e.Message);
            }
        }

        public static void Tick() { if (SteamOk) SteamClient.RunCallbacks(); }

        public static void ShutdownSteam() { if (SteamOk) { Lobby?.Leave(); SteamClient.Shutdown(); SteamOk = false; } }

        public static bool StartSolo()
        {
            Solo = true;
            NM.NetworkConfig.NetworkTransport = Utp;
            for (ushort p = Port; p < Port + 20; p++)
            {
                Utp.SetConnectionData("127.0.0.1", p, "127.0.0.1");
                if (NM.StartHost()) return true;
                NM.Shutdown();
            }
            Status = "Couldn't start the game (no free local port).";
            return false;
        }

        public static async Task HostCoop(bool lan)
        {
            Solo = false;
            if (lan || !SteamOk)
            {
                NM.NetworkConfig.NetworkTransport = Utp;
                Utp.SetConnectionData("127.0.0.1", Port, "0.0.0.0");
            }
            else NM.NetworkConfig.NetworkTransport = Steam;
            if (!NM.StartHost()) { Status = "Couldn't host (is another copy already hosting?)."; return; }
            Debug.Log($"[PB] hosting via {(NM.NetworkConfig.NetworkTransport == Utp ? "LAN" : "Steam")}");
            if (!SteamOk) return;
            var l = await SteamMatchmaking.CreateLobbyAsync(4);
            if (l == null) { Status = "Steam lobby failed; LAN still works."; return; }
            var lobby = l.Value;
            lobby.SetPublic();
            lobby.SetJoinable(true);
            lobby.SetData(LobbyKey, LobbyVal);
            lobby.SetData("name", SteamClient.Name + "'s port");
            lobby.SetData("mode", lan ? "lan" : "steam");
            Lobby = lobby;
            Debug.Log($"[PB] lobby created {lobby.Id} owner {SteamClient.SteamId} mode {(lan ? "lan" : "steam")}");
        }

        public static void Invite()
        {
            if (SteamOk && Lobby != null) SteamFriends.OpenGameInviteOverlay(Lobby.Value.Id);
        }

        public static async Task<Lobby[]> FindLobbies()
        {
            if (!SteamOk) return Array.Empty<Lobby>();
            var r = await SteamMatchmaking.LobbyList.WithKeyValue(LobbyKey, LobbyVal).FilterDistanceWorldwide().WithMaxResults(20).RequestAsync();
            return r ?? Array.Empty<Lobby>();
        }

        public static async Task JoinLobby(Lobby l)
        {
            if (NM.IsListening) return;
            Solo = false;
            var r = await l.Join();
            if (r != RoomEnter.Success) { Status = "Couldn't join that lobby: " + r; return; }
            Lobby = l;
            var owner = l.Owner.Id;
            bool lan = l.GetData("mode") == "lan";
            Debug.Log($"[PB] joined lobby {l.Id} members {l.MemberCount} owner {owner} mode {(lan ? "lan" : "steam")}");
            bool forceSteam = Array.IndexOf(Environment.GetCommandLineArgs(), "-pb-force-steam") >= 0; // test hook
            if (!forceSteam && (lan || owner == SteamClient.SteamId))
            {
                // Same Steam account on this PC (testing) or a LAN-mode host: connect locally.
                JoinLan("127.0.0.1");
                return;
            }
            NM.NetworkConfig.NetworkTransport = Steam;
            Steam.TargetSteamId = owner;
            if (!NM.StartClient()) Status = "Couldn't connect to host.";
        }

        public static void JoinLan(string ip)
        {
            Solo = false;
            NM.NetworkConfig.NetworkTransport = Utp;
            Utp.SetConnectionData(ip, Port);
            if (!NM.StartClient()) Status = "Couldn't connect to " + ip;
        }

        public static void Leave()
        {
            if (NM != null && NM.IsListening) NM.Shutdown();
            Lobby?.Leave();
            Lobby = null;
        }
    }
}
