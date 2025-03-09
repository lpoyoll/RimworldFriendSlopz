using GameServer.Core;
using GameServer.Managers;
using GameServer.Misc;
using Shared;
using System.Net;
using System.Net.Sockets;
using static Shared.CommonEnumerators;

namespace GameServer.TCP
{
    public static class Network
    {
        private static IPAddress localAddress = IPAddress.Parse(Master.serverConfig.IP);

        public static int port = int.Parse(Master.serverConfig.Port);

        private static TcpListener connection;

        public static List<ServerClient> connectedClients = new List<ServerClient>();

        public static void ReadyServer()
        {
            if (Master.serverConfig.UseUPnP) { _ = new UPnP(); }

            connection = new TcpListener(localAddress, port);
            connection.Start();

            Printer.Warning("Server launched");
            Printer.Warning($"Listening for users at {localAddress}:{port}");
            Printer.Warning("Type 'help' to get a list of available commands");

            Threader.GenerateServerThread(Threader.ServerMode.Sites);

            Main_.ChangeTitle();

            while (true) ListenForIncomingUsers();
        }

        private static void ListenForIncomingUsers()
        {
            TcpClient newTCP = connection.AcceptTcpClient();
            ServerClient newServerClient = new ServerClient(newTCP);
            Listener newListener = new Listener(newServerClient, newTCP);
            newServerClient.listener = newListener;

            if (Master.isClosing)
            {
                newServerClient.listener.DisconnectFlag = true;
            }

            else if (NetworkHelper.GetConnectedClientsSafe().Length >= int.Parse(Master.serverConfig.MaxPlayers))
            {
                LoginManagerH.DenyConnectionWithReason(newServerClient, LoginResponse.ServerFull);
            }

            else if (Master.worldValues == null && NetworkHelper.GetConnectedClientsSafe().Length > 0)
            {
                LoginManagerH.DenyConnectionWithReason(newServerClient, LoginResponse.NoWorld);
            }

            else
            {
                connectedClients.Add(newServerClient);

                Main_.ChangeTitle();

                InformationDisplayer.DisplayConnect(newServerClient);

                VersionManager.AskForClientVersion(newServerClient);
            }
        }

        public static void KickClient(ServerClient client)
        {
            try
            {
                connectedClients.Remove(client);
                client.listener.DestroyConnection();

                Main_.ChangeTitle();
                UserManager.SendPlayerRecount();
                InformationDisplayer.DisplayDisconnect(client);
                if (Master.chatConfig.DisconnectNotifications) ChatManager.BroadcastServerNotification($"{client.userFile.Uid} has left the server!");
            }
            catch { Printer.Warning($"Error disconnecting user {client.userFile.Uid}, this will cause memory overhead"); }
        }
    }

    public static class NetworkHelper
    {
        public static ServerClient[] GetConnectedClientsSafe(ServerClient toExclude = null)
        {
            if (toExclude != null) return Network.connectedClients.Where(fetch => fetch.userFile.Uid != toExclude.userFile.Uid).ToArray();
            else return Network.connectedClients.ToArray();
        }

        public static ServerClient GetConnectedClientFromUid(string uid)
        {
            return GetConnectedClientsSafe().FirstOrDefault(fetch => fetch.userFile.Uid == uid);
        }

        public static void SendPacketToAllClients(Packet packet, ServerClient toExclude = null)
        {
            foreach (ServerClient client in GetConnectedClientsSafe(toExclude))
            {
                client.listener.EnqueuePacket(packet);
            }
        }
    }
}