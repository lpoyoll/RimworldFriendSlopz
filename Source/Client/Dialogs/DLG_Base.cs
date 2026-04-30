using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs
{
    public class DLG_Base : Window
    {
        public static Window CurrentDialog { get; private set; } = null;

        public static Window PreviousDialog { get; private set; } = null;

        public static Vector2 DefaultButtonSize { get; private set; } = new(250f, 38f);

        public static Vector2 SmallButtonSize { get; private set; } = new(150f, 38f);

        public static Vector2 SmallerButtonSize { get; private set; } = new(137f, 38f);

        public static Vector2 TinyButtonSize { get; private set; } = new(47f, 25f);

        public static Vector2 SlimButtonSize { get; private set; } = new(100f, 38f);

        public static Vector2 LongButtonSize { get; private set; } = new Vector2(100f, 25f);

        public static Vector2 KnobButtonSize { get; private set; } = new Vector2(30f, 30f);

        public static float DefaultMargin { get; private set; } = 8.0f;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public Action OnAccept { get; set; } = null;

        public Action OnCancel { get; set; } = null;

        public bool AcceptsInput => StartAcceptingInputAtFrame <= Time.frameCount;

        public int StartAcceptingInputAtFrame { get; set; }

        public Vector2 ScrollPosition = Vector2.zero;

        public DLG_Base() 
        { 
            forcePause = true;
            closeOnAccept = false;
            closeOnCancel = false;
            preventCameraMotion = true;
            absorbInputAroundWindow = true;
            soundAppear = SoundDefOf.CommsWindow_Open;
        }

        public override void DoWindowContents(Rect inRect) { }

        public static void PushNewDialog(Window window)
        {
            PreviousDialog = CurrentDialog;
            Find.WindowStack.Add(window);
            CurrentDialog = window;
        }

        public enum RectLocation { TopRight, BottomLeft }

        public enum FillLocation { Bottom }

        public static Rect GetRectForLocation(Rect origin, Vector2 reference, RectLocation desiredLocation)
        {
            return desiredLocation switch
            {
                RectLocation.TopRight => new Rect(new Vector2(origin.xMax - reference.x, origin.yMin), reference),
                RectLocation.BottomLeft => new Rect(new Vector2(origin.xMin, origin.yMax - reference.y), reference),
                _ => throw new IndexOutOfRangeException()
            };
        }

        public static Rect GetFillForLocation(Rect origin, Vector2 reference, FillLocation desiredLocation, int buttonCount, int index)
        {
            reference = new Vector2((origin.width / buttonCount), reference.y);

            return desiredLocation switch
            {
                FillLocation.Bottom => new Rect(new Vector2(origin.xMin + (reference.x * (index - 1)), origin.yMax - reference.y), reference),
                _ => throw new IndexOutOfRangeException()
            };
        }

        public static float GetRectMiddle(Rect rect) { return rect.width / 2; }
    }
}
