using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RTClient.Managers;
using RTClient.PacketManagers;
using RTClient.PacketManagers.Synchronous;
using RTClient.WorldObjects;
using RTNetwork.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimjob.SharedColony
{
    internal static class SharedColonyJoin
    {
        private static WO_Settlement pendingHost;
        private static Map pendingGuestMap;
        private static Caravan pendingGuestCaravan;
        private static int promptAtTick = -1;

        public static void DetectSharedTile(int tile)
        {
            WO_Settlement host = PM_Settlements.PlayerSettlements
                .FirstOrDefault(settlement => settlement != null && settlement.Tile == tile);

            if (host == null) return;

            pendingHost = host;
            promptAtTick = Find.TickManager.TicksGame + 90;
            Log.Message($"[Rimjob] Shared colony detected at tile {tile}; preparing live join");
        }

        public static void Tick()
        {
            if (pendingHost == null || promptAtTick < 0 || Find.TickManager.TicksGame < promptAtTick) return;

            promptAtTick = -1;
            string hostName = pendingHost.LabelCap;
            string text = $"Join {hostName} live?\n\nYour pawns and loose starting supplies will move onto the host's map. You will keep control of your own pawns; the host's pawns remain under their control.";
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, Begin));
        }

        private static void Begin()
        {
            try
            {
                Map map = Find.AnyPlayerHomeMap;
                if (map == null || pendingHost == null)
                {
                    Fail("The local or host map is no longer available.");
                    return;
                }

                List<Pawn> pawns = map.mapPawns.AllPawns
                    .Where(pawn => pawn != null && !pawn.Dead && pawn.Faction == Faction.OfPlayer)
                    .ToList();

                if (pawns.Count == 0)
                {
                    Fail("There are no player-controlled pawns to move into the shared colony.");
                    return;
                }

                pendingGuestMap = map;
                pendingGuestCaravan = CaravanExitMapUtility.ExitMapAndCreateCaravan(
                    pawns,
                    Faction.OfPlayer,
                    map.Tile,
                    map.Tile,
                    PlanetTile.Invalid,
                    sendMessage: false);

                if (pendingGuestCaravan == null)
                {
                    Fail("RimWorld could not form the joining caravan.");
                    return;
                }

                MoveLooseSupplies(map, pendingGuestCaravan);

                SessionManager.ChosenSettlement = pendingHost;
                SessionManager.ChosenCaravan = pendingGuestCaravan;
                SharedColonyBootstrap.EnableSynchronousMode();
                PM_Synchronous.Ask(pendingHost.Tile, PKT_Synchronous.Type.Visit);
            }
            catch (Exception exception)
            {
                Log.Error($"[Rimjob] Failed to begin shared-colony join: {exception}");
                RestoreGuestMap();
            }
        }

        private static void MoveLooseSupplies(Map map, Caravan caravan)
        {
            List<Thing> supplies = map.listerThings.AllThings
                .Where(thing => thing != null && thing.Spawned && !(thing is Pawn) && thing.def.EverHaulable)
                .ToList();

            foreach (Thing thing in supplies)
            {
                thing.DeSpawn();
                CaravanInventoryUtility.GiveThing(caravan, thing);
            }
        }

        public static void PrepareForAcceptedSession()
        {
            if (pendingGuestMap == null) return;

            Map map = pendingGuestMap;
            pendingGuestMap = null;

            if (Find.Maps.Contains(map)) Current.Game.DeinitAndRemoveMap(map, false);
        }

        public static void CompleteAcceptedSession()
        {
            pendingHost = null;
            pendingGuestMap = null;
            pendingGuestCaravan = null;
        }

        public static void RestoreGuestMap()
        {
            if (pendingGuestMap != null && pendingGuestCaravan != null && Find.Maps.Contains(pendingGuestMap))
            {
                CaravanEnterMapUtility.Enter(
                    pendingGuestCaravan,
                    pendingGuestMap,
                    CaravanEnterMode.Center,
                    CaravanDropInventoryMode.DoNotDrop,
                    draftColonists: false);
            }

            pendingHost = null;
            pendingGuestMap = null;
            pendingGuestCaravan = null;
            promptAtTick = -1;
        }

        private static void Fail(string message)
        {
            Messages.Message($"Rimjob shared-colony join failed: {message}", MessageTypeDefOf.RejectInput, false);
            RestoreGuestMap();
        }
    }

    public sealed class SharedColonyComponent : GameComponent
    {
        public SharedColonyComponent(Game game) { }

        public override void GameComponentTick()
        {
            SharedColonyJoin.Tick();
        }
    }

    [HarmonyPatch(typeof(PM_Settlements), nameof(PM_Settlements.SendNewPlayerSettlement))]
    internal static class SendNewSettlementPatch
    {
        [HarmonyPostfix]
        private static void Postfix(int settlementTile)
        {
            SharedColonyJoin.DetectSharedTile(settlementTile);
        }
    }

    [HarmonyPatch(typeof(PM_Synchronous), "OnAsk")]
    internal static class SynchronousAskPatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            SharedColonyBootstrap.EnableSynchronousMode();
        }
    }

    [HarmonyPatch(typeof(PM_Synchronous), "OnAccept")]
    internal static class SynchronousAcceptPatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            SharedColonyJoin.PrepareForAcceptedSession();
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            SharedColonyJoin.CompleteAcceptedSession();
        }
    }

    [HarmonyPatch(typeof(PM_Synchronous), "OnReject")]
    internal static class SynchronousRejectPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            SharedColonyJoin.RestoreGuestMap();
        }
    }

    [HarmonyPatch(typeof(PM_ResponseShortcuts), nameof(PM_ResponseShortcuts.Receive))]
    internal static class SynchronousUnavailablePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            SharedColonyJoin.RestoreGuestMap();
        }
    }
}
