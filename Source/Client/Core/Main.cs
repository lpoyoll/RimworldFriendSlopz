using GameClient.Core.Preferences;
using GameClient.Misc;
using HarmonyLib;
using Shared;
using Steamworks;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Steam;

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
                LoadAllManagers();

                CaravanManagerH.SetCaravanDef();
                SiteManagerH.SetSiteDefs();
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
            Master.savesFolderPath = GenFilePaths.SavedGamesFolderPath;

            Master.appdataPath = GenFilePaths.SaveDataFolderPath;
            Master.appdataRTPath = Path.Combine(Master.appdataPath, "RimWorld Together");
            Master.appdataTempPath = Path.Combine(Master.appdataRTPath, "Temp");
            Master.appdataTempVersionPath = Path.Combine(Master.appdataTempPath, "Version");
            Master.appdataTempModsPath = Path.Combine(Master.appdataTempPath, "Mods");

            Master.modMainPath = LoadedModManager.RunningMods.First(m => m.PackageId == Master.modPackageID).RootDir;
            Master.modAddonsPath = Path.Combine(Master.modMainPath, "Addons");
            Master.modAssemblyPath = Path.Combine(Master.modMainPath, "Current", "Assemblies");

            Master.connectionDataPath = Path.Combine(Master.appdataRTPath, "ConnectionData.json");
            Master.clientPreferencesPath = Path.Combine(Master.appdataRTPath, "Preferences.json");
            Master.recentServersPath = Path.Combine(Master.appdataRTPath, "RecentServers.json");
            Master.loginDataPath = Path.Combine(Master.appdataRTPath, "LoginData.json");

            if (!Directory.Exists(Master.appdataRTPath)) Directory.CreateDirectory(Master.appdataRTPath);
            if (!Directory.Exists(Master.appdataTempPath)) Directory.CreateDirectory(Master.appdataTempPath);
            if (!Directory.Exists(Master.appdataTempVersionPath)) Directory.CreateDirectory(Master.appdataTempVersionPath);
            if (!Directory.Exists(Master.appdataTempModsPath)) Directory.CreateDirectory(Master.appdataTempModsPath);
            if (!Directory.Exists(Master.modAddonsPath)) Directory.CreateDirectory(Master.modAddonsPath);        
        }

        private static void CreateUnityDispatcher()
        {
            if (MainThreadHandler.Instance == null)
            {
                GameObject go = UnityEngine.Object.Instantiate(new GameObject());
                go.AddComponent(typeof(MainThreadHandler));
            }
        }

        public static void LoadAllManagers() 
        {
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes()) 
            {
                if (type.GetCustomAttributes(typeof(RTManager), false).Length != 0)
                {
                    try { Master.managerDictionary[type.Name] = type.GetMethod("ParsePacket", BindingFlags.Static | BindingFlags.NonPublic); }
                    catch (Exception exception) { Printer.Error($"{type.Name} failed to load > {exception}"); }
                }
            }
        }
    }
}