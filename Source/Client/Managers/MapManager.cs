using GameClient.Scribers;
using GameClient.TCP;
using Shared;
using Verse;

namespace GameClient.Managers
{
    [RTManager]
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
            MapData mapData = new MapData();
            mapData._mapFile = MapScriber.MapToString(map, true, true, true, true, true, true);

            Packet packet = Packet.CreateFromObject(nameof(MapManager), mapData);
            Network.listener.EnqueuePacket(packet);
        }
    }
}
