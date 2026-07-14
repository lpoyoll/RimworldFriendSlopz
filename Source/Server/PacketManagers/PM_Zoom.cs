using RTNetwork.Components;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using RTServer.Core;
using RTServer.Managers;
using RTShared.Files.Player;
using RTShared.Misc;
using static RTNetwork.Packets.PKT_Zoom;

namespace RTServer.PacketManagers
{
    public class PM_Zoom : PM_Base
    {
        [HandlesPacket(PacketHeader.Zoom)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            if (!FL_PlayerCooldown.CheckIfCanZoom(client.GetData<FL_Player>(), Master.ActionConfigs.ZoomAction)) ResponseShortcutManager.SendUnavailablePacket(client);
            else
            {
                PKT_Zoom data = Serializer.ConvertBytesToObject<PKT_Zoom>(bytes);

                switch (data.CurrentStepMode)
                {
                    case StepMode.Request:
                        SendRequestedMap(client, data);
                        break;
                }

                client.GetData<FL_Player>().Cooldowns.SetZoomTimer(client.GetData<FL_Player>());
            }
        }

        private static void SendRequestedMap(ServerClient client, PKT_Zoom data)
        {
            if (!PM_Map.CheckIfMapExists(data.TargetTile))
            {
                data.CurrentStepMode = StepMode.Deny;
                client.Listener.EnqueuePacket(PacketHeader.Zoom, data);
            }

            else
            {
                data.CurrentStepMode = StepMode.Request;
                data.MapBytes = PM_Map.GetMapFromTile(data.TargetTile);
                client.Listener.EnqueuePacket(PacketHeader.Zoom, data);
            }
        }
    }
}
