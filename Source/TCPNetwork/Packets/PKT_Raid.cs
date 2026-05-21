using Shared.Files;

namespace TCPNetwork.Packets
{
    public class PKT_Raid : PKT_Base
    {
        public StepMode CurrentStepMode { get; set; } = StepMode.Request;

        public enum StepMode { Request, Deny }

        public int TargetTile { get; set; } = int.MinValue;

        public FL_Map Map { get; set; } = null;
    }
}