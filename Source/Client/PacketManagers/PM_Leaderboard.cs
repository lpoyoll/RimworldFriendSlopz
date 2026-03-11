using GameClient.Dialogs;
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
            LeaderboardData data = Serializer.ConvertBytesToObject<LeaderboardData>(bytes);
            DisplayLeaderboard(data._file); 
        }

        public static void Ask()
        {
            LeaderboardData data = new LeaderboardData();
            Network.ServerEndpoint.EnqueuePacket(PacketHeader.LeaderboardManager, data);
        }

        private static void DisplayLeaderboard(LeaderboardFile file)
        {
            List<string> toDisplay = new List<string>();
            foreach (KeyValuePair<string, double> pair in file.Scores.OrderByDescending(fetch => fetch.Value))
            {
                toDisplay.Add($"{pair.Key} - {pair.Value} points");
            }

            string title = "Leaderboard";
            string description = "Server's current leaderboard";
            DLG_Base.PushNewDialog(new DLG_Listing(title, description, toDisplay.ToArray()));
        }
    }
}
