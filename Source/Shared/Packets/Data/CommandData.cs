using System;
using MessagePack;
using static Shared.CommonEnumerators;

namespace Shared
{
    [MessagePackObject]
    public class CommandData
    {
        public CommandMode _commandMode;

        public string _details;
    }
}
