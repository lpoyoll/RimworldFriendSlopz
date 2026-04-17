using System.Collections.Generic;

namespace Shared.Files.Configs
{
    public class FL_WhitelistConfig : FL_Base
    {
        public static string SavePath { get; set; } = string.Empty;

        public bool UseWhitelist { get; set; } = false;

        public List<string> WhitelistedUsers { get; set; } = new List<string>() { };
    }
}
