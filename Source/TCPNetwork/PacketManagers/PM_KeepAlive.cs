using Shared;
using System.Diagnostics;

namespace TCPNetwork.PacketManagers
{
    public class PM_KeepAlive : PM_Base
    {
        private static Stopwatch LatencyStopwatch { get; set; } = new Stopwatch();

        public static int RawPing { get; set; } = 0;

        public static int CurrentPing { get; set; } = 0;

        [HandlesPacket(PacketHeader.KeepAlive)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header) { ComparePing(); }

        private static void ComparePing()
        {
            LatencyStopwatch.Stop();

            RawPing = (int)LatencyStopwatch.ElapsedMilliseconds;
            int value = (int)(RawPing - Network.KeepAliveInterval.TotalMilliseconds);
            CurrentPing = value > 0 ? value : 0;

            LatencyStopwatch.Restart();
        }
    }
}
