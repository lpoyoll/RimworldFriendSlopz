using GameClient.Managers;
using GameClient.PacketManagers;
using Shared.Files.Mods;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs
{
    public class DLG_ModConfig : DLG_Base
    {
        public override Vector2 InitialSize => new Vector2(500f, 400f);

        public List<ModConfig> Keys { get; private set; } = new List<ModConfig>();

        public enum PossibleValues { Required, Optional, Forbidden }

        public PossibleValues[] Values { get; private set; } = new PossibleValues[] { PossibleValues.Required, PossibleValues.Optional, PossibleValues.Forbidden };

        public List<string> ValueString { get; private set; } = new List<string>();

        public List<int> ValueInt { get; private set; } = new List<int>();

        public static List<ModConfig> ResultMods { get; private set; } = new List<ModConfig>();

        public static List<int> ResultInt { get; private set; } = new List<int>();

        public DLG_ModConfig(List<ModConfig> keys)
        {
            this.Title = "Mod Manager";
            this.Description = "Manage mods for the server";
            this.Keys = keys;
        }

        public override void PreOpen()
        {
            base.PreOpen();

            List<ModConfig> LocalMods = ModManagerH.GetRunningModList().ModConfigs;

            // Add mods that aren't actively listed in the server

            foreach (ModConfig config in LocalMods)
            {
                ModConfig toFind = this.Keys.FirstOrDefault(fetch => fetch.FileName == config.FileName);
                if (toFind == null) this.Keys.Add(config);
            }

            // Remove mods that aren't actively listed in the client

            foreach (ModConfig config in this.Keys.ToArray())
            {
                ModConfig toFind = LocalMods.FirstOrDefault(fetch => fetch.FileName == config.FileName);
                if (toFind == null) this.Keys.Remove(config);
            }

            for (int i = 0; i < this.Keys.Count; i++) ValueString.Add(this.Keys[i].Type.ToString());
            for (int i = 0; i < this.Keys.Count; i++) ValueInt.Add((int)this.Keys[i].Type);
        }

        public override void DoWindowContents(Rect rect)
        {
            float windowDescriptionDif = Text.CalcSize(Description).y + StandardMargin;
            float descriptionLineDif1 = windowDescriptionDif - Text.CalcSize(Description).y * 0.25f;
            float descriptionLineDif2 = windowDescriptionDif + Text.CalcSize(Description).y * 1.1f;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(DLG_Base.GetRectMiddle(rect) - Text.CalcSize(Title).x / 2, rect.y, Text.CalcSize(Title).x, Text.CalcSize(Title).y), Title);

            Widgets.DrawLineHorizontal(rect.x, descriptionLineDif1, rect.width);
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(DLG_Base.GetRectMiddle(rect) - Text.CalcSize(Description).x / 2, windowDescriptionDif, Text.CalcSize(Description).x, Text.CalcSize(Description).y), Description);
            Text.Font = GameFont.Medium;
            Widgets.DrawLineHorizontal(rect.x, descriptionLineDif2, rect.width);

            FillMainRect(new Rect(0f, descriptionLineDif2 + 10f, rect.width, rect.height - DefaultButtonSize.y - 85f));

            Text.Font = GameFont.Small;

            if (Widgets.ButtonText(GetRectForLocation(rect, TinyButtonSize, RectLocation.TopRight), "▶")) ShowFloatMenu(-1, true);

            if (Widgets.ButtonText(GetFillForLocation(rect, DefaultButtonSize, FillLocation.Bottom, 1, 1), "Accept"))
            {
                ResultMods = Keys;
                ResultInt = ValueInt;
                PM_GameParameter.SendCurrentModConfigs(false);

                Close();
            }
        }

        private void FillMainRect(Rect mainRect)
        {
            float height = 6f + Keys.Count * 30f;
            Rect viewRect = new Rect(0f, 0f, mainRect.width - 16f, height);
            Widgets.BeginScrollView(mainRect, ref ScrollPosition, viewRect);
            float num = 0;
            float num2 = ScrollPosition.y - 30f;
            float num3 = ScrollPosition.y + mainRect.height;
            int num4 = 0;

            for (int i = 0; i < Keys.Count; i++)
            {
                if (num > num2 && num < num3)
                {
                    Rect rect = new Rect(0f, num, viewRect.width, 30f);
                    DrawCustomRow(rect, Keys[i], num4);
                }

                num += 30f;
                num4++;
            }

            Widgets.EndScrollView();
        }

        private void DrawCustomRow(Rect rect, ModConfig element, int index)
        {
            Text.Font = GameFont.Small;
            Rect fixedRect = new Rect(new Vector2(rect.x, rect.y + 5f), new Vector2(rect.width - 16f, rect.height - 5f));
            if (index % 2 == 0) Widgets.DrawHighlight(fixedRect);

            Widgets.Label(fixedRect, element.FileName);
            string buttonLabel = ValueString[index];
            if (Widgets.ButtonText(new Rect(new Vector2(rect.xMax - LongButtonSize.x, rect.yMax - LongButtonSize.y), LongButtonSize), buttonLabel))
            {
                ShowFloatMenu(index, false);
            }
        }

        private void ShowFloatMenu(int index, bool globalChange)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();

            foreach (PossibleValues value in Values)
            {
                Action changeSingleValue = delegate
                {
                    ValueString[index] = value.ToString();
                    ValueInt[index] = GetValueFromString(value.ToString());
                };

                Action changeAllValues = delegate
                {
                    for (int i = 0; i < ValueString.Count; i++)
                    {
                        ValueString[i] = value.ToString();
                        ValueInt[i] = GetValueFromString(ValueString[i]);
                    }
                };

                list.Add(new FloatMenuOption(value.ToString(), delegate
                {
                    if (globalChange) changeAllValues();
                    else changeSingleValue();
                }));
            }

            Find.WindowStack.Add(new FloatMenu(list));
        }

        private int GetValueFromString(string str) { return Values.FirstIndexOf(fetch => fetch.ToString() == str); }
    }
}
