using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameClient.Dialogs;
using GameClient.Misc;
using GameClient.TCP;
using GameClient.Values;

namespace GameClient.Managers
{
    public static class AdminMenuManager
    {
        private static string DialogTitle { get; set; } = "Admin menu";
         
        private static string DialogDescription { get; set; } = "Choose which action to execute";

        private static readonly string dialogDescription = "Choose which action to execute";

        private static readonly string[] MenuButtons = new string[] { "Mod Manager", "Save Uploader (BETA)"};

        public static void ShowAdminMenu()
        {
            RT_Dialog_ScrollButtons d1 = new RT_Dialog_ScrollButtons(DialogTitle, DialogDescription,
                MenuButtons, delegate { OpenSpecificMenu(); }, null);

            RT_Dialog_Base.PushNewDialog(d1);
        }

        public static void OpenSpecificMenu()
        {
            switch (RT_Dialog_ScrollButtons.SelectedScrollButton)
            {
                case 0:
                    ModManager.OpenModManagerMenu(false);
                    break;

                case 1:
                    Dictionary<string, string> saves = SaveManager.GetAllSaveFiles();
                    RT_Dialog_ListingWithButton dialog = new RT_Dialog_ListingWithButton("Save uploader",
                        "Select a save to upload:",
                        saves.Keys.ToArray(),
                        delegate
                        {
                            RT_Dialog_YesNo D2 = new RT_Dialog_YesNo("This feature is in beta and might fail, are you sure?", delegate
                            {
                                if (saves.TryGetValue(RT_Dialog_ListingWithButton.DialogButtonListingResultString, out string file))
                                {
                                    byte[] data = File.ReadAllBytes(file);
                                    File.WriteAllBytes(SaveManager.SaveFilePath, data);
                                    RT_Dialog_Base.PushNewDialog(new RT_Dialog_Wait("Waiting for save upload"));

                                    DisconnectionManager.SetIntentionalDisconnect(true, DisconnectionManager.DCReason.SaveQuitToMenu);
                                    SaveSenderManager.SendSaveToServer();
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