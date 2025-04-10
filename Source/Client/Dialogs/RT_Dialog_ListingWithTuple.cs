using System;
using System.Collections.Generic;
using GameClient.Managers;
using RimWorld;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs
{
    public class RT_Dialog_ListingWithTuple : RT_Dialog_Base
    {
        public override Vector2 InitialSize => new Vector2(500f, 400f);

        public readonly string[] keys;

        public string[] values;

        public string[] valueString;

        public int[] valueInt;

        public static string[] dialogTupleListingResultString;

        public static int[] dialogTupleListingResultInt;

        public RT_Dialog_ListingWithTuple(string title, string description, string[] keys, string[] values, Action actionAccept = null)
        {
            this.Title = title;
            this.Description = description;
            this.keys = keys;
            this.values = values;
            this.OnAccept = actionAccept;

            closeOnAccept = false;
            closeOnCancel = false;

            List<string> strings = new List<string>();
            for (int i = 0; i < keys.Length; i++) strings.Add(values[0]);
            valueString = strings.ToArray();

            List<int> ints = new List<int>();
            for (int i = 0; i < keys.Length; i++) ints.Add(0);
            valueInt = ints.ToArray();
        }

        public override void DoWindowContents(Rect rect)
        {
            float centeredX = rect.width / 2;

            float windowDescriptionDif = Text.CalcSize(Description).y + StandardMargin;
            float descriptionLineDif1 = windowDescriptionDif - Text.CalcSize(Description).y * 0.25f;
            float descriptionLineDif2 = windowDescriptionDif + Text.CalcSize(Description).y * 1.1f;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(centeredX - Text.CalcSize(Title).x / 2, rect.y, Text.CalcSize(Title).x, Text.CalcSize(Title).y), Title);

            Widgets.DrawLineHorizontal(rect.x, descriptionLineDif1, rect.width);
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(centeredX - Text.CalcSize(Description).x / 2, windowDescriptionDif, Text.CalcSize(Description).x, Text.CalcSize(Description).y), Description);
            Text.Font = GameFont.Medium;
            Widgets.DrawLineHorizontal(rect.x, descriptionLineDif2, rect.width);

            FillMainRect(new Rect(0f, descriptionLineDif2 + 10f, rect.width, rect.height - DefaultButtonSize.y - 85f));

            Text.Font = GameFont.Small;

            if (Widgets.ButtonText(GetRectForLocation(rect, TinyButtonSize, RectLocation.TopRight), "▶")) ShowFloatMenu(-1, true);

            if (Widgets.ButtonText(GetRectForLocation(rect, DefaultButtonSize, RectLocation.BottomCenter), "Accept"))
            {
                dialogTupleListingResultString = keys;
                dialogTupleListingResultInt = valueInt;
                OnAccept?.Invoke();
                Close();
            }
        }

        private void FillMainRect(Rect mainRect)
        {
            float height = 6f + keys.Length * 30f;
            Rect viewRect = new Rect(0f, 0f, mainRect.width - 16f, height);
            Widgets.BeginScrollView(mainRect, ref ScrollPosition, viewRect);
            float num = 0;
            float num2 = ScrollPosition.y - 30f;
            float num3 = ScrollPosition.y + mainRect.height;
            int num4 = 0;

            for (int i = 0; i < keys.Length; i++)
            {
                if (num > num2 && num < num3)
                {
                    Rect rect = new Rect(0f, num, viewRect.width, 30f);
                    DrawCustomRow(rect, keys[i], num4);
                }

                num += 30f;
                num4++;
            }

            Widgets.EndScrollView();
        }

        private void DrawCustomRow(Rect rect, string element, int index)
        {
            Text.Font = GameFont.Small;
            Rect fixedRect = new Rect(new Vector2(rect.x, rect.y + 5f), new Vector2(rect.width - 16f, rect.height - 5f));
            if (index % 2 == 0) Widgets.DrawHighlight(fixedRect);

            Widgets.Label(fixedRect, element);
            string buttonLabel = valueString[index];
            if (Widgets.ButtonText(new Rect(new Vector2(rect.xMax - LongButtonSize.x, rect.yMax - LongButtonSize.y), LongButtonSize), buttonLabel))
            {
                ShowFloatMenu(index, false);
            }
        }

        private void ShowFloatMenu(int index, bool globalChange)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();

            foreach (string str in values)
            {
                Action changeSingleValue = delegate
                {
                    valueString[index] = str;
                    valueInt[index] = GetValueFromString(str);
                };

                Action changeAllValues = delegate
                {
                    for (int i = 0; i < valueString.Length; i++)
                    {
                        valueString[i] = str;
                        valueInt[i] = GetValueFromString(valueString[i]);
                    }
                };

                list.Add(new FloatMenuOption(str, delegate
                {
                    if (globalChange) changeAllValues();
                    else changeSingleValue();
                }));
            }

            Find.WindowStack.Add(new FloatMenu(list));
        }

        private int GetValueFromString(string str) { return values.FirstIndexOf(fetch => fetch == str); }
    }
}