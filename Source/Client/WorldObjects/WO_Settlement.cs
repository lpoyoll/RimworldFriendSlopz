using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Managers;
using GameClient.PacketManagers;
using GameClient.PacketManagers.Synchronous;
using RimWorld;
using RimWorld.Planet;
using RTShared;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static RTShared.CommonEnumerators;
using static RTNetwork.Packets.PKT_Raid;
using static RTNetwork.Packets.PKT_Transfer;

namespace GameClient.WorldObjects
{
    public class WO_Settlement : MapParent
    {
        private string nameInt;

        private Material cachedMat;

        public override string Label => nameInt ?? base.Label;

        public override Texture2D ExpandingIcon => base.Faction.def.FactionIcon;

        public string Name
        {
            get { return nameInt; }
            set { nameInt = value; }
        }

        public override Material Material
        {
            get
            {
                if (cachedMat == null)
                {
                    cachedMat = MaterialPool.MatFrom(base.Faction.def.settlementTexturePath, ShaderDatabase.WorldOverlayTransparentLit, 
                        base.Faction.Color, 3550);
                }

                return cachedMat;
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            List<Gizmo> gizmos = new List<Gizmo>();

            if (Find.AnyPlayerHomeMap == null) return gizmos;

            Command_Action command_Goodwill = new Command_Action
            {
                defaultLabel = "Change Goodwill",
                defaultDesc = "Change the goodwill of this settlement",
                icon = ContentFinder<Texture2D>.Get("Commands/Goodwill"),
                action = delegate
                {
                    SessionManager.ChosenSettlement = this;

                    Action r1 = delegate
                    {
                        PM_Goodwills.TryRequestGoodwill(Goodwill.Enemy,
                        GoodwillTarget.Settlement);
                    };

                    Action r2 = delegate
                    {
                        PM_Goodwills.TryRequestGoodwill(Goodwill.Neutral,
                        GoodwillTarget.Settlement);
                    };

                    Action r3 = delegate
                    {
                        PM_Goodwills.TryRequestGoodwill(Goodwill.Ally,
                        GoodwillTarget.Settlement);
                    };

                    DLG_Buttons d1 = new DLG_Buttons("Change Goodwill", "Set settlement's goodwill to",
                        new string[] { "Enemy", "Neutral", "Ally" },
                        new Action[] { r1, r2, r3 },
                        null);

                    DLG_Base.PushNewDialog(d1);
                }
            };

            Command_Action command_FactionMenu = new Command_Action
            {
                defaultLabel = "Guild Menu",
                defaultDesc = "Access your guild menu",
                icon = ContentFinder<Texture2D>.Get("Commands/Guild"),
                action = delegate
                {
                    SessionManager.ChosenSettlement = this;

                    if (SessionManager.CurrentActionValues.EnableFactions)
                    {
                        if (SessionManager.ChosenSettlement.Faction == SessionManager.GuildFaction) PM_Guilds.OnFactionOpenOnMember();
                        else PM_Guilds.OnFactionOpenOnNonMember();
                    }
                    else DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                }
            };

            Command_Action command_Caravan = new Command_Action
            {
                defaultLabel = "Form Caravan",
                defaultDesc = "Form a new caravan",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/FormCaravan"),
                action = delegate
                {
                    SessionManager.ChosenSettlement = this;

                    Dialog_FormCaravan d1 = new Dialog_FormCaravan(this.Map, mapAboutToBeRemoved: true);
                    DLG_Base.PushNewDialog(d1);
                }
            };

            Command_Action command_Aid = new Command_Action
            {
                defaultLabel = "Aid",
                defaultDesc = "Send aid to this settlement",
                icon = ContentFinder<Texture2D>.Get("Commands/Aid"),
                action = delegate
                {
                    SessionManager.ChosenSettlement = this;

                    if (SessionManager.CurrentActionValues.AidAction.IsEnabled)
                    {
                        List<string> pawnNames = new List<string>();
                        foreach (Pawn pawn in RimworldManager.GetAllSettlementsPawns(Faction.OfPlayer, false)) pawnNames.Add(pawn.LabelCapNoCount);
                        DLG_Base.PushNewDialog(new DLG_ListingWithButton("Aid menu", "Select the pawn you want to send for aid",
                            pawnNames.ToArray(), PM_Aid.SendAidRequest));
                    }
                    else DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                }
            };

            Command_Action command_Event = new Command_Action
            {
                defaultLabel = "Send Event",
                defaultDesc = "Send an event to this settlement",
                icon = ContentFinder<Texture2D>.Get("Commands/Event"),
                action = delegate
                {
                    SessionManager.ChosenSettlement = this;

                    if (SessionManager.CurrentActionValues.EventAction.IsEnabled) PM_Events.ShowEventMenu();
                    else DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                }
            };

            Command_Action command_Zoom = new Command_Action
            {
                defaultLabel = "View",
                defaultDesc = "View this settlement",
                icon = ContentFinder<Texture2D>.Get("Commands/View"),
                action = delegate
                {
                    SessionManager.ChosenSettlement = this;
                    PM_Zoom.RequestZoom(SessionManager.ChosenSettlement.Tile);
                }
            };

            Command_Action command_StopZoom = new Command_Action
            {
                defaultLabel = "Stop viewing",
                defaultDesc = "Stops viewing this settlement",
                icon = ContentFinder<Texture2D>.Get("Commands/View"),
                action = delegate
                {
                    SessionManager.ChosenSettlement = this;
                    PM_Settlements.RegenSettlement(SessionManager.ChosenSettlement);
                }
            };

            Command_Action command_Info = new Command_Action
            {
                defaultLabel = "Info",
                defaultDesc = "Shows if the player is connected",
                icon = ContentFinder<Texture2D>.Get("Commands/Info"),
                action = delegate
                {
                    SessionManager.ChosenSettlement = this;
                    PM_Information.AskForInformation();
                }
            };

            Command_Action command_Wealth = new Command_Action
            {
                defaultLabel = "Wealth",
                defaultDesc = "Shows the selected settlement's wealth",
                icon = ContentFinder<Texture2D>.Get("Commands/Wealth"),
                action = delegate
                {
                    SessionManager.ChosenSettlement = this;
                    PM_Information.AskForWealth();
                }
            };

            gizmos.Add(command_Info);
            gizmos.Add(command_Wealth);
            gizmos.Add(command_Goodwill);
            gizmos.Add(command_Event);
            gizmos.Add(command_Aid);

            if (this.Map == null) gizmos.Add(command_Zoom);
            else gizmos.Add(command_StopZoom);

            if (this.Map != null) gizmos.Add(command_Caravan);
            if (SessionManager.HasFaction) gizmos.Add(command_FactionMenu);

            return gizmos;
        }

        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
        {
            List<Gizmo> gizmos = new List<Gizmo>();

            Command_Action command_OnlineVisit = new Command_Action
            {
                defaultLabel = "Online Visit",
                defaultDesc = "Visit this player in real time",
                icon = ContentFinder<Texture2D>.Get("Commands/OnlineVisit"),
                action = delegate
                {
                    if (DLG_Options.EnablePreviewFeatures)
                    {
                        SessionManager.ChosenSettlement = this;
                        SessionManager.ChosenCaravan = caravan;
                        PM_Synchronous.Ask(SessionManager.ChosenSettlement.Tile, RTNetwork.Packets.PKT_Synchronous.Type.Visit);
                    }

                    else
                    {
                        string title = "ERROR";
                        string description = "Please enable 'preview features' to use this action!";
                        DLG_Base.PushNewDialog(new DLG_Message(title, new string[] { description }));
                    }
                }
            };

            Command_Action command_OnlineRaid = new Command_Action
            {
                defaultLabel = "Online Raid",
                defaultDesc = "Raid this player in real time",
                icon = ContentFinder<Texture2D>.Get("Commands/OnlineRaid"),
                action = delegate
                {
                    if (DLG_Options.EnablePreviewFeatures)
                    {
                        SessionManager.ChosenSettlement = this;
                        SessionManager.ChosenCaravan = caravan;
                        PM_Synchronous.Ask(SessionManager.ChosenSettlement.Tile, RTNetwork.Packets.PKT_Synchronous.Type.Raid);
                    }

                    else
                    {
                        string title = "ERROR";
                        string description = "Please enable 'preview features' to use this action!";
                        DLG_Base.PushNewDialog(new DLG_Message(title, new string[] { description }));
                    }
                }
            };

            Command_Action command_Raid = new Command_Action
            {
                defaultLabel = "Raid",
                defaultDesc = "Raid this location",
                icon = ContentFinder<Texture2D>.Get("Commands/Raid"),
                action = delegate
                {
                    SessionManager.ChosenSettlement = this;
                    SessionManager.ChosenCaravan = caravan;
                    PM_Raid.RequestRaid(SessionManager.ChosenSettlement.Tile);
                }
            };

            Command_Action command_Transfer = new Command_Action
            {
                defaultLabel = "Transfer Items",
                defaultDesc = "Transfer items between settlements",
                icon = ContentFinder<Texture2D>.Get("Commands/Transfer"),
                action = delegate
                {
                    SessionManager.ChosenSettlement = this;
                    SessionManager.ChosenCaravan = caravan;

                    if (!SessionManager.CurrentActionValues.EnableTrading)
                    {
                        DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                        return;
                    }

                    else
                    {
                        Settlement settlement = Find.World.worldObjects.Settlements.First(fetch => fetch.Faction != Faction.OfPlayer);
                        Pawn negotiator = RimworldManager.GetIfSocialPawnInCaravan(SessionManager.ChosenCaravan);

                        if (negotiator != null)
                        {
                            SessionManager.LastTradeStep = CommonEnumerators.TradeMode.Sending;
                            Find.WindowStack.Add(new Dialog_Trade(negotiator, settlement));
                        }

                        else
                        {
                            DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "You do not have any pawn capable of trading!" }));
                        }
                    }
                }
            };

            gizmos.Add(command_Raid);
            gizmos.Add(command_Transfer);
            gizmos.Add(command_OnlineVisit);
            gizmos.Add(command_OnlineRaid);

            return gizmos;
        }

        public override IEnumerable<FloatMenuOption> GetTransportersFloatMenuOptions(IEnumerable<IThingHolder> pods, Action<PlanetTile, TransportersArrivalAction> action)
        {
            yield return new FloatMenuOption($"Gift contents to '{this.Label}'",
            delegate
            {
                SessionManager.ChosenSettlement = this;
                action(this.Tile, new RTTransportersArrivalAction_TransportPod(this));
            },
                MenuOptionPriority.Default,
                null, null, 0f, null, null, true, 0
            );
        }

        public class RTTransportersArrivalAction_TransportPod : TransportersArrivalAction
        {
            private WO_Settlement settlement;

            public override bool GeneratesMap => false;

            public RTTransportersArrivalAction_TransportPod(WO_Settlement settlement) { this.settlement = settlement; }

            public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> transporters, PlanetTile destinationTile)
            {
                return FloatMenuAcceptanceReport.WasAccepted;
            }

            public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
            {
                if (SessionManager.IsInTransfer) return;

                SessionManager.IsInTransfer = true;
                SessionManager.ChosenSettlement = settlement;

                TakeTransferItemsFromPods(transporters.Cast<IThingHolder>());
                PM_Transfers.SendRequest(TransferLocation.Pod);
            }

            private void TakeTransferItemsFromPods(IEnumerable<IThingHolder> pods)
            {
                SessionManager.OutgoingManifest.CurrentTransferMode = TransferMode.Pod;

                foreach (IThingHolder pod in pods)
                {
                    ThingOwner directlyHeldThings = pod.GetDirectlyHeldThings();
                    for (int i = 0; i < directlyHeldThings.Count(); i++) PM_Transfers.AddToTransferManifest(directlyHeldThings[i], directlyHeldThings[i].stackCount);
                }
            }
        }
    }
}
