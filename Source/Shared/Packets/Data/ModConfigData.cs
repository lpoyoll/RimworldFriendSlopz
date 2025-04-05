using System;
using MessagePack;
using static Shared.CommonEnumerators;

namespace Shared
{
    [MessagePackObject]
    public class ModConfigData
    {
        public ModConfigStepMode _stepMode;

        public ModConfigFile _configFile;
    }
}