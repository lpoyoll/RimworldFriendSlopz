using MessagePack;
using Shared;
using Shared.Misc;
using System;
using static Shared.CommonEnumerators;
using static TCPNetwork.Packets.PKT_Synchronous;

namespace TCPNetwork.Packets
{
    public class PKT_Save : PKT_Base
    {
        public SaveStepMode _stepMode { get; set; } = SaveStepMode.Send;

        public enum SaveStepMode { Send, Receive, Reset }

        public bool _forceDisconnect { get; set; } = false;

        public bool _forceUseSave { get; set; } = false;

        public byte[] _fileBytes { get; set; } = null;
    }
}
