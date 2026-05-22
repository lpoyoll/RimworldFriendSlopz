using GameClient.Core;
using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Misc;
using Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets.ServerBrowser;
using TCPNetwork.Packets.VersionDownloader;
using UnityEngine;

namespace GameClient.Hooks.VersionDownloader
{
    public static class VersionDownloadManager
    {
        private static string VersionToDownload { get; set; } = string.Empty;

        public static void ChangeVersion(string version)
        {
            VersionToDownload = version;

            TryConnect();
        }

        private static Action<PacketHeader, byte[], ServerClient> OnReadPacket { get; set; } = delegate (PacketHeader header, byte[] buffer, ServerClient client)
        {
            MainThreadHandler.Instance.Enqueue(delegate
            {
                MethodInfo method = (MethodInfo)PM_Base.PacketDictionary[header][1];
                method.Invoke(PM_Base.PacketDictionary[header][0], new object[] { client, buffer, header });
            });
        };

        private static Action<ServerClient> OnDisconnect { get; set; } = delegate (ServerClient client) { DLG_Wait.Instance.Close(); };

        public static void TryConnect()
        {
            DLG_Base.PushNewDialog(new DLG_Wait());

            Task.Run(delegate
            {
                if (ConnectToServer()) AskForVersionDownload();
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
                ServerClient client = new ServerClient(new TcpClient(Network.MultipurposeIP, Network.VersionDownloaderPort), new NetworkRuleset(null, OnDisconnect, OnReadPacket, null));
                Network.MultipurposeEndpoint = client.Listener;
                PM_Handshake.Send(client);
                return true;
            }
            catch { return false; }
        }

        private static void AskForVersionDownload()
        {
            PKT_VersionDownload data = new PKT_VersionDownload();
            data.CurrentStepMode = PKT_VersionDownload.StepMode.Ask;
            data.RequestedVersion = VersionToDownload;

            Network.MultipurposeEndpoint.EnqueuePacket(PacketHeader.VersionDownload, data);
        }
    }
}
