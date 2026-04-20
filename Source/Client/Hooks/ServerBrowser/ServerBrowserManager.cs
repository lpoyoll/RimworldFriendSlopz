using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Misc;
using RimWorld;
using Shared;
using Shared.Misc;
using System;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets.ServerBrowser;

namespace GameClient.Hooks.ServerBrowser
{
    public static class ServerBrowserManager
    {
        private static Action<PacketHeader, byte[], ServerClient> OnReadPacket { get; set; } = delegate (PacketHeader header, byte[] buffer, ServerClient client)
        {
            MainThreadHandler.Instance.Enqueue(delegate
            {
                MethodInfo method = (MethodInfo)PM_Base.PacketDictionary[header][1];
                method.Invoke(PM_Base.PacketDictionary[header][0], new object[] { client, buffer, header });
            });
        };

        public static void TryConnect() 
        {
            DLG_Base.PushNewDialog(new DLG_Wait());

            Task.Run(delegate
            {
                if (ConnectToServer()) AskForServerListings();
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

        private static bool ConnectToServer()
        {
            try
            {
                ServerClient client = new ServerClient(new TcpClient(Network.MultipurposeIP, Network.BrowserClientPort), new NetworkRuleset(null, null, OnReadPacket, null, false));
                Network.MultipurposeEndpoint = client.Listener;
                return true;
            }
            catch { return false; }
        }

        private static void AskForServerListings()
        {
            PKT_ServerInformation listing = new PKT_ServerInformation();
            listing.ClientVersion = CommonValues.ExecutableVersion;
            Network.MultipurposeEndpoint.EnqueuePacket(PacketHeader.ServerBrowserListing, listing);
        }
    }
}
