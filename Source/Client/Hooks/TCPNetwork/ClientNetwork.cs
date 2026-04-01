using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Files;
using GameClient.Managers;
using GameClient.Misc;
using Shared;
using Shared.Misc;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.Files.Client;
using static Shared.Misc.Printer;

namespace GameClient.Hooks.TCPNetwork
{
    public class ClientNetwork
    {
        private static readonly PacketHeader[] BypassReadyPackets =
        {
            PacketHeader.LoginManager,
            PacketHeader.KeepAliveManager,
            PacketHeader.VersionManager,
            PacketHeader.SaveManager,
            PacketHeader.WorldManager,
            PacketHeader.GlobalDataManager,
            PacketHeader.RecountManager,
            PacketHeader.ChatManager,
            PacketHeader.ConsoleManager
        };
        public enum ClientNetworkState { Disconnected, Connected }

        private Action<PacketHeader, byte[], ServerClient> OnReadPacket { get; set; } = delegate (PacketHeader header, byte[] buffer, ServerClient client)
        {
            if (!SessionHandler.IsReadyToPlay && !BypassReadyPackets.Contains(header)) return;
            else
            {
                MainThreadHandler.Instance.Enqueue(delegate
                {
                    MethodInfo method = (MethodInfo)PacketGatherer.PacketDictionary[header][1];
                    method.Invoke(PacketGatherer.PacketDictionary[header][0], new object[] { client, buffer, header });
                });
            }
        };

        private Action<ServerClient> OnConnect { get; set; } = delegate
        {
            MainThreadHandler.Instance.Enqueue(delegate { HarmonyHandler.EnableMainPatches(); });
        };

        private Action<ServerClient> OnDisconnect { get; set; } = delegate 
        {
            MainThreadHandler.Instance.Enqueue(delegate
            {
                DisconnectionManager.HandleDisconnect();
                MainThreadHandler.Instance.DoOnEndMethods();
                Printer.Warning($"Disconnecting from server", LogImportanceMode.Verbose);
            });
        };

        public ClientNetwork() 
        {
            DLG_Base.PushNewDialog(new DLG_Wait());

            Task.Run(delegate
            {
                if (TryConnect())
                {
                    SessionHandler.CurrentNetworkState = ClientNetworkState.Connected;

                    PersistentSettings settings = PersistentSettings.Load();
                    settings.ServerSettings.Set(Network.Ip, Network.Port);
                    settings.Save();

                    Printer.Message($"Connected to server");
                }

                else
                {
                    MainThreadHandler.Instance.Enqueue(delegate
                    {
                        DLG_Wait.Instance.Close();
                        DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "The server did not respond in time" }));
                    });
                }
            });
        }

        private bool TryConnect()
        {
            if (SessionHandler.CurrentNetworkState != ClientNetworkState.Disconnected) return false;
            else
            {
                try
                {
                    ServerClient client = new ServerClient(new TcpClient(Network.Ip, Network.Port), new NetworkRuleset(OnConnect, OnDisconnect, OnReadPacket, null));
                    Network.ServerEndpoint = client.Listener;
                    return true;
                }
                catch { return false; }
            }
        }
    }
}