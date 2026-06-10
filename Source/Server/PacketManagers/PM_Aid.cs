using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using RTShared;
using RTShared.Files;
using RTShared.Files.ServerClient;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using static RTNetwork.Packets.PKT_Aid;
using RTShared.Files.Player;
using RTNetwork.Components;

namespace GameServer.PacketManager
{
    public class PM_Aid : PM_Base
    {
        [HandlesPacket(PacketHeader.Aid)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Aid data = Serializer.ConvertBytesToObject<PKT_Aid>(bytes);

            if (!FL_PlayerCooldown.CheckIfCanAid(client.GetData<FL_Player>(), Master.ActionConfigs.AidAction))
            {
                data._stepMode = AidStepMode.Reject;
                client.Listener.EnqueuePacket(PacketHeader.Aid, data);
            }

            else
            {
                switch (data._stepMode)
                {
                    case AidStepMode.Send:
                        SendAidRequest(client, data);
                        break;

                    case AidStepMode.Accept:
                        SendAidAccept(client, data);
                        break;

                    case AidStepMode.Reject:
                        SendAidReject(client, data);
                        break;
                }

                client.GetData<FL_Player>().Cooldowns.SetAidTimer(client.GetData<FL_Player>());
            }
        }

        private static void SendAidRequest(ServerClient client, PKT_Aid data)
        {
            if (!PM_Settlements.CheckIfTileIsInUse(data._toTile)) ResponseShortcutManager.SendIllegalPacket(client, $"Player {client.GetData<FL_Player>().Username} attempted to send an aid packet to settlement at tile {data._toTile}, but it has no settlement");
            else
            {
                FL_Settlement settlementFile = PM_Settlements.GetSettlementFileFromTile(data._toTile);
                if (UserManagerH.CheckIfUserIsConnected(settlementFile.Username))
                {
                    data._stepMode = AidStepMode.Receive;
                    ServerClient target = ServerNetwork.GetConnectedClientFromUsername(settlementFile.Username);
                    target.Listener.EnqueuePacket(PacketHeader.Aid, data);
                }

                else
                {
                    data._stepMode = AidStepMode.Reject;
                    client.Listener.EnqueuePacket(PacketHeader.Aid, data);
                }
            }
        }

        private static void SendAidAccept(ServerClient client, PKT_Aid data)
        {
            if (!PM_Settlements.CheckIfTileIsInUse(data._fromTile)) ResponseShortcutManager.SendIllegalPacket(client, $"Player {client.GetData<FL_Player>().Username} attempted to send an aid packet to settlement at tile {data._fromTile}, but it has no settlement");
            else
            {
                FL_Settlement settlementFile = PM_Settlements.GetSettlementFileFromTile(data._fromTile);
                if (UserManagerH.CheckIfUserIsConnected(settlementFile.Username))
                {
                    ServerClient target = ServerNetwork.GetConnectedClientFromUsername(settlementFile.Username);
                    target.Listener.EnqueuePacket(PacketHeader.Aid, data);
                }

                //Back to client sending the request

                else
                {
                    data._stepMode = AidStepMode.Reject;
                    client.Listener.EnqueuePacket(PacketHeader.Aid, data);
                }
            }
        }

        private static void SendAidReject(ServerClient client, PKT_Aid data)
        {
            if (!PM_Settlements.CheckIfTileIsInUse(data._fromTile)) ResponseShortcutManager.SendIllegalPacket(client, $"Player {client.GetData<FL_Player>().Username} attempted to send an aid packet to settlement at tile {data._fromTile}, but it has no settlement");
            else
            {
                FL_Settlement settlementFile = PM_Settlements.GetSettlementFileFromTile(data._fromTile);
                if (UserManagerH.CheckIfUserIsConnected(settlementFile.Username))
                {
                    ServerClient target = ServerNetwork.GetConnectedClientFromUsername(settlementFile.Username);
                    target.Listener.EnqueuePacket(PacketHeader.Aid, data);
                }

                //Back to client sending the request

                else client.Listener.EnqueuePacket(PacketHeader.Aid, data);
            }
        }
    }
}
