namespace Shared
{
    public static class CommonValues
    {
        public readonly static string ExecutableVersion = "1.6";

        public static readonly string DefaultParserMethodName = "ParsePacket";

        public static readonly PacketHeader[] IgnoredLogPackets = { PacketHeader.KeepAliveManager };

        public static readonly int KeepAliveCooldown = 3000;
    }
}