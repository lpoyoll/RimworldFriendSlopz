using GameServer.Core;
using Shared;
using TCPNetwork.Packets;
using Shared.Files.ServerClient;
using GameServer.Managers;
using TCPNetwork.PacketManagers;
using static TCPNetwork.Packets.PKT_Zoom;
using TCPNetwork;

namespace GameServer.PacketManager
{
    public class PM_Zoom : PM_Base
    {
        [HandlesPacket(PacketHeader.Zoom)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            if (!Master.ActionConfigs.ZoomAction.IsEnabled) ResponseShortcutManager.SendIllegalPacket(client, "Tried to use disabled feature!");
            else if (!FL_PlayerCooldown.CheckIfCanZoom(client.GetData<FL_Player>(), Master.ActionConfigs.ZoomAction)) ResponseShortcutManager.SendUnavailablePacket(client);
            else
            {
                PKT_Zoom data = Serializer.ConvertBytesToObject<PKT_Zoom>(bytes);

                switch (data.CurrentStepMode)
                {
                    case StepMode.Request:
                        SendRequestedMap(client, data);
                        break;
                }
            }
        }

        private static void SendRequestedMap(ServerClient client, PKT_Zoom data)
        {
            if (!PM_Map.CheckIfMapExists(data.TargetTile))
            {
                data.CurrentStepMode = StepMode.Deny;
                client.Listener.EnqueuePacket(PacketHeader.Raid, data);
            }

            else
            {
                data.CurrentStepMode = StepMode.Request;
                data.Map = PM_Map.GetMapFromTile(data.TargetTile);
                client.Listener.EnqueuePacket(PacketHeader.Raid, data);
                client.GetData<FL_Player>().Cooldowns.SetZoomTimer(client.GetData<FL_Player>());
            }
        }
    }
}
