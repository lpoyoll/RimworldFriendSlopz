using Shared;
using Shared.Files;
using TCPNetwork;
using Shared.Files.ServerClient;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;

namespace GameServer.PacketManager
{
    public class PM_Leaderboard : PM_Base
    {
        private static float ScoreMultiplier = 0.001f;

        [HandlesPacket(PacketHeader.Leaderboard)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header) { SendLeaderboard(client); }

        private static void SendLeaderboard(ServerClient client)
        {
            PKT_Leaderboard data = new PKT_Leaderboard();
            data._file = (FL_Leaderboard)FL_Leaderboard.Load<FL_Leaderboard>(FL_Leaderboard.SavePath);
            client.Listener.EnqueuePacket(PacketHeader.Leaderboard, data);
        }

        public static void UpdateLeaderboard(ServerClient client, FL_Map map)
        {
            FL_Leaderboard file = (FL_Leaderboard)FL_Leaderboard.Load<FL_Leaderboard>(FL_Leaderboard.SavePath);
            double scoreValue = Math.Round(map.Wealth * ScoreMultiplier) + 1;
            
            if (!file.Scores.Keys.Contains(client.GetData<FL_Player>().Username)) file.Scores.Add(client.GetData<FL_Player>().Username, scoreValue);
            else
            {
                foreach (KeyValuePair<string, double> pair in file.Scores.ToArray())
                {
                    if (pair.Key == client.GetData<FL_Player>().Username)
                    {
                        double currentScore = pair.Value;
                        file.Scores.Remove(pair.Key);
                        file.Scores.Add(client.GetData<FL_Player>().Username, currentScore + scoreValue);
                    }
                }
            }

            FL_Leaderboard.Save(FL_Leaderboard.SavePath, file);
        }
    }
}
