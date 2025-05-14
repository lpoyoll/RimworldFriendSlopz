using GameClient.Dialogs;

namespace GameClient.Managers
{
    public static class AdminMenuManager
    {
        private static readonly string dialogTitle = "Admin menu";

        private static readonly string dialogDescription = "Choose which action to execute";

        private static readonly string[] menuButtons = new string[] { "Mod Manager" };

        public static void ShowAdminMenu()
        {
            RT_Dialog_ScrollButtons d1 = new RT_Dialog_ScrollButtons(dialogTitle, dialogDescription,
                menuButtons, delegate { OpenSpecificMenu(); }, null);

            RT_Dialog_Base.PushNewDialog(d1);
        }

        public static void OpenSpecificMenu()
        {
            switch (RT_Dialog_ScrollButtons.SelectedScrollButton)
            {
                case 0:
                    ModManager.OpenModManagerMenu(false);
                    break;
            }
        }
    }
}