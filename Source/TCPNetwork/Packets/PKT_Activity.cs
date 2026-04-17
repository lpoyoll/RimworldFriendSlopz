using Shared.Files;

namespace TCPNetwork.Packets
{
    public class PKT_Activity : PKT_Base
    {
        public ActivityStepMode _stepMode { get; set; } = ActivityStepMode.Request;

        public enum ActivityStepMode { Request, Deny }

        public enum ActivityType { Raid, Zoom }

        public int _targetTile { get; set; } = -1;

        public FL_Map _file { get; set; } = null;
    }
}