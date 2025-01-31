using System.Linq;
using Shared;
using GameClient.Core.Preferences;
using GameClient.Dialogs;
using GameClient.Files;
using GameClient.Misc;
using GameClient.TCP;

namespace GameClient.Managers
{
    [RTManager]
    public static class ConnectionManager
    {
        public static void ShowConnectDialogs()
        {
            DialogManager.PushNewDialog(new RT_Dialog_Inputs("Connection Details", new string[] { "IP", "Port" }, new bool[] { false, false },
                delegate { ParseConnectionDetails(); }));
        }

        public static void ParseConnectionDetails()
        {
            bool isInvalid = false;

            if (string.IsNullOrWhiteSpace(DialogManager.dialogInputResults[0])) isInvalid = true;
            if (string.IsNullOrWhiteSpace(DialogManager.dialogInputResults[1])) isInvalid = true;
            if (DialogManager.dialogInputResults[1].Count() > 5) isInvalid = true;
            if (!DialogManager.dialogInputResults[1].All(char.IsDigit)) isInvalid = true;

            if (isInvalid) DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "Server details are invalid! Please try again!" }));
            else
            {
                Network.ip = DialogManager.dialogInputResults[0];
                Network.port = DialogManager.dialogInputResults[1];
                ConnectionDataManager.SaveConnectionData(DialogManager.dialogInputResults[0], DialogManager.dialogInputResults[1]);

                DialogManager.PushNewDialog(new RT_Dialog_Wait("Trying to connect to server"));
                Threader.GenerateThread(Threader.Mode.Start);
            }
        }
    }
}