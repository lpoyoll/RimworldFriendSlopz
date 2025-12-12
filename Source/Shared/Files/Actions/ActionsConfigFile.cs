using System;
using System.IO;

namespace Shared.Files.Actions
{
    public class ActionsConfigFile : BaseFile
    {
        public static string Path { get; set; } = string.Empty;

        public bool EnableFactions { get; set; } = true;

        public bool EnableTrading { get; set; } = true;

        public bool EnableCustomScenarios { get; set; } = true;

        public bool EnableNPCDestruction { get; set; } = false;

        public bool EnablePollutionSpread { get; set; } = true;

        public ActivityAction ActivityAction { get; set; } = new ActivityAction();

        public EventAction EventAction { get; set; } = new EventAction();

        public AidAction AidAction { get; set; } = new AidAction();

        public RoadsAction RoadsAction { get; set; } = new RoadsAction();

        public SiteAction SiteAction { get; set; } = new SiteAction();

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
                ActionsConfigFile file = new ActionsConfigFile();
                Serializer.SerializeToFile(Path, file);
                return file;
            }
        }
    }
}
