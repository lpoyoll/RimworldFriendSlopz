using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using GameServer.Misc;
using RTShared;
using RTShared.Files;
using RTNetwork;
using RTShared.Files.ServerClient;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using static RTShared.CommonEnumerators;

namespace GameServer.PacketManager
{
    public class PM_Settlements : PM_Base
    {
        [HandlesPacket(PacketHeader.Settlement)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_PlayerSettlement data = Serializer.ConvertBytesToObject<PKT_PlayerSettlement>(bytes);

            switch (data._stepMode)
            {
                case SettlementStepMode.Add:
                    AddSettlement(client, data);
                    break;

                case SettlementStepMode.Remove:
                    RemoveSettlement(client, data);
                    break;
            }
        }

        public static void AddSettlement(ServerClient client, PKT_PlayerSettlement settlementData)
        {
            if (CheckIfTileIsInUse(settlementData._settlementFile.Tile)) ResponseShortcutManager.SendIllegalPacket(client, $"Player {client.GetData<FL_Player>().Username} attempted to add a settlement at tile {settlementData._settlementFile.Tile}, but that tile already has a settlement");
            else
            {
                FL_Settlement settlementFile = new FL_Settlement();
                settlementFile.Tile = settlementData._settlementFile.Tile;
                settlementFile.Username = client.GetData<FL_Player>().Username;
                settlementFile.Username = client.GetData<FL_Player>().Username;
                settlementData._settlementFile = settlementFile;

                Serializer.SerializeToFile(Path.Combine(Master.SettlementsPath, settlementFile.Tile + CommonValues.DefaultSaveFormat), settlementFile);

                settlementData._stepMode = SettlementStepMode.Add;
                foreach (ServerClient cClient in ServerNetwork.GetConnectedClients())
                {
                    if (cClient == client) continue;
                    else
                    {
                        settlementData._settlementFile.Goodwill = PM_Goodwills.GetSettlementGoodwill(cClient, settlementFile);

                        cClient.Listener.EnqueuePacket(PacketHeader.Settlement, settlementData);
                    }
                }

                InformationDisplayer.DisplayAddSettlement(settlementFile.Tile.ToString());
            }
        }

        public static void RemoveSettlement(ServerClient client, PKT_PlayerSettlement settlementData)
        {
            if (!CheckIfTileIsInUse(settlementData._settlementFile.Tile)) ResponseShortcutManager.SendIllegalPacket(client, $"Settlement at tile {settlementData._settlementFile.Tile} was attempted to be removed, but the tile doesn't contain a settlement");

            FL_Settlement settlementFile = GetSettlementFileFromTile(settlementData._settlementFile.Tile);

            if (client != null)
            {
                if (settlementFile.Username != client.GetData<FL_Player>().Username)
                {
                    ResponseShortcutManager.SendIllegalPacket(client, $"Settlement at tile {settlementData._settlementFile.Tile} attempted to be removed by " +
                        $"{client.GetData<FL_Player>().Username}, but {settlementFile.Username} owns the settlement");
                }

                else
                {
                    Delete();
                    SendRemovalSignal();
                }
            }

            else
            {
                Delete();
                SendRemovalSignal();
            }

            void Delete()
            {
                File.Delete(Path.Combine(Master.SettlementsPath, settlementFile.Tile + CommonValues.DefaultSaveFormat));

                InformationDisplayer.DisplayRemoveSettlement(settlementFile.Tile.ToString());
            }

            void SendRemovalSignal()
            {
                settlementData._stepMode = SettlementStepMode.Remove;

                ServerNetwork.SendPacketToAllClients(PacketHeader.Settlement, settlementData, client);
            }
        }

        public static bool CheckIfTileIsInUse(int tileToCheck)
        {
            string[] settlements = Directory.GetFiles(Master.SettlementsPath);
            foreach (string settlement in settlements)
            {
                FL_Settlement settlementJSON = Serializer.SerializeFromFile<FL_Settlement>(settlement);
                if (settlementJSON.Tile == tileToCheck) return true;
            }

            return false;
        }

        public static FL_Settlement GetSettlementFileFromTile(int tileToGet)
        {
            string[] settlements = Directory.GetFiles(Master.SettlementsPath);
            foreach (string settlement in settlements)
            {
                FL_Settlement settlementFile = Serializer.SerializeFromFile<FL_Settlement>(settlement);
                if (settlementFile.Tile == tileToGet) return settlementFile;
            }

            return null;
        }

        public static FL_Settlement GetSettlementFileFromUsername(string usernameToGet)
        {
            string[] settlements = Directory.GetFiles(Master.SettlementsPath);
            foreach (string settlement in settlements)
            {
                FL_Settlement settlementFile = Serializer.SerializeFromFile<FL_Settlement>(settlement);
                if (settlementFile.Username == usernameToGet) return settlementFile;
            }

            return null;
        }

        public static FL_Settlement[] GetAllSettlements()
        {
            List<FL_Settlement> settlementList = new List<FL_Settlement>();

            string[] settlements = Directory.GetFiles(Master.SettlementsPath);
            foreach (string settlement in settlements) settlementList.Add(Serializer.SerializeFromFile<FL_Settlement>(settlement));

            return settlementList.ToArray();
        }

        public static FL_Settlement[] GetAllSettlementsFromUsername(string usernameToCheck)
        {
            List<FL_Settlement> settlementList = new List<FL_Settlement>();

            string[] settlements = Directory.GetFiles(Master.SettlementsPath);
            foreach (string settlement in settlements)
            {
                FL_Settlement settlementFile = Serializer.SerializeFromFile<FL_Settlement>(settlement);
                if (settlementFile.Username == usernameToCheck) settlementList.Add(settlementFile);
            }

            return settlementList.ToArray();
        }

        public static List<FL_Settlement> GetSettlementsFromGoodwill(ServerClient client)
        {
            List<FL_Settlement> tempList = new List<FL_Settlement>();
            foreach (FL_Settlement settlement in PM_Settlements.GetAllSettlements())
            {
                FL_Settlement file = new FL_Settlement();

                if (settlement.Username == client.GetData<FL_Player>().Username) continue;
                else
                {
                    file.Tile = settlement.Tile;
                    file.Username = settlement.Username;
                    file.Username = settlement.Username;
                    file.Goodwill = PM_Goodwills.GetSettlementGoodwill(client, settlement);

                    tempList.Add(file);
                }
            }

            return tempList;
        }
    }
}
