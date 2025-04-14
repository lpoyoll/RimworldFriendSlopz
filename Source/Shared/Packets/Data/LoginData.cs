using System.Collections.Generic;
using static Shared.CommonEnumerators;

namespace Shared
{

    public class LoginData
    {
        public string _uid { get; set; }

        public string _username { get; set; }

        public ModConfigFile _runningMods { get; set; }

        public LoginResponse _tryResponse { get; set; }

        public List<string> _extraDetails { get; set; } = new List<string>();
    }
}
