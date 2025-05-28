using System.Globalization;
using GameServer.Core.Configs;
using GameServer.Managers;
using GameServer.Misc;
using GameServer.TCP;
using Shared;
using static Shared.CommonEnumerators;

namespace GameServer.Core
{
    public static class Main_
    {
        static void Main()
        {
            Console.ForegroundColor = ConsoleColor.White;

            SetPaths();
            SetCulture();
            LoadResources();
            ChangeTitle();
            MethodGatherer.CacheAllMethods(MethodGatherer.AssemblyType.Server);

            if (Master.ActionConfigs.EnableSites) SiteManager.UpdateAllSiteInfo();

            Threader.GenerateServerThread(Threader.ServerMode.Start);
            Threader.GenerateServerThread(Threader.ServerMode.Console);
            if (Master.BackupConfig.AutomaticBackups) Threader.GenerateServerThread(Threader.ServerMode.Backup);
            ServerBrowserManager.StartLoops();

            while (true) Thread.Sleep(1);
        }

        public static void SetPaths()
        {
            Master.MainPath = Directory.GetCurrentDirectory();
            Master.ConfigsPath = Path.Combine(Master.MainPath, "Configs");
            Master.TempPath = Path.Combine(Master.MainPath, "Temp");

            Master.AssetsPath = Path.Combine(Master.MainPath, "Assets");
            Master.MapsPath = Path.Combine(Master.AssetsPath, "Maps");
            Master.UsersPath = Path.Combine(Master.AssetsPath, "Users");
            Master.SavesPath = Path.Combine(Master.AssetsPath, "Saves");
            Master.SitesPath = Path.Combine(Master.AssetsPath, "Sites");
            Master.FactionsPath = Path.Combine(Master.AssetsPath, "Factions");
            Master.SettlementsPath = Path.Combine(Master.AssetsPath, "Settlements");
            Master.EventsPath = Path.Combine(Master.AssetsPath, "Events");
            Master.CompatibilityPatchesPath = Path.Combine(Master.AssetsPath, "Patches");

            Master.LogsPath = Path.Combine(Master.MainPath, "Logs");
            Master.SystemLogsPath = Path.Combine(Master.LogsPath, "System");
            Master.ChatLogsPath = Path.Combine(Master.LogsPath, "Chat");

            Master.BackupsPath = Path.Combine(Master.MainPath, "Backups");
            Master.BackupUsersPath = Path.Combine(Master.BackupsPath, "Users");
            Master.BackupServerPath = Path.Combine(Master.BackupsPath, "Servers");

            if (!Directory.Exists(Master.AssetsPath)) Directory.CreateDirectory(Master.AssetsPath);
            if (!Directory.Exists(Master.ConfigsPath)) Directory.CreateDirectory(Master.ConfigsPath);
            if (!Directory.Exists(Master.LogsPath)) Directory.CreateDirectory(Master.LogsPath);
            if (!Directory.Exists(Master.BackupsPath)) Directory.CreateDirectory(Master.BackupsPath);
            if (!Directory.Exists(Master.TempPath)) Directory.CreateDirectory(Master.TempPath);

            if (!Directory.Exists(Master.UsersPath)) Directory.CreateDirectory(Master.UsersPath);
            if (!Directory.Exists(Master.SavesPath)) Directory.CreateDirectory(Master.SavesPath);
            if (!Directory.Exists(Master.MapsPath)) Directory.CreateDirectory(Master.MapsPath);
            if (!Directory.Exists(Master.SystemLogsPath)) Directory.CreateDirectory(Master.SystemLogsPath);
            if (!Directory.Exists(Master.ChatLogsPath)) Directory.CreateDirectory(Master.ChatLogsPath);
            if (!Directory.Exists(Master.SitesPath)) Directory.CreateDirectory(Master.SitesPath);
            if (!Directory.Exists(Master.FactionsPath)) Directory.CreateDirectory(Master.FactionsPath);
            if (!Directory.Exists(Master.SettlementsPath)) Directory.CreateDirectory(Master.SettlementsPath);
            if (!Directory.Exists(Master.EventsPath)) Directory.CreateDirectory(Master.EventsPath);

            if (!Directory.Exists(Master.BackupUsersPath)) Directory.CreateDirectory(Master.BackupUsersPath);
            if (!Directory.Exists(Master.BackupServerPath)) Directory.CreateDirectory(Master.BackupServerPath);

            if (!Directory.Exists(Master.CompatibilityPatchesPath)) Directory.CreateDirectory(Master.CompatibilityPatchesPath);
        }

        private static void SetCulture()
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
            CultureInfo.CurrentUICulture = new CultureInfo("en-US", false);
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US", false);
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US", false);

            Printer.Title($"Server culture > [{CultureInfo.CurrentCulture}]");
        }

        public static void LoadResources()
        {
            Printer.Title($"Server version {CommonValues.ExecutableVersion}");
            Printer.Title($"Loading all necessary resources");
            Printer.Title($"----------------------------------------");

            Master.ServerConfig = ServerConfigFile.Load();

            Master.ActionConfigs = ActionValuesFile.Load();

            Master.SiteValues = SiteValuesFile.Load();

            Master.RoadValues = RoadValuesFile.Load();

            Master.Whitelist = WhitelistConfigFile.Load();

            Master.DifficultyValues = DifficultyValuesFile.Load();

            Master.ScenarioValues = ScenarioValuesFile.Load();

            Master.StorytellerValues =  StorytellerValuesFile.Load();

            Master.BackupConfig = BackupConfigFile.Load();

            Master.ModConfig = ModConfigFile.Load();
            
            Master.ChatConfig = ChatConfigFile.Load();

            Master.WorldValues = WorldValuesFile.Load();

            Master.ServerBrowserConfig = ServerBrowserConfig.Load();

            EventManager.LoadEvents();
        }

        public static void ChangeTitle()
        {
            Console.Title = $"RimWorld Together {CommonValues.ExecutableVersion} - " +
                $"Players [{NetworkHelper.GetConnectedClientsSafe().Length}/{Master.ServerConfig.MaxPlayers}]";
        }
    }
}