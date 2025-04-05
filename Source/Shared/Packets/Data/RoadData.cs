using System;
using MessagePack;
using static Shared.CommonEnumerators;

namespace Shared
{
    [MessagePackObject]
    public class RoadData
    {
        public RoadStepMode _stepMode;
        
        public RoadDetails _details;
    }
}