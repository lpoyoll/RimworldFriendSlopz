using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Dialogs.ServerBrowser;
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
            PKT_ServerInformation listing = Serializer.ConvertBytesToObject<PKT_ServerInformation>(bytes);

            Action r1 = delegate
            {
                PKT_ServerTelemetry selectedServer = listing.Listings[DLG_ServerBrowser.ResultInt];
                DLG_Base.PushNewDialog(new DLG_ServerListing(selectedServer, delegate { ConnectToServer(selectedServer); }));
            };

            DLG_Wait.Instance.Close();
            DLG_Base.PushNewDialog(new DLG_ServerBrowser(listing.Listings, r1));
        }

        private void ConnectToServer(PKT_ServerTelemetry telemetry)
        {
            Network.Ip = telemetry.Endpoint;
            Network.Port = telemetry.Port;

            DLG_Base.PushNewDialog(new DLG_Wait("Trying to connect to server"));
            ClientNetwork _ = new ClientNetwork();
        }
    }
}
