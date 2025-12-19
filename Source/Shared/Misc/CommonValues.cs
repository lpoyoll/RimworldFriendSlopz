namespace Shared;

public static class CommonValues
{
    public const string MasterServer = "https://rimworldtogether.eragon.dev";
    public const string ExecutableVersion = "25.12.16.1";
    public const string DefaultSaveFormat = ".json";

    public const string TempSaveFormat = ".temp";

    public const string CompressedSaveFormat = ".zip";

    public static string ServerUsersPath { get; set; } = string.Empty;

    public static string ServerSitesPath { get; set; } = string.Empty;
}