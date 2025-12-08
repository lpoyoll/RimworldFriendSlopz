namespace Shared
{
    public static class CommonValues
    {
        public static string ExecutableVersion { get; set; } = "dev";

        public static string DefaultSaveFormat { get; set; } = ".json";

        public static string TempSaveFormat { get; set; } = ".temp";

        public static string CompressedSaveFormat { get; set; } = ".zip";

        public static string ServerUsersPath { get; set; } = string.Empty;
    }
}