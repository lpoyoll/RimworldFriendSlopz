using GameClient.Dialogs.Default;
using GameClient.PacketManagers;
using RTShared;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs
{
    public class DLG_Options : DLG_Base
    {
        public override Vector2 InitialSize => new Vector2(300f, 267f);

        public static DLG_Options Instance { get; private set; } = null;

        public static bool IsDialogOpen { get; set; } = false;

        public static bool AutorejectTransfersBool = false;

        public static bool AutorejectSiteRewardsBool = false;

        public static bool EnablePreviewFeatures = false;

        public enum SyncingMode { Fast, Complete }

        public static SyncingMode CurrentSyncingMode = SyncingMode.Fast;

        public DLG_Options() 
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

            listingStandard.Label("Parameters");
            listingStandard.CheckboxLabeled("Reject all transfers", ref AutorejectTransfersBool, "Automatically denies transfers");
            listingStandard.CheckboxLabeled("Reject all site rewards", ref AutorejectSiteRewardsBool, "Automatically denies site rewards");
            listingStandard.CheckboxLabeled("Enable preview features", ref EnablePreviewFeatures, "Enable preview features");

            listingStandard.GapLine();
            listingStandard.Label("Syncing");
            if (listingStandard.ButtonTextLabeled("Syncing mode", $"{CurrentSyncingMode}")) ShowSyncMenu();

            listingStandard.GapLine();
            listingStandard.Label("Dangerous");
            GUI.color = Color.red;
            if (listingStandard.ButtonText("Reset Save")) ShowResetMenu();
            GUI.color = Color.white;

            listingStandard.End();
        }

        private void ShowResetMenu()
        {
            Action r1 = delegate
            {
                DLG_Base.PushNewDialog(new DLG_Wait());
                PM_Saves.RequestResetSave();
            };

            DLG_YesNo d1 = new DLG_YesNo("Are you sure you want to delete your save?", r1, null, "DELETE!", "No", Color.red);
            DLG_Base.PushNewDialog(d1);
        }

        private void ShowSyncMenu()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();

            List<Tuple<string, SyncingMode>> modes = new List<Tuple<string, SyncingMode>>()
            {
                Tuple.Create("Fast", SyncingMode.Fast),
                Tuple.Create("Complete", SyncingMode.Complete),
            };

            foreach (Tuple<string, SyncingMode> tuple in modes)
            {
                FloatMenuOption item = new FloatMenuOption(tuple.Item1, delegate
                {
                    CurrentSyncingMode = tuple.Item2;
                });

                list.Add(item);
            }

            Find.WindowStack.Add(new FloatMenu(list));
        }

        [OnSessionEnd]
        private static void CloseTab() { IsDialogOpen = false; }
    }
}
