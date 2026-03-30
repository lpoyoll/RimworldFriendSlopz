using GameServer.Core;
using GameServer.Managers;
using GameServer.Misc;
using GameServer.PacketManager;
using Shared;
using Shared.Misc;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using TCPNetwork;
using TCPNetwork.Files.Client;
using static TCPNetwork.Packets.PKT_Login;

namespace GameServer.Hooks.TCPNetwork
{
    public class ServerNetwork
    {
        private Action<PacketHeader, byte[], ServerClient> OnReadPacket { get; set; } = delegate (PacketHeader header, byte[] buffer, ServerClient client)
        {
            MethodInfo method = (MethodInfo)PacketGatherer.PacketDictionary[header][1];
            method.Invoke(PacketGatherer.PacketDictionary[header][0], new object[] { client, buffer, header });
        };

        private Action<ServerClient> OnDisconnect { get; set; } = delegate (ServerClient client) 
        {
            try
            {
                Network.ServerClients.Remove(client);
                InformationDisplayer.DisplayDisconnect(client);
                if (Master.ChatConfig.DisconnectNotifications) PM_Chat.BroadcastServerNotification($"{client.UserFile.Username} has left the server!");

                UserManager.SendPlayerRecount();
            }
            catch (Exception ex) { Printer.Error(ex); }
        };

        public ServerNetwork() 
        {
            try
            {
                Network.Ip = Master.ServerConfig.IP;
                Network.Port = Master.ServerConfig.Port;

                if (Master.ServerConfig.UseUPnP) { _ = new UPnP(); }


                Network.ServerListener = new TcpListener(IPAddress.Parse(Network.Ip), Network.Port);
                Network.ServerListener.Start();

                Printer.Warning("Server launched");
                Printer.Warning($"Listening for users at {Network.Ip}:{Network.Port}");
                Printer.Warning("Type 'help' to get a list of available commands");

                Task.Run(delegate { while (true) ListenForNewClients(); });
            }
            catch (Exception e) { Printer.Error(e); }
        }

        private void ListenForNewClients()
        {
            ServerClient client = new ServerClient(Network.ServerListener.AcceptTcpClient(), new NetworkRuleset(null, OnDisconnect, OnReadPacket, null));

            if (GetConnectedClients().Length >= Master.ServerConfig.MaxPlayers) LoginManagerH.DenyConnectionWithReason(client, LoginResponse.Full);
            else if (Master.WorldValues == null && GetConnectedClients().Length > 0) LoginManagerH.DenyConnectionWithReason(client, LoginResponse.NoWorld);
            else
            {
                Network.ServerClients.Add(client);
                InformationDisplayer.DisplayConnect(client);
                PM_Version.AskForClientVersion(client);
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