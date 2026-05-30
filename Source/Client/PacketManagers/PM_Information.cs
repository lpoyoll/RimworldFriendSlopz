using GameClient.Dialogs;
using RTNetwork.Packets;
using RTShared;
using RTNetwork;
using RTShared.Files;
using GameClient.Dialogs.Default;
using RTNetwork.PacketManagers;
using RTNetwork.Components;
using GameClient.Managers;

namespace GameClient.PacketManagers
{
    public class PM_Information : PM_Base
    {
        [HandlesPacket(PacketHeader.Information)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Information data = Serializer.ConvertBytesToObject<PKT_Information>(bytes);

            switch (data._stepMode)
            {
                case PKT_Information.InfoStepMode.Connection:
                    ReceiveInformation(data);
                    break;

                case PKT_Information.InfoStepMode.Wealth:
                    ReceiveWealth(data);
                    break;
            }
        }

        public static void AskForInformation()
        {
            DLG_Base.PushNewDialog(new DLG_Wait());

            PKT_Information data = new PKT_Information();
            data._stepMode = PKT_Information.InfoStepMode.Connection;
            data._settlementTile = SessionManager.ChosenSettlement.Tile;

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Information, data);
        }

        public static void AskForWealth()
        {
            DLG_Base.PushNewDialog(new DLG_Wait());

            PKT_Information data = new PKT_Information();
            data._stepMode = PKT_Information.InfoStepMode.Wealth;
            data._settlementTile = SessionManager.ChosenSettlement.Tile;

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Information, data);
        }

        public static void ReceiveInformation(PKT_Information data)
        {
            DLG_Wait.Instance.Close();

            string connectionString = data._isPlayerOnline ? "connected" : "not connected";

            string title = "Information";
            string[] messages = new string[] { $"The player is {connectionString}" };
            DLG_Base.PushNewDialog(new DLG_Message(title, messages));
        }

        public static void ReceiveWealth(PKT_Information data)
        {
            DLG_Wait.Instance.Close();

            string title = "Information";
            string[] messages = new string[] { $"The wealth of the map is {data._settlementWealth}" };
            DLG_Base.PushNewDialog(new DLG_Message(title, messages));
        }
    }
}
