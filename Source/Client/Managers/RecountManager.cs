using System.Collections.Generic;
using Shared;

namespace GameClient.Managers
{
    [RTManager]
    public static class RecountManager
    {
        public static int CurrentPlayers { get; private set; }

        public static List<string> CurrentPlayerNames { get; private set; }

        private static void ParsePacket(Packet packet) { SetServerPlayers(packet); }

        public static void SetServerPlayers(Packet packet)
        {
            PlayerRecountData playerRecountData = Serializer.ConvertBytesToObject<PlayerRecountData>(packet.Contents);
            CurrentPlayers = playerRecountData._currentPlayerCount;
            CurrentPlayerNames = playerRecountData._currentPlayerNames;
        }
    }
}