using GameClient.Dialogs;
using GameClient.Hooks.TCPNetwork;
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
                foreach (ModConfig mod in selectedServer.Mods.Where(fetch => fetch.Type != ModConfigFile.ModType.Forbidden)) modNames.Add(mod.FileName);

                DLG_Listing dialog = new DLG_Listing(selectedServer.Name, "Server Mods", modNames.ToArray(), 
                    delegate { ConnectToServer(selectedServer); }, "Connect");

                DLG_Base.PushNewDialog(dialog);
            };

            DLG_Wait.Instance.Close();
            DLG_Base.PushNewDialog(new DLG_ListingWithButton(title, description, serverNames.ToArray(), r1));
        }

        private void ConnectToServer(PKT_BrowserTelemetry telemetry)
        {
            Network.Ip = telemetry.Endpoint;
            Network.Port = telemetry.Port;

            DLG_Base.PushNewDialog(new DLG_Wait("Trying to connect to server"));
            ClientNetwork _ = new ClientNetwork();
        }
    }
}
