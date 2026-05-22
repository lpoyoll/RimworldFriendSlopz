using System;
using System.Collections.Generic;
using System.Text;

namespace TCPNetwork.Packets
{
    public class PKT_Handshake : PKT_Base
    {
        public List<string> IncomingManagers { get; set; } = new List<string>();

        public enum StepMode { Check, Accept, Deny }

        public StepMode CurrentMode { get; set; } = StepMode.Check;
    }
}
