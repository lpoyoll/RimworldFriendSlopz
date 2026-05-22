using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Files;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.PacketManagers;
using Shared;
using Shared.Misc;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.PacketManagers;
using static Shared.Misc.Printer;

namespace GameClient.Hooks.TCPNetwork
{
    public class ClientNetwork
    {
        public enum ClientNetworkState { Disconnected, Connected }

        private static Action<PacketHeader, byte[], ServerClient> OnReadPacket { get; set; } = delegate (PacketHeader header, byte[] buffer, ServerClient client)
        {
            MainThreadHandler.Instance.Enqueue(delegate
            {
                MethodInfo method = (MethodInfo)PM_Base.PacketDictionary[header][1];
                method.Invoke(PM_Base.PacketDictionary[header][0], new object[] { client, buffer, header });
            });
        };

        private static Action<ServerClient> OnConnect { get; set; } = delegate (ServerClient client)
        {
            MainThreadHandler.Instance.Enqueue(delegate { HarmonyHandler.EnableMainPatches(); });
            Network.ServerEndpoint = client.Listener;
            PM_Handshake.Send(client);
            PM_Version.Send(client);
        };

        private static Action<ServerClient> OnDisconnect { get; set; } = delegate 
        {
            MainThreadHandler.Instance.Enqueue(delegate
            {
                DisconnectionManager.HandleDisconnect();
                MainThreadHandler.Instance.DoOnEndMethods();
                Printer.Warning($"Disconnecting from server", LogImportanceMode.Verbose);
            });
        };

        public static void StartFeature()
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

        private static bool TryConnect()
        {
            if (SessionHandler.CurrentNetworkState != ClientNetworkState.Disconnected) return false;
            else
            {
                try
                {
                    ServerClient client = new ServerClient(new TcpClient(Network.Ip, Network.Port), new NetworkRuleset(OnConnect, OnDisconnect, OnReadPacket, null));
                    client.Listener.Ruleset.OnConnect?.Invoke(client);
                    return true;
                }
                catch { return false; }
            }
        }
    }
}