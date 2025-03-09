using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Misc;
using GameServer.TCP;
using Shared;
using static Shared.CommonEnumerators;

namespace GameServer.Managers
{
    [RTManager]
    public static class VersionManager
    {
        private static void ParsePacket(ServerClient client, Packet packet)
        {
            VersionData data = Serializer.ConvertBytesToObject<VersionData>(packet.Contents);

            if (data._version == CommonValues.ExecutableVersion)
            {
                data._step = VersionData.VersionStep.Pass;
                client.listener.EnqueuePacket(Packet.CreateFromObject(nameof(VersionManager), data));
            }
            else LoginManagerH.DenyConnectionWithReason(client, LoginResponse.WrongVersion);
        }

        public static void AskForClientVersion(ServerClient client)
        {
            VersionData data = new VersionData();
            data._step = VersionData.VersionStep.Ask;

            Packet packet = Packet.CreateFromObject(nameof(VersionManager), data);
            client.listener.EnqueuePacket(packet);
        }
    }
}
