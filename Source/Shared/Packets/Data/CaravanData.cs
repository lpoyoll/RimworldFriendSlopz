using System;
using MessagePack;
using static Shared.CommonEnumerators;

namespace Shared
{
    [MessagePackObject]
    public class CaravanData
    {
        public CaravanStepMode _stepMode;

        public CaravanFile _caravanFile;
    }
}