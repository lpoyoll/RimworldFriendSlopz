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
            
            switch (packet.CurrentStep)
            {
                case PKT_SettlementCustomization.StepMode.Set:
                    ChangeCustomizations(client, packet);
                    break;
                
                case PKT_SettlementCustomization.StepMode.Refresh:
                    SendCustomizations(client, packet);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            
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

        private static void SendCustomizations(ServerClient client, PKT_SettlementCustomization packet)
        {
            FL_Player playerFile = client.GetData<FL_Player>();

            packet.IconID = playerFile.Customizations.SettlementIconID;
            packet.IconColor = playerFile.Customizations.SettlementIconColor;
            client.Listener.EnqueuePacket(PacketHeader.SettlementCustomization, packet);
        }
    }
}