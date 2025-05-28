#if SERVER
using GameServer.Core;
#endif
namespace Shared
{
    public class ScenarioValuesFile
    {
        public bool EnforceScenario;

        public string ScenarioName;

        public override string ToString()
        {
            return $"ScenarioValuesFile:|{EnforceScenario}|{ScenarioName}";
        }
#if SERVER
        private static string FilePath => Path.Combine(Master.ConfigsPath, "ScenarioConfig.json");

        public static ScenarioValuesFile Load()
        {
            if (File.Exists(FilePath)) return Serializer.SerializeFromFile<ScenarioValuesFile>(FilePath);
            else
            {
                ScenarioValuesFile obj = new ScenarioValuesFile();
                Serializer.SerializeToFile(FilePath, obj);
                return obj;
            }
        }

        public static bool Save()
        {
            try
            {
                Serializer.SerializeToFile(FilePath, Master.ScenarioValues);
                return true;
            }
            catch { return false; }
        }
#endif
    }
}