using System;
using MessagePack;
using static Shared.CommonEnumerators;

namespace Shared
{
    [MessagePackObject]
    public class EventData
    {
        public EventStepMode _stepMode;
        
        public int _fromTile;

        public int _toTile;

        public EventFile _eventFile;
    }
}
