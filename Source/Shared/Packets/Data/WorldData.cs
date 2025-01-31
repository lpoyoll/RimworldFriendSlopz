using System;
using static Shared.CommonEnumerators;

namespace Shared
{
    [Serializable]
    public class WorldData
    {
        public WorldStepMode _stepMode;

        public byte[] _fileBytes;
    }
}
