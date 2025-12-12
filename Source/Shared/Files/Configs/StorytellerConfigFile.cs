using System;
using System.IO;

namespace Shared.Files.Configs
{
    public class StorytellerConfigFile : BaseFile
    {
        public static string Path { get; set; } = string.Empty;

        public bool EnforceStoryteller { get; set; } = false;

        public string StorytellerDefname { get; set; } = string.Empty;

        public override void Save()
        {
            try { Serializer.SerializeToFile(Path, this); }
            catch (Exception e) { throw new Exception(e.ToString()); }
        }

        public static object Load<T>()
        {
            if (File.Exists(Path)) return Serializer.SerializeFromFile<T>(Path);
            else
            {
                StorytellerConfigFile file = new StorytellerConfigFile();
                Serializer.SerializeToFile(Path, file);
                return file;
            }
        }
    }
}