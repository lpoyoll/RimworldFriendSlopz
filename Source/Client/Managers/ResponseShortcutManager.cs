using GameClient.Dialogs;
using Shared;

namespace GameClient.Managers
{
    [RTManager]
    public static class ResponseShortcutManager
    {
        [HandlesPacket(PacketHeader.ResponseShortcutManager)]
        private static void ParsePacket(byte[] bytes)
        {
            ResponseShortcutData data = Serializer.ConvertBytesToObject<ResponseShortcutData>(bytes);

            switch (data.stepMode)
            {
                case CommonEnumerators.ResponseStepMode.IllegalAction:
                    DialogManager.PopWaitDialog();
                    DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "Kicked for ilegal actions!" }));
                    break;

                case CommonEnumerators.ResponseStepMode.UserUnavailable:
                    DialogManager.PopWaitDialog();
                    DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "Player is not currently available!" }));
                    break;

                case CommonEnumerators.ResponseStepMode.Pop:
                    DialogManager.PopWaitDialog();
                    break;
            }
        }
    }
}