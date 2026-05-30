using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Files;
using GameClient.Managers;
using GameClient.PacketManagers;
using RTShared;
using RTShared.Misc;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using RTNetwork;
using RTNetwork.PacketManagers;
using static RTShared.Misc.Printer;
using RTNetwork.Components;

namespace GameClient.Hooks.TCPNetwork
{
    public class ClientNetwork
    {
        public enum ClientNetworkState { Disconnected, Connected }

        private static Action<PacketHeader, byte[], ServerClient> OnReadPacket { get; set; } = delegate (PacketHeader header, byte[] buffer, ServerClient client)
        {
            Action toDo = delegate
            {
                MethodInfo method = (MethodInfo)PM_Base.PacketDictionary[header][1];
                method.Invoke(PM_Base.PacketDictionary[header][0], new object[] { client, buffer, header });
            };

            if (header == PacketHeader.Handshake) toDo.Invoke();
            else MainThreadManager.Instance.Enqueue(toDo);
        };

        private static Action<ServerClient> OnConnect { get; set; } = delegate (ServerClient client)
        {
            MainThreadManager.Instance.Enqueue(delegate { HarmonyManager.EnableMainPatches(); });
            Network.ServerEndpoint = client.Listener;
            PM_Handshake.Send(client);
            PM_Version.Send(client);
        };

        private static Action<ServerClient> OnDisconnect { get; set; } = delegate 
        {
            MainThreadManager.Instance.Enqueue(delegate
            {
                DisconnectionManager.HandleDisconnect();
                MainThreadManager.Instance.DoOnEndMethods();
                Printer.Warning($"Disconnecting from server", Verbosity.Verbose);
            });
        };

        public static void StartFeature()
        {
            DLG_Base.PushNewDialog(new DLG_Wait());

            Task.Run(delegate
            {
                if (TryConnect())
                {
                    SessionManager.CurrentNetworkState = ClientNetworkState.Connected;

                    PersistentSettings settings = PersistentSettings.Load();
                    settings.ServerSettings.Set(Network.Ip, Network.Port);
                    settings.Save();

                    Printer.Message($"Connected to server");
                }

                else
                {
                    MainThreadManager.Instance.Enqueue(delegate
                    {
                        DLG_Wait.Instance.Close();
                        DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "The server did not respond in time" }));
                    });
                }
            });
        }

        private static bool TryConnect()
        {
            if (SessionManager.CurrentNetworkState != ClientNetworkState.Disconnected) return false;
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