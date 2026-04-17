using GameServer.Core;
using GameServer.Misc;
using Shared;
using Shared.Files;
using TCPNetwork.Files.Client;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;

namespace GameServer.PacketManager
{
    public class PM_Maps : PM_Base
    {
        [HandlesPacket(PacketHeader.MapManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Map data = Serializer.ConvertBytesToObject<PKT_Map>(bytes);

            SaveUserMap(client, data);
        }

        public static void SaveUserMap(ServerClient client, PKT_Map data)
        {
            Serializer.SerializeToFile(Path.Combine(Master.MapsPath, data.File.Tile + CommonValues.DefaultSaveFormat), data.File);
            PM_Leaderboard.UpdateLeaderboard(client, data.File);
            InformationDisplayer.DisplaySaveMap(client);
        }

        public static string[] GetAllMaps() { return Directory.GetFiles(Master.MapsPath); }

        public static bool CheckIfMapExists(int mapTileToCheck)
        {
            string toFind = GetAllMaps().FirstOrDefault(fetch => Path.GetFileNameWithoutExtension(fetch) == mapTileToCheck.ToString());
            if (toFind != null) return true;
            else return false;
        }

        public static FL_Map GetMapFromTile(int mapTileToGet)
        {
            string path = Path.Combine(Master.MapsPath, mapTileToGet + CommonValues.DefaultSaveFormat);
            if (File.Exists(path)) return Serializer.SerializeFromFile<FL_Map>(path);
            else return null;
        }
    }
}
