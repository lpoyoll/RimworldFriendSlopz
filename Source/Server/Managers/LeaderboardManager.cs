using Shared;
using Shared.Files;
using Shared.Files.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;

namespace GameServer.Managers
{
    public static class LeaderboardManager
    {
        private static float ScoreMultiplier = 0.001f;

        [HandlesPacket(PacketHeader.LeaderboardManager)]
        private static void ParsePacket(ServerClient client, byte[] bytes, PacketHeader header) { SendLeaderboard(client); }

        private static void SendLeaderboard(ServerClient client)
        {
            LeaderboardData data = new LeaderboardData();
            data._file = (LeaderboardFile)LeaderboardFile.Load<LeaderboardFile>();
            client.Listener.EnqueuePacket(PacketHeader.LeaderboardManager, data);
        }

        public static void UpdateLeaderboard(ServerClient client, MapFile map)
        {
            LeaderboardFile file = (LeaderboardFile)LeaderboardFile.Load<LeaderboardFile>();
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

            file.Save();
        }
    }
}
