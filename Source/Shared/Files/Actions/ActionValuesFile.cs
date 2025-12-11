using System;
using System.IO;

namespace Shared.Files.Actions
{
    public class ActionValuesFile : BaseFile
    {
        public static string Path { get; set; } = string.Empty;

        public bool EnableActivities { get; set; } = true;

        public bool EnableSites { get; set; } = true;

        public bool EnableRoads { get; set; } = true;

        public bool EnableFactions { get; set; } = true;

        public bool EnableTrading { get; set; } = true;

        public bool EnableCustomScenarios { get; set; } = true;

        public bool EnableNPCDestruction { get; set; } = false;

        public bool EnablePollutionSpread { get; set; } = true;

        public EventAction EventAction { get; set; } = new EventAction();

        public AidAction AidAction { get; set; } = new AidAction();

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
                ActionValuesFile file = new ActionValuesFile();
                Serializer.SerializeToFile(Path, file);
                return file;
            }
        }
    }
}
