using GameClient.Dialogs;
using RimWorld;
using Shared;
using Shared.Network.Client;
using System;
using System.Reflection;
using Verse;
using static Shared.CommonEnumerators;
using static UnityEngine.GraphicsBuffer;

namespace GameClient.Managers
{
    public static class DifficultyManager
    {


        public static void OpenDifficultyMenu()
        {
            string description = "Do you want to enforce the current difficulty?";
            Action actionYes = delegate
            {
                GameParameterManager.SendCurrentStoryteller(true);
                GameParameterManager.SendCurrentDifficulty(true);

                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("MESSAGE", new string[] { "Difficulty has been enforced!" }));
            };
            RT_Dialog_YesNo dialog = new RT_Dialog_YesNo(description, actionYes);
            RT_Dialog_Base.PushNewDialog(dialog);
        }
    }
}
