using Shared.Files.Configs.Mods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPNetwork.Packets.ServerBrowser
{
    public class PKT_BrowserTelemetry : PKT_Base
    {
        public string Hash { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public string Endpoint { get; set; } = string.Empty;

        public int Port { get; set; } = int.MaxValue;

        public int CurrentPopulation { get; set; } = int.MaxValue;

        public int MaxPopulation { get; set; } = int.MaxValue;

        public bool IsPrivate { get; set; } = false;

        public List<ModConfig> Mods { get; set; } = new List<ModConfig>();
    }
}
