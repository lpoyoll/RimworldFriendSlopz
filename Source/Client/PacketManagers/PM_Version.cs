using GameClient.Core;
using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Managers;
using Shared;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using TCPNetwork;
using TCPNetwork.Files.Client;
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
            PKT_Version data = Serializer.ConvertBytesToObject<PKT_Version>(bytes);

            switch (data._step)
            {
                case PKT_Version.VersionStep.Ask:
                    SendClientVersion();
                    break;

                case PKT_Version.VersionStep.Pass:
                    PM_Login.UseLoginData();
                    break;
            }
        }

        public static void SendClientVersion()
        {
            Network.ServerEndpoint.TargetClient.VerifyClient();

            PKT_Version data = new PKT_Version();
            data._version = CommonValues.ExecutableVersion;
            Network.ServerEndpoint.EnqueuePacket(PacketHeader.VersionManager, data);
        }

        public static void PromptChangeVersion()
        {
            DLG_Base.PushNewDialog(new DLG_Inputs("Version selection", new string[] { "Release number" }, new bool[] { false }, 
                delegate { ModVersionManager.ChangeVersion(DLG_Inputs.DialogInputResults[0]); }));
        }
    }
}
