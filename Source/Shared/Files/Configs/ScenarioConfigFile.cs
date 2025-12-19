using System;
using System.IO;

namespace Shared.Files.Configs
{
    public class ScenarioConfigFile : BaseFile
    {
        public static string SavePath { get; set; } = string.Empty;

        public bool IsEnforced { get; set; } = false;

        public string Name { get; set; } = string.Empty;

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
                ScenarioConfigFile file = new ScenarioConfigFile();
                Serializer.SerializeToFile(SavePath, file);
                return file;
            }
        }
    }
}