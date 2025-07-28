using System;
using System.Linq;
using GameClient.Managers;
using GameClient.Values;
using Shared;
using Shared.Network.Client;
using UnityEngine;
using Verse;
using static Shared.CommonEnumerators;
using static Shared.TransferData;

namespace GameClient.Dialogs
{
    public class RT_Dialog_ItemListing : RT_Dialog_Base
    {
        public override Vector2 InitialSize => new Vector2(350f, 512f);

        private Thing[] ListedThings { get; set; }

        private TransferMode TransferMode { get; set; }

        public static RT_Dialog_Base Instance { get; private set; } = null;

        public RT_Dialog_ItemListing(Thing[] listedThings, TransferMode transferMode)
        {
            this.ListedThings = listedThings;
            this.TransferMode = transferMode;
            this.Title = "Item Listing";
            Instance = this;

            ClientValues.ToggleTransfer(true);

            closeOnAccept = false;
            closeOnCancel = false;
        }

        public override void DoWindowContents(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.width / 2 - Text.CalcSize(Title).x / 2, rect.y, rect.width, Text.CalcSize(Title).y), Title);

            FillMainRect(new Rect(0f, 35f, rect.width, rect.height - SlimButtonSize.y - 45));

            Text.Font = GameFont.Small;

            if (Widgets.ButtonText(new Rect(new Vector2(rect.x, rect.yMax - SlimButtonSize.y), SlimButtonSize), "Accept"))
            {
                Accept();
            }

            if (Widgets.ButtonText(new Rect(new Vector2(rect.xMax - SlimButtonSize.x, rect.yMax - SlimButtonSize.y), SlimButtonSize), "Cancel"))
            {
                OnReject();
            }
        }

        private void FillMainRect(Rect mainRect)
        {
            Widgets.DrawLineHorizontal(mainRect.x, mainRect.y - 1, mainRect.width);

            float height = 6f + ListedThings.Count() * 30f;
            Rect viewRect = new Rect(0f, 0f, mainRect.width - 16f, height);
            Widgets.BeginScrollView(mainRect, ref ScrollPosition, viewRect);
            float num = 0;
            float num2 = ScrollPosition.y - 30f;
            float num3 = ScrollPosition.y + mainRect.height;
            int num4 = 0;

            for (int i = 0; i < ListedThings.Count(); i++)
            {
                if (num > num2 && num < num3)
                {
                    Rect rect = new Rect(0f, num, viewRect.width, 30f);
                    DrawCustomRow(rect, ListedThings[i], num4);
                }

                num += 30f;
                num4++;
            }

            Widgets.EndScrollView();
        }

        private void DrawCustomRow(Rect rect, Thing thing, int index)
        {
            Text.Font = GameFont.Small;
            Rect fixedRect = new Rect(new Vector2(rect.x, rect.y + 5f), new Vector2(rect.width - 16f, rect.height - 5f));
            if (index % 2 == 0) Widgets.DrawHighlight(fixedRect);

            string itemName = thing.LabelShort;
            if (itemName.Length > 1) itemName = char.ToUpper(itemName[0]) + itemName.Substring(1);
            else itemName = itemName.ToUpper();

            if (ScriberH.CheckIfThingIsHuman(thing))
            {
                Widgets.Label(fixedRect, $"[H] {itemName}");
            }

            else if (ScriberH.CheckIfThingIsAnimal(thing))
            {
                Widgets.Label(fixedRect, $"[A] {itemName}");
            }

            else
            {
                Widgets.Label(fixedRect, $"[I] {itemName} (x{thing.stackCount}) ({thing.HitPoints} HP)");
            }
        }

        private void Accept()
        {
            Action r1 = delegate
            {
                if (TransferMode == TransferMode.Gift)
                {
                    TransferManager.GetTransferedItemsToSettlement(ListedThings);
                }

                else if (TransferMode == TransferMode.Trade)
                {
                    if (RimworldManager.CheckIfSocialPawnInMap(Find.AnyPlayerHomeMap))
                    {
                        RT_Dialog_Base.PushNewDialog(new RT_Dialog_TransferMenu(TransferLocation.Settlement, true, true, true));
                    }

                    else
                    {
                        RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "You do not have any pawn capable of trading!" }));
                        TransferManager.RejectRequest(TransferMode);
                    }
                }

                else if (TransferMode == TransferMode.Pod)
                {
                    TransferManager.GetTransferedItemsToSettlement(ListedThings);
                }

                else if (TransferMode == TransferMode.Rebound)
                {
                    SessionValues.IncomingManifest._stepMode = TransferStepMode.TradeReAccept;

                    Network.Listener.EnqueuePacket(PacketHeader.TransferManager, SessionValues.IncomingManifest);

                    TransferManager.GetTransferedItemsToCaravan(ListedThings);
                }

                Close();
            };

            RT_Dialog_Base.PushNewDialog(new RT_Dialog_YesNo("Are you sure you want to accept?", r1, null));
        }

        private void OnReject()
        {
            Action r1 = delegate
            {
                TransferManager.RejectRequest(TransferMode);

                Close();
            };

            RT_Dialog_Base.PushNewDialog(new RT_Dialog_YesNo("Are you sure you want to decline?", r1, null));
        }
    }
}
