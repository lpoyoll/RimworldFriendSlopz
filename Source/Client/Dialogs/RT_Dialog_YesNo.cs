using System;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs;

public class RT_Dialog_YesNo : RT_Dialog_Base
{
    public override Vector2 InitialSize => new Vector2(400f, 150f);

    private readonly string YesText;

    private readonly string NoText;

    private readonly Color YesColor;

    private readonly Color NoColor;

    public RT_Dialog_YesNo(string description, Action actionYes, Action actionNo = null, 
        string yText = "Yes", string nText = "No", Color? yesColor = null, Color? noColor = null)
    {
        Title = "OPTION";
        Description = description;
        OnAccept = actionYes;
        OnCancel = actionNo;
        YesText = yText;
        NoText = nText;

        if(yesColor == null)
            yesColor = Color.white;
        if(noColor == null)
            noColor = Color.white;
            
        YesColor = yesColor.Value;
        NoColor = noColor.Value;
            
        closeOnAccept = false;
        closeOnCancel = false;
    }

    public override void DoWindowContents(Rect rect)
    {
        float centeredX = rect.width / 2;
        float horizontalLineDif = Text.CalcSize(Description).y + StandardMargin / 2;
        float windowDescriptionDif = Text.CalcSize(Description).y + StandardMargin;

        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(centeredX - Text.CalcSize(Title).x / 2, rect.y, Text.CalcSize(Title).x, Text.CalcSize(Title).y), Title);

        Widgets.DrawLineHorizontal(rect.x, horizontalLineDif, rect.width);

        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(centeredX - Text.CalcSize(Description).x / 2, windowDescriptionDif, Text.CalcSize(Description).x, Text.CalcSize(Description).y), Description);

        GUI.color = YesColor;
        if (Widgets.ButtonText(GetRectForLocation(rect, SmallButtonSize, RectLocation.BottomLeft), YesText))
        {
            OnAccept?.Invoke();
            Close();
        }
        GUI.color = NoColor;
        if (Widgets.ButtonText(GetRectForLocation(rect, SmallButtonSize, RectLocation.BottomRight), NoText))
        {
            OnCancel?.Invoke();
            Close();
        }
        GUI.color = Color.white;
    }
}