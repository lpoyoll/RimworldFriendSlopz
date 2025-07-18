using System;
using System.Collections.Generic;
using System.Linq;
using GameClient.Managers;
using GameClient.Values;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;
using static Shared.CommonEnumerators;

namespace GameClient.Dialogs
{
    public class RT_Dialog_TransferMenu : RT_Dialog_Base
    {
        public override Vector2 InitialSize => new Vector2(600f, 512f);

        private TransferLocation TransferLocation { get; set; }

        private List<Tradeable> CachedTradeables { get; set; }

        private Pawn PlayerNegotiator { get; set; }

        private bool AllowItems { get; set; }

        private bool AllowAnimals { get; set; }

        private bool AllowHumans { get; set; }

        private bool AllowFreeThings { get; set; }

        public RT_Dialog_TransferMenu(TransferLocation transferLocation, bool allowItems = false, bool allowAnimals = false, bool allowHumans = false, bool allowFreeThings = true)
        {
            this.Title = "Transfer Menu";
            this.Description = "Select the items you wish to transfer";
            this.TransferLocation = transferLocation;
            this.AllowItems = allowItems;
            this.AllowAnimals = allowAnimals;
            this.AllowHumans = allowHumans;
            this.AllowFreeThings = allowFreeThings;

            ClientValues.ToggleTransfer(true);

            closeOnAccept = false;
            closeOnCancel = false;

            PrepareWindow();
        }

        private void PrepareWindow()
        {
            GetNegotiator();

            GenerateTradeList();

            LoadAllAvailableTradeables();

            SetupTrade();
        }

        public override void DoWindowContents(Rect rect)
        {
            float windowDescriptionDif = Text.CalcSize(Description).y + 8;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.width / 2 - Text.CalcSize(Title).x / 2, rect.y, rect.width, Text.CalcSize(Title).y), Title);

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.width / 2 - Text.CalcSize(Description).x / 2, windowDescriptionDif, rect.width, Text.CalcSize(Description).y), Description);
            Text.Font = GameFont.Medium;

            FillMainRect(new Rect(0f, 55f, rect.width, rect.height - SlimButtonSize.y - 65));

            Text.Font = GameFont.Small;
            if (Widgets.ButtonText(new Rect(new Vector2(rect.x, rect.yMax - SlimButtonSize.y), SlimButtonSize), "Accept")) Accept();
            if (Widgets.ButtonText(new Rect(new Vector2(rect.width / 2 - SlimButtonSize.x / 2, rect.yMax - SlimButtonSize.y), SlimButtonSize), "Reset")) Reset();
            if (Widgets.ButtonText(new Rect(new Vector2(rect.xMax - SlimButtonSize.x, rect.yMax - SlimButtonSize.y), SlimButtonSize), "Cancel")) Cancel();
        }

        private void FillMainRect(Rect mainRect)
        {
            Widgets.DrawLineHorizontal(mainRect.x, mainRect.y - 1, mainRect.width);
            Widgets.DrawLineHorizontal(mainRect.x, mainRect.yMax + 1, mainRect.width);

            float height = 6f + CachedTradeables.Count * 30f;
            Rect viewRect = new Rect(0f, 0f, mainRect.width - 16f, height);
            Widgets.BeginScrollView(mainRect, ref ScrollPosition, viewRect);
            float num = 0;
            float num2 = ScrollPosition.y - 30f;
            float num3 = ScrollPosition.y + mainRect.height;
            int num4 = 0;

            for (int i = 0; i < CachedTradeables.Count; i++)
            {
                if (num > num2 && num < num3)
                {
                    Rect rect = new Rect(0f, num, viewRect.width, 30f);
                    DrawCustomRow(rect, CachedTradeables[i], num4);
                }

                num += 30f;
                num4++;
            }

            Widgets.EndScrollView();
        }

        private void Accept()
        {
            if (TransferLocation == TransferLocation.Caravan)
            {
                Action r1 = delegate
                {
                    SessionValues.OutgoingManifest._transferMode = TransferMode.Gift;
                    postChoosing();
                };

                Action r2 = delegate
                {
                    SessionValues.OutgoingManifest._transferMode = TransferMode.Trade;
                    postChoosing();
                };

                RT_Dialog_Buttons d2 = new RT_Dialog_Buttons("Transfer Type", "Please choose the transfer type to use",
                    new string[] { "Gift", "Trade" }, new Action[] { r1, r2 }, null);

                RT_Dialog_YesNo d1 = new RT_Dialog_YesNo("Are you sure you want to continue with the transfer?",
                    delegate { RT_Dialog_Base.PushNewDialog(d2); }, null);

                RT_Dialog_Base.PushNewDialog(d1);
            }

            else if (TransferLocation == TransferLocation.Settlement)
            {
                Action r1 = delegate
                {
                    SessionValues.OutgoingManifest._transferMode = TransferMode.Rebound;
                    RT_Dialog_ItemListing.Instance.Close();
                    postChoosing();
                };

                RT_Dialog_YesNo d1 = new RT_Dialog_YesNo("Are you sure you want to continue with the transfer?",
                    r1, null);

                RT_Dialog_Base.PushNewDialog(d1);
            }

            void postChoosing()
            {
                TransferManager.TakeTransferItems(TransferLocation);
                TransferManager.SendTransferRequestToServer(TransferLocation);
                Close();
            }
        }

        private void Cancel()
        {
            Action r1 = delegate
            {
                if (TransferLocation == TransferLocation.Settlement)
                {
                    TransferManager.RejectRequest(TransferMode.Trade);
                }

                TransferManager.FinishTransfer(false);

                Close();
            };

            if (TransferLocation == TransferLocation.Settlement)
            {
                RT_Dialog_Base.PushNewDialog(new RT_Dialog_YesNo("Are you sure you want to decline?",
                    r1, null));
            }
            else r1.Invoke();
        }

        private void Reset()
        {
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            GenerateTradeList();
            LoadAllAvailableTradeables();
            TradeSession.deal.Reset();
        }

        private void GetNegotiator()
        {
            if (TransferLocation == TransferLocation.Caravan)
            {
                PlayerNegotiator = SessionValues.ChosenCaravan.PawnsListForReading.Find(fetch => fetch.IsColonist && !fetch.skills.skills[10].PermanentlyDisabled);
            }

            else if (TransferLocation == TransferLocation.Settlement)
            {
                PlayerNegotiator = Find.AnyPlayerHomeMap.mapPawns.AllPawns.Find(fetch => fetch.IsColonist && !fetch.skills.skills[10].PermanentlyDisabled);
            }
        }

        private void SetupTrade()
        {
            if (TransferLocation == TransferLocation.Caravan)
            {
                TradeSession.SetupWith(SessionValues.ChosenSettlement, PlayerNegotiator, true);
            }

            else if (TransferLocation == TransferLocation.Settlement)
            {
                TradeSession.SetupWith(Find.WorldObjects.SettlementAt(SessionValues.IncomingManifest._fromTile),
                    PlayerNegotiator, true);
            }
        }

        private void DrawCustomRow(Rect rect, Tradeable trad, int index)
        {
            Text.Font = GameFont.Small;
            float width = rect.width;

            Widgets.DrawLightHighlight(rect);

            GUI.BeginGroup(rect);

            Rect rect5 = new Rect(width - 225, 0f, 240f, rect.height);
            bool flash = Time.time - Dialog_Trade.lastCurrencyFlashTime < 1f && trad.IsCurrency;
            TransferableUIUtility.DoCountAdjustInterface(rect5, trad, index, trad.GetMinimumToTransfer(), trad.GetMaximumToTransfer(), flash);

            width -= 225;

            int num2 = trad.CountHeldBy(Transactor.Colony);
            if (num2 != 0)
            {
                Rect rect6 = new Rect(width, 0f, 100f, rect.height);
                Rect rect7 = new Rect(rect6.x - 75f, 0f, 75f, rect.height);
                if (Mouse.IsOver(rect7)) Widgets.DrawHighlight(rect7);

                Rect rect8 = rect7;
                rect8.xMin += 5f;
                rect8.xMax -= 5f;
                Widgets.Label(rect8, num2.ToStringCached());
                TooltipHandler.TipRegionByKey(rect7, "ColonyCount");
            }

            width -= 90f;

            TransferableUIUtility.DoExtraIcons(trad, rect, ref width);

            Rect idRect = new Rect(0f, 0f, width, rect.height);
            TransferableUIUtility.DrawTransferableInfo(trad, idRect, Color.white);
            GenUI.ResetLabelAlign();
            GUI.EndGroup();
        }

        public void GenerateTradeList()
        {
            SessionValues.ListToShowInTradesMenu = new List<Tradeable>();

            if (TransferLocation == TransferLocation.Caravan)
            {
                List<Thing> caravanItems = CaravanInventoryUtility.AllInventoryItems(SessionValues.ChosenCaravan);

                if (AllowItems)
                {
                    foreach (Thing thing in caravanItems)
                    {
                        if (thing.MarketValue == 0 && !AllowFreeThings) continue;
                        else
                        {
                            Tradeable tradeable = new Tradeable();
                            tradeable.AddThing(thing, Transactor.Colony);
                            SessionValues.ListToShowInTradesMenu.Add(tradeable);
                        }
                    }
                }

                if (AllowHumans || AllowAnimals)
                {
                    foreach (Pawn pawn in SessionValues.ChosenCaravan.pawns)
                    {
                        if (ScriberH.CheckIfThingIsHuman(pawn))
                        {
                            if (AllowHumans)
                            {
                                if (pawn == PlayerNegotiator) continue;
                                else
                                {
                                    Tradeable tradeable = new Tradeable();
                                    tradeable.AddThing(pawn, Transactor.Colony);
                                    SessionValues.ListToShowInTradesMenu.Add(tradeable);
                                }
                            }
                        }

                        else if (ScriberH.CheckIfThingIsAnimal(pawn))
                        {
                            if (AllowAnimals)
                            {
                                Tradeable tradeable = new Tradeable();
                                tradeable.AddThing(pawn, Transactor.Colony);
                                SessionValues.ListToShowInTradesMenu.Add(tradeable);
                            }
                        }
                    }
                }
            }

            else if (TransferLocation == TransferLocation.Settlement)
            {
                Map map = Find.Maps.Find(x => x.Tile == SessionValues.IncomingManifest._toTile);

                List<Pawn> pawnsInMap = map.mapPawns.PawnsInFaction(Faction.OfPlayer).ToList();
                pawnsInMap.AddRange(map.mapPawns.PrisonersOfColony);

                Thing[] thingsInMap = RimworldManager.GetAllThingsInMap(map);

                if (AllowItems)
                {
                    foreach (Thing thing in thingsInMap)
                    {
                        if (thing.MarketValue == 0 && !AllowFreeThings) continue;
                        {
                            Tradeable tradeable = new Tradeable();
                            tradeable.AddThing(thing, Transactor.Colony);
                            SessionValues.ListToShowInTradesMenu.Add(tradeable);
                        }
                    }
                }

                if (AllowHumans || AllowAnimals)
                {
                    foreach (Pawn pawn in pawnsInMap)
                    {
                        if (ScriberH.CheckIfThingIsAnimal(pawn))
                        {
                            if (AllowAnimals)
                            {
                                Tradeable tradeable = new Tradeable();
                                tradeable.AddThing(pawn, Transactor.Colony);
                                SessionValues.ListToShowInTradesMenu.Add(tradeable);
                            }
                        }

                        else
                        {
                            if (AllowHumans)
                            {
                                if (pawn == PlayerNegotiator) continue;
                                else
                                {
                                    Tradeable tradeable = new Tradeable();
                                    tradeable.AddThing(pawn, Transactor.Colony);
                                    SessionValues.ListToShowInTradesMenu.Add(tradeable);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void LoadAllAvailableTradeables()
        {
            CachedTradeables = (from tr in SessionValues.ListToShowInTradesMenu
                                orderby 0 descending
                                select tr)
                .ThenBy((tr) => tr.ThingDef.label)
                .ThenBy((tr) => tr.AnyThing.TryGetQuality(out QualityCategory qc) ? (int)qc : -1)
                .ThenBy((tr) => tr.AnyThing.HitPoints)
                .ToList();
        }
    }
}
