namespace Shared
{
    public static class CommonValues
    {
        public readonly static string ExecutableVersion = "25.6.28.1";

        public static readonly string DefaultParserMethodName = "ParsePacket";

        public static readonly PacketHeader[] IgnoredLogPackets = { PacketHeader.KeepAliveManager };

        public static readonly int KeepAliveCooldown = 3000;
    }
}