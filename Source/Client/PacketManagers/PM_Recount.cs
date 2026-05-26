using GameClient.Misc;
using Shared;
using System.Collections.Generic;
using TCPNetwork;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;

namespace GameClient.PacketManagers
{
    public class PM_Recount : PM_Base
    {
        [HandlesPacket(PacketHeader.PlayerRecount)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header) { SetServerPlayers(bytes); }

        public static void SetServerPlayers(byte[] bytes)
        {
            PKT_PlayerRecount data = Serializer.ConvertBytesToObject<PKT_PlayerRecount>(bytes);
            SessionHandler.CurrentServerPlayers = data.CurrentPlayerCount;
        }
    }
}