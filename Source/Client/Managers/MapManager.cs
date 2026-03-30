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
            PKT_Map mapData = new PKT_Map();
            mapData.File = MapSaveLoader.MapToString(map);
            Network.ServerEndpoint.EnqueuePacket(PacketHeader.MapManager, mapData);
        }
    }
}
