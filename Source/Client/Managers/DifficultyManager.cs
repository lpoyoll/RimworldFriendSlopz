using GameClient.Dialogs;
using GameClient.TCP;
using RimWorld;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient.Managers
{
    public static class DifficultyManager
    {
        public static DifficultyValuesFile GetDifficulty(Page_SelectStoryteller __instance)
        {
            Difficulty difficulty = GameParameterManagerH.GetDifficultyReference(__instance);

            DifficultyValuesFile file = new DifficultyValuesFile();

            file.ScribeData = ScribeManager.SerializeToString(difficulty, ScribeManager.SerializableType.Other);

            return file;
        }

        public static void SetDifficulty(DifficultyValuesFile file, bool bypass = false)
        {
            if (!file.EnforceDifficulty && !bypass) return;

            Current.Game.storyteller.difficultyDef = DifficultyDefOf.Rough;
            Current.Game.storyteller.difficulty = (Difficulty)ScribeManager.SerializeFromString<Difficulty>(file.ScribeData);
        }

        public static void SendDifficulty(DifficultyValuesFile file, bool mode)
        {
            file.EnforceDifficulty = mode;

            GameParameterData data = new GameParameterData();
            data._stepMode = GenStepMode.Difficulty;
            data._difficulty = file;

            Network.Listener.EnqueuePacket(PacketHeader.GameParameterManager, data);
        }

        public static void OpenDifficultyMenu()
        {
            string description = "Do you want to enforce the current difficulty?";
            Action actionYes = delegate
            {
                StorytellerValuesFile storyteller = new StorytellerValuesFile();
                storyteller.StorytellerDefname = Find.Storyteller.def.defName;
                GameParameterManager.SendStoryteller(storyteller, true);

                DifficultyValuesFile difficulty = new DifficultyValuesFile();
                difficulty.ScribeData = ScribeManager.SerializeToString(Current.Game.storyteller.difficulty, ScribeManager.SerializableType.Other);
                SendDifficulty(difficulty, true);

                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("MESSAGE", new string[] { "Difficulty has been enforced!" }));
            };
            RT_Dialog_YesNo dialog = new RT_Dialog_YesNo(description, actionYes);
            RT_Dialog_Base.PushNewDialog(dialog);
        }
    }
}
