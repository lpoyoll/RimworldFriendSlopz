namespace Shared
{
    public static class CommonValues
    {
        public readonly static string executableVersion = "25.1.31.1";

        public static readonly string defaultParserMethodName = "ParsePacket";

        public static readonly string[] ignoredLogPackets = { "OnlineActivityManager", "KeepAliveManager" };
    }
}