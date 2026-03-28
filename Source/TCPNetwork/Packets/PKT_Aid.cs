namespace TCPNetwork.Packets
{
    public class PKT_Aid : PKT_Base
    {
        public AidStepMode _stepMode { get; set; } = AidStepMode.Send;

        public enum AidStepMode { Send, Receive, Accept, Reject }

        public int _fromTile { get; set; } = -1;

        public int _toTile { get; set; } = -1;

        public string _humanData { get; set; } = string.Empty;
    }
}