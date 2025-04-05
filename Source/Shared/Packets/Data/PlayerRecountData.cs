using System;
using System.Collections.Generic;
using MessagePack;

namespace Shared
{
    [MessagePackObject]
    public class PlayerRecountData
    {
        public int _currentPlayerCount;

        public List<string> _currentPlayerNames = new List<string>();
    }
}
