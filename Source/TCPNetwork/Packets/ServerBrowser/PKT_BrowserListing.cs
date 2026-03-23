using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPNetwork.Packets.ServerBrowser
{
    public class PKT_BrowserListing : PKT_Base
    {
        public string ClientVersion { get; set; } = string.Empty;

        public List<PKT_BrowserTelemetry> Listings = new List<PKT_BrowserTelemetry>();
    }
}
