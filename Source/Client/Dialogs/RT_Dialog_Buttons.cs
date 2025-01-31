using System;
using GameClient.Managers;
using RimWorld;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs
{
    public class RT_Dialog_Buttons : Window
    {
        private readonly string title;

        private readonly string description;

        private readonly string[] labels;

        private readonly Action[] actions;

        private readonly Action onCancel;

        private readonly Vector2 buttonSize = new(250f, 38f);

        public RT_Dialog_Buttons(string title, string description, string[] labels, Action[] actions, Action onCancel = null)
        {
            DialogManager.dialogButtons = this;

            this.title = title;
            this.description = description;
            this.labels = labels;
            this.actions = actions;
            this.onCancel = onCancel;

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
            float horizontalLineDif = Text.CalcSize(description).y + StandardMargin / 2;
            float windowDescriptionDif = Text.CalcSize(description).y + StandardMargin;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(centeredX - Text.CalcSize(title).x / 2, rect.y, Text.CalcSize(title).x, Text.CalcSize(title).y), title);
            Widgets.DrawLineHorizontal(rect.x, horizontalLineDif, rect.width);
            Text.Font = GameFont.Small;

            Widgets.Label(new Rect(centeredX - Text.CalcSize(description).x / 2, windowDescriptionDif, 
                Text.CalcSize(description).x, Text.CalcSize(description).y), description);

            DrawCancelButton(centeredX, rect.yMax - buttonSize.y);

            DrawButton(centeredX, rect.yMax - buttonSize.y * 2 - 10f, 0);
            if (labels.Length > 1) DrawButton(centeredX, rect.yMax - buttonSize.y * 3 - 20f, 1);
            if (labels.Length > 2) DrawButton(centeredX, rect.yMax - buttonSize.y * 4 - 30f, 2);
        }

        private void CalculateWindowSize()
        {
            Vector2 sizeVector;

            switch (labels.Length)
            {
                case 2:
                    sizeVector = new Vector2(350f, 250f);
                    windowRect = new Rect(new Vector2((UI.screenWidth - sizeVector.x) / 2f, (UI.screenHeight - sizeVector.y) / 2f), sizeVector);
                    windowRect.Rounded();
                    break;

                case 3:
                    sizeVector = new Vector2(350f, 285f);
                    windowRect = new Rect(new Vector2((UI.screenWidth - sizeVector.x) / 2f, (UI.screenHeight - sizeVector.y) / 2f), sizeVector);
                    windowRect.Rounded();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DrawButton(float centeredX, float height, int index)
        {
            if (Widgets.ButtonText(new Rect(new Vector2(centeredX - buttonSize.x / 2, height), buttonSize), labels[index]))
            {
                actions[index]?.Invoke();
                Close();
            }
        }

        private void DrawCancelButton(float centeredX, float height)
        {
            if (Widgets.ButtonText(new Rect(new Vector2(centeredX - buttonSize.x / 2, height), buttonSize), "Cancel"))
            {
                onCancel?.Invoke();
                Close();
            }
        }
    }
}
