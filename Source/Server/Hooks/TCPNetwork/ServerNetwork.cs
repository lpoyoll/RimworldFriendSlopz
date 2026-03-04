using GameServer.Core;
using GameServer.Managers;
using GameServer.Misc;
using TCPNetwork;
using Shared;
using System.Net;
using System.Net.Sockets;
using static Shared.CommonEnumerators;
using TCPNetwork.Files.Client;
using Shared.Misc;
using TCPNetwork.Misc;

namespace GameServer.Hooks.TCPNetwork
{
    public class ServerNetwork
    {
        private Action<PacketHeader, byte[], ServerClient> OnReadPacket { get; set; } = delegate (PacketHeader header, byte[] buffer, ServerClient client)
        {
            PacketCache.ServerMethodDictionary[header](client, buffer, header);
        };

        private Action<ServerClient> OnWritePacket { get; set; } = delegate (ServerClient client) { };

        private Action<ServerClient> OnConnect { get; set; } = delegate (ServerClient client) { };

        private Action<ServerClient> OnDisconnect { get; set; } = delegate (ServerClient client) 
        {
            try
            {
                Network.ServerClients.Remove(client);

                Main_.ChangeTitle();
                UserManager.SendPlayerRecount();
                InformationDisplayer.DisplayDisconnect(client);
                if (Master.ChatConfig.DisconnectNotifications) ChatManager.BroadcastServerNotification($"{client.UserFile.Username} has left the server!");
            }
            catch { Printer.Warning($"Error disconnecting user {client.UserFile.Username}, this will cause memory overhead"); }
        };

        public ServerNetwork()
        {
            Network.Ip = Master.ServerConfig.IP;
            Network.Port = Master.ServerConfig.Port;

            Task.Run(Setup);
        }

        private void Setup()
        {
            if (Master.ServerConfig.UseUPnP) { _ = new UPnP(); }

            try
            {
                Network.ServerListener = new TcpListener(IPAddress.Parse(Network.Ip), Network.Port);
                Network.ServerListener.Start();
            }
            catch (Exception e) { Printer.Error(e); }

            Main_.ChangeTitle();
            Printer.Warning("Server launched");
            Printer.Warning($"Listening for users at {Network.Ip}:{Network.Port}");
            Printer.Warning("Type 'help' to get a list of available commands");

            while (true) ListenForNewClients();
        }

        private void ListenForNewClients()
        {
            ServerClient client = new ServerClient(Network.ServerListener.AcceptTcpClient(), 
                new NetworkRuleset(OnConnect, OnDisconnect, OnReadPacket, OnWritePacket));

            if (GetConnectedClients().Length >= Master.ServerConfig.MaxPlayers) LoginManagerH.DenyConnectionWithReason(client, LoginResponse.Full);
            else if (Master.WorldValues == null && GetConnectedClients().Length > 0) LoginManagerH.DenyConnectionWithReason(client, LoginResponse.NoWorld);
            else
            {
                Network.ServerClients.Add(client);

                Main_.ChangeTitle();

                InformationDisplayer.DisplayConnect(client);

                VersionManager.AskForClientVersion(client);
            }
        }

        public static ServerClient[] GetConnectedClients(ServerClient toExclude = null)
        {
            if (toExclude != null) return Network.ServerClients.Where(fetch => fetch.UserFile.Username != toExclude.UserFile.Username).ToArray();
            else return Network.ServerClients.ToArray();
        }

        public static ServerClient GetConnectedClientFromUsername(string username)
        {
            return GetConnectedClients().FirstOrDefault(fetch => fetch.UserFile.Username == username);
        }

        public static void SendPacketToAllClients(PacketHeader header, object obj, ServerClient toExclude = null)
        {
            foreach (ServerClient client in GetConnectedClients(toExclude))
            {
                client.Listener.EnqueuePacket(header, obj);
            }
        }
    }
}