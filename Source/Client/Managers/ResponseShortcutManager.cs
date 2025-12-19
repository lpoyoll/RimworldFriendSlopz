using GameClient.Dialogs;
using TCPNetwork.Packets;
using Shared;

namespace GameClient.Managers;

public static class ResponseShortcutManager
{
    [HandlesPacket(PacketHeader.ResponseShortcutManager)]
    private static void ParsePacket(byte[] bytes)
    {
        ResponseShortcutData data = Serializer.ConvertBytesToObject<ResponseShortcutData>(bytes);

        switch (data._stepMode)
        {
            case CommonEnumerators.ResponseStepMode.IllegalAction:
                RT_Dialog_Wait.Instance.Close();
                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", ["Kicked for illegal actions!"]));
                break;

            case CommonEnumerators.ResponseStepMode.UserUnavailable:
                RT_Dialog_Wait.Instance.Close();
                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", ["Player is not currently available!"]));
                break;

            case CommonEnumerators.ResponseStepMode.NoPower:
                RT_Dialog_Wait.Instance.Close();
                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR",
                    ["You don't have enough power for this action!"]));
                break;

            case CommonEnumerators.ResponseStepMode.Pop:
                RT_Dialog_Wait.Instance.Close();
                break;
        }
    }
}