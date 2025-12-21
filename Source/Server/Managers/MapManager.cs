using GameServer.Core;
using GameServer.Misc;
using Shared;
using static Shared.CommonEnumerators;
using Shared.Files;
using Shared.Misc;
using TCPNetwork.Packets;
using TCPNetwork.Files.Client;

namespace GameServer.Managers
{
    public static class MapManager
    {
        [HandlesPacket(PacketHeader.MapManager)]
        private static void ParsePacket(ServerClient client, byte[] bytes, PacketHeader header)
        {
            MapFileHeader mapHeader = MapFile.FromBytes(bytes, out _);
            SaveUserMap(client, mapHeader, bytes);
        }

        /// <summary>
        /// Delete after next version (current version is 25.12.19.1)
        /// </summary>
        public static void ClearPreviousMaps()
        {
            if(!Directory.Exists(Master.MapsPath))
                return;
            foreach (var map in Directory.GetFiles(Master.MapsPath))
            {
                if (map.EndsWith(".json"))
                {
                    File.Delete(map);
                }
            }
            Printer.Error($"Deleted all old map data while migrating to new update, players will need to save at least once for some features to be activated!\n" +
                            $"This does not affect save files");
        }
        
        public static void SaveUserMap(ServerClient client, MapFileHeader header, byte[] mapBytes)
        {
            header.Username = client.UserFile.Username;
            string path = Path.Combine(Master.MapsPath, header.Tile + CommonValues.MapSaveFormat);
            File.WriteAllBytes(path, mapBytes);

            InformationDisplayer.DisplaySaveMap(client);
        }

        public static void DeleteMap(MapFile mapFile)
        {
            File.Delete(Path.Combine(Master.MapsPath, mapFile.Header.Tile + CommonValues.MapSaveFormat));
            InformationDisplayer.DisplayRemoveMap(mapFile.Header.Tile.ToString());
        }

        public static string[] GetAllMaps()
        {
            return Directory.GetFiles(Master.MapsPath);
        }

        public static bool CheckIfMapExists(int mapTileToCheck)
        {
            string toFind = GetAllMaps().FirstOrDefault(fetch => Path.GetFileNameWithoutExtension(fetch) == mapTileToCheck.ToString());
            if (toFind != null) return true;
            else return false;
        }

        public static MapFileHeader GetMapFromTile(int mapTileToGet, out byte[] allBytes)
        {
            
            string path = Path.Combine(Master.MapsPath, mapTileToGet + CommonValues.MapSaveFormat);
            if (File.Exists(path))
            {
                allBytes = File.ReadAllBytes(path);
                return MapFile.FromBytes(allBytes, out _);
            }
            allBytes = null; 
            return null;
        }
    }
}
