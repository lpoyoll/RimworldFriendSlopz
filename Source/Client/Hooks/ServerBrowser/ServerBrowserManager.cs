using GameClient.Core.Configs;
using GameClient.Misc;
using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets.ServerBrowser;

namespace GameClient.Hooks.ServerBrowser
{
    public static class ServerBrowserManager
    {
        private static Action<PacketHeader, byte[], ServerClient> OnReadPacket { get; set; } = delegate (PacketHeader header, byte[] buffer, ServerClient client)
        {
            MainThreadHandler.Instance.Enqueue(delegate
            {
                MethodInfo method = (MethodInfo)MethodGatherer.ClientMethodDictionary[header][1];
                method.Invoke(MethodGatherer.ClientMethodDictionary[header][0], new object[] { client, buffer, header });
            });
        };

        public static bool ConnectToServerBrowser()
        {
            try
            {
                ServerClient client = new ServerClient(new TcpClient("127.0.0.1", 7777), new NetworkRuleset(null, null, OnReadPacket, null));
                Network.BrowserEndpoint = client.Listener;
                AskForServerListings();
                return true;
            }

            catch (Exception ex)
            {
                Printer.Error(ex);
                return false;
            }
        }

        private static void AskForServerListings()
        {
            PKT_BrowserListing listing = new PKT_BrowserListing();
            Network.BrowserEndpoint.EnqueuePacket(PacketHeader.ServerBrowserListing, listing);
        }
    }
}
