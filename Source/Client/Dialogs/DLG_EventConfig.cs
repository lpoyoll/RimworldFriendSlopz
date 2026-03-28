using GameClient.PacketManagers;
using Shared;
using Shared.Files;
using System;
using System.Collections.Generic;
using TCPNetwork;
using TCPNetwork.Packets;
using UnityEngine;
using Verse;
using static TCPNetwork.Packets.PKT_Event;

namespace GameClient.Dialogs
{
    public class DLG_EventConfig : DLG_Base
    {
        public override Vector2 InitialSize => new Vector2(500f, 400f);

        public List<EventFile> Keys { get; private set; } = new List<EventFile>();

        public enum PossibleValues { Enabled, Disabled }

        public PossibleValues[] Values { get; private set; } = new PossibleValues[] { PossibleValues.Disabled, PossibleValues.Enabled };

        public List<string> ValueString { get; private set; } = new List<string>();

        public List<bool> ValueBool { get; private set; } = new List<bool>();

        public static List<bool> ResultBool { get; private set; } = new List<bool>();

        public DLG_EventConfig(List<EventFile> keys)
        {
            this.Title = "Event Manager";
            this.Description = "Manage events for the server";
            this.Keys = keys;

            for (int i = 0; i < keys.Count; i++) ValueString.Add(keys[i].IsEnabled ? "Enabled" : "Disabled");
            for (int i = 0; i < keys.Count; i++) ValueBool.Add(keys[i].IsEnabled);
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

            if (Widgets.ButtonText(GetRectForLocation(rect, DefaultButtonSize, RectLocation.BottomCenter), "Accept"))
            {
                ResultBool = ValueBool;

                for (int i = 0; i < PM_Events.AvailableEvents.Count; i++)
                {
                    EventFile file = PM_Events.AvailableEvents[i];
                    file.IsEnabled = ResultBool[i];
                }

                PKT_Event data = new PKT_Event();
                data._stepMode = EventStepMode.Set;
                data._eventFiles = PM_Events.AvailableEvents;
                Network.ServerEndpoint.EnqueuePacket(PacketHeader.EventManager, data);

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

        private void DrawCustomRow(Rect rect, EventFile element, int index)
        {
            Text.Font = GameFont.Small;
            Rect fixedRect = new Rect(new Vector2(rect.x, rect.y + 5f), new Vector2(rect.width - 16f, rect.height - 5f));
            if (index % 2 == 0) Widgets.DrawHighlight(fixedRect);

            Widgets.Label(fixedRect, element.Name);
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
                    ValueBool[index] = GetValueFromString(value.ToString()) == 1 ? true : false;
                };

                Action changeAllValues = delegate
                {
                    for (int i = 0; i < ValueString.Count; i++)
                    {
                        ValueString[i] = value.ToString();
                        ValueBool[i] = GetValueFromString(ValueString[i]) == 1 ? true : false;
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
