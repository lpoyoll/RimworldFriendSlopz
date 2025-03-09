using System;
using System.Collections.Generic;

namespace Shared
{
    [Serializable]
    public class PlayerRecountData
    {
        public int _currentPlayerCount;

        public List<string> _currentPlayerNames = new List<string>();
    }
}
