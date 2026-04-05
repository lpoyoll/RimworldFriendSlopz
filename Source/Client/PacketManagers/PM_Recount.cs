using Shared;
using System.Collections.Generic;
using TCPNetwork.Files.Client;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;

namespace GameClient.PacketManagers
{

    public class PM_Recount : PM_Base
    {
        public static int CurrentPlayers { get; private set; }

        public static List<string> CurrentPlayerNames { get; private set; }

        [HandlesPacket(PacketHeader.RecountManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header) { SetServerPlayers(bytes); }

        public static void SetServerPlayers(byte[] bytes)
        {
            PKT_PlayerRecount data = Serializer.ConvertBytesToObject<PKT_PlayerRecount>(bytes);

            CurrentPlayers = data._currentPlayerCount;
            CurrentPlayerNames = data._currentPlayerNames;
        }
    }
}