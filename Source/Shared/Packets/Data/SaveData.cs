using System;
using static Shared.CommonEnumerators;

namespace Shared
{
    [Serializable]
    public class SaveData
    {
        public SaveStepMode _stepMode;

        public byte[] _fileBytes;
        
        public int _instructions = -1;
    }
}
