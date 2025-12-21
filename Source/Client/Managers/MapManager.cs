using GameClient.Misc;
using Shared;
using Shared.Files;
using Verse;
using TCPNetwork.Packets;

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
            MapFile mapFile = MapSaveLoader.MapToString(map, true, true, true, true, true, true);
            var packet = mapFile.CompressIntoBytes();
            ClientNetwork.Instance.ClientListener.EnqueueBytes(PacketHeader.MapManager, packet);
        }
    }
}
