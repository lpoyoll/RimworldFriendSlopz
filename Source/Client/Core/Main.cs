using GameClient.Core.Preferences;
using GameClient.Managers;
using GameClient.Misc;
using HarmonyLib;
using Shared;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;
using Verse;

namespace GameClient.Core
{
    //Class that works as an entry point for the mod

    public static class Main_
    {
        [StaticConstructorOnStartup]
        public static class RimworldTogether
        {
            static RimworldTogether()
            {
                ApplyHarmonyPathches();
                PrepareCulture();
                PreparePaths();
                CreateUnityDispatcher();

                CaravanManagerHelper.SetCaravanDefs();
                SiteManager.SetSiteDefs();

                PlayerPreferenceManager.LoadPlayerPreferences();
                CompatibilityManager.LoadAllPatchedAssemblies();
            }
        }

        private static void ApplyHarmonyPathches()
        {
            Harmony harmony = new Harmony(Master.modID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private static void PrepareCulture()
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
            CultureInfo.CurrentUICulture = new CultureInfo("en-US", false);
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US", false);
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US", false);
        }

        private static void PreparePaths()
        {
            Master.appdataPath = GenFilePaths.SaveDataFolderPath;
            Master.appdataFolderPath = Path.Combine(Master.appdataPath, "RimWorld Together");
            Master.tempFolderPath = Path.Combine(Master.appdataFolderPath, "Temp");
            Master.modAssemblyPath = Assembly.GetExecutingAssembly().Location;
            Master.modAssemblyFolderPath = Directory.GetParent(Master.modAssemblyPath).ToString();

            Master.connectionDataPath = Path.Combine(Master.appdataFolderPath, "ConnectionData.json");
            Master.clientPreferencesPath = Path.Combine(Master.appdataFolderPath, "Preferences.json");
            Master.recentServersPath = Path.Combine(Master.appdataFolderPath, "RecentServers.json");
            Master.loginDataPath = Path.Combine(Master.appdataFolderPath, "LoginData.json");
            Master.savesFolderPath = GenFilePaths.SavedGamesFolderPath;

            if (!Directory.Exists(Master.appdataFolderPath)) Directory.CreateDirectory(Master.appdataFolderPath);
            if (!Directory.Exists(Master.tempFolderPath)) Directory.CreateDirectory(Master.tempFolderPath);
        }

        private static void CreateUnityDispatcher()
        {
            if (Master.threadDispatcher == null)
            {
                GameObject go = new GameObject("Dispatcher");
                Master.threadDispatcher = go.AddComponent(typeof(UnityMainThreadDispatcher)) as UnityMainThreadDispatcher;
                UnityEngine.Object.Instantiate(go);

                Printer.Message($"Created dispatcher for version '{CommonValues.executableVersion}'");
            }
        }
    }
}