using Shared.Files;
using System.Collections.Generic;

namespace TCPNetwork.Packets
{
    public class PKT_Site : PKT_Base
    {
        public enum SiteStepMode { Accept, Build, Destroy, Rewards, Worker,
            RetrieveWorker
        }

        public SiteStepMode _stepMode { get; set; } = SiteStepMode.Accept;

        public FL_Site File { get; set; } = new FL_Site();

        public List<FL_Site> Files { get; set; } = new List<FL_Site>();
    }
}
