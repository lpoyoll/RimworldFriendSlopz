using Shared.Files.Actions;
using Shared.Files.Configs;
using Shared.Files.Configs.Mods;

namespace GameServer.Core;

public static class Master
{
    public static readonly string MainPath  = Directory.GetCurrentDirectory();

    public static readonly string AssetsPath = Path.Combine(MainPath, "Assets");

    public static readonly string BackupsPath = Path.Combine(MainPath, "Backups");

    public static readonly string BackupServerPath = Path.Combine(BackupsPath, "Servers");

    public static readonly string BackupUsersPath = Path.Combine(BackupsPath, "Users");

    public static readonly string ConfigsPath = Path.Combine(MainPath, "Configs");

    public static readonly string LogsPath = Path.Combine(MainPath, "Logs");

    public static readonly string SystemLogsPath = Path.Combine(LogsPath, "System");

    public static readonly string ChatLogsPath = Path.Combine(LogsPath, "Chat");

    public static readonly string TempPath = Path.Combine(MainPath, "Temp");

    public static readonly string MapsPath = Path.Combine(AssetsPath, "Maps");

    public static readonly string UsersPath = Path.Combine(AssetsPath, "Users");

    public static readonly string SavesPath = Path.Combine(AssetsPath, "Saves");

    public static readonly string SitesPath = Path.Combine(AssetsPath, "Sites");

    public static readonly string GuildsPath = Path.Combine(AssetsPath, "Guilds");

    public static readonly string SettlementsPath = Path.Combine(AssetsPath, "Settlements");

    public static readonly string EventsPath = Path.Combine(AssetsPath, "Events");

    public static readonly string WorldPath = Path.Combine(AssetsPath, "World");

    //References

    public static WhitelistConfigFile Whitelist { get; set; } = null;

    public static PlanetConfigFile WorldValues { get; set; } = null;

    public static ServerConfigFile ServerConfig { get; set; } = null;

    public static ActionsConfigFile ActionConfigs { get; set; } = null;

    public static DifficultyConfigFile DifficultyValues { get; set; } = null;

    public static StorytellerConfigFile StorytellerValues { get; set; } = null;

    public static ScenarioConfigFile ScenarioValues { get; set; } = null;

    public static BackupsConfigFile BackupConfig { get; set; } = null;

    public static ModsConfigFile ModConfig { get; set; } = null;

    public static ChatConfigFile ChatConfig { get; set; } = null;

    public static ServerBrowserConfigFile ServerBrowserConfig { get; set; } = null;
}