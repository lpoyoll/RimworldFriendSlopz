using GameServer.Core;
using GameServer.Misc;
using Shared;
using static Shared.CommonEnumerators;
using TCPNetwork.Packets;
using TCPNetwork.Files.Client;
using GameServer.Managers;
using TCPNetwork;
using static TCPNetwork.Packets.PKT_Activity;

namespace GameServer.PacketManager
{
    public class PM_Activity : PM_Base
    {
        [HandlesPacket(PacketHeader.ActivityManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            if (!Master.ActionConfigs.ActivityAction.IsEnabled)
            {
                ResponseShortcutManager.SendIllegalPacket(client, "Tried to use disabled feature!");
                return;
            }

            PKT_Activity data = Serializer.ConvertBytesToObject<PKT_Activity>(bytes);

            switch (data._stepMode)
            {
                case ActivityStepMode.Request:
                    SendRequestedMap(client, data);
                    break;
            }
        }

        private static void SendRequestedMap(ServerClient client, PKT_Activity data)
        {
            if (!PM_Maps.CheckIfMapExists(data._targetTile))
            {
                data._stepMode = ActivityStepMode.Deny;
                client.Listener.EnqueuePacket(PacketHeader.ActivityManager, data);
            }

            else
            {
                data._stepMode = ActivityStepMode.Request;
                data._mapRawData = PM_Maps.GetMapFromTile(data._targetTile);

                client.Listener.EnqueuePacket(PacketHeader.ActivityManager, data);
            }
        }
    }
}
