using System.IO;
using System.Linq;
using GameClient.Dialogs;
using GameClient.TCP;

namespace GameClient.Managers
{
    public static class AdminMenuManager
    {
        private static readonly string dialogTitle = "Admin menu";

        private static readonly string dialogDescription = "Choose which action to execute";

        private static readonly string[] menuButtons = new string[] { "Mod Manager", "Upload save"};

        public static void ShowAdminMenu()
        {
            RT_Dialog_ScrollButtons d1 = new RT_Dialog_ScrollButtons(dialogTitle, dialogDescription,
                menuButtons, delegate { OpenSpecificMenu(); }, null);

            RT_Dialog_Base.PushNewDialog(d1);
        }

        public static void OpenSpecificMenu()
        {
            switch (RT_Dialog_ScrollButtons.selectedScrollButton)
            {
                case 0:
                    ModManager.OpenModManagerMenu(false);
                    break;
                case 1:
                    var saves = SaveManager.GetAllSaveFiles();
                    var dialog = new RT_Dialog_ListingWithButton(
                        "Save menu",
                        "Select a save to upload:",
                        saves.Keys.ToArray(),
                        delegate
                        {
                            var D2 = new RT_Dialog_YesNo("This will replace your current RT save, are you sure?", delegate
                            {
                                if (saves.TryGetValue(RT_Dialog_ListingWithButton.dialogButtonListingResultString, out var file))
                                {
                                    var data = File.ReadAllBytes(file);
                                    File.WriteAllBytes(SaveManager.saveFilePath, data);
                                    SaveSenderManager.SendSaveToServer();
                                    DisconnectionManager.isIntentionalDisconnect = true;
                                    DisconnectionManager.intentionalDisconnectReason = DisconnectionManager.DCReason.UploadSave;
                                    Network.DisconnectFromServer();
                                }
                            });
                            RT_Dialog_Base.PushNewDialog(D2);
                        });
                    RT_Dialog_Base.PushNewDialog(dialog);
                    break;
            }
        }
    }
}