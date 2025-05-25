#if SERVER
using GameServer.Core;
#endif
namespace Shared
{
    public class StorytellerValuesFile
    {
        public bool EnforceStoryteller;

        public string StorytellerDefname;

        public override string ToString()
        {
            return $"StorytellerValuesFile:|{EnforceStoryteller}|{StorytellerDefname}";
        }
#if SERVER
        private static string FilePath => Path.Combine(Master.ConfigsPath, "StorytellerConfig.json");

        public static StorytellerValuesFile Load()
        {
            if (File.Exists(FilePath))
            {
                return Serializer.SerializeFromFile<StorytellerValuesFile>(FilePath);
            }
            else
            {
                var obj = new StorytellerValuesFile();
                Serializer.SerializeToFile(FilePath, obj);
                return obj;
            }
        }

        public static bool Save()
        {
            try
            {
                Serializer.SerializeToFile(FilePath, Master.StorytellerValues);
                return true;
            }
            catch
            {
                return false;
            }
        }
#endif
    }
}