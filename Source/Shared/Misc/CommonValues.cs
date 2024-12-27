namespace Shared
{
    public static class CommonValues
    {
        public readonly static string executableVersion = "Experimental";

        public static readonly string defaultParserMethodName = "ParsePacket";

        public static readonly string[] ignoredLogPackets = { "OnlineActivityManager", "KeepAliveManager" };
    }
}