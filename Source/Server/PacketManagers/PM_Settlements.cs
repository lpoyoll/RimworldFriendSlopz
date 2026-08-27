using RTNetwork.Components;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using RTServer.Core;
using RTServer.Hooks.TCPNetwork;
using RTServer.Managers;
using RTServer.Misc;
using RTShared.Files;
using RTShared.Files.Player;
using RTShared.Misc;

namespace RTServer.PacketManagers
{
    public class PM_Settlements : PM_Base
    {
        [HandlesPacket(PacketHeader.Settlement)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_PlayerSettlement data = Serializer.ConvertBytesToObject<PKT_PlayerSettlement>(bytes);

            switch (data.StepMode)
            {
                case PKT_PlayerSettlement.SettlementStepMode.Add:
                    AddSettlement(client, data);
                    break;

                case PKT_PlayerSettlement.SettlementStepMode.Remove:
                    RemoveSettlement(client, data);
                    break;
            }
        }

        public static void AddSettlement(ServerClient client, PKT_PlayerSettlement packet)
        {
            string username = client.GetData<FL_Player>().Username;
            if (!SharedColonyManager.CanAddSettlement(packet.File.Tile, username, out string reason))
            {
                PM_Chat.SendServerMessage(client, reason);
                ResponseShortcutManager.SendUnavailablePacket(client);
            }
            else
            {
                FL_Settlement settlementFile = new FL_Settlement();
                settlementFile.Tile = packet.File.Tile;
                settlementFile.Username = username;

                string path = SharedColonyManager.Enabled
                    ? SharedColonyManager.GetSettlementPath(settlementFile.Tile, username)
                    : Path.Combine(Master.SettlementsPath, settlementFile.Tile + CommonValues.DefaultSaveFormat);
                Serializer.SerializeToFile(path, settlementFile);
                SharedColonyManager.RegisterSettlement(settlementFile);

                // v0.1.14: acknowledge the requester's own settlement only after
                // it is durably registered. The shared-map client waits for this
                // before sending its synchronous Ask, removing a race in v0.1.13
                // where PM_Synchronous could run before the requester's settlement
                // existed and therefore could not resolve FromTile.
                if (SharedColonyManager.Enabled)
                {
                    PM_Chat.SendProtocolMessage(client,
                        $"{SharedColonyManager.ProtocolPrefix}|SETTLED|{settlementFile.Tile}|{username}");
                }

                packet.StepMode = PKT_PlayerSettlement.SettlementStepMode.Add;
                packet.File = settlementFile;
                packet.File.IconID = client.GetData<FL_Player>().Customizations.SettlementIconID;
                packet.File.IconColor = client.GetData<FL_Player>().Customizations.SettlementIconColor;
                
                foreach (ServerClient cClient in ServerNetwork.GetConnectedClients())
                {
                    if (cClient == client) continue;
                    else cClient.Listener.EnqueuePacket(PacketHeader.Settlement, packet);
                }

                InformationDisplayer.DisplayAddSettlement(settlementFile.Tile.ToString());
            }
        }

        public static void RemoveSettlement(ServerClient client, PKT_PlayerSettlement settlementData)
        {
            if (!CheckIfTileIsInUse(settlementData.File.Tile))
            {
                if (client != null) ResponseShortcutManager.SendIllegalPacket(client, $"Settlement at tile {settlementData.File.Tile} was attempted to be removed, but the tile doesn't contain a settlement");
                return;
            }

            FL_Settlement settlementFile = client == null
                ? GetSettlementFileFromTile(settlementData.File.Tile)
                : GetSettlementFileFromTileAndUsername(settlementData.File.Tile, client.GetData<FL_Player>().Username);

            if (settlementFile == null)
            {
                if (client != null) ResponseShortcutManager.SendIllegalPacket(client, $"Player does not own a settlement at tile {settlementData.File.Tile}");
                return;
            }

            if (client != null)
            {
                if (settlementFile.Username != client.GetData<FL_Player>().Username)
                {
                    ResponseShortcutManager.SendIllegalPacket(client, $"Settlement at tile {settlementData.File.Tile} attempted to be removed by " +
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
                string path = SharedColonyManager.FindSettlementPath(settlementFile.Tile, settlementFile.Username);
                if (path != null) File.Delete(path);
                SharedColonyManager.UnregisterSettlement(settlementFile);

                InformationDisplayer.DisplayRemoveSettlement(settlementFile.Tile.ToString());
            }

            void SendRemovalSignal()
            {
                settlementData.StepMode = PKT_PlayerSettlement.SettlementStepMode.Remove;

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
            List<FL_Settlement> settlements = GetAllSettlementsAtTile(tileToGet);
            if (settlements.Count == 0) return null;

            if (!SharedColonyManager.Enabled) return settlements[0];

            string host = SharedColonyManager.GetMapHostUsername(tileToGet);
            return settlements.FirstOrDefault(fetch => fetch.Username == host) ?? settlements[0];
        }

        public static FL_Settlement GetSettlementFileFromTileAndUsername(int tileToGet, string username)
        {
            return GetAllSettlementsAtTile(tileToGet).FirstOrDefault(fetch => fetch.Username == username);
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

        public static List<FL_Settlement> GetAllSettlements()
        {
            List<FL_Settlement> settlementList = new List<FL_Settlement>();
            string[] settlements = Directory.GetFiles(Master.SettlementsPath);
            
            foreach (string settlement in settlements)
            {
                FL_Settlement file = Serializer.SerializeFromFile<FL_Settlement>(settlement);
                FL_Player userFile = UserManagerH.GetUserFileFromName(file.Username);
                
                file.IconID = userFile.Customizations.SettlementIconID;
                file.IconColor = userFile.Customizations.SettlementIconColor;
                settlementList.Add(file);
            }

            return settlementList;
        }

        public static List<FL_Settlement> GetAllSettlementsAtTile(int tile)
        {
            List<FL_Settlement> settlementList = new List<FL_Settlement>();
            foreach (string path in Directory.GetFiles(Master.SettlementsPath))
            {
                try
                {
                    FL_Settlement settlement = Serializer.SerializeFromFile<FL_Settlement>(path);
                    if (settlement.Tile == tile) settlementList.Add(settlement);
                }
                catch (Exception ex)
                {
                    Printer.Warning($"Unable to inspect settlement file '{path}': {ex.Message}");
                }
            }

            return settlementList;
        }

        public static FL_Settlement[] GetAllSettlementsFromUsername(string username)
        {
            List<FL_Settlement> settlementList = new List<FL_Settlement>();

            foreach (FL_Settlement settlement in PM_Settlements.GetAllSettlements())
            {
                if (settlement.Username == username) settlementList.Add(settlement);
            }

            return settlementList.ToArray();
        }
    }
}