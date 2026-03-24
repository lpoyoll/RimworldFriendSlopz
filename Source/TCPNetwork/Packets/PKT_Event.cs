using Shared.Files;
using System.Collections.Generic;
using static Shared.CommonEnumerators;

namespace TCPNetwork.Packets
{
    public class PKT_Event : PKT_Base
    {
        public enum EventStepMode { Send, Receive, Recover, Customize, Set }

        public EventStepMode _stepMode { get; set; } = EventStepMode.Send;

        public int _fromTile { get; set; } = -1;

        public int _toTile { get; set; } = -1;

        public EventFile _eventFile { get; set; } = null;

        public List<EventFile> _eventFiles { get; set; } = null;
    }
}
