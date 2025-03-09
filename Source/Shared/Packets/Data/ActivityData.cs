using System;
using static Shared.CommonEnumerators;

namespace Shared
{
    [Serializable]
    public class ActivityData
    {
        public ActivityStepMode _stepMode;

        public int _targetTile;
        
        public MapFile _mapFile;
    }
}