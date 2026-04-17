using Shared.Files.Configs;
using System.Collections.Generic;

namespace TCPNetwork.Packets
{
    public class PKT_Login : PKT_Base
    {
        public enum LoginResponse { Invalid, Ban, Duplicate, Mods, Version, Full, Whitelist, NoWorld }

        public string _username { get; set; } = string.Empty;

        public string _password { get; set; } = string.Empty;

        public FL_ModConfig _runningMods { get; set; } = null;

        public LoginResponse _tryResponse { get; set; } = LoginResponse.Invalid;

        public List<string> _extraDetails { get; set; } = new List<string>();
    }
}
