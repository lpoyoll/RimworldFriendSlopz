using System;
using MessagePack;
using static Shared.CommonEnumerators;

namespace Shared
{
    [MessagePackObject]
    public class PlayerGuildData
    {
        public GuildStepMode _stepMode;

        public GuildFile _file = new GuildFile();

        public int _dataInt;
    }
}
