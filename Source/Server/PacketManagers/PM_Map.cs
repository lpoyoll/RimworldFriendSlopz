using RTNetwork.Components;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using RTServer.Core;
using RTServer.Misc;
using RTShared.Files.Player;
using RTShared.Misc;

namespace RTServer.PacketManagers
{
    public class PM_Map : PM_Base
    {
        [HandlesPacket(PacketHeader.Map)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Map data = Serializer.ConvertBytesToObject<PKT_Map>(bytes);

            SaveUserMap(client, data);
        }

        public static void SaveUserMap(ServerClient client, PKT_Map data)
        {
            FL_Settlement settlement = PM_Settlements.GetSettlementFileFromTile(data.Tile);
            if (settlement != null && settlement.Username != client.GetData<FL_Player>().Username)
            {
                // Same-tile guests interact with the host's live map. Their local
                // pre-session map must never overwrite the canonical host snapshot.
                Printer.Message($"[Shared colony] > Ignored non-owner map save from {client.GetData<FL_Player>().Username} at tile {data.Tile}");
                return;
            }

            File.WriteAllBytes(Path.Combine(Master.MapsPath, data.Tile + CommonValues.DefaultSaveFormat), data.Bytes);
            PM_Leaderboard.UpdateLeaderboard(client, data.Wealth);
            InformationDisplayer.DisplaySaveMap(client);
        }

        public static string[] GetAllMaps() { return Directory.GetFiles(Master.MapsPath); }

        public static bool CheckIfMapExists(int mapTileToCheck)
        {
            string toFind = GetAllMaps().FirstOrDefault(fetch => Path.GetFileNameWithoutExtension(fetch) == mapTileToCheck.ToString());
            if (toFind != null) return true;
            else return false;
        }

        public static byte[] GetMapFromTile(int mapTileToGet)
        {
            string path = Path.Combine(Master.MapsPath, mapTileToGet + CommonValues.DefaultSaveFormat);
            if (File.Exists(path)) return File.ReadAllBytes(path);
            else return null;
        }
    }
}
