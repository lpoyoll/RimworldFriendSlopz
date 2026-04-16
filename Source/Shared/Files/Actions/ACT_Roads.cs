using Shared.Files.Configs;

namespace Shared.Files.Actions
{
    public class ACT_Roads : ACT_Base
    {
        public override bool IsEnabled { get; set; } = true;

        public override double Cooldown { get; set; } = 1000;

        public RoadsConfigFile RoadValues { get; set; } = new RoadsConfigFile();
    }
}
