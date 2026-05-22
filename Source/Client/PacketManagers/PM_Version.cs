using GameClient.Core;
using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Hooks.VersionDownloader;
using Shared;
using Shared.Misc;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using TCPNetwork;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using UnityEngine;

namespace GameClient.PacketManagers
{
    public class PM_Version : PM_Base
    {
        [HandlesPacket(PacketHeader.VersionManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header) 
        {
            PM_Login.UseLoginData();
        }

        public static void Send(ServerClient client)
        {
            PKT_Version data = new PKT_Version() { Version = CommonValues.ExecutableVersion };
            Network.ServerEndpoint.EnqueuePacket(PacketHeader.VersionManager, data);
        }

        public static void PromptChangeVersion()
        {
            DLG_Base.PushNewDialog(new DLG_Inputs("Version selection", new string[] { "Release number" }, new bool[] { false }, 
                delegate { VersionDownloadManager.ChangeVersion(DLG_Inputs.DialogInputResults[0]); }));
        }
    }
}
