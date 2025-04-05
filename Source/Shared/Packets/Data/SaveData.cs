using System;
using MessagePack;
using static Shared.CommonEnumerators;

namespace Shared
{
    [MessagePackObject]
    public class SaveData
    {
        public SaveStepMode _stepMode;

        public byte[] _fileBytes;
        
        public int _instructions = -1;
    }
}
