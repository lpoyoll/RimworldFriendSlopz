using GameClient.Hooks.TCPNetwork;
using GameClient.Misc;
using Shared;
using Shared.Files;
using Shared.Misc;
using TCPNetwork;
using TCPNetwork.Packets;
using Verse;
using static Shared.CommonEnumerators;
using static Shared.Misc.Printer;

namespace GameClient.Managers
{
    public static class MapManager
    {
        public static void SendPlayerMapsToServer()
        {
            Printer.Message("Sending maps to server", LogImportanceMode.Verbose);

            foreach (Map map in Find.Maps.ToArray())
            {
                if (map.IsPlayerHome)
                {
                    SendMapToServer(map);
                }
            }
        }

        public static void SendMapToServer(Map map)
        {
            MapFile mapFile = MapSaveLoader.MapToString(map);

            PKT_Map mapData = new PKT_Map();
            mapData._mapTile = mapFile.Tile;
            mapData._mapFile.Wealth = mapFile.Wealth;
            mapData._rawData = Serializer.ConvertObjectToBytes(mapFile);

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.MapManager, mapData);
        }
    }
}
