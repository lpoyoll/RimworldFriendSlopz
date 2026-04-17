using Shared.Files.Mods;
using System.Collections.Generic;

namespace Shared.Files.Configs
{
    public class FL_ModConfig : FL_Base
    {
        public static string SavePath { get; set; } = string.Empty;

        public enum ModType { Required, Optional, Forbidden };

        public List<ModConfig> ModConfigs { get; set; } = new List<ModConfig>();
    }
}