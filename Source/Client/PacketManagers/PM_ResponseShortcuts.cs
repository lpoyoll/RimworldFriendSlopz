using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using Shared;
using TCPNetwork;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using static TCPNetwork.Packets.PKT_ResponseShortcut;

namespace GameClient.PacketManagers
{
    public class PM_ResponseShortcuts : PM_Base
    {
        [HandlesPacket(PacketHeader.ResponseShortcutManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_ResponseShortcut data = Serializer.ConvertBytesToObject<PKT_ResponseShortcut>(bytes);

            switch (data._stepMode)
            {
                case ResponseStepMode.IllegalAction:
                    DLG_Wait.Instance.Close();
                    DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "Kicked for ilegal actions!" }));
                    break;

                case ResponseStepMode.UserUnavailable:
                    DLG_Wait.Instance.Close();
                    DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "Player is not currently available!" }));
                    break;

                case ResponseStepMode.Unavailable:
                    DLG_Wait.Instance.Close();
                    DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "Action is not currently available!" }));
                    break;

                case ResponseStepMode.NoPower:
                    DLG_Wait.Instance.Close();
                    DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "You don't have enough power for this action!" }));
                    break;

                case ResponseStepMode.Pop:
                    DLG_Wait.Instance.Close();
                    break;
            }
        }
    }
}