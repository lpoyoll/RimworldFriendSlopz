using GameClient.Dialogs;
using GameClient.Managers;
using GameClient.Patches.Tabs;
using Shared.Network.Client;
using GameClient.Values;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static Shared.CommonEnumerators;
using static Shared.TransferData;

namespace GameClient.Patches
{
    [HarmonyPatch(typeof(Settlement), "GetGizmos")]
    public static class SettlementGizmoPatch
    {
        [HarmonyPostfix]
        public static void DoPost(ref IEnumerable<Gizmo> __result, Settlement __instance)
        {
            if (Network.State == ClientNetworkState.Disconnected) return;

            List<Gizmo> gizmoList = __result.ToList();

            Command_Action command_Goodwill = new Command_Action
            {
                defaultLabel = "Change Goodwill",
                defaultDesc = "Change the goodwill of this settlement",
                icon = ContentFinder<Texture2D>.Get("Commands/Goodwill"),
                action = delegate
                {
                    SessionValues.ChosenSettlement = __instance;

                    Action r1 = delegate
                    {
                        GoodwillManager.TryRequestGoodwill(Goodwill.Enemy,
                        GoodwillTarget.Settlement);
                    };

                    Action r2 = delegate
                    {
                        GoodwillManager.TryRequestGoodwill(Goodwill.Neutral,
                        GoodwillTarget.Settlement);
                    };

                    Action r3 = delegate
                    {
                        GoodwillManager.TryRequestGoodwill(Goodwill.Ally,
                        GoodwillTarget.Settlement);
                    };

                    RT_Dialog_Buttons d1 = new RT_Dialog_Buttons("Change Goodwill", "Set settlement's goodwill to",
                        new string[] { "Enemy", "Neutral", "Ally" },
                        new Action[] { r1, r2, r3 },
                        null);

                    RT_Dialog_Base.PushNewDialog(d1);
                }
            };

            Command_Action command_FactionMenu = new Command_Action
            {
                defaultLabel = "Faction Menu",
                defaultDesc = "Access your faction menu",
                icon = ContentFinder<Texture2D>.Get("Commands/FactionMenu"),
                action = delegate
                {
                    SessionValues.ChosenSettlement = __instance;

                    if (SessionValues.ActionValues.EnableFactions)
                    {
                        if (SessionValues.ChosenSettlement.Faction == ClientValues.YourOnlineFaction) GuildManager.OnFactionOpenOnMember();
                        else GuildManager.OnFactionOpenOnNonMember();
                    }
                    else RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                }
            };

            Command_Action command_Caravan = new Command_Action
            {
                defaultLabel = "Form Caravan",
                defaultDesc = "Form a new caravan",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/FormCaravan"),
                action = delegate
                {
                    SessionValues.ChosenSettlement = __instance;

                    Dialog_FormCaravan d1 = new Dialog_FormCaravan(__instance.Map, mapAboutToBeRemoved: true);
                    RT_Dialog_Base.PushNewDialog(d1);
                }
            };

            Command_Action command_Aid = new Command_Action
            {
                defaultLabel = "Aid",
                defaultDesc = "Send aid to this settlement",
                icon = ContentFinder<Texture2D>.Get("Commands/Aid"),
                action = delegate
                {
                    SessionValues.ChosenSettlement = __instance;

                    if (SessionValues.ActionValues.EnableAids)
                    {
                        List<string> pawnNames = new List<string>();
                        foreach (Pawn pawn in RimworldManager.GetAllSettlementsPawns(Faction.OfPlayer, false)) pawnNames.Add(pawn.LabelCapNoCount);
                        RT_Dialog_Base.PushNewDialog(new RT_Dialog_ListingWithButton("Aid menu", "Select the pawn you want to send for aid",
                            pawnNames.ToArray(), AidManager.SendAidRequest));
                    }
                    else RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                }
            };

            Command_Action command_Event = new Command_Action
            {
                defaultLabel = "Send Event",
                defaultDesc = "Send an event to this settlement",
                icon = ContentFinder<Texture2D>.Get("Commands/Event"),
                action = delegate
                {
                    SessionValues.ChosenSettlement = __instance;

                    if (SessionValues.ActionValues.EnableEvents) EventManager.ShowEventMenu();
                    else RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                }
            };

            Command_Action command_Spy = new Command_Action
            {
                defaultLabel = "Spy",
                defaultDesc = "Spy this location",
                icon = ContentFinder<Texture2D>.Get("Commands/Spy"),
                action = delegate
                {
                    SessionValues.ChosenSettlement = __instance;

                    ActivityManager.RequestActivity(ActivityType.Spy, 
                        SessionValues.ChosenSettlement.Tile);
                }
            };

            Command_Action command_Info = new Command_Action
            {
                defaultLabel = "Info",
                defaultDesc = "Shows if the player is connected",
                icon = ContentFinder<Texture2D>.Get("Commands/Info"),
                action = delegate
                {
                    SessionValues.ChosenSettlement = __instance;
                    InformationManager.AskForInformation();
                }
            };

            Command_Action command_Wealth = new Command_Action
            {
                defaultLabel = "Wealth",
                defaultDesc = "Shows the selected settlement's wealth",
                icon = ContentFinder<Texture2D>.Get("Commands/Wealth"),
                action = delegate
                {
                    SessionValues.ChosenSettlement = __instance;
                    InformationManager.AskForWealth();
                }
            };

            Command_Action command_PersonalFactionMenu = new Command_Action
            {
                defaultLabel = "Faction Menu",
                defaultDesc = "Access your faction menu",
                icon = ContentFinder<Texture2D>.Get("Commands/FactionMenu"),
                action = delegate
                {
                    SessionValues.ChosenSettlement = __instance;

                    if (SessionValues.ActionValues.EnableFactions)
                    {
                        if (ClientValues.HasFaction) GuildManager.OnFactionOpen();
                        else GuildManager.OnNoFactionOpen();
                    }
                    else RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                }
            };

            if (Find.AnyPlayerHomeMap == null)
            {
                if (ClientValues.PlayerFactions.Contains(__instance.Faction))
                {
                    gizmoList.Add(command_Info);
                    gizmoList.Add(command_Wealth);
                    gizmoList.Add(command_Goodwill);
                }

                __result = gizmoList;
            }

            else
            {
                if (__instance.Faction == Find.FactionManager.OfPlayer)
                {
                    gizmoList.Add(command_PersonalFactionMenu);
                    __result = gizmoList;
                }

                else if (ClientValues.PlayerFactions.Contains(__instance.Faction))
                {
                    gizmoList.Clear();

                    if (__instance.Map != null) gizmoList.Add(command_Caravan);
                    else
                    {
                        if (__instance.Faction != ClientValues.YourOnlineFaction)
                        {
                            gizmoList.Add(command_Goodwill);
                            gizmoList.Add(command_Spy);
                        }

                        if (ClientValues.HasFaction) gizmoList.Add(command_FactionMenu);

                        gizmoList.Add(command_Event);
                        gizmoList.Add(command_Aid);
                        gizmoList.Add(command_Info);
                        gizmoList.Add(command_Wealth);
                    }

                    __result = gizmoList;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Settlement), "GetCaravanGizmos")]
    public static class CaravanSettlementGizmoPatch
    {
        [HarmonyPostfix]
        public static void DoPost(ref IEnumerable<Gizmo> __result, Settlement __instance, Caravan caravan)
        {
            if (Network.State == ClientNetworkState.Disconnected) return;

            if (ClientValues.PlayerFactions.Contains(__instance.Faction))
            {
                List<Gizmo> gizmoList = __result.ToList();

                List<Gizmo> removeList = new List<Gizmo>();
                foreach (Command_Action action in gizmoList.ToList())
                {
                    if (action.defaultLabel == "CommandAttackSettlement".Translate()) removeList.Add(action);
                    else if (action.defaultLabel == "CommandOfferGifts".Translate()) removeList.Add(action);
                    else if (action.defaultLabel == "CommandTrade".Translate()) removeList.Add(action);
                }
                foreach (Gizmo g in removeList) gizmoList.Remove(g);

                Command_Action command_Raid = new Command_Action
                {
                    defaultLabel = "Raid",
                    defaultDesc = "Raid this location",
                    icon = ContentFinder<Texture2D>.Get("Commands/Raid"),
                    action = delegate
                    {
                        SessionValues.ChosenSettlement = __instance;
                        SessionValues.ChosenCaravan = caravan;

                        ActivityManager.RequestActivity(ActivityType.Raid, 
                            SessionValues.ChosenSettlement.Tile);
                    }
                };

                Command_Action command_Visit = new Command_Action
                {
                    defaultLabel = "Visit",
                    defaultDesc = "Visit this location",
                    icon = ContentFinder<Texture2D>.Get("Commands/Visit"),
                    action = delegate
                    {
                        SessionValues.ChosenSettlement = __instance;
                        SessionValues.ChosenCaravan = caravan;

                        ActivityManager.RequestActivity(ActivityType.Visit, 
                            SessionValues.ChosenSettlement.Tile);
                    }
                };

                Command_Action command_Transfer = new Command_Action
                {
                    defaultLabel = "Transfer Items",
                    defaultDesc = "Transfer items between settlements",
                    icon = ContentFinder<Texture2D>.Get("Commands/Transfer"),
                    action = delegate
                    {
                        SessionValues.ChosenSettlement = __instance;
                        SessionValues.ChosenCaravan = caravan;

                        if (!SessionValues.ActionValues.EnableTrading)
                        {
                            RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                            return;
                        }

                        else
                        {
                            if (RimworldManager.CheckIfSocialPawnInCaravan(SessionValues.ChosenCaravan))
                            {
                                RT_Dialog_Base.PushNewDialog(new RT_Dialog_TransferMenu(TransferLocation.Caravan, true, true, true));
                            }
                            else RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "You do not have any pawn capable of trading!" }));
                        }
                    }
                };

                if (RimworldManager.CheckIfPlayerHasMap())
                {
                    gizmoList.Add(command_Transfer);
                    gizmoList.Add(command_Visit);
                }

                if (__instance.Faction != ClientValues.YourOnlineFaction) gizmoList.Add(command_Raid);

                __result = gizmoList;
            }
        }
    }

    [HarmonyPatch(typeof(Settlement), "GetFloatMenuOptions")]
    public static class PatchPlayerSettlements
    {
        [HarmonyPostfix]
        public static void DoPost(ref IEnumerable<FloatMenuOption> __result, Caravan caravan, Settlement __instance)
        {
            if (ClientValues.PlayerFactions.Contains(__instance.Faction))
            {
                List<FloatMenuOption> gizmoList = __result.ToList();

                gizmoList.Clear();

                if (CaravanVisitUtility.SettlementVisitedNow(caravan) != __instance)
                {
                    foreach (FloatMenuOption floatMenuOption2 in CaravanArrivalAction_VisitSettlement.GetFloatMenuOptions(caravan, __instance))
                    {
                        gizmoList.Add(floatMenuOption2);
                    }
                }

                __result = gizmoList;
            }
        }
    }

    [HarmonyPatch(typeof(Site), "GetGizmos")]
    public static class SiteGizmoPatch
    {
        [HarmonyPostfix]
        public static void DoPost(ref IEnumerable<Gizmo> __result, Site __instance)
        {
            if (Network.State == ClientNetworkState.Disconnected) return;

            List<Gizmo> gizmoList = __result.ToList();

            Command_Action command_Caravan = new Command_Action
            {
                defaultLabel = "Form Caravan",
                defaultDesc = "Form a new caravan",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/FormCaravan"),
                action = delegate
                {
                    SessionValues.ChosenSite = __instance;

                    Dialog_FormCaravan d1 = new Dialog_FormCaravan(__instance.Map, mapAboutToBeRemoved: true);
                    RT_Dialog_Base.PushNewDialog(d1);
                }
            };

            Command_Action command_Goodwill = new Command_Action
            {
                defaultLabel = "Change Goodwill",
                defaultDesc = "Change the goodwill of this site",
                icon = ContentFinder<Texture2D>.Get("Commands/Goodwill"),
                action = delegate
                {
                    SessionValues.ChosenSite = __instance;

                    Action r1 = delegate
                    {
                        GoodwillManager.TryRequestGoodwill(Goodwill.Enemy,
                        GoodwillTarget.Site);
                    };

                    Action r2 = delegate
                    {
                        GoodwillManager.TryRequestGoodwill(Goodwill.Neutral,
                        GoodwillTarget.Site);
                    };

                    Action r3 = delegate
                    {
                        GoodwillManager.TryRequestGoodwill(Goodwill.Ally,
                        GoodwillTarget.Site);
                    };

                    RT_Dialog_Buttons d1 = new RT_Dialog_Buttons("Change Goodwill", "Set site's goodwill to",
                        new string[] { "Enemy", "Neutral", "Ally" },
                        new Action[] { r1, r2, r3 },
                        null);

                    RT_Dialog_Base.PushNewDialog(d1);
                }
            };

            Command_Action command_Config = new Command_Action
            {
                defaultLabel = "Change site configs",
                defaultDesc = "Change the configuration of your sites. These settings affect all sites currently under your control.",
                icon = ContentFinder<Texture2D>.Get("Commands/SiteConfig"),
                action = delegate
                {
                    if (SessionValues.ActionValues.EnableSites) RT_Dialog_Base.PushNewDialog(new RT_Dialog_SiteMenu(true));
                    else RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                }
            };

            if (ClientValues.PlayerFactions.Contains(__instance.Faction))
            {
                gizmoList.Clear();

                if (__instance.Faction == ClientValues.YourOnlineFaction) gizmoList.Add(command_Config);
                else
                {
                    if (__instance.Map == null)
                    {
                        gizmoList.Add(command_Goodwill);
                    }
                }

                if (__instance.Map != null) gizmoList.Add(command_Caravan);
            }

            else if (__instance.Faction == Faction.OfPlayer)
            {
                gizmoList.Clear();

                if (__instance.Map != null) gizmoList.Add(command_Caravan);

                gizmoList.Add(command_Config);
            }

            __result = gizmoList;
        }
    }

    [HarmonyPatch(typeof(Site), "GetFloatMenuOptions")]
    public static class PatchPlayerSites
    {
        [HarmonyPostfix]
        public static void DoPost(Site __instance, ref IEnumerable<FloatMenuOption> __result)
        {
            if (ClientValues.PlayerFactions.Contains(__instance.Faction) || __instance.Faction == Faction.OfPlayer)
            {
                List<FloatMenuOption> floatMenuList = __result.ToList();

                floatMenuList.Clear();

                __result = floatMenuList;
            }
        }
    }

    [HarmonyPatch(typeof(Caravan), "GetGizmos")]
    public static class PatchCaravanGizmos
    {
        [HarmonyPostfix]
        public static void ModifyPost(ref IEnumerable<Gizmo> __result, Caravan __instance)
        {
            if (Network.State == ClientNetworkState.Connected && RimworldManager.CheckIfPlayerHasMap())
            {
                Site presentSite = Find.World.worldObjects.Sites.ToList().Find(x => x.Tile == __instance.Tile);
                Settlement presentSettlement = Find.World.worldObjects.Settlements.ToList().Find(x => x.Tile == __instance.Tile);
                List<Gizmo> gizmoList = __result.ToList();

                Command_Action Command_BuildSite = new Command_Action
                {
                    defaultLabel = "Build a Site",
                    defaultDesc = "Build an utility site for your faction",
                    icon = ContentFinder<Texture2D>.Get("Commands/FSite"),
                    action = delegate
                    {
                        SessionValues.ChosenCaravan = __instance;

                        if (SessionValues.ActionValues.EnableSites)
                        {
                            RT_Dialog_Base.PushNewDialog(new RT_Dialog_SiteMenu(false));
                        }
                        else RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                    }
                };

                Command_Action command_VisitSite = new Command_Action
                {
                    defaultLabel = "Visit",
                    defaultDesc = "Visit this location",
                    icon = ContentFinder<Texture2D>.Get("Commands/Visit"),
                    action = delegate
                    {
                        SessionValues.ChosenCaravan = __instance;
                        SessionValues.ChosenSite = Find.WorldObjects.Sites.Find(x => x.Tile == __instance.Tile);

                        if (SessionValues.ActionValues.EnableSites) SiteManager.RequestVisitSite();
                        else RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                    }
                };

                Command_Action command_RaidSite = new Command_Action
                {
                    defaultLabel = "Raid",
                    defaultDesc = "Visit this location",
                    icon = ContentFinder<Texture2D>.Get("Commands/Raid"),
                    action = delegate
                    {
                        SessionValues.ChosenCaravan = __instance;
                        SessionValues.ChosenSite = Find.WorldObjects.Sites.Find(x => x.Tile == __instance.Tile);

                        if (SessionValues.ActionValues.EnableSites) SiteManager.RequestRaidSite();
                        else RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                    }
                };

                Command_Action command_DestroySite = new Command_Action
                {
                    defaultLabel = "Destroy",
                    defaultDesc = "Destroy this location",
                    icon = ContentFinder<Texture2D>.Get("Commands/DestroySite"),
                    action = delegate
                    {
                        SessionValues.ChosenCaravan = __instance;
                        SessionValues.ChosenSite = Find.WorldObjects.Sites.Find(x => x.Tile == __instance.Tile);

                        if (SessionValues.ActionValues.EnableSites) SiteManager.RequestDestroySite();
                        else RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                    }
                };

                Command_Action Command_BuildRoad = new Command_Action
                {
                    defaultLabel = "Road Builder",
                    defaultDesc = "Build and destroy roads",
                    icon = ContentFinder<Texture2D>.Get("Commands/Road"),
                    action = delegate
                    {
                        SessionValues.ChosenCaravan = __instance;

                        if (SessionValues.ActionValues.EnableRoads)
                        {
                            List<PlanetTile> neighborTiles = new List<PlanetTile>();
                            Find.WorldGrid.GetTileNeighbors(SessionValues.ChosenCaravan.Tile, neighborTiles);

                            SurfaceTile selectedTile = (SurfaceTile)Find.WorldGrid[__instance.Tile];
                            RoadManagerHelper.ShowRoadChooseDialog(neighborTiles.ToArray(), selectedTile.Roads != null);
                        }
                        else RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                    }
                };

                if (presentSettlement == null && presentSite == null) gizmoList.Add(Command_BuildSite);
                else if (presentSite != null)
                {
                    if (presentSite.Faction == Faction.OfPlayer)
                    {
                        gizmoList.Add(command_VisitSite);
                        gizmoList.Add(command_DestroySite);
                    }

                    else if (ClientValues.PlayerFactions.Contains(presentSite.Faction))
                    {
                        if (presentSite.Faction != ClientValues.YourOnlineFaction) gizmoList.Add(command_RaidSite);
                    }
                }

                gizmoList.Add(Command_BuildRoad);

                __result = gizmoList;
            }
        }
    }

    [HarmonyPatch(typeof(TransportersArrivalAction_GiveGift), "GetFloatMenuOptions")]
    public static class PatchDropGift
    {
        [HarmonyPostfix]
        public static void ModifyPost(ref IEnumerable<FloatMenuOption> __result, Settlement settlement, IEnumerable<IThingHolder> pods)
        {
            if (ClientValues.PlayerFactions.Contains(settlement.Faction))
            {
                List<FloatMenuOption> floatMenuList = __result.ToList();
                floatMenuList.Clear();

                if (Network.State == ClientNetworkState.Connected)
                {
                    SessionValues.ChosenSettlement = settlement;
                    SessionValues.ChosenPods = pods;

                    string optionLabel = $"Transfer things to {settlement.Name}";
                    Action toDo = delegate
                    {
                        TransferManager.TakeTransferItemsFromPods(SessionValues.ChosenPods);
                        TransferManager.SendTransferRequestToServer(TransferLocation.Pod);
                    };

                    FloatMenuOption floatMenuOption = new FloatMenuOption(optionLabel, toDo);
                    floatMenuList.Add(floatMenuOption);
                }

                __result = floatMenuList;
            }
        }
    }

    [HarmonyPatch(typeof(TransportersArrivalAction_AttackSettlement), "GetFloatMenuOptions")]
    public static class PatchDropAttack
    {
        [HarmonyPostfix]
        public static void ModifyPost(ref IEnumerable<FloatMenuOption> __result, Settlement settlement)
        {
            if (ClientValues.PlayerFactions.Contains(settlement.Faction))
            {
                List<FloatMenuOption> floatMenuList = __result.ToList();

                floatMenuList.Clear();

                __result = floatMenuList;
            }
        }
    }

    [HarmonyPatch(typeof(DestroyedSettlement), "GetGizmos")]
    public static class DestroyedSettlementPatch
    {
        [HarmonyPostfix]
        public static void DoPost(ref IEnumerable<Gizmo> __result)
        {
            if (Network.State == ClientNetworkState.Disconnected) return;

            List<Gizmo> gizmoList = __result.ToList();
            List<Gizmo> removeList = new List<Gizmo>();
            foreach (Command_Action action in gizmoList.ToList())
            {
                if (action.defaultLabel == "CommandSettle".Translate()) removeList.Add(action);
            }

            foreach (Gizmo gizmo in removeList) gizmoList.Remove(gizmo);

            __result = gizmoList;
        }
    }

    [HarmonyPatch(typeof(MapParent), nameof(MapParent.CheckRemoveMapNow))]
    public static class PatchExitMap
    {
        [HarmonyPrefix]
        public static bool DoPre(MapParent __instance)
        {
            if (Network.State == ClientNetworkState.Disconnected) return true;
            else if (__instance.Faction != Faction.OfPlayer && !ClientValues.PlayerFactions.Contains(__instance.Faction)) return true;
            else
            {
                if (__instance.ParentHolder != null)
                {
                    if (__instance.HasMap && __instance.ShouldRemoveMapNow(out bool alsoRemoveWorldObject))
                    {
                        if (__instance.Faction == Faction.OfPlayer) MapManager.SendMapToServer(__instance.Map);
                        Current.Game.DeinitAndRemoveMap(__instance.Map, notifyPlayer: true);
                    }
                }

                else
                {
                    if (__instance.HasMap && __instance.ShouldRemoveMapNow(out bool alsoRemoveWorldObject))
                    {
                        Current.Game.DeinitAndRemoveMap(__instance.Map, notifyPlayer: true);
                        if (!__instance.Destroyed && (alsoRemoveWorldObject || __instance.forceRemoveWorldObjectWhenMapRemoved))
                        {
                            __instance.Destroy();
                        }
                    }
                }

                return false;
            }
        }
    }

    [HarmonyPatch(typeof(WorldInspectPane), "CurTabs", MethodType.Getter)]
    public static class AddSideTabs
    {
        [HarmonyPrefix]
        public static bool DoPre(WorldInspectPane __instance, ref IEnumerable<InspectTabBase> __result)
        {
            if (Network.State != ClientNetworkState.Connected) return false;
            else
            {
                if (Find.WorldSelector.NumSelectedObjects == 1)
                {
                    __result = Find.WorldSelector.SingleSelectedObject.GetInspectTabs();
                }

                if (Find.WorldSelector.NumSelectedObjects == 0 && Find.WorldSelector.SelectedTile.Valid)
                {
                    __result = PlanetLayer.Selected.Def.Tabs;
                    __result = __result.AddItem(new PlayersUI());
                    __result = __result.AddItem(new BasesUI());
                    __result = __result.AddItem(new SitesUI());
                }

                return false;
            }
        }
    }

    [HarmonyPatch(typeof(SettlementProximityGoodwillUtility), "AppendProximityGoodwillOffsets")]
    public static class PrevenGoodwillChangePatch
    {
        [HarmonyPrefix]
        public static bool DoPre(ref int tile, ref List<Pair<Settlement, int>> outOffsets)
        {
            if (Network.State == ClientNetworkState.Disconnected) return true;

            int maxDist = SettlementProximityGoodwillUtility.MaxDist;
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement settlement = settlements[i];

                if (ClientValues.PlayerFactions.Contains(settlement.Faction) || settlement.Faction == Faction.OfPlayer) continue;
                else
                {
                    int num = Find.WorldGrid.TraversalDistanceBetween(tile, settlement.Tile, passImpassable: false, maxDist);
                    if (num != int.MaxValue)
                    {
                        int num2 = Mathf.RoundToInt(DiplomacyTuning.Goodwill_PerQuadrumFromSettlementProximity.Evaluate(num));
                        if (num2 != 0) outOffsets.Add(new Pair<Settlement, int>(settlement, num2));
                    }
                }
            }

            return false;
        }
    }
}
