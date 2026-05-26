using Shared.Files;
using System;
using System.Collections.Generic;
using System.Text;

namespace TCPNetwork.Packets
{
    public class PKT_WorldObject : PKT_Base
    {
        public enum StepMode { Add, Remove }

        public StepMode CurrentStepMode { get; set; } = StepMode.Add;

        public enum WorldObjectMode { Settlement, Site}

        public WorldObjectMode Type { get; set; } = WorldObjectMode.Settlement;

        public FL_WorldObject WorldObject { get; set; } = new FL_WorldObject();
    }
}
