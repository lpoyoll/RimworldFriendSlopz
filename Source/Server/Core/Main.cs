using GameServer.Files;
using GameServer.Hooks.ServerBrowser;
using GameServer.Hooks.Shared;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using GameServer.PacketManager;
using Shared;
using Shared.Files;
using Shared.Files.Actions;
using Shared.Files.Configs;
using Shared.Misc;
using TCPNetwork;
using TCPNetwork.PacketManagers;
using static Shared.Misc.Printer;

namespace GameServer.Core
{
    public static class Main_
    {
        static void Main()
        {
            ServerPrinter.CreateLogger();
            CultureHandler.SetCulture();

            SetPaths();
            CreateFolders();
            LoadFiles();

            Printer.Title($"Server version {CommonValues.ExecutableVersion}");
            Printer.Title($"Loading all necessary resources");
            Printer.Title(Printer.SeparatorString);

            EventManagerH.LoadAllEvents();
            Printer.Title(Printer.SeparatorString, LogImportanceMode.Extreme);
            CMD_Base.GetAllCommands();
            Printer.Title(Printer.SeparatorString, LogImportanceMode.Extreme);
            PM_Base.CacheAllPackets(PM_Base.AssemblyType.Server);
            Printer.Title(Printer.SeparatorString, LogImportanceMode.Extreme);

            ServerNetwork.StartFeature();
            Task.Run(BackupManager.StartFeature);
            Task.Run(ServerBrowserManager.StartFeature);

            while (true) CMD_Base.ListenForCommands();
        }

        private static void SetPaths()
        {
            ServerConfigFile.SavePath = Path.Combine(Master.ConfigsPath, "ServerConfig.json");
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
            FL_Guild.SavePath = Path.Combine(Master.GuildsPath);
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
            if (!Directory.Exists(Master.TempPath)) Directory.CreateDirectory(Master.TempPath);
            if (!Directory.Exists(Master.UsersPath)) Directory.CreateDirectory(Master.UsersPath);
            if (!Directory.Exists(Master.SavesPath)) Directory.CreateDirectory(Master.SavesPath);
            if (!Directory.Exists(Master.MapsPath)) Directory.CreateDirectory(Master.MapsPath);
            if (!Directory.Exists(Master.SitesPath)) Directory.CreateDirectory(Master.SitesPath);
            if (!Directory.Exists(Master.GuildsPath)) Directory.CreateDirectory(Master.GuildsPath);
            if (!Directory.Exists(Master.SettlementsPath)) Directory.CreateDirectory(Master.SettlementsPath);
            if (!Directory.Exists(Master.EventsPath)) Directory.CreateDirectory(Master.EventsPath);
        }

        private static void LoadFiles()
        {
            Master.ServerConfig = (ServerConfigFile)ServerConfigFile.Load<ServerConfigFile>(ServerConfigFile.SavePath);
            Master.ActionConfigs = (FL_ActionsConfig)FL_ActionsConfig.Load<FL_ActionsConfig>(FL_ActionsConfig.SavePath);
            Master.Whitelist = (FL_WhitelistConfig)FL_WhitelistConfig.Load<FL_WhitelistConfig>(FL_WhitelistConfig.SavePath);
            Master.DifficultyValues = (FL_DifficultyConfig)FL_DifficultyConfig.Load<FL_DifficultyConfig>(FL_DifficultyConfig.SavePath);
            Master.ScenarioValues = (FL_ScenarioConfig)FL_ScenarioConfig.Load<FL_ScenarioConfig>(FL_ScenarioConfig.SavePath);
            Master.StorytellerValues = (FL_StorytellerConfig)FL_StorytellerConfig.Load<FL_StorytellerConfig>(FL_StorytellerConfig.SavePath);
            Master.BackupConfig = (FL_BackupsConfig)FL_BackupsConfig.Load<FL_BackupsConfig>(FL_BackupsConfig.SavePath);
            Master.ModConfig = (FL_ModConfig)FL_ModConfig.Load<FL_ModConfig>(FL_ModConfig.SavePath);
            Master.ChatConfig = (FL_ChatConfig)FL_ChatConfig.Load<FL_ChatConfig>(FL_ChatConfig.SavePath);
            Master.WorldValues = (FL_PlanetConfig)FL_PlanetConfig.Load<FL_PlanetConfig>(FL_PlanetConfig.SavePath, false);
            Master.LeaderboardFile = (FL_Leaderboard)FL_Leaderboard.Load<FL_Leaderboard>(FL_Leaderboard.SavePath);
        }
    }
}