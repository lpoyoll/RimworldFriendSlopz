
using Verse;
using GameClient.Dialogs;
using GameClient.Misc;
using GameClient.Values;
using Shared;
using GameClient.TCP;

namespace GameClient.Managers
{
    //Class that contains all the disconnection functions that the mod uses
    public static class DisconnectionManager
    {
        //Useful disconnection variables

        public enum DCReason { None, SaveQuitToMenu, SaveQuitToOS, QuitToMenu, ConnectionLost, UploadSave }

        public static DCReason intentionalDisconnectReason;

        public static bool isIntentionalDisconnect;

        [HandlesPacket(PacketHeader.DisconnectSafe)]
        public static void HandleDisconnectFromServer() 
        {
            Network.listener.ClosingFlag = false;
            Network.listener.DisconnectFlag = true;
        }

        //Executes different actions depending on the disconnection mode

        public static void HandleDisconnect()
        {
            if (isIntentionalDisconnect)
            {
                string reason = "ERROR";

                switch (intentionalDisconnectReason)
                {
                    case DCReason.None:
                        reason = "No reason given";
                        DisconnectToMenu();
                        break;

                    case DCReason.QuitToMenu:
                        reason = "Quit to menu";
                        DisconnectToMenu();
                        break;

                    case DCReason.SaveQuitToMenu:
                        reason = "Save and Quit to Menu";
                        RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("MESSAGE", new string[] { "Your progress has been saved!" }, DisconnectToMenu));
                        break;

                    case DCReason.SaveQuitToOS:
                        reason = "Save and Quit to OS";
                        RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("MESSAGE", new string[] { "Your progress has been saved!" }, QuitGame));
                        break;

                    case DCReason.ConnectionLost:
                        reason = "Connection to server lost";
                        DisconnectToMenu();
                        break;

                    default:
                        reason = $"{intentionalDisconnectReason}";
                        DisconnectToMenu();
                        break;
                }

                Printer.Message($"Disconnected from server: {reason}");
            }

            else
            {
                Printer.Message($"Disconnected from server: Connection Lost");

                if (Current.ProgramState != ProgramState.Entry)
                {
                    RT_Dialog_Base.PushNewDialog(new RT_Dialog_YesNo("Connection lost. Save game?",
                        delegate { SaveManager.ForceSave(); DisconnectToMenu(); }, delegate { DisconnectToMenu(); }));
                }
                else DisconnectToMenu();
            }
        }

        //Kicks the client into the main menu

        public static void DisconnectToMenu()
        {
            ClientValues.CleanValues();
            SessionValues.CleanValues();
            ChatManager.CleanChat();

            RT_Dialog_Wait.Instance.Close();

            if (Current.ProgramState != ProgramState.Entry)
            {
                LongEventHandler.QueueLongEvent(delegate { }, "Entry", "", doAsynchronously: false, null);
            }
        }

        //Kicks the client into closing the game

        public static void QuitGame() { Root.Shutdown(); }

        //Kicks the client into restarting the game

        public static void RestartGame() { GenCommandLine.Restart(); }
    }
}
