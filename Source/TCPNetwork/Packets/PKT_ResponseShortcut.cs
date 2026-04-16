namespace TCPNetwork.Packets
{
    public class PKT_ResponseShortcut : PKT_Base
    {
        public ResponseStepMode _stepMode { get; set; } = ResponseStepMode.IllegalAction;

        public enum ResponseStepMode { IllegalAction, UserUnavailable, Pop, NoPower, Unavailable }
    }
}