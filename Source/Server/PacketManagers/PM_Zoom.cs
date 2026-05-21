using GameServer.Core;
using Shared;
using TCPNetwork.Packets;
using TCPNetwork.Files.Client;
using GameServer.Managers;
using TCPNetwork.PacketManagers;
using static TCPNetwork.Packets.PKT_Zoom;

namespace GameServer.PacketManager
{
    public class PM_Zoom : PM_Base
    {
        [HandlesPacket(PacketHeader.ZoomManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            if (!Master.ActionConfigs.ZoomAction.IsEnabled) ResponseShortcutManager.SendIllegalPacket(client, "Tried to use disabled feature!");
            else if (!PlayerCooldown.CheckIfCanZoom(client.GetData<UserFile>(), Master.ActionConfigs.ZoomAction)) ResponseShortcutManager.SendUnavailablePacket(client);
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
            if (!PM_Maps.CheckIfMapExists(data.TargetTile))
            {
                data.CurrentStepMode = StepMode.Deny;
                client.Listener.EnqueuePacket(PacketHeader.RaidManager, data);
            }

            else
            {
                data.CurrentStepMode = StepMode.Request;
                data.Map = PM_Maps.GetMapFromTile(data.TargetTile);
                client.Listener.EnqueuePacket(PacketHeader.RaidManager, data);
                client.GetData<UserFile>().Cooldowns.SetZoomTimer(client.GetData<UserFile>());
            }
        }
    }
}
