using Shared.Files;

namespace TCPNetwork.Packets
{
    public class PKT_Caravan : PKT_Base
    {
        public CaravanStepMode _stepMode { get; set; } = CaravanStepMode.Add;

        public enum CaravanStepMode { Add, Remove, Move }

        public FL_Caravan _caravanFile { get; set; } = null;
    }
}