using GameClient.Core.Configs;
using GameClient.Dialogs;
using GameClient.Files;
using GameClient.Managers;
using GameClient.Misc;
using Shared;
using Shared.Misc;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.Files.Client;
using Verse;
using static Shared.CommonEnumerators;
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
            Thread.Sleep(250 * (int)ModConfigGetter.CurrentSimulatedLag);

            if (!SessionHandler.IsReadyToPlay && !BypassReadyPackets.Contains(header)) return;
            else
            {
                MainThreadHandler.Instance.Enqueue(delegate
                {
                    MethodInfo method = (MethodInfo)MethodGatherer.ClientMethodDictionary[header][1];
                    method.Invoke(MethodGatherer.ClientMethodDictionary[header][0], new object[] { client, buffer, header });
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
                DLG_Wait.Instance.Close();
                DLG_Message d1 = new DLG_Message("ERROR", new string[] { "The server did not respond in time" });
                DLG_Base.PushNewDialog(d1);
                OnDisconnect.Invoke(null);
            }
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