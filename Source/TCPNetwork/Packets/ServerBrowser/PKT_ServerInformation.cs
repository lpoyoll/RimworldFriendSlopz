using System.Collections.Generic;

namespace TCPNetwork.Packets.ServerBrowser
{
    public class PKT_ServerInformation : PKT_Base
    {
        public string ClientVersion { get; set; } = string.Empty;

        public List<PKT_ServerTelemetry> Listings = new List<PKT_ServerTelemetry>();
    }
}
