using Shared.Files;
using System;
using System.IO;

namespace GameServer.Files
{
    public class ServerBrowserConfigFile : BaseFile
    {
        public static string SavePath { get; set; } = string.Empty;

        public bool EnableServerBrowser { get; set; } = true;
    }
}
