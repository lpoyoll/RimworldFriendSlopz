using System.IO;
using System;
using System.Net.Sockets;
using GameClient.Core.Preferences;
using GameClient.Dialogs;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.Values;
using static Shared.CommonEnumerators;
using System.Threading;
using System.Threading.Tasks;
using Shared;

namespace GameClient.TCP
{
    //Main class that is used to handle the connection with the server

    public static class Network
    {
        //Variables that points what the state of the network might be for the client

        public static ClientNetworkState state;

        //IP and Port that the connection will be bound to

        public static string ip = "";

        public static string port = "";

        //TCP listener that will handle the connection with the server

        public static Listener listener;

        //Entry point function of the network class

        public static void StartConnection()
        {
            if (TryConnectToServer())
            {
                ClientValues.ManageDevOptions();
                ConnectionDataHandler.SaveConnectionData(ip, port);

                state = ClientNetworkState.Connected;

                Printer.Message($"Connected to server");
            }

            else
            {
                RT_Dialog_Wait.Instance.Close();
                RT_Dialog_Message d1 = new RT_Dialog_Message("ERROR", new string[] { "The server did not respond in time" });
                RT_Dialog_Base.PushNewDialog(d1);
                DisconnectFromServerInstant();
            }
        }

        //Tries to connect into the specified server

        public static bool TryConnectToServer()
        {
            if (state != ClientNetworkState.Disconnected) return false;

            try
            {
                state = ClientNetworkState.Connecting;
                listener = new Listener(new(ip, int.Parse(port)));
            }
            catch { return false; }

            return true;
        }

        //Disconnects client from the server

        public static void DisconnectFromServerInstant()
        {
            CleanNetworkVariables();
            DisconnectionManager.HandleDisconnect();
        }

        //Disconnects client from the server, but empties the packet buffer first

        public static void DisconnectFromServer()
        {
            DisconnectionManager.isIntentionalDisconnect = true;
            DisconnectionManager.intentionalDisconnectReason = DisconnectionManager.DCReason.UploadSave;
            listener.EnqueuePacket(PacketHeader.DisconnectSafe, new KeepAliveData());
            listener.ClosingFlag = true;
        }
        
        public static void CleanNetworkVariables()
        {
            state = ClientNetworkState.Disconnected;

            if (listener != null)
            {
                listener.DestroyConnection();
                listener = null;
            }
        }
    }
}
