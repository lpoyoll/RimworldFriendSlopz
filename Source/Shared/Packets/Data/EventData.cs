using static Shared.CommonEnumerators;

namespace Shared
{

    public class EventData
    {
        public EventStepMode _stepMode { get; set; }

        public int _fromTile { get; set; }

        public int _toTile { get; set; }

        public EventFile _eventFile { get; set; }
    }
}
