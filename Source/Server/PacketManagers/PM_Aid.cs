using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using Shared;
using Shared.Files;
using TCPNetwork.Files.Client;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static TCPNetwork.Packets.PKT_Aid;

namespace GameServer.PacketManager
{
    public class PM_Aid : PM_Base
    {
        [HandlesPacket(PacketHeader.AidManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Aid data = Serializer.ConvertBytesToObject<PKT_Aid>(bytes);

            if (!PlayerCooldown.CheckIfCanAid(client.UserFile, Master.ActionConfigs.AidAction))
            {
                data._stepMode = AidStepMode.Reject;
                client.Listener.EnqueuePacket(PacketHeader.AidManager, data);
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
            }
        }

        private static void SendAidRequest(ServerClient client, PKT_Aid data)
        {
            if (!PM_Settlements.CheckIfTileIsInUse(data._toTile)) ResponseShortcutManager.SendIllegalPacket(client, $"Player {client.UserFile.Username} attempted to send an aid packet to settlement at tile {data._toTile}, but it has no settlement");
            else
            {
                FL_Settlement settlementFile = PM_Settlements.GetSettlementFileFromTile(data._toTile);
                if (UserManagerH.CheckIfUserIsConnected(settlementFile.Username))
                {
                    data._stepMode = AidStepMode.Receive;
                    ServerClient target = ServerNetwork.GetConnectedClientFromUsername(settlementFile.Username);
                    target.Listener.EnqueuePacket(PacketHeader.AidManager, data);
                }

                else
                {
                    data._stepMode = AidStepMode.Reject;
                    client.Listener.EnqueuePacket(PacketHeader.AidManager, data);
                }
            }
        }

        private static void SendAidAccept(ServerClient client, PKT_Aid data)
        {
            if (!PM_Settlements.CheckIfTileIsInUse(data._fromTile)) ResponseShortcutManager.SendIllegalPacket(client, $"Player {client.UserFile.Username} attempted to send an aid packet to settlement at tile {data._fromTile}, but it has no settlement");
            else
            {
                FL_Settlement settlementFile = PM_Settlements.GetSettlementFileFromTile(data._fromTile);
                if (UserManagerH.CheckIfUserIsConnected(settlementFile.Username))
                {
                    client.UserFile.Cooldowns.SetAidTimer(client.UserFile);

                    ServerClient target = ServerNetwork.GetConnectedClientFromUsername(settlementFile.Username);
                    target.Listener.EnqueuePacket(PacketHeader.AidManager, data);
                }

                //Back to client sending the request

                else
                {
                    data._stepMode = AidStepMode.Reject;
                    client.Listener.EnqueuePacket(PacketHeader.AidManager, data);
                }
            }
        }

        private static void SendAidReject(ServerClient client, PKT_Aid data)
        {
            if (!PM_Settlements.CheckIfTileIsInUse(data._fromTile)) ResponseShortcutManager.SendIllegalPacket(client, $"Player {client.UserFile.Username} attempted to send an aid packet to settlement at tile {data._fromTile}, but it has no settlement");
            else
            {
                FL_Settlement settlementFile = PM_Settlements.GetSettlementFileFromTile(data._fromTile);
                if (UserManagerH.CheckIfUserIsConnected(settlementFile.Username))
                {
                    ServerClient target = ServerNetwork.GetConnectedClientFromUsername(settlementFile.Username);
                    target.Listener.EnqueuePacket(PacketHeader.AidManager, data);
                }

                //Back to client sending the request

                else client.Listener.EnqueuePacket(PacketHeader.AidManager, data);
            }
        }
    }
}
