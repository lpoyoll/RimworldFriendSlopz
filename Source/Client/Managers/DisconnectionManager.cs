using GameClient.Dialogs;
using GameClient.Misc;
using Verse;
using static RimWorld.PsychicRitualRoleDef;

namespace GameClient.Managers
{
    public static class DisconnectionManager
    {
        public static void HandleDisconnect()
        {
            if (Current.ProgramState != ProgramState.Entry && !SessionHandler.IsIntentionalDisconnect)
            {
                RT_Dialog_Base.PushNewDialog(new RT_Dialog_YesNo("Connection lost. Save game?",
                    delegate { SaveManager.ForceSave(); DisconnectToMenu(); }, delegate { DisconnectToMenu(); }));
            }
            else DisconnectToMenu();
        }

        public static void DisconnectToMenu()
        {
            RT_Dialog_Wait.Instance.Close();

            if (Current.ProgramState != ProgramState.Entry)
            {
                LongEventHandler.QueueLongEvent(delegate { }, "Entry", "", doAsynchronously: false, null);
            }
        }
    }
}
