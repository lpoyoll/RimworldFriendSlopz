using RTServer.Hooks.ServerBrowser;
using RTShared.Commands;
using RTShared.Misc;

namespace RTServer.Commands
{
    public class CMD_Ip : CMD_Base
    {
        public CMD_Ip()
        {
            Prefix = "getip";
            Description = "Returns the public IPV4 of this server";
        }

        public override void Action() { Printer.Title($"Your server IPV4 is {ServerBrowserManager.GetPublicIP().Result}"); }
    }
}
