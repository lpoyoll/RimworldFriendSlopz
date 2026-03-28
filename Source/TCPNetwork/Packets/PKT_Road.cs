using Shared.Details.Planet;

namespace TCPNetwork.Packets
{
    public class PKT_Road : PKT_Base
    {
        public RoadStepMode _stepMode { get; set; } = RoadStepMode.Add;

        public enum RoadStepMode { Add, Remove }

        public RoadDetail _details { get; set; } = null;
    }
}