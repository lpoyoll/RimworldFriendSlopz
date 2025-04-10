using System;
using GameClient.Managers;
using RimWorld;
using UnityEngine;
using Verse;
using static GameClient.Managers.DialogManagerH;

namespace GameClient.Dialogs
{
    public class RT_Dialog_Inputs : Window
    {
        // Essentials

        private bool AcceptsInput => startAcceptingInputAtFrame <= Time.frameCount;

        private readonly int startAcceptingInputAtFrame;

        // Parameters

        private readonly string title;

        private readonly float inputWidth = 300f;

        private readonly float inputHeight = 30f;

        private readonly int maxChars = 512;

        private readonly Action onConfirm;

        private readonly Action onCancel;

        // Inputs

        private readonly string[] labels = new string[] { };

        private readonly bool[] censors = new bool[] { };

        private readonly string[] results = new string[] { };

        private readonly string[] censorResult = new string[] { };

        public RT_Dialog_Inputs(string title, string[] labels, bool[] censors, Action onConfirm = null, Action onCancel = null, string onConfirmText = "Confirm", string OnCancel = "Cancel")
        {
            DialogManager.dialogInput = this;

            this.title = title;
            this.onConfirm = onConfirm;
            this.onCancel = onCancel;

            this.labels = labels;
            this.censors = censors;
            results = new string[] { "", "", "" };
            censorResult = new string[] { "", "", "" };

            forcePause = true;
            absorbInputAroundWindow = true;
            soundAppear = SoundDefOf.CommsWindow_Open;

            closeOnAccept = false;
            closeOnCancel = false;
        }

        public override void PreOpen() { CalculateWindowSize(); }

        public override void DoWindowContents(Rect rect)
        {
            float centeredX = rect.width / 2;
            float titleSeparator = Text.CalcSize(title).y + StandardMargin / 2;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(centeredX - Text.CalcSize(title).x / 2, rect.y, Text.CalcSize(title).x, Text.CalcSize(title).y), title);
            Widgets.DrawLineHorizontal(rect.x, titleSeparator, rect.width);
            Text.Font = GameFont.Small;

            float inputOneLabelDif = Text.CalcSize(labels[0]).y + StandardMargin;
            float inputOneDif = inputOneLabelDif + 30f;
            DrawInput(centeredX, inputOneLabelDif, inputOneDif, 0);

            if (labels.Length > 1)
            {
                float inputTwoLabelDif = inputOneDif + Text.CalcSize(labels[1]).y + StandardMargin * 2;
                float inputTwoDif = inputTwoLabelDif + 30f;
                DrawInput(centeredX, inputTwoLabelDif, inputTwoDif, 1);

                if (labels.Length == 3)
                {
                    float inputThreeLabelDif = inputTwoDif + Text.CalcSize(labels[2]).y + StandardMargin * 2;
                    float inputThreeDif = inputThreeLabelDif + 30f;
                    DrawInput(centeredX, inputThreeLabelDif, inputThreeDif, 2);
                }
            }

            if (Widgets.ButtonText(GetRectForLocation(rect, defaultButtonSize, RectLocation.BottomLeft), "Confirm"))
            {
                DialogManager.dialogInputResults = new string[] { results[0], results[1], results[2] };
                onConfirm?.Invoke();
                Close();
            }

            if (Widgets.ButtonText(GetRectForLocation(rect, defaultButtonSize, RectLocation.BottomRight), "Cancel"))
            {
                onCancel?.Invoke();
                Close();
            }
        }

        private void CalculateWindowSize()
        {
            Vector2 sizeVector;

            switch (labels.Length)
            {
                case 1:
                    sizeVector = new Vector2(400f, 190f);
                    windowRect = new Rect(new Vector2((UI.screenWidth - sizeVector.x) / 2f, (UI.screenHeight - sizeVector.y) / 2f), sizeVector);
                    windowRect.Rounded();
                    break;

                case 2:
                    sizeVector = new Vector2(400f, 280f);
                    windowRect = new Rect(new Vector2((UI.screenWidth - sizeVector.x) / 2f, (UI.screenHeight - sizeVector.y) / 2f), sizeVector);
                    windowRect.Rounded();
                    break;

                case 3:
                    sizeVector = new Vector2(400f, 370f);
                    windowRect = new Rect(new Vector2((UI.screenWidth - sizeVector.x) / 2f, (UI.screenHeight - sizeVector.y) / 2f), sizeVector);
                    windowRect.Rounded();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DrawInput(float centeredX, float labelDif, float normalDif, int index)
        {
            Widgets.Label(new Rect(centeredX - Text.CalcSize(labels[index]).x / 2, labelDif, Text.CalcSize(labels[index]).x, Text.CalcSize(labels[index]).y), labels[index]);
            string input = Widgets.TextField(new Rect(centeredX - inputWidth / 2, normalDif, inputWidth, inputHeight), results[index]);
            if (AcceptsInput && input.Length <= maxChars) results[index] = input;

            if (censors[index])
            {
                string censorOne = Widgets.TextField(new Rect(centeredX - inputWidth / 2, normalDif, inputWidth, inputHeight), censorResult[index]);
                if (AcceptsInput && censorOne.Length <= maxChars)
                {
                    Text.Font = GameFont.Medium;
                    censorResult[index] = new string('█', input.Length);
                    Text.Font = GameFont.Small;
                }
            }
        }
    }
}
