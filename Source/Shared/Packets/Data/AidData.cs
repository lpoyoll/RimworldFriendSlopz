using static Shared.CommonEnumerators;

namespace Shared
{

    public class AidData
    {
        public AidStepMode _stepMode { get; set; }

        public int _fromTile { get; set; }

        public int _toTile { get; set; }

        public HumanFile _humanData { get; set; }
    }
}