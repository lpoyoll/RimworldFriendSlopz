using RTShared;
using System.Collections.Generic;
using RTNetwork;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using RTNetwork.Components;
using GameClient.Managers;

namespace GameClient.PacketManagers
{
    public class PM_Recount : PM_Base
    {
        [HandlesPacket(PacketHeader.PlayerRecount)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header) { SetServerPlayers(bytes); }

        public static void SetServerPlayers(byte[] bytes)
        {
            PKT_PlayerRecount data = Serializer.ConvertBytesToObject<PKT_PlayerRecount>(bytes);
            SessionManager.CurrentServerPlayers = data.CurrentPlayerCount;
        }
    }
}