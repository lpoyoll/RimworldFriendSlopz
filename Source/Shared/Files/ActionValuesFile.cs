using System;
#if SERVER
using GameServer.Core;
#endif

namespace Shared
{
    [Serializable]
    public class ActionValuesFile
    {
        public bool EnableActivities = true;

        public bool EnableEvents = true;

        public bool EnableSites = true;

        public bool EnableRoads = true;

        public bool EnableFactions = true;

        public bool EnableAids = true;

        public bool EnableTrading = true;

        public bool EnableSpying = true;

        public bool EnableCustomScenarios = true;

        public bool EnableNPCDestruction = false;

        public bool EnablePollutionSpread = true;

        public int EnforcedGameSpeed = 0;

        public int SpyCost = 100;

        public override string ToString()
        {
            return $"ActionValuesFile:|{EnableActivities}|{EnableEvents}|{EnableSites}|{EnableRoads}|{EnableFactions}|{EnableAids}|{EnableTrading}" +
                $"|{EnableSpying}|{EnableCustomScenarios}|{EnableNPCDestruction}|{EnablePollutionSpread}|{EnforcedGameSpeed}|{SpyCost}";
        }

#if SERVER
        private static string FilePath => Path.Combine(Master.ConfigsPath, "ActionConfig.json");

        public static ActionValuesFile Load()
        {
            if (File.Exists(FilePath)) return Serializer.SerializeFromFile<ActionValuesFile>(FilePath);
            else
            {
                ActionValuesFile obj = new ActionValuesFile();
                Serializer.SerializeToFile(FilePath, obj);
                return obj;
            }
        }

        public static bool Save()
        {
            try
            {
                Serializer.SerializeToFile(FilePath, Master.ActionConfigs);
                return true;
            }
            catch { return false; }
        }
#endif

    }
}
