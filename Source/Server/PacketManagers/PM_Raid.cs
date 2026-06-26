using GameServer.Core;
using RTNetwork.Packets;
using GameServer.Managers;
using static RTNetwork.Packets.PKT_Raid;
using RTNetwork.PacketManagers;
using RTShared.Files.Player;
using RTNetwork.Components;
using RTShared.Misc;

namespace GameServer.PacketManager
{
    public class PM_Raid : PM_Base
    {
        [HandlesPacket(PacketHeader.Raid)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            if (!FL_PlayerCooldown.CheckIfCanRaid(client.GetData<FL_Player>(), Master.ActionConfigs.RaidAction)) ResponseShortcutManager.SendUnavailablePacket(client);
            else
            {
                PKT_Raid data = Serializer.ConvertBytesToObject<PKT_Raid>(bytes);

                switch (data.CurrentStepMode)
                {
                    case StepMode.Request:
                        SendRequestedMap(client, data);
                        break;
                }

                client.GetData<FL_Player>().Cooldowns.SetRaidTimer(client.GetData<FL_Player>());
            }
        }

        private static void SendRequestedMap(ServerClient client, PKT_Raid data)
        {
            if (!PM_Map.CheckIfMapExists(data.TargetTile))
            {
                data.CurrentStepMode = StepMode.Deny;
                client.Listener.EnqueuePacket(PacketHeader.Raid, data);
            }

            else
            {
                data.CurrentStepMode = StepMode.Request;
                data.MapBytes = PM_Map.GetMapFromTile(data.TargetTile);
                client.Listener.EnqueuePacket(PacketHeader.Raid, data);
            }
        }
    }
}
