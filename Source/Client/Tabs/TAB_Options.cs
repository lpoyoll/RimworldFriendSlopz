using GameClient.Core.Configs;
using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.PacketManagers;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace GameClient.Tabs
{
    public class TAB_Options : DLG_Base
    {
        // Add 30 Y value per button

        public override Vector2 InitialSize => new Vector2(250f, 194f);

        public static TAB_Options Instance { get; private set; } = null;

        public static bool IsTabOpen { get; set; } = false;

        public TAB_Options() 
        {
            Instance = this;
            layer = WindowLayer.Dialog;
            absorbInputAroundWindow = false;
        }

        public override void PostOpen()
        {
            base.PostOpen();
            IsTabOpen = true;
        }

        public override void PostClose()
        {
            base.PostClose();
            IsTabOpen = false;
        }

        public override void DoWindowContents(Rect rect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(rect);

            if (listingStandard.ButtonText("Mod Manager") && SessionHandler.IsAdmin) PM_Mods.OpenModManagerMenu();

            if (listingStandard.ButtonText("Event Manager") && SessionHandler.IsAdmin) PM_Events.OpenEventManagerMenu();

            if (listingStandard.ButtonText("Difficulty Manager") && SessionHandler.IsAdmin) DifficultyManager.OpenDifficultyManagerMenu();

            if (listingStandard.ButtonText("Force Save") && SessionHandler.IsAdmin) PM_Saves.ForceSave();

            GUI.color = Color.red;
            if (listingStandard.ButtonText("Reset Save")) ShowResetMenu();
            GUI.color = Color.white;

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
    }
}
