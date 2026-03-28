using GameClient.Misc;
using Shared;
using Shared.Files;
using TCPNetwork;
using TCPNetwork.Packets;
using Verse;

namespace GameClient.Managers
{
    public static class MapManager
    {
        public static void SendPlayerMapsToServer()
        {
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
