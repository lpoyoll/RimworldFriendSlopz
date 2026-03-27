using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using Shared;
using Shared.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using TCPNetwork.Packets.Goodwills;

namespace GameClient.PacketManagers
{
    public class PM_Leaderboard : PM_Base
    {
        [HandlesPacket(PacketHeader.LeaderboardManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header) 
        {
            PKT_Leaderboard data = Serializer.ConvertBytesToObject<PKT_Leaderboard>(bytes);
            DisplayLeaderboard(data._file); 
        }

        public static void Ask()
        {
            PKT_Leaderboard data = new PKT_Leaderboard();
            Network.ServerEndpoint.EnqueuePacket(PacketHeader.LeaderboardManager, data);
        }

        private static void DisplayLeaderboard(LeaderboardFile file) { DLG_Base.PushNewDialog(new DLG_Leaderboard(file)); }
    }
}
