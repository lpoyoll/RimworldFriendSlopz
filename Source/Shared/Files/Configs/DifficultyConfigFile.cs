using System;
using System.IO;
using MessagePack;
using Newtonsoft.Json;
using Shared.Misc;

namespace Shared.Files.Configs
{
    public class DifficultyConfigFile : BaseFile
    {
        public static string SavePath { get; set; } = string.Empty;

        public bool IsEnforced { get; set; } = false;

        public string ScribeData { get; set; } = string.Empty;

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
                DifficultyConfigFile file = new DifficultyConfigFile();
                Serializer.SerializeToFile(SavePath, file);
                return file;
            }
        }
    }
}
