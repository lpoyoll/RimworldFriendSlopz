using System;
using MessagePack;
using static Shared.CommonEnumerators;

namespace Shared
{
    [MessagePackObject]
    public class ActivityData
    {
        public ActivityStepMode _stepMode;

        public int _targetTile;
        
        public MapFile _mapFile;
    }
}