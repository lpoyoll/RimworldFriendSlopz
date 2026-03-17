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

namespace GameServer.PacketManager
{
    public class PM_Leaderboard : PM_Base
    {
        private static float ScoreMultiplier = 0.001f;

        [HandlesPacket(PacketHeader.LeaderboardManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header) { SendLeaderboard(client); }

        private static void SendLeaderboard(ServerClient client)
        {
            PKT_Leaderboard data = new PKT_Leaderboard();
            data._file = (LeaderboardFile)LeaderboardFile.Load<LeaderboardFile>(LeaderboardFile.SavePath);
            client.Listener.EnqueuePacket(PacketHeader.LeaderboardManager, data);
        }

        public static void UpdateLeaderboard(ServerClient client, MapFile map)
        {
            LeaderboardFile file = (LeaderboardFile)LeaderboardFile.Load<LeaderboardFile>(LeaderboardFile.SavePath);
            double scoreValue = Math.Round(map.Wealth * ScoreMultiplier) + 1;
            
            if (!file.Scores.Keys.Contains(client.UserFile.Username)) file.Scores.Add(client.UserFile.Username, scoreValue);
            else
            {
                foreach (KeyValuePair<string, double> pair in file.Scores.ToArray())
                {
                    if (pair.Key == client.UserFile.Username)
                    {
                        double currentScore = pair.Value;
                        file.Scores.Remove(pair.Key);
                        file.Scores.Add(client.UserFile.Username, currentScore + scoreValue);
                    }
                }
            }

            LeaderboardFile.Save(LeaderboardFile.SavePath, file);
        }
    }
}
