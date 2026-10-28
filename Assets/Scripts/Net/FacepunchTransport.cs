using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;

namespace PortBoss.Net
{
    /// <summary>
    /// Netcode for GameObjects transport over Steam Networking Sockets (relay) via Facepunch.Steamworks.
    /// Steam must already be initialised elsewhere (SteamClient.Init / RunCallbacks); this class never inits or shuts Steam down.
    /// Transport client ids: the host is 0 (ServerClientId); remote clients use their Steam connection Id (never 0).
    /// </summary>
    public class FacepunchTransport : NetworkTransport, ISocketManager, IConnectionManager
    {
        [Tooltip("SteamID of the host this client connects to.")]
        public ulong TargetSteamId;

        const string Tag = "[SteamTransport]";

        struct TransportEvent
        {
            public NetworkEvent Type;
            public uint Connection;
            public byte[] Data;
        }

        // Steam callbacks may fire off the main thread (async callbacks), so they only enqueue;
        // everything else happens on the main thread in OnEarlyUpdate.
        readonly ConcurrentQueue<TransportEvent> m_Events = new ConcurrentQueue<TransportEvent>();
        readonly HashSet<uint> m_Clients = new HashSet<uint>(); // server only: fully connected remote clients
        SocketManager m_Server;
        ConnectionManager m_Client;

        public override ulong ServerClientId => 0;

        public override void Initialize(NetworkManager networkManager = null) { }

        public override bool StartServer()
        {
            if (!SteamClient.IsValid)
            {
                Debug.LogError($"{Tag} StartServer failed: Steam is not initialised.");
                return false;
            }

            m_Server = SteamNetworkingSockets.CreateRelaySocket(0, this);
            if (m_Server == null)
            {
                Debug.LogError($"{Tag} CreateRelaySocket failed.");
                return false;
            }

            Debug.Log($"{Tag} Relay socket listening as {SteamClient.SteamId}.");
            return true;
        }

        public override bool StartClient()
        {
            if (!SteamClient.IsValid)
            {
                Debug.LogError($"{Tag} StartClient failed: Steam is not initialised.");
                return false;
            }

            if (TargetSteamId == 0)
            {
                Debug.LogError($"{Tag} StartClient failed: TargetSteamId is not set.");
                return false;
            }

            m_Client = SteamNetworkingSockets.ConnectRelay(TargetSteamId, 0, this);
            if (m_Client == null)
            {
                Debug.LogError($"{Tag} ConnectRelay to {TargetSteamId} failed.");
                return false;
            }

            Debug.Log($"{Tag} Connecting to {TargetSteamId}...");
            return true;
        }

        // Events are pushed via InvokeOnTransportEvent (same as UnityTransport), so polling always reports nothing.
        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            clientId = default;
            payload = default;
            receiveTime = default;
            return NetworkEvent.Nothing;
        }

        protected override void OnEarlyUpdate()
        {
            if (SteamClient.IsValid)
            {
                // Receive() dispatches pending messages to our OnMessage callbacks.
                m_Server?.Receive(256);
                m_Client?.Receive(256);
            }

            while (m_Events.TryDequeue(out var e))
                Dispatch(e);

            base.OnEarlyUpdate();
        }

        void Dispatch(TransportEvent e)
        {
            bool isServer = m_Server != null;
            if (!isServer && m_Client == null)
                return; // stale event after local disconnect/shutdown

            switch (e.Type)
            {
                case NetworkEvent.Connect:
                    if (isServer) m_Clients.Add(e.Connection);
                    break;
                case NetworkEvent.Disconnect:
                    // Server: ignore connections that never finished connecting or that we already closed.
                    if (isServer && !m_Clients.Remove(e.Connection)) return;
                    if (!isServer)
                    {
                        if (SteamClient.IsValid) m_Client.Close(); // harmless if Steam already closed it
                        m_Client = null;
                    }
                    break;
                case NetworkEvent.Data:
                    if (isServer && !m_Clients.Contains(e.Connection)) return;
                    break;
            }

            ulong clientId = isServer ? e.Connection : ServerClientId;
            var payload = e.Data == null ? default : new ArraySegment<byte>(e.Data);
            InvokeOnTransportEvent(e.Type, clientId, payload, Time.realtimeSinceStartup);
        }

        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
            if (!TryGetConnection(clientId, out var connection))
                return;

            // NGO already batches per frame, so skip Nagle's extra delay on everything.
            var sendType = networkDelivery is NetworkDelivery.Unreliable or NetworkDelivery.UnreliableSequenced
                ? SendType.Unreliable | SendType.NoNagle
                : SendType.Reliable | SendType.NoNagle;

            var result = connection.SendMessage(payload.Array, payload.Offset, payload.Count, sendType);
            if (result != Result.OK)
                Debug.LogWarning($"{Tag} Send to {clientId} failed: {result}");
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            if (!TryGetConnection(clientId, out var connection))
                return 0;
            int ping = connection.QuickStatus().Ping;
            return ping > 0 ? (ulong)ping : 0;
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            if (m_Server == null || !m_Clients.Remove((uint)clientId))
                return;
            if (SteamClient.IsValid)
                ((Connection)(uint)clientId).Close(true); // linger so NGO's disconnect reason still flushes
            Debug.Log($"{Tag} Disconnected client {clientId}.");
        }

        public override void DisconnectLocalClient()
        {
            if (m_Client == null)
                return;
            if (SteamClient.IsValid)
                m_Client.Close(true);
            m_Client = null;
            Debug.Log($"{Tag} Disconnected from host.");
            // Mirror UnityTransport: a local disconnect is reported back to NGO immediately.
            InvokeOnTransportEvent(NetworkEvent.Disconnect, ServerClientId, default, Time.realtimeSinceStartup);
        }

        public override void Shutdown()
        {
            // If Steam already shut down (e.g. app quit), its handles are gone; just drop our references.
            bool steam = SteamClient.IsValid;

            if (m_Server != null)
            {
                // ponytail: closing the listen socket right after a lingering close may cut final reliable data; fine for shutdown.
                if (steam)
                {
                    foreach (var id in m_Clients)
                        ((Connection)id).Close();
                    m_Server.Close();
                }
                m_Server = null;
                Debug.Log($"{Tag} Server shut down.");
            }

            if (m_Client != null)
            {
                if (steam) m_Client.Close();
                m_Client = null;
                Debug.Log($"{Tag} Client shut down.");
            }

            m_Clients.Clear();
            while (m_Events.TryDequeue(out _)) { }
        }

        bool TryGetConnection(ulong clientId, out Connection connection)
        {
            connection = default;
            if (!SteamClient.IsValid)
                return false;
            if (m_Server != null)
            {
                if (!m_Clients.Contains((uint)clientId)) return false;
                connection = (uint)clientId;
                return true;
            }
            if (m_Client == null)
                return false;
            connection = m_Client.Connection;
            return true;
        }

        void Enqueue(NetworkEvent type, uint connection, byte[] data = null) =>
            m_Events.Enqueue(new TransportEvent { Type = type, Connection = connection, Data = data });

        // Steam frees the message after the callback returns, so copy it out.
        static byte[] Copy(IntPtr data, int size)
        {
            var bytes = new byte[size];
            Marshal.Copy(data, bytes, 0, size);
            return bytes;
        }

        // ---- Server callbacks (ISocketManager) ----

        void ISocketManager.OnConnecting(Connection connection, ConnectionInfo info)
        {
            var result = connection.Accept();
            if (result != Result.OK)
                Debug.LogWarning($"{Tag} Accept {info.Identity} failed: {result}");
        }

        void ISocketManager.OnConnected(Connection connection, ConnectionInfo info)
        {
            Debug.Log($"{Tag} Client {info.Identity} connected (connection {connection.Id}).");
            Enqueue(NetworkEvent.Connect, connection.Id);
        }

        void ISocketManager.OnDisconnected(Connection connection, ConnectionInfo info)
        {
            Debug.Log($"{Tag} Client {info.Identity} disconnected: {info.State} / {info.EndReason}.");
            Enqueue(NetworkEvent.Disconnect, connection.Id);
        }

        void ISocketManager.OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel) =>
            Enqueue(NetworkEvent.Data, connection.Id, Copy(data, size));

        // ---- Client callbacks (IConnectionManager) ----

        void IConnectionManager.OnConnecting(ConnectionInfo info) { }

        void IConnectionManager.OnConnected(ConnectionInfo info)
        {
            Debug.Log($"{Tag} Connected to host {info.Identity}.");
            Enqueue(NetworkEvent.Connect, 0);
        }

        void IConnectionManager.OnDisconnected(ConnectionInfo info)
        {
            Debug.Log($"{Tag} Disconnected from host: {info.State} / {info.EndReason}.");
            Enqueue(NetworkEvent.Disconnect, 0);
        }

        void IConnectionManager.OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel) =>
            Enqueue(NetworkEvent.Data, 0, Copy(data, size));
    }
}
