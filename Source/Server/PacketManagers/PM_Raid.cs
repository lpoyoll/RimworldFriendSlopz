using GameServer.Core;
using Shared;
using TCPNetwork.Packets;
using TCPNetwork.Files.Client;
using GameServer.Managers;
using static TCPNetwork.Packets.PKT_Raid;
using TCPNetwork.PacketManagers;
using TCPNetwork;

namespace GameServer.PacketManager
{
    public class PM_Raid : PM_Base
    {
        [HandlesPacket(PacketHeader.Raid)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            if (!Master.ActionConfigs.RaidAction.IsEnabled) ResponseShortcutManager.SendIllegalPacket(client, "Tried to use disabled feature!");
            else if (!PlayerCooldown.CheckIfCanRaid(client.GetData<UserFile>(), Master.ActionConfigs.RaidAction)) ResponseShortcutManager.SendUnavailablePacket(client);
            else
            {
                PKT_Raid data = Serializer.ConvertBytesToObject<PKT_Raid>(bytes);

                switch (data.CurrentStepMode)
                {
                    case StepMode.Request:
                        SendRequestedMap(client, data);
                        break;
                }
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
                data.Map = PM_Map.GetMapFromTile(data.TargetTile);
                client.Listener.EnqueuePacket(PacketHeader.Raid, data);
                client.GetData<UserFile>().Cooldowns.SetRaidTimer(client.GetData<UserFile>());
            }
        }
    }
}
