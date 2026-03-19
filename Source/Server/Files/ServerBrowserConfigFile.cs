using Shared.Files;
using System;
using System.IO;

namespace GameServer.Files
{
    public class ServerBrowserConfigFile : BaseFile
    {
        public static string SavePath { get; set; } = string.Empty;

        public bool EnableServerBrowser { get; set; } = false;

        public bool EnableServerTelemetry { get; set; } = true;

        public string PublicEndPoint { get; set; } = string.Empty;
    }
}
