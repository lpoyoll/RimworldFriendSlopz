using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Dialogs.ServerBrowser;
using GameClient.Hooks.TCPNetwork;
using Shared;
using System;
using TCPNetwork;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets.ServerBrowser;

namespace GameClient.PacketManagers
{
    public class PM_ServerBrowserListing : PM_Base
    {
        [HandlesPacket(PacketHeader.ServerBrowserListing)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_ServerInformation listing = Serializer.ConvertBytesToObject<PKT_ServerInformation>(bytes);
            DLG_Base.PushNewDialog(new DLG_ServerBrowser(listing.Listings));
            DLG_Wait.Instance.Close();
        }
    }
}
