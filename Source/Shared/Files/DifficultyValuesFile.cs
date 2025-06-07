using System;
#if SERVER
using GameServer.Core;
#endif
namespace Shared
{
    [Serializable]
    public class DifficultyValuesFile
    {
        public bool EnforceDifficulty = false;

        public string ScribeData = string.Empty;
        
        public override string ToString()
        {
            return $"DifficultyValuesFile:|{EnforceDifficulty}";
        }
#if SERVER
        private static string FilePath => Path.Combine(Master.ConfigsPath, "DifficultyConfig.json");

        public static DifficultyValuesFile Load()
        {
            if (File.Exists(FilePath)) return Serializer.SerializeFromFile<DifficultyValuesFile>(FilePath);
            else
            {
                DifficultyValuesFile obj = new DifficultyValuesFile();
                Serializer.SerializeToFile(FilePath, obj);
                return obj;
            }
        }

        public static bool Save()
        {
            try
            {
                Serializer.SerializeToFile(FilePath, Master.DifficultyValues);
                return true;
            }
            catch { return false; }
        }
#endif
    }
}
