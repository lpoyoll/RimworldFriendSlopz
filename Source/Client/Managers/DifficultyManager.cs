using GameClient.Dialogs;
using System;

namespace GameClient.Managers;

public static class DifficultyManager
{
    public static void OpenDifficultyMenu()
    {
        string description = "Do you want to enforce the current difficulty?";
        Action actionYes = delegate
        {
            GameParameterManager.SendCurrentStoryteller(true);
            GameParameterManager.SendCurrentDifficulty(true);
        };

        RT_Dialog_Base.PushNewDialog(new RT_Dialog_YesNo(description, actionYes));
    }
}