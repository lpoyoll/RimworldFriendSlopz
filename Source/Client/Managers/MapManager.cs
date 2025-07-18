using GameClient.Misc;
using Shared.Network.Client;
using Shared;
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
            MapData mapData = new MapData();
            mapData._mapFile = MapSaveLoader.MapToString(map, true, true, true, true, true, true);

            Network.Listener.EnqueuePacket(PacketHeader.MapManager, mapData);
        }
    }
}
