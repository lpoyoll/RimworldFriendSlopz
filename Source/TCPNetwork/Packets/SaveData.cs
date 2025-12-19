using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using MessagePack;
using Shared;
using Shared.Misc;
using static Shared.CommonEnumerators;

namespace TCPNetwork.Packets
{
    [MessagePackObject]
    public class SaveData
    {
        [Key(0)] public SaveDataHeader _header;
        [Key(1)] public byte[] _fileBytes { get; set; } = null;
        
        public static int ParseSavePacket(byte[] packet, out SaveDataHeader header)
        {
            if (packet.Length < 3)
            {
                throw new Exception($"Invalid packet length ({packet.Length}) for save data!");
            }
            
            var mode = (SaveStepMode)packet[0];
            var forceDisconnect = packet[1] == 1;
            var forceUseSave = packet[2] == 1;
            
            header = new SaveDataHeader(mode, forceDisconnect, forceUseSave);
            return 3;
        }

        public byte[] SerializeSavePacket()
        {
            byte[] result = new byte[3 + (_fileBytes?.Length ?? 0)];
            result[0] = (byte)_header._stepMode;
            result[1] = (byte)(_header._forceDisconnect ? 1 : 0);
            result[2] = (byte)(_header._forceUseSave ? 1 : 0);
            if (_fileBytes != null)
            {
                _fileBytes.CopyTo(result.AsSpan().Slice(3));
            } 
            return result;
        }
    }

    [MessagePackObject]
    [StructLayout(LayoutKind.Sequential)]
    public struct SaveDataHeader(SaveStepMode stepMode, bool forceDisconnect = false, bool forceUseSave = false)
    {
        [Key(0)] public SaveStepMode _stepMode { get; set; } = stepMode;
        [Key(1)] public bool _forceDisconnect { get; set; } = forceDisconnect;
        [Key(2)] public bool _forceUseSave { get; set; } = forceUseSave;
    }
}
