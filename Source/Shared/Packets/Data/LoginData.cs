using System;
using System.Collections.Generic;
using MessagePack;
using static Shared.CommonEnumerators;

namespace Shared
{
    [MessagePackObject]
    public class LoginData
    {
        public string _uid;

        public string _username;

        public ModConfigFile _runningMods;

        public LoginResponse _tryResponse;

        public List<string> _extraDetails = new List<string>();
    }
}
