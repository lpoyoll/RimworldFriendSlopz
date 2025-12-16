using GameClient.Core.Configs;
using GameClient.Dialogs;
using GameClient.Files;
using GameClient.Managers;
using GameClient.Misc;
using Shared;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.Files.Client;
using static Shared.CommonEnumerators;

namespace GameClient
{
    public class ClientNetwork : Network
    {
        public static ClientNetwork Instance { get; private set; } = null;

        public override Action<PacketHeader, byte[], ServerClient> OnReadPacket { get; set; } = delegate (PacketHeader header, byte[] buffer, ServerClient client)
        {
            Thread.Sleep(250 * (int)ModConfigGetter.CurrentSimulatedLag);

            if (!SessionHandler.IsReadyToPlay && !Listener.BypassReadyPackets.Contains(header)) return;
            else
            {
                MainThreadHandler.Instance.Enqueue(delegate
                {
                    MethodGatherer.ClientMethodDictionary[header].Invoke(null, new object[] { buffer });
                });
            }
        };

        public override Action<bool> OnWritePacket { get; set; } = delegate (bool mode) { };

        public override Action<ServerClient> OnDisconnect { get; set; } = delegate 
        {
            MainThreadHandler.Instance.Enqueue(delegate
            {
                Instance.Disconnect();
                MainThreadHandler.Instance.DoOnEndMethods();
            });
        };

        public override Action<ServerClient> OnKAFlag { get; set; } = delegate (ServerClient client)
        {
            if (SessionHandler.IsIntentionalDisconnect)
            {
                Printer.Warning("We executed this");
                ClientNetwork.Instance.ClientListener.DisconnectFlag = true;
                ClientNetwork.Instance.ClientListener.OnDisconnect(client);
            }
        };

        public override Action<object, LogImportanceMode> OnMessage { get; set; } = delegate (object obj, LogImportanceMode mode)
        {
            Printer.Message(obj, mode);
        };

        public override Action<object, LogImportanceMode> OnWarning { get; set; } = delegate (object obj, LogImportanceMode mode)
        {
            Printer.Warning(obj, mode);
        };

        public override Action<object, LogImportanceMode> OnError { get; set; } = delegate (object obj, LogImportanceMode mode)
        {
            Printer.Error(obj, mode);
        };

        public ClientNetwork()
        {
            Instance = this;

            StartConnection();
        }

        public void StartConnection()
        {
            if (TryConnect())
            {
                SessionHandler.CurrentNetworkState = ClientNetworkState.Connected;

                PersistentSettings settings = PersistentSettings.Load();
                settings.ServerSettings.Set(Ip, Port);
                settings.Save();

                Printer.Message($"Connected to server");
            }

            else
            {
                RT_Dialog_Wait.Instance.Close();
                RT_Dialog_Message d1 = new RT_Dialog_Message("ERROR", new string[] { "The server did not respond in time" });
                RT_Dialog_Base.PushNewDialog(d1);
                Disconnect();
            }
        }

        public bool TryConnect()
        {
            if (SessionHandler.CurrentNetworkState != ClientNetworkState.Disconnected) return false;

            try
            {
                TcpClient tcpClient = new TcpClient(Ip, int.Parse(Port));

                ClientListener = new Listener(null, tcpClient, OnReadPacket, OnWritePacket, OnDisconnect, OnKAFlag, 
                    OnMessage, OnWarning, OnError, Listener.ListenerMode.Client);
            }
            catch { return false; }

            return true;
        }

        public void Disconnect()
        {
            Printer.Warning($"Disconnecting from server", LogImportanceMode.Verbose);

            SessionHandler.CurrentNetworkState = ClientNetworkState.Disconnected;

            if (ClientListener != null)
            {
                ClientListener.DestroyConnection();
                ClientListener = null;
            }

            DisconnectionManager.HandleDisconnect();
        }
    }
}