using System;
using System.IO;

namespace Shared.Files.Configs
{
    public class ModsConfigFile : BaseFile
    {
        public enum ModType { Required, Optional, Forbidden };

        public static string SavePath { get; set; } = string.Empty;

        public string[] UnsortedMods { get; set; } = new string[0];

        public ulong[] AllModIds { get; set; } = new ulong[0];

        public string[] RequiredMods { get; set; } = new string[0];

        public string[] OptionalMods { get; set; } = new string[0];

        public string[] ForbiddenMods { get; set; } = new string[0];

        public bool EnforcedConfigs { get; set; } = false;

        public string[] ModFileNames { get; set; } = new string[0];

        public string[] ModConfigs { get; set; } = new string[0];

        public override void Save()
        {
            try { Serializer.SerializeToFile(SavePath, this); }
            catch (Exception e) { throw new Exception(e.ToString()); }
        }

        public static object Load<T>()
        {
            if (File.Exists(SavePath)) return Serializer.SerializeFromFile<T>(SavePath);
            else
            {
                ModsConfigFile file = new ModsConfigFile();
                Serializer.SerializeToFile(SavePath, file);
                return file;
            }
        }
    }
}