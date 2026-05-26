using GameClient.Misc;
using Shared;
using Shared.Files;
using TCPNetwork;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using Verse;

namespace GameClient.PacketManagers
{
    public class PM_Map : PM_Base
    {
        [HandlesPacket(PacketHeader.Map)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            throw new System.NotImplementedException();
        }

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
            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Map, mapData);
        }
    }
}
