using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using Shared;
using Shared.Details.Planet;
using Shared.Files.Configs;
using Shared.Misc;
using TCPNetwork.Files.Client;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using static Shared.CommonEnumerators;

namespace GameServer.PacketManager
{
    public class PM_Npcs : PM_Base
    {
        [HandlesPacket(PacketHeader.NPCManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            if (!PlayerCooldown.CheckIfCanNPC(client.GetOrSetClientData<UserFile>(), Master.ActionConfigs.NPCAction)) return;
            else
            {
                PKT_NPCSettlement data = Serializer.ConvertBytesToObject<PKT_NPCSettlement>(bytes);
                switch (data._stepMode)
                {
                    case SettlementStepMode.Add:
                        ResponseShortcutManager.SendIllegalPacket(client, "Tried to execute unimplemented action");
                        break;

                    case SettlementStepMode.Remove:
                        RemoveNPCSettlement(client, data._settlementData);
                        break;
                }
            }
        }

        public static void RemoveNPCSettlement(ServerClient client, NPCSettlementDetail settlement)
        {
            if (!NPCSettlementManagerHelper.CheckIfSettlementFromTileExists(settlement.Tile))
            {
                ResponseShortcutManager.SendIllegalPacket(client, "Tried removing a non-existing NPC settlement");
            }

            else
            {
                DeleteSettlement(settlement);

                BroadcastSettlementDeletion(settlement);

                client.GetOrSetClientData<UserFile>().Cooldowns.SetNPCTimer(client.GetOrSetClientData<UserFile>());

                Printer.Warning($"[Delete NPC settlement] > {settlement.Tile} > {client.GetOrSetClientData<UserFile>().Username}");
            }
        }

        private static void DeleteSettlement(NPCSettlementDetail settlement)
        {
            List<NPCSettlementDetail> finalSettlements = Master.WorldValues.NPCSettlements.ToList();
            finalSettlements.Remove(NPCSettlementManagerHelper.GetSettlementFromTile(settlement.Tile));

            Master.WorldValues.NPCSettlements = finalSettlements;
            FL_PlanetConfig.Save(FL_PlanetConfig.SavePath, Master.WorldValues);
        }

        private static void BroadcastSettlementDeletion(NPCSettlementDetail settlement)
        {
            PKT_NPCSettlement data = new PKT_NPCSettlement();
            data._stepMode = SettlementStepMode.Remove;
            data._settlementData = settlement;

            ServerNetwork.SendPacketToAllClients(PacketHeader.NPCManager, data);
        }
    }

    public class NPCSettlementManagerHelper
    {
        public static bool CheckIfSettlementFromTileExists(int tile)
        {
            foreach (NPCSettlementDetail settlement in Master.WorldValues.NPCSettlements.ToArray())
            {
                if (settlement.Tile == tile) return true;
            }

            return false;
        }

        public static NPCSettlementDetail GetSettlementFromTile(int tile)
        {
            return Master.WorldValues.NPCSettlements.FirstOrDefault(fetch => fetch.Tile == tile); ;
        }
    }
}
