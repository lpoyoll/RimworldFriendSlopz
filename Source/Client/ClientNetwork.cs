using GameClient.Core.Configs;
using GameClient.Dialogs;
using GameClient.Files;
using GameClient.Managers;
using GameClient.Misc;
using Shared;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using TCPNetwork;
using TCPNetwork.Files.Client;
using static Shared.CommonEnumerators;

namespace GameClient;

public class ClientNetwork : Network
{
    public static ClientNetwork Instance { get; private set; } = null;

    protected override Action<PacketHeader, byte[], ServerClient> OnReadPacket { get; set; } = delegate (PacketHeader header, byte[] buffer, ServerClient client)
    {
        Thread.Sleep(250 * (int)ModConfigGetter.CurrentSimulatedLag);

        if (!SessionHandler.IsReadyToPlay && !Listener.BypassReadyPackets.Contains(header)) return;
        else
        {
            MainThreadHandler.Instance.Enqueue(delegate
            {
                MethodGatherer.ClientMethodDictionary[header].Invoke(null, [buffer]);
            });
        }
    };

    protected override Action<bool> OnWritePacket { get; set; } = delegate (bool mode) { };

    protected override Action<ServerClient> OnDisconnect { get; set; } = delegate 
    {
        MainThreadHandler.Instance.Enqueue(delegate
        {
            DisconnectionManager.HandleDisconnect();
            MainThreadHandler.Instance.DoOnEndMethods();
            SessionHandler.CurrentNetworkState = ClientNetworkState.Disconnected;
            Printer.Warning($"Disconnecting from server", LogImportanceMode.Verbose);
        });
    };

    protected override Action<object, LogImportanceMode> OnMessage { get; set; } = delegate (object obj, LogImportanceMode mode)
    {
        MainThreadHandler.Instance.Enqueue(delegate { Printer.Message(obj, mode); });
    };

    protected override Action<object, LogImportanceMode> OnWarning { get; set; } = delegate (object obj, LogImportanceMode mode)
    {
        MainThreadHandler.Instance.Enqueue(delegate { Printer.Warning(obj, mode); });
    };

    protected override Action<object, LogImportanceMode> OnError { get; set; } = delegate (object obj, LogImportanceMode mode)
    {
        MainThreadHandler.Instance.Enqueue(delegate { Printer.Error(obj, mode); });
    };

    public ClientNetwork()
    {
        Instance = this;

        StartConnection();
    }

    private void StartConnection()
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
            RT_Dialog_Message d1 = new RT_Dialog_Message("ERROR", ["The server did not respond in time"]);
            RT_Dialog_Base.PushNewDialog(d1);
            OnDisconnect.Invoke(null);
        }
    }

    private bool TryConnect()
    {
        if (SessionHandler.CurrentNetworkState != ClientNetworkState.Disconnected) return false;

        try
        {
            TcpClient tcpClient = new TcpClient(Ip, int.Parse(Port));

            ClientListener = new Listener(null, tcpClient, OnReadPacket, OnWritePacket, OnDisconnect, 
                OnMessage, OnWarning, OnError, Listener.ListenerMode.Client);
        }
        catch { return false; }

        return true;
    }
}