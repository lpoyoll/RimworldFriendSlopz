using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using System;

namespace GameClient.Managers
{
    public static class DifficultyManager
    {
        public static void OpenDifficultyManagerMenu()
        {
            string description = "Do you want to enforce the current difficulty?";
            Action actionYes = delegate
            {
                PM_GameParameter.SendCurrentStoryteller(true);
                PM_GameParameter.SendCurrentDifficulty(true);
            };

            DLG_Base.PushNewDialog(new DLG_YesNo(description, actionYes));
        }
    }
}
