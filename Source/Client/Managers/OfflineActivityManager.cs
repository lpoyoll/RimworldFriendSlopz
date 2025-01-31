using RimWorld.Planet;
using RimWorld;
using Shared;
using System;
using System.Linq;
using Verse.AI.Group;
using Verse;
using static Shared.CommonEnumerators;
using GameClient.Dialogs;
using GameClient.Scribers;
using GameClient.Values;
using GameClient.TCP;

namespace GameClient.Managers
{
    [RTManager]
    public static class OfflineActivityManager
    {
        public static void ParsePacket(Packet packet)
        {
            OfflineActivityData offlineVisitData = Serializer.ConvertBytesToObject<OfflineActivityData>(packet.contents);

            switch (offlineVisitData._stepMode)
            {
                case OfflineActivityStepMode.Request:
                    OnRequestAccepted(offlineVisitData);
                    break;

                case OfflineActivityStepMode.Deny:
                    OnOfflineActivityDeny();
                    break;
            }
        }

        //Requests a raid to the server

        public static void RequestOfflineActivity(OfflineActivityType activityType)
        {
            if (!SessionValues.actionValues.EnableOfflineActivities)
            {
                DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                return;
            }

            SessionValues.ToggleOfflineActivity(activityType);

            SendRequest();
        }

        private static void SendRequest()
        {
            DialogManager.PushNewDialog(new RT_Dialog_Wait("Waiting for map"));

            OfflineActivityData data = new OfflineActivityData();
            data._stepMode = OfflineActivityStepMode.Request;
            data._targetTile = SessionValues.chosenSettlement.Tile;

            Packet packet = Packet.CreatePacketFromObject(nameof(OfflineActivityManager), data);
            Network.listener.EnqueuePacket(packet);
        }

        //Executes when offline visit is denied

        private static void OnOfflineActivityDeny()
        {
            DialogManager.PopWaitDialog();

            DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "This player is currently unavailable!" }));
        }

        //Executes when offline visit is accepted

        private static void OnRequestAccepted(OfflineActivityData offlineVisitData)
        {
            DialogManager.PopWaitDialog();

            Action r1 = delegate { PrepareMapForOfflineActivity(offlineVisitData._mapFile); };

            r1.Invoke();
        }

        //Prepares a map for the offline visit feature from a request

        private static void PrepareMapForOfflineActivity(MapFile mapFile)
        {
            Map map = null;

            if (SessionValues.latestOfflineActivity == OfflineActivityType.Visit)
            {
                map = MapScriber.StringToMap(mapFile, false, true, true, true, true, true, false);
            }

            else if (SessionValues.latestOfflineActivity == OfflineActivityType.Raid)
            {
                map = MapScriber.StringToMap(mapFile, true, true, true, true, true, true, true);
            }

            Faction faction;
            if (SessionValues.latestOfflineActivity == OfflineActivityType.Visit) faction = FactionValues.allyPlayer;
            else faction = FactionValues.enemyPlayer;

            RimworldManager.HandleMapFactions(map, faction);

            RimworldManager.PrepareMapLord(map, faction);

            if (SessionValues.latestOfflineActivity == OfflineActivityType.Visit)
            {
                CaravanEnterMapUtility.Enter(SessionValues.chosenCaravan, map, CaravanEnterMode.Edge,
                    CaravanDropInventoryMode.DoNotDrop, draftColonists: true);
            }

            else if (SessionValues.latestOfflineActivity == OfflineActivityType.Raid)
            {
                SettlementUtility.Attack(SessionValues.chosenCaravan, SessionValues.chosenSettlement);
            }
        }
    }
}
