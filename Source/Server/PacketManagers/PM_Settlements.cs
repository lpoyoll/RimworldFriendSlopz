using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using GameServer.Misc;
using Shared;
using Shared.Files;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using static Shared.CommonEnumerators;

namespace GameServer.PacketManager
{
    public class PM_Settlements : PM_Base
    {
        [HandlesPacket(PacketHeader.SettlementManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PlayerSettlementData data = Serializer.ConvertBytesToObject<PlayerSettlementData>(bytes);

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

        public static void AddSettlement(ServerClient client, PlayerSettlementData settlementData)
        {
            if (CheckIfTileIsInUse(settlementData._settlementFile.Tile)) ResponseShortcutManager.SendIllegalPacket(client, $"Player {client.UserFile.Username} attempted to add a settlement at tile {settlementData._settlementFile.Tile}, but that tile already has a settlement");
            else
            {
                SettlementFile settlementFile = new SettlementFile();
                settlementFile.Tile = settlementData._settlementFile.Tile;
                settlementFile.Username = client.UserFile.Username;
                settlementFile.Username = client.UserFile.Username;
                settlementData._settlementFile = settlementFile;

                Serializer.SerializeToFile(Path.Combine(Master.SettlementsPath, settlementFile.Tile + CommonValues.DefaultSaveFormat), settlementFile);

                settlementData._stepMode = SettlementStepMode.Add;
                foreach (ServerClient cClient in ServerNetwork.GetConnectedClients())
                {
                    if (cClient == client) continue;
                    else
                    {
                        settlementData._settlementFile.Goodwill = PM_Goodwills.GetSettlementGoodwill(cClient, settlementFile);

                        cClient.Listener.EnqueuePacket(PacketHeader.SettlementManager, settlementData);
                    }
                }

                InformationDisplayer.DisplayAddSettlement(settlementFile.Tile.ToString());
            }
        }

        public static void RemoveSettlement(ServerClient client, PlayerSettlementData settlementData)
        {
            if (!CheckIfTileIsInUse(settlementData._settlementFile.Tile)) ResponseShortcutManager.SendIllegalPacket(client, $"Settlement at tile {settlementData._settlementFile.Tile} was attempted to be removed, but the tile doesn't contain a settlement");

            SettlementFile settlementFile = GetSettlementFileFromTile(settlementData._settlementFile.Tile);

            if (client != null)
            {
                if (settlementFile.Username != client.UserFile.Username)
                {
                    ResponseShortcutManager.SendIllegalPacket(client, $"Settlement at tile {settlementData._settlementFile.Tile} attempted to be removed by " +
                        $"{client.UserFile.Username}, but {settlementFile.Username} owns the settlement");
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

                ServerNetwork.SendPacketToAllClients(PacketHeader.SettlementManager, settlementData, client);
            }
        }

        public static bool CheckIfTileIsInUse(int tileToCheck)
        {
            string[] settlements = Directory.GetFiles(Master.SettlementsPath);
            foreach (string settlement in settlements)
            {
                SettlementFile settlementJSON = Serializer.SerializeFromFile<SettlementFile>(settlement);
                if (settlementJSON.Tile == tileToCheck) return true;
            }

            return false;
        }

        public static SettlementFile GetSettlementFileFromTile(int tileToGet)
        {
            string[] settlements = Directory.GetFiles(Master.SettlementsPath);
            foreach (string settlement in settlements)
            {
                SettlementFile settlementFile = Serializer.SerializeFromFile<SettlementFile>(settlement);
                if (settlementFile.Tile == tileToGet) return settlementFile;
            }

            return null;
        }

        public static SettlementFile GetSettlementFileFromUsername(string usernameToGet)
        {
            string[] settlements = Directory.GetFiles(Master.SettlementsPath);
            foreach (string settlement in settlements)
            {
                SettlementFile settlementFile = Serializer.SerializeFromFile<SettlementFile>(settlement);
                if (settlementFile.Username == usernameToGet) return settlementFile;
            }

            return null;
        }

        public static SettlementFile[] GetAllSettlements()
        {
            List<SettlementFile> settlementList = new List<SettlementFile>();

            string[] settlements = Directory.GetFiles(Master.SettlementsPath);
            foreach (string settlement in settlements) settlementList.Add(Serializer.SerializeFromFile<SettlementFile>(settlement));

            return settlementList.ToArray();
        }

        public static SettlementFile[] GetAllSettlementsFromUsername(string usernameToCheck)
        {
            List<SettlementFile> settlementList = new List<SettlementFile>();

            string[] settlements = Directory.GetFiles(Master.SettlementsPath);
            foreach (string settlement in settlements)
            {
                SettlementFile settlementFile = Serializer.SerializeFromFile<SettlementFile>(settlement);
                if (settlementFile.Username == usernameToCheck) settlementList.Add(settlementFile);
            }

            return settlementList.ToArray();
        }
    }
}
