using RimWorld.Planet;
using RimWorld;
using Shared;
using Verse;
using static Shared.CommonEnumerators;
using GameClient.Dialogs;
using GameClient.Values;
using GameClient.TCP;
using GameClient.Misc;

namespace GameClient.Managers
{

    public static class ActivityManager
    {
        [HandlesPacket(PacketHeader.ActivityManager)]
        private static void ParsePacket(byte[] bytes)
        {
            ActivityData data = Serializer.ConvertBytesToObject<ActivityData>(bytes);

            switch (data._stepMode)
            {
                case ActivityStepMode.Request:
                    OnAccept(data);
                    break;

                case ActivityStepMode.Deny:
                    OnDeny();
                    break;
            }
        }

        public static void RequestActivity(ActivityType type, int targetTile)
        {
            if (!SessionValues.ActionValues.EnableActivities)
            {
                DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                return;
            }

            SessionValues.ToggleActivity(type);

            SendRequest(targetTile);
        }

        private static void SendRequest(int targetTile)
        {
            DialogManager.PushNewDialog(new RT_Dialog_Wait("Waiting for map"));

            ActivityData data = new ActivityData();
            data._stepMode = ActivityStepMode.Request;
            data._targetTile = targetTile;

            Network.listener.EnqueuePacket(PacketHeader.ActivityManager, data);
        }

        private static void OnAccept(ActivityData offlineVisitData) 
        {
            DialogManager.PopWaitDialog();

            PrepareMap(offlineVisitData._mapFile); 
        }

        private static void OnDeny()
        {
            DialogManager.PopWaitDialog();

            DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "This map is currently unavailable!" }));
        }

        private static void PrepareMap(MapFile mapFile)
        {
            Map map = null;

            if (SessionValues.latestActivity == ActivityType.Visit)
            {
                map = MapSaveLoader.StringToMap(mapFile, false, true, true, true, true, true, false);
            }

            else if (SessionValues.latestActivity == ActivityType.Raid)
            {
                map = MapSaveLoader.StringToMap(mapFile, true, true, true, true, true, true, true);
            }

            else if (SessionValues.latestActivity == ActivityType.Spy)
            {
                map = MapSaveLoader.StringToMap(mapFile, true, true, true, true, true, true, true);
            }

            Faction faction;
            if (SessionValues.latestActivity == ActivityType.Visit) faction = ClientValues.allyPlayer;
            else if (SessionValues.latestActivity == ActivityType.Raid) faction = ClientValues.enemyPlayer;
            else faction = ClientValues.enemyPlayer;

            RimworldManager.HandleMapFactions(map, faction);

            RimworldManager.PrepareMapLord(map, faction);

            if (SessionValues.latestActivity == ActivityType.Visit)
            {
                CaravanEnterMapUtility.Enter(SessionValues.ChosenCaravan, map, CaravanEnterMode.Edge,
                    CaravanDropInventoryMode.DoNotDrop, draftColonists: false);
            }

            else if (SessionValues.latestActivity == ActivityType.Raid)
            {
                SettlementUtility.Attack(SessionValues.ChosenCaravan, SessionValues.ChosenSettlement);
            }

            else if (SessionValues.latestActivity == ActivityType.Spy)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                Caravan caravan = CaravanMaker.MakeCaravan(new Pawn[] { pawn }, Faction.OfPlayer, map.Tile, true);

                CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Edge,
                    CaravanDropInventoryMode.DoNotDrop, draftColonists: true);
            }
        }
    }
}
