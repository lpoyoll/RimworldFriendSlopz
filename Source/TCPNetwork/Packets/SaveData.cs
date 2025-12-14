using static Shared.CommonEnumerators;

namespace TCPNetwork.Packets
{

    public class SaveData
    {
        public SaveStepMode _stepMode { get; set; } = SaveStepMode.Send;

        public byte[] _fileBytes { get; set; } = null;

        public bool _forceDisconnect { get; set; } = false;

        public bool _forceUseSave { get; set; } = false;
    }
}
