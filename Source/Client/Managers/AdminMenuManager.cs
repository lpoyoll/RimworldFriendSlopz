using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameClient.Dialogs;
using GameClient.Misc;
using GameClient.Values;
using Shared;

namespace GameClient.Managers
{
    public static class AdminMenuManager
    {
        private static string DialogTitle { get; set; } = "Admin menu";
         
        private static string DialogDescription { get; set; } = "Choose which action to execute";

        private static string[] MenuButtons { get; set; } = new string[]
        {
            "Mod Manager",
            "Event Manager",
            "Save Manager (BETA)"
        };

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
                    EventManager.ShowEventTweakerMenu();
                    break;

                case 2:
                    SaveManager.OpenSaveUploaderMenu();
                    break;
            }
        }
    }
}