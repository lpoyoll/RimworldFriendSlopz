using GameClient.Dialogs;
using Shared;
using Shared.Files;
using TCPNetwork;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;

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

        private static void DisplayLeaderboard(FL_Leaderboard file) { DLG_Base.PushNewDialog(new DLG_Leaderboard(file)); }
    }
}
