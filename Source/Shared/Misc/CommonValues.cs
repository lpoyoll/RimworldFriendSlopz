namespace Shared
{
    public static class CommonValues
    {
        public readonly static string ExecutableVersion = "25.3.9.1";

        public static readonly string DefaultParserMethodName = "ParsePacket";

        public static readonly string[] IgnoredLogPackets = { "OnlineActivityManager", "KeepAliveManager" };

        public static readonly int KeepAliveCooldown = 3000;
    }
}