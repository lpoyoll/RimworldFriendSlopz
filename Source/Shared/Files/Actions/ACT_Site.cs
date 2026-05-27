using System.Collections.Generic;

namespace Shared.Files.Actions
{
    public class ACT_Site : ACT_Base
    {
        public override bool IsEnabled { get; set; } = true;

        public override double Cooldown { get; set; } = -1;

        public double TimeInterval { get; set; } = 1800000;

        public int BuildingCost { get; set; } = 3000;

        public int RewardsCount { get; set; } = 100;
    }
}
