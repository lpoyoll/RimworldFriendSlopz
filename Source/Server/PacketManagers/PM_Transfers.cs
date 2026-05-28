using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using Shared;
using Shared.Files;
using TCPNetwork;
using Shared.Files.ServerClient;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using static TCPNetwork.Packets.PKT_Transfer;

namespace GameServer.PacketManager
{
    public class PM_Transfers : PM_Base
    {
        [HandlesPacket(PacketHeader.Transfer)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            if (!Master.ActionConfigs.EnableTrading)
            {
                ResponseShortcutManager.SendIllegalPacket(client, "Tried to use disabled feature!");
                return;
            }

            PKT_Transfer data = Serializer.ConvertBytesToObject<PKT_Transfer>(bytes);

            switch (data.CurrentStepMode)
            {
                case TransferStepMode.TradeRequest:
                    TransferThings(client, data);
                    break;

                case TransferStepMode.TradeReject:
                    RejectTransfer(client, bytes);
                    break;

                case TransferStepMode.TradeReRequest:
                    TransferThingsRebound(client, bytes);
                    break;

                case TransferStepMode.TradeReAccept:
                    AcceptReboundTransfer(client, bytes);
                    break;

                case TransferStepMode.TradeReReject:
                    RejectReboundTransfer(client, bytes);
                    break;
            }
        }

        public static void TransferThings(ServerClient client, PKT_Transfer packet)
        {
            if (!PM_Settlements.CheckIfTileIsInUse(packet.ToTile)) ResponseShortcutManager.SendIllegalPacket(client, $"Player {client.GetData<PlayerFile>().Username} attempted to send items to a settlement at tile {packet.ToTile}, but no settlement could be found");
            else
            {
                FL_Settlement settlement = PM_Settlements.GetSettlementFileFromTile(packet.ToTile);

                if (!UserManagerH.CheckIfUserIsConnected(settlement.Username)) 
                {
                    packet.CurrentStepMode = TransferStepMode.Recover;
                    client.Listener.EnqueuePacket(PacketHeader.Transfer, packet);
                }

                else
                {
                    if (packet.CurrentTransferMode == TransferMode.Gift)
                    {
                        packet.CurrentStepMode = TransferStepMode.TradeAccept;
                        client.Listener.EnqueuePacket(PacketHeader.Transfer, packet);
                    }

                    else if (packet.CurrentTransferMode == TransferMode.Pod)
                    {
                        packet.CurrentStepMode = TransferStepMode.TradeAccept;
                        client.Listener.EnqueuePacket(PacketHeader.Transfer, packet);
                    }

                    packet.CurrentStepMode = TransferStepMode.TradeRequest;
                    ServerNetwork.GetConnectedClientFromUsername(settlement.Username).Listener.EnqueuePacket(PacketHeader.Transfer, packet);
                }
            }
        }

        public static void RejectTransfer(ServerClient client, byte[] bytes)
        {
            PKT_Transfer transferData = Serializer.ConvertBytesToObject<PKT_Transfer>(bytes);

            FL_Settlement settlement = PM_Settlements.GetSettlementFileFromTile(transferData.FromTile);
            if (!UserManagerH.CheckIfUserIsConnected(settlement.Username))
            {
                transferData.CurrentStepMode = TransferStepMode.Recover;
                client.Listener.EnqueuePacket(PacketHeader.Transfer, transferData);
            }

            else
            {
                transferData.CurrentStepMode = TransferStepMode.TradeReject;
                ServerNetwork.GetConnectedClientFromUsername(settlement.Username).Listener.EnqueuePacket(PacketHeader.Transfer, transferData);
            }
        }

        public static void TransferThingsRebound(ServerClient client, byte[] bytes)
        {
            PKT_Transfer transferData = Serializer.ConvertBytesToObject<PKT_Transfer>(bytes);

            FL_Settlement settlement = PM_Settlements.GetSettlementFileFromTile(transferData.ToTile);
            if (!UserManagerH.CheckIfUserIsConnected(settlement.Username))
            {
                transferData.CurrentStepMode = TransferStepMode.TradeReReject;
                client.Listener.EnqueuePacket(PacketHeader.Transfer, transferData);
            }

            else
            {
                transferData.CurrentTransferMode = TransferMode.Rebound;
                transferData.CurrentStepMode = TransferStepMode.TradeReRequest;
                ServerNetwork.GetConnectedClientFromUsername(settlement.Username).Listener.EnqueuePacket(PacketHeader.Transfer, transferData);
            }
        }

        public static void AcceptReboundTransfer(ServerClient client, byte[] bytes)
        {
            PKT_Transfer transferData = Serializer.ConvertBytesToObject<PKT_Transfer>(bytes);

            FL_Settlement settlement = PM_Settlements.GetSettlementFileFromTile(transferData.FromTile);
            if (!UserManagerH.CheckIfUserIsConnected(settlement.Username))
            {
                transferData.CurrentStepMode = TransferStepMode.Recover;
                client.Listener.EnqueuePacket(PacketHeader.Transfer, transferData);
            }

            else
            {
                transferData.CurrentStepMode = TransferStepMode.TradeReAccept;
                ServerNetwork.GetConnectedClientFromUsername(settlement.Username).Listener.EnqueuePacket(PacketHeader.Transfer, transferData);
            }
        }

        public static void RejectReboundTransfer(ServerClient client, byte[] bytes)
        {
            PKT_Transfer transferData = Serializer.ConvertBytesToObject<PKT_Transfer>(bytes);

            FL_Settlement settlement = PM_Settlements.GetSettlementFileFromTile(transferData.FromTile);
            if (!UserManagerH.CheckIfUserIsConnected(settlement.Username))
            {
                transferData.CurrentStepMode = TransferStepMode.Recover;
                client.Listener.EnqueuePacket(PacketHeader.Transfer, transferData);
            }

            else
            {
                transferData.CurrentStepMode = TransferStepMode.TradeReReject;
                ServerNetwork.GetConnectedClientFromUsername(settlement.Username).Listener.EnqueuePacket(PacketHeader.Transfer, transferData);
            }
        }
    }
}
