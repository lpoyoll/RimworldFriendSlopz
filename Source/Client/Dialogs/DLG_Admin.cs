using GameClient.Dialogs.Default;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.PacketManagers;
using Shared;
using System;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs
{
    public class DLG_Admin : DLG_Base
    {
        public override Vector2 InitialSize => new Vector2(300f, 162f);

        public static DLG_Admin Instance { get; private set; } = null;

        public static bool IsDialogOpen { get; set; } = false;

        public DLG_Admin() 
        {
            Instance = this;
            closeOnCancel = true;
        }

        public override void PostOpen()
        {
            base.PostOpen();
            IsDialogOpen = true;
        }

        public override void PostClose()
        {
            base.PostClose();
            IsDialogOpen = false;
        }

        public override void DoWindowContents(Rect rect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(rect);

            if (listingStandard.ButtonText("Mod Manager") && SessionHandler.IsAdmin) PM_Mods.OpenModManagerMenu();

            if (listingStandard.ButtonText("Event Manager") && SessionHandler.IsAdmin) PM_Events.OpenEventManagerMenu();

            if (listingStandard.ButtonText("Difficulty Manager") && SessionHandler.IsAdmin) DifficultyManager.OpenDifficultyManagerMenu();

            if (listingStandard.ButtonText("Force Save") && SessionHandler.IsAdmin) PM_Saves.ForceSave();

            listingStandard.End();
        }

        private void ShowResetMenu()
        {
            Action r1 = delegate
            {
                DLG_Base.PushNewDialog(new DLG_Wait("Waiting for server response"));
                PM_Saves.RequestResetSave();
            };

            DLG_YesNo d1 = new DLG_YesNo("Are you sure you want to delete your save?", r1, null, "DELETE!", "No", Color.red);
            DLG_Base.PushNewDialog(d1);
        }

        [OnSessionEnd]
        private static void CloseTab() { IsDialogOpen = false; }
    }
}
