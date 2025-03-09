using System;
using GameClient.Dialogs;
using GameClient.Values;
using UnityEngine;
using Verse;
using Shared;

namespace GameClient.Managers
{
    [RTManager]
    public static class DialogManager
    {
        // Variables

        public static Window currentDialog;

        public static Window previousDialog;

        // Dialogs

        public static RT_Dialog_Wait dialogWait;

        public static RT_Dialog_YesNo dialogYesNo;

        public static RT_Dialog_Message dialogMessage;

        public static RT_Dialog_Buttons dialogButtons;

        public static RT_Dialog_SiteMenu dialogSiteMenu;

        public static RT_Dialog_SiteMenu_Config dialogSiteMenuConfig;

        public static RT_Dialog_SiteMenu_Info dialogSiteMenuInfo;

        // Scroll

        public static RT_Dialog_ScrollButtons dialogScrollButtons;

        public static int selectedScrollButton;

        // Input

        public static RT_Dialog_Inputs dialogInput;

        public static string[] dialogInputResults;

        // Button listing

        public static RT_Dialog_ListingWithButton dialogButtonListing;

        public static string dialogButtonListingResultString;

        public static int dialogButtonListingResultInt;

        // Server listing

        public static RT_Dialog_ServerListing dialogServerListing;

        public static int dialogServerListingIndex;

        // Tuple listing

        public static RT_Dialog_ListingWithTuple dialogTupleListing;

        public static string[] dialogTupleListingResultString;

        public static int[] dialogTupleListingResultInt;

        // More

        public static RT_Dialog_TransferMenu dialogTransferMenu;

        public static RT_Dialog_ItemListing dialogItemListing;

        public static RT_Dialog_Listing dialogListing;

        public static void PushNewDialog(Window window)
        {
            if (ClientValues.IsReadyToPlay || Current.ProgramState == ProgramState.Entry)
            {
                previousDialog = currentDialog;
                currentDialog = window;

                Find.WindowStack.Add(window);
            }
        }

        public static void PopDialog(Window window) { window?.Close(); }

        public static void PopWaitDialog() { dialogWait?.Close(); }
    }

    public static class DialogManagerH
    {
        public enum RectLocation { TopLeft, TopCenter, TopRight, MiddleLeft, MiddleCenter, MiddleRight, BottomLeft, BottomCenter, BottomRight }

        public static readonly Vector2 defaultButtonSize = new Vector2(150f, 38f);

        public static Rect GetRectForLocation(Rect origin, Vector2 reference, RectLocation desiredLocation)
        {
            return desiredLocation switch
            {
                RectLocation.TopLeft => new Rect(new Vector2(origin.xMin, origin.yMin), reference),
                RectLocation.TopCenter => new Rect(new Vector2(origin.width / 2 - reference.x / 2, origin.yMin), reference),
                RectLocation.TopRight => new Rect(new Vector2(origin.xMax - reference.x, origin.yMin), reference),
                RectLocation.MiddleLeft => new Rect(new Vector2(origin.xMin, origin.height / 2 - reference.y / 2), reference),
                RectLocation.MiddleCenter => new Rect(new Vector2(origin.width / 2 - reference.x / 2, origin.height / 2 - reference.y / 2), reference),
                RectLocation.MiddleRight => new Rect(new Vector2(origin.xMax - reference.x, origin.height / 2 - reference.y / 2), reference),
                RectLocation.BottomLeft => new Rect(new Vector2(origin.xMin, origin.yMax - reference.y), reference),
                RectLocation.BottomCenter => new Rect(new Vector2(origin.width / 2 - reference.x / 2, origin.yMax - reference.y), reference),
                RectLocation.BottomRight => new Rect(new Vector2(origin.xMax - reference.x, origin.yMax - reference.y), reference),
                _ => throw new IndexOutOfRangeException()
            };
        }
    }
}
