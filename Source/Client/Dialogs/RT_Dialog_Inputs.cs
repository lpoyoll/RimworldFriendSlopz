using System;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs;

public class RT_Dialog_Inputs : RT_Dialog_Base
{
    private readonly float InputWidth = 300f;

    private readonly float InputHeight = 30f;

    private readonly int MaxChars = 512;

    private readonly string[] Labels  = [];

    private readonly bool[] Censors = [];

    private readonly string[] Results = [];

    private readonly string[] CensorResult = [];

    public static string[] DialogInputResults { get; private set; }

    public RT_Dialog_Inputs(string title, string[] labels, bool[] censors, Action onConfirm = null, Action onCancel = null)
    {
        Title = title;
        OnAccept = onConfirm;
        this.OnCancel = onCancel;

        Labels = labels;
        Censors = censors;
        Results = ["", "", ""];
        CensorResult = ["", "", ""];

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

        float inputOneLabelDif = Text.CalcSize(Labels[0]).y + StandardMargin;
        float inputOneDif = inputOneLabelDif + 30f;
        DrawInput(centeredX, inputOneLabelDif, inputOneDif, 0);

        if (Labels.Length > 1)
        {
            float inputTwoLabelDif = inputOneDif + Text.CalcSize(Labels[1]).y + StandardMargin * 2;
            float inputTwoDif = inputTwoLabelDif + 30f;
            DrawInput(centeredX, inputTwoLabelDif, inputTwoDif, 1);

            if (Labels.Length == 3)
            {
                float inputThreeLabelDif = inputTwoDif + Text.CalcSize(Labels[2]).y + StandardMargin * 2;
                float inputThreeDif = inputThreeLabelDif + 30f;
                DrawInput(centeredX, inputThreeLabelDif, inputThreeDif, 2);
            }
        }

        if (Widgets.ButtonText(GetRectForLocation(rect, SmallButtonSize, RectLocation.BottomLeft), "Confirm"))
        {
            DialogInputResults = [Results[0], Results[1], Results[2]];
            OnAccept?.Invoke();
            Close();
        }

        if (Widgets.ButtonText(GetRectForLocation(rect, SmallButtonSize, RectLocation.BottomRight), "Cancel"))
        {
            OnCancel?.Invoke();
            Close();
        }
    }

    private void CalculateWindowSize()
    {
        Vector2 sizeVector;

        switch (Labels.Length)
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
        Widgets.Label(new Rect(centeredX - Text.CalcSize(Labels[index]).x / 2, labelDif, Text.CalcSize(Labels[index]).x, Text.CalcSize(Labels[index]).y), Labels[index]);
        string input = Widgets.TextField(new Rect(centeredX - InputWidth / 2, normalDif, InputWidth, InputHeight), Results[index]);
        if (AcceptsInput && input.Length <= MaxChars) Results[index] = input;

        if (Censors[index])
        {
            string censorOne = Widgets.TextField(new Rect(centeredX - InputWidth / 2, normalDif, InputWidth, InputHeight), CensorResult[index]);
            if (AcceptsInput && censorOne.Length <= MaxChars)
            {
                Text.Font = GameFont.Medium;
                CensorResult[index] = new string('█', input.Length);
                Text.Font = GameFont.Small;
            }
        }
    }
}