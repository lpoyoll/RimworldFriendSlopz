using Shared.Files.Configs;

namespace Shared.Files.Actions
{
    public class ACT_Road : ACT_Base
    {
        public override bool IsEnabled { get; set; } = true;

        public override double Cooldown { get; set; } = 1000;

        public FL_RoadsConfig RoadValues { get; set; } = new FL_RoadsConfig();
    }
}
