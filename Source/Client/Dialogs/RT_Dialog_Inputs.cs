using System;
using GameClient.Managers;
using RimWorld;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs
{
    public class RT_Dialog_Inputs : RT_Dialog_Base
    {
        // Parameters

        private readonly float inputWidth = 300f;

        private readonly float inputHeight = 30f;

        private readonly int maxChars = 512;

        // Inputs

        private readonly string[] labels = new string[] { };

        private readonly bool[] censors = new bool[] { };

        private readonly string[] results = new string[] { };

        private readonly string[] censorResult = new string[] { };

        public static string[] dialogInputResults;

        public RT_Dialog_Inputs(string title, string[] labels, bool[] censors, Action onConfirm = null, Action onCancel = null)
        {
            this.Title = title;
            this.OnAccept = onConfirm;
            this.OnCancel = onCancel;

            this.labels = labels;
            this.censors = censors;
            results = new string[] { "", "", "" };
            censorResult = new string[] { "", "", "" };

            closeOnAccept = false;
            closeOnCancel = false;
        }

        public override void PreOpen() { CalculateWindowSize(); }

        public override void DoWindowContents(Rect rect)
        {
            float centeredX = rect.width / 2;
            float titleSeparator = Text.CalcSize(Title).y + StandardMargin / 2;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(centeredX - Text.CalcSize(Title).x / 2, rect.y, Text.CalcSize(Title).x, Text.CalcSize(Title).y), Title);
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

            if (Widgets.ButtonText(GetRectForLocation(rect, DefaultButtonSize, RectLocation.BottomLeft), "Confirm"))
            {
                RT_Dialog_Inputs.dialogInputResults = new string[] { results[0], results[1], results[2] };
                OnAccept?.Invoke();
                Close();
            }

            if (Widgets.ButtonText(GetRectForLocation(rect, DefaultButtonSize, RectLocation.BottomRight), "Cancel"))
            {
                OnCancel?.Invoke();
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
