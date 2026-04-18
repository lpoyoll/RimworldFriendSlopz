using GameClient.Hooks.TCPNetwork;
using Shared.Files.Mods;
using System;
using System.Linq;
using TCPNetwork;
using TCPNetwork.Packets.ServerBrowser;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace GameClient.Dialogs.ServerBrowser
{
    public class DLG_ServerListing : DLG_Base
    {
        public override Vector2 InitialSize => new Vector2(600f, 400f);

        public PKT_ServerTelemetry Element { get; private set; } = null;

        public DLG_ServerListing(PKT_ServerTelemetry element)
        {
            this.Element = element;
            this.Title = Element.Name;
        }

        public override void DoWindowContents(Rect rect)
        {
            float windowDescriptionDif = Text.CalcSize(Description).y + StandardMargin;
            float descriptionLineDif1 = windowDescriptionDif - Text.CalcSize(Description).y * 0.25f;
            string moddedBool = Element.Mods.Count > 0 ? "Yes" : "No";

            Text.Font = GameFont.Medium;

            Widgets.Label(new Rect(DLG_Base.GetRectMiddle(rect) - Text.CalcSize(Title).x / 2, rect.y, Text.CalcSize(Title).x, Text.CalcSize(Title).y), Title);

            Widgets.DrawLineHorizontal(rect.x, descriptionLineDif1, rect.width);

            Text.Font = GameFont.Small;

            Rect toUse = new Rect(rect.x, descriptionLineDif1 + 10f, rect.width, 147f);
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(toUse);
            listingStandard.Label($"Endpoint: {Element.Endpoint}:{Element.Port}");
            listingStandard.Label($"Population: {Element.CurrentPopulation}/{Element.MaxPopulation}");
            listingStandard.Label($"Version: {Element.Version}");
            listingStandard.Label($"Modded: {moddedBool}");
            listingStandard.End();

            toUse = new Rect(rect.x, toUse.height, rect.width, 165f);
            Widgets.DrawBox(toUse);
            Widgets.TextArea(toUse, Element.Description, true);

            if (Widgets.ButtonText(DLG_Base.GetRectForLocation(rect, SlimButtonSize, RectLocation.BottomLeft, 3), "Connect"))
            {
                Network.Ip = Element.Endpoint;
                Network.Port = Element.Port;
                ClientNetwork.StartFeature();

                Close();
            }

            if (Widgets.ButtonText(DLG_Base.GetRectForLocation(rect, SlimButtonSize, RectLocation.BottomCenter, 3), "Mods"))
            {
                DLG_Base.PushNewDialog(new DLG_ServerMods(Element));
            }

            if (Widgets.ButtonText(DLG_Base.GetRectForLocation(rect, SlimButtonSize, RectLocation.BottomRight, 3), "Back")) Close();
        }

        private void FillMainRect(Rect mainRect)
        {
            float height = 6f + Element.Mods.Count() * 30f;
            Rect viewRect = new Rect(0f, 0f, mainRect.width - 16f, height);
            Widgets.BeginScrollView(mainRect, ref ScrollPosition, viewRect);
            float num = 0;
            float num2 = ScrollPosition.y - 30f;
            float num3 = ScrollPosition.y + mainRect.height;
            int num4 = 0;

            for (int i = 0; i < Element.Mods.Count(); i++)
            {
                if (num > num2 && num < num3)
                {
                    Rect rect = new Rect(0f, num, viewRect.width, 30f);
                    DrawCustomRow(rect, Element.Mods[i], num4);
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

            Widgets.Label(fixedRect, $"{element.FileName}");
        }
    }
}
