using System;
using MessagePack;
using static Shared.CommonEnumerators;

namespace Shared
{
    [MessagePackObject]
    public class WorldData
    {
        public WorldStepMode _stepMode;

        public byte[] _fileBytes;
    }
}
