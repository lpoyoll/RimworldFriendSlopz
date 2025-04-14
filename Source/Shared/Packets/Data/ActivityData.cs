using static Shared.CommonEnumerators;

namespace Shared
{

    public class ActivityData
    {
        public ActivityStepMode _stepMode { get; set; }

        public int _targetTile { get; set; }
        
        public MapFile _mapFile { get; set; }
    }
}