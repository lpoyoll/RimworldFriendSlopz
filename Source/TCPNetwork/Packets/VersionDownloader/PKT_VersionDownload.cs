using System;
using System.Collections.Generic;
using System.Text;

namespace TCPNetwork.Packets.VersionDownloader
{
    public class PKT_VersionDownload : PKT_Base
    {
        public enum StepMode { Ask, Receive, Deny }

        public StepMode CurrentStepMode { get; set; } = StepMode.Ask;

        public string RequestedVersion { get; set; } = string.Empty;

        public byte[] VersionContents { get; set; } = null;
    }
}
