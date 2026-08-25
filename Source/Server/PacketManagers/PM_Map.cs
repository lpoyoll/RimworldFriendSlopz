using RTNetwork.Components;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using RTServer.Core;
using RTServer.Misc;
using RTServer.Managers;
using RTShared.Misc;
using RTShared.Files.Player;

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
            string username = client.GetData<FL_Player>().Username;
            if (!SharedColonyManager.MapBytesHaveConfiguredSize(data.Bytes))
            {
                PM_Chat.SendServerMessage(client,
                    $"Map upload rejected: shared colony maps must be {Master.ServerConfig.SharedColonyMapSize}x{Master.ServerConfig.SharedColonyMapSize}.");
                return;
            }

            if (!SharedColonyManager.CanSaveCanonicalMap(username, data.Tile))
            {
                PM_Chat.SendServerMessage(client,
                    $"Map upload ignored: {SharedColonyManager.GetMapHostUsername(data.Tile)} is the save authority for shared tile {data.Tile}.");
                PM_Leaderboard.UpdateLeaderboard(client, data.Wealth);
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
