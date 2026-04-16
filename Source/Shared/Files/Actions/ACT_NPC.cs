using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Files.Actions
{
    public class ACT_NPC : ACT_Base
    {
        public override bool IsEnabled { get; set; } = false;

        public override double Cooldown { get; set; } = 60000;
    }
}
