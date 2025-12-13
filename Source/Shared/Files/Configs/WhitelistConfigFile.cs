using System;
using System.Collections.Generic;
using System.IO;

namespace Shared.Files.Configs
{
    public class WhitelistConfigFile : BaseFile
    {
        public static string SavePath { get; set; } = string.Empty;

        public bool UseWhitelist { get; set; } = false;

        public List<string> WhitelistedUsers { get; set; } = new List<string>() { };

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
                WhitelistConfigFile file = new WhitelistConfigFile();
                Serializer.SerializeToFile(SavePath, file);
                return file;
            }
        }
    }
}
