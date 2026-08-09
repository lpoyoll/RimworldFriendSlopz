using RTNetwork.Components;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using RTServer.Misc;
using RTShared.Files.Player;
using RTShared.Misc;

namespace RTServer.PacketManagers
{
    public class PM_SettlementCustomization : PM_Base
    {
        [HandlesPacket(PacketHeader.SettlementCustomization)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_SettlementCustomization packet = Serializer.ConvertBytesToObject<PKT_SettlementCustomization>(bytes);
            ChangeCustomizations(client, packet);
        }

        private static void ChangeCustomizations(ServerClient client, PKT_SettlementCustomization packet)
        {
            FL_Player playerFile = client.GetData<FL_Player>();
            playerFile.Customizations.SettlementIconID = packet.IconID;
            playerFile.Customizations.SettlementIconColor = packet.IconColor;
            playerFile.SaveUserFile();
            
            client.Listener.EnqueuePacket(PacketHeader.SettlementCustomization, packet);
            InformationDisplayer.DisplayChangedCustomizations(playerFile.Username);
        }
    }
}