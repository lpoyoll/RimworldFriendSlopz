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

        public string Endpoint { get; set; } = string.Empty;

        public int Port { get; set; } = int.MaxValue;

        public int Population { get; set; } = int.MaxValue;
    }
}
