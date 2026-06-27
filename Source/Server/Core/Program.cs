using RTServer.Files;
using RTServer.Hooks.ServerBrowser;
using RTServer.Hooks.Shared;
using RTServer.Hooks.TCPNetwork;
using RTServer.Managers;
using RTServer.PacketManager;
using RTShared.Commands;
using RTShared.Files;
using RTShared.Files.Configs;
using RTShared.Misc;
using RTNetwork.PacketManagers;
using static RTShared.Misc.Printer;
using RTShared.Files.Marketplace;

namespace RTServer.Core
{
    public static class Program
    {
        static void Main()
        {
            ServerPrinter.CreateLogger();
            CultureHandler.SetCulture();

            SetPaths();
            CreateFolders();
            LoadFiles();

            Printer.Title($"Server version {CommonValues.ExecutableVersion} ({CommonValues.HotfixVersion})");
            Printer.Title($"Loading all necessary resources");
            Printer.Title(Printer.SeparatorString);

            PM_Events.LoadAllEvents();
            Printer.Title(Printer.SeparatorString, Verbosity.Extreme);
            CMD_Base.GetAllCommands();
            Printer.Title(Printer.SeparatorString, Verbosity.Extreme);
            PM_Base.CacheAllPackets(PM_Base.AssemblyType.Server);
            Printer.Title(Printer.SeparatorString, Verbosity.Extreme);

            ServerNetwork.StartFeature();
            Task.Run(BackupManager.StartFeature);
            Task.Run(ServerBrowserManager.StartFeature);

            while (true) CMD_Base.ListenForCommands();
        }

        private static void SetPaths()
        {
            FL_ServerConfig.SavePath = Path.Combine(Master.ConfigsPath, "ServerConfig.json");
            FL_ActionsConfig.SavePath = Path.Combine(Master.ConfigsPath, "ActionConfig.json");
            FL_PlanetConfig.SavePath = Path.Combine(Master.AssetsPath, "WorldValuesFile.json");
            FL_StorytellerConfig.SavePath = Path.Combine(Master.ConfigsPath, "StorytellerConfig.json");
            FL_ScenarioConfig.SavePath = Path.Combine(Master.ConfigsPath, "ScenarioConfig.json");
            FL_ModConfig.SavePath = Path.Combine(Master.ConfigsPath, "ModConfig.json");
            FL_DifficultyConfig.SavePath = Path.Combine(Master.ConfigsPath, "DifficultyConfig.json");
            FL_WhitelistConfig.SavePath = Path.Combine(Master.ConfigsPath, "WhitelistConfig.json");
            FL_BackupsConfig.SavePath = Path.Combine(Master.ConfigsPath, "BackupConfig.json");
            FL_ChatConfig.SavePath = Path.Combine(Master.ConfigsPath, "ChatConfig.json");
            FL_Leaderboard.SavePath = Path.Combine(Master.AssetsPath, "Leaderboard.json");
            FL_Market.SavePath = Path.Combine(Master.AssetsPath, "Market.json");
            FL_Guild.SavePath = Path.Combine(Master.GuildsPath);

            //Find a way to move these two to another place or merge with the above
            CommonValues.ServerUsersPath = Master.UsersPath;
            CommonValues.ServerSitesPath = Master.SitesPath;
        }

        private static void CreateFolders()
        {
            if (!Directory.Exists(Master.AssetsPath)) Directory.CreateDirectory(Master.AssetsPath);
            if (!Directory.Exists(Master.ConfigsPath)) Directory.CreateDirectory(Master.ConfigsPath);
            if (!Directory.Exists(Master.LogsPath)) Directory.CreateDirectory(Master.LogsPath);
            if (!Directory.Exists(Master.SystemLogsPath)) Directory.CreateDirectory(Master.SystemLogsPath);
            if (!Directory.Exists(Master.ChatLogsPath)) Directory.CreateDirectory(Master.ChatLogsPath);
            if (!Directory.Exists(Master.BackupsPath)) Directory.CreateDirectory(Master.BackupsPath);
            if (!Directory.Exists(Master.BackupUsersPath)) Directory.CreateDirectory(Master.BackupUsersPath);
            if (!Directory.Exists(Master.BackupServerPath)) Directory.CreateDirectory(Master.BackupServerPath);
            if (!Directory.Exists(Master.UsersPath)) Directory.CreateDirectory(Master.UsersPath);
            if (!Directory.Exists(Master.SavesPath)) Directory.CreateDirectory(Master.SavesPath);
            if (!Directory.Exists(Master.MapsPath)) Directory.CreateDirectory(Master.MapsPath);
            if (!Directory.Exists(Master.SitesPath)) Directory.CreateDirectory(Master.SitesPath);
            if (!Directory.Exists(Master.GuildsPath)) Directory.CreateDirectory(Master.GuildsPath);
            if (!Directory.Exists(Master.SettlementsPath)) Directory.CreateDirectory(Master.SettlementsPath);
            if (!Directory.Exists(Master.EventsPath)) Directory.CreateDirectory(Master.EventsPath);
            if (!Directory.Exists(Master.WorldObjectsPath)) Directory.CreateDirectory(Master.WorldObjectsPath);
        }

        private static void LoadFiles()
        {
            Master.ServerConfig = (FL_ServerConfig)FL_ServerConfig.Load<FL_ServerConfig>(FL_ServerConfig.SavePath);
            if (CommonValues.ExecutableVersion == "dev") Master.ServerConfig.EnableServerTelemetry = false;
            if (CommonValues.ExecutableVersion == "dev") Master.ServerConfig.EnableServerBrowser = false;
            if (CommonValues.ExecutableVersion == "dev") Master.ServerConfig.SyncLocalSave = false;
            FL_ServerConfig.Save(FL_ServerConfig.SavePath, Master.ServerConfig);

            Master.ActionConfigs = (FL_ActionsConfig)FL_ActionsConfig.Load<FL_ActionsConfig>(FL_ActionsConfig.SavePath);
            FL_ActionsConfig.Save(FL_ActionsConfig.SavePath, Master.ActionConfigs);

            Master.Whitelist = (FL_WhitelistConfig)FL_WhitelistConfig.Load<FL_WhitelistConfig>(FL_WhitelistConfig.SavePath);
            FL_WhitelistConfig.Save(FL_WhitelistConfig.SavePath, Master.Whitelist);

            Master.DifficultyValues = (FL_DifficultyConfig)FL_DifficultyConfig.Load<FL_DifficultyConfig>(FL_DifficultyConfig.SavePath);
            FL_DifficultyConfig.Save(FL_DifficultyConfig.SavePath, Master.DifficultyValues);

            Master.ScenarioValues = (FL_ScenarioConfig)FL_ScenarioConfig.Load<FL_ScenarioConfig>(FL_ScenarioConfig.SavePath);
            FL_ScenarioConfig.Save(FL_ScenarioConfig.SavePath, Master.ScenarioValues);

            Master.StorytellerValues = (FL_StorytellerConfig)FL_StorytellerConfig.Load<FL_StorytellerConfig>(FL_StorytellerConfig.SavePath);
            FL_StorytellerConfig.Save(FL_StorytellerConfig.SavePath, Master.StorytellerValues);

            Master.BackupConfig = (FL_BackupsConfig)FL_BackupsConfig.Load<FL_BackupsConfig>(FL_BackupsConfig.SavePath);
            FL_BackupsConfig.Save(FL_BackupsConfig.SavePath, Master.BackupConfig);

            Master.ModConfig = (FL_ModConfig)FL_ModConfig.Load<FL_ModConfig>(FL_ModConfig.SavePath);
            FL_ModConfig.Save(FL_ModConfig.SavePath, Master.ModConfig);

            Master.ChatConfig = (FL_ChatConfig)FL_ChatConfig.Load<FL_ChatConfig>(FL_ChatConfig.SavePath);
            FL_ChatConfig.Save(FL_ChatConfig.SavePath, Master.ChatConfig);

            Master.LeaderboardFile = (FL_Leaderboard)FL_Leaderboard.Load<FL_Leaderboard>(FL_Leaderboard.SavePath);
            FL_Leaderboard.Save(FL_Leaderboard.SavePath, Master.LeaderboardFile);

            Master.MarketFile = (FL_Market)FL_Market.Load<FL_Market>(FL_Market.SavePath);
            FL_Market.Save(FL_Market.SavePath, Master.MarketFile);

            // Don't automatically save this one
            // We require this file to be saved after a client upload
            Master.WorldValues = (FL_PlanetConfig)FL_PlanetConfig.Load<FL_PlanetConfig>(FL_PlanetConfig.SavePath, false);
        }
    }
}