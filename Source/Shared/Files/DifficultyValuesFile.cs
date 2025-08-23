using Shared.Files;
using System;
using System.IO;
using MessagePack;
using Newtonsoft.Json;
using Shared.Misc;

namespace Shared.Files
{
    [Serializable]
    public class DifficultyValuesFile : BaseFile
    {
        public static string Path { get; set; } = string.Empty;
        public string ScribeData { get; set; } = string.Empty;
        [JsonIgnore] [IgnoreMember] public bool EnforceDifficulty => !string.IsNullOrEmpty(ScribeData);

        public override void Save()
        {
            XmlHelper.WriteXmlToFile(ScribeData, Path, true);
        }

        public static object Load<T>()
        {
            DifficultyValuesFile file = new DifficultyValuesFile();
            if (File.Exists(Path))
            {
                file.ScribeData = XmlHelper.ReadXmlFromFile(Path);
            }
            return file;
        }
    }
}
