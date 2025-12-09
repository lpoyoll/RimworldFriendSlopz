using GameClient.Files;
using GameClient.Misc;
using HarmonyLib;
using Shared;
using System.Globalization;
using System.IO;
using System.Linq;
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
                MethodGatherer.CacheAllMethods(MethodGatherer.AssemblyType.Client);

                CaravanManagerH.SetCaravanDef();
                SiteManagerH.SetSiteDefs();

                PersistentSettings.SetFilePath(Path.Combine(Master.AppdataRTPath, "PersistentSettings" + CommonValues.DefaultSaveFormat));
            }
        }

        private static void ApplyHarmonyPathches()
        {
            Harmony harmony = new Harmony(Master.ModID);
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
            Master.SavesFolderPath = GenFilePaths.SavedGamesFolderPath;

            Master.AppdataPath = GenFilePaths.SaveDataFolderPath;
            Master.AppdataRTPath = Path.Combine(Master.AppdataPath, "RimWorld Together");
            Master.AppdataTempPath = Path.Combine(Master.AppdataRTPath, "Temp");

            string mod = LoadedModManager.RunningMods.First(m => (m.PackageId == Master.ModPackageID || m.PackageId == Master.ModPackageID + "_steam") 
                && ModLister.GetActiveModWithIdentifier(m.PackageId) != null).RootDir;

            Master.ModMainPath = mod;
            Master.ModTempPath = Path.Combine(Master.ModMainPath, "Temp");
            Master.ModScriptsPath = Path.Combine(Master.ModMainPath, "Scripts");
            Master.ModAssemblyPath = Path.Combine(Master.ModMainPath, "Current", "Assemblies");

            Master.ConnectionDataPath = Path.Combine(Master.AppdataRTPath, "ConnectionData.json");
            Master.ClientPreferencesPath = Path.Combine(Master.AppdataRTPath, "Preferences.json");
            Master.RecentServersPath = Path.Combine(Master.AppdataRTPath, "RecentServers.json");
            Master.LoginDataPath = Path.Combine(Master.AppdataRTPath, "LoginData.json");

            if (!Directory.Exists(Master.AppdataRTPath)) Directory.CreateDirectory(Master.AppdataRTPath);
            if (!Directory.Exists(Master.AppdataTempPath)) Directory.CreateDirectory(Master.AppdataTempPath);
        }

        private static void CreateUnityDispatcher()
        {
            if (MainThreadHandler.Instance == null)
            {
                GameObject go = UnityEngine.Object.Instantiate(new GameObject());
                go.AddComponent(typeof(MainThreadHandler));
            }
        }
    }
}