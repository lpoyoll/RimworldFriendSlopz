using GameClient.Dialogs;
using Shared;
using Shared.Files.Configs.Mods;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets.ServerBrowser;

namespace GameClient.PacketManagers
{
    public class PM_ServerBrowserListing : PM_Base
    {
        [HandlesPacket(PacketHeader.ServerBrowserListing)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_BrowserListing listing = Serializer.ConvertBytesToObject<PKT_BrowserListing>(bytes);

            string title = "Server Browser";
            string description = "Current available servers on the browser";
            List<string> serverNames = new List<string>();
            foreach (PKT_BrowserTelemetry telemetry in listing.Listings) serverNames.Add(telemetry.Name);

            Action r1 = delegate
            {
                PKT_BrowserTelemetry selectedServer = listing.Listings[DLG_ListingWithButton.DialogButtonListingResultInt];

                List<string> modNames = new List<string>();
                foreach (ModConfig mod in selectedServer.Mods) modNames.Add(mod.FileName);
                DLG_Base.PushNewDialog(new DLG_Listing(selectedServer.Name, "Server Mods", modNames.ToArray()));
            };

            DLG_Wait.Instance.Close();
            DLG_Base.PushNewDialog(new DLG_ListingWithButton(title, description, serverNames.ToArray(), r1));
        }
    }
}
