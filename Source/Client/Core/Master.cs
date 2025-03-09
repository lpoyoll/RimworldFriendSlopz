using System.Collections.Generic;
using System.Reflection;
using GameClient.Core.Configs;
using GameClient.Misc;

namespace GameClient.Core
{
    public static class Master
    {
        // Instances

        public static ModConfigGetter modConfigs = new ModConfigGetter();

        public static Dictionary<string, MethodInfo> managerDictionary = new Dictionary<string, MethodInfo>();

        // Paths

        public static string appdataPath;

        public static string appdataRTPath;

        public static string appdataTempPath;

        public static string appdataTempVersionPath;

        public static string appdataTempModsPath;

        public static string modMainPath;

        public static string modAssemblyPath;

        public static string modAddonsPath;

        public static string connectionDataPath;

        public static string loginDataPath;

        public static string clientPreferencesPath;

        public static string recentServersPath;

        public static string savesFolderPath;

        // Values

        public static readonly string modPackageID = "nova.rimworldtogether";

        public static readonly string modID = "RimWorld Together";
    }
}
