using System;
using System.Collections.Generic;
using System.Text;

namespace TCPNetwork.Packets
{
    public class PKT_Version : PKT_Base
    {
        public string Version { get; set; } = string.Empty;
    }
}