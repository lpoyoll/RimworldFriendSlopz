using Shared.Files;

namespace TCPNetwork.Packets
{
    public class PKT_Caravan : PKT_Base
    {
        public CaravanStepMode _stepMode { get; set; } = CaravanStepMode.Add;

        public enum CaravanStepMode { Add, Remove, Move }

        public CaravanFile _caravanFile { get; set; } = null;
    }
}