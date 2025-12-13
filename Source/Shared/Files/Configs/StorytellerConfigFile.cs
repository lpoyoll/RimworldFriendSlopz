using System;
using System.IO;

namespace Shared.Files.Configs
{
    public class StorytellerConfigFile : BaseFile
    {
        public static string SavePath { get; set; } = string.Empty;

        public bool EnforceStoryteller { get; set; } = false;

        public string StorytellerDefname { get; set; } = string.Empty;

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
                StorytellerConfigFile file = new StorytellerConfigFile();
                Serializer.SerializeToFile(SavePath, file);
                return file;
            }
        }
    }
}