using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Managers;
using RimWorld;
using RTShared;
using RTNetwork;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using static RTShared.CommonEnumerators;
using RTNetwork.Components;

namespace GameClient.PacketManagers
{
    public class PM_Console : PM_Base
    {
        [HandlesPacket(PacketHeader.Console)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Command data = Serializer.ConvertBytesToObject<PKT_Command>(bytes);

            switch (data._commandMode)
            {
                case CommandMode.Op:
                    OnOpCommand();
                    break;

                case CommandMode.Deop:
                    OnDeopCommand();
                    break;

                case CommandMode.Broadcast:
                    OnBroadcastCommand(data);
                    break;

                case CommandMode.ForceSave:
                    OnForceSaveCommand();
                    break;
            }
        }

        private static void OnOpCommand()
        {
            SessionManager.IsAdmin = true;
            DLG_Base.PushNewDialog(new DLG_Message("MESSAGE", new string[] { "You are now an admin!" }));
        }

        private static void OnDeopCommand()
        {
            SessionManager.IsAdmin = false;
            DLG_Base.PushNewDialog(new DLG_Message("MESSAGE", new string[] { "You are no longer an admin!" }));
        }

        private static void OnBroadcastCommand(PKT_Command commandData)
        {
            RimworldManager.GenerateLetter("Server Broadcast", PM_Chat.ParseMessage(commandData._details, true), LetterDefOf.PositiveEvent);
        }

        private static void OnForceSaveCommand() { PM_Saves.ForceSave(); }
    }
}
