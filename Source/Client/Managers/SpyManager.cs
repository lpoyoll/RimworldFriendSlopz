using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameClient.Dialogs;
using GameClient.Scribers;
using GameClient.TCP;
using GameClient.Values;
using RimWorld;
using RimWorld.Planet;
using Shared;
using UnityEngine.Tilemaps;
using Verse;
using Verse.AI.Group;
using static Shared.CommonEnumerators;

namespace GameClient.Managers
{
    [RTManager]
    public static class SpyManager
    {
        public static void ParsePacket(Packet packet)
        {
            SpyData data = Serializer.ConvertBytesToObject<SpyData>(packet.contents);

            switch (data._stepMode)
            {
                case SpyStepMode.Accept:
                    Spy(data);
                    break;

                case SpyStepMode.Deny:
                    DenySpy();
                    break;
            }
        }

        public static void RequestSpy(WorldObjectMode mode)
        {
            if (!SessionValues.actionValues.EnableSpying)
            {
                DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                return;
            }

            Action a1 = delegate
            {
                if (!RimworldManager.CheckIfHasEnoughSilverInMap(Find.AnyPlayerHomeMap, SessionValues.actionValues.SpyCost))
                {
                    DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "You do not have enough silver!" }));
                }

                else
                {
                    RimworldManager.RemoveThingFromSettlement(Find.AnyPlayerHomeMap, ThingDefOf.Silver, SessionValues.actionValues.SpyCost);

                    SpyData data = new SpyData();
                    data._stepMode = SpyStepMode.Request;
                    data._worldObjectMode = mode;

                    if (mode == WorldObjectMode.Settlement) data._mapTile = SessionValues.chosenSettlement.Tile;
                    else data._mapTile = SessionValues.chosenSite.Tile;

                    Packet packet = Packet.CreatePacketFromObject(nameof(SpyManager), data);
                    Network.listener.EnqueuePacket(packet);

                    DialogManager.PushNewDialog(new RT_Dialog_Wait("Waiting for server response"));

                    SaveManager.ForceSave();
                }
            };

            DialogManager.PushNewDialog(new RT_Dialog_YesNo($"Spying costs {SessionValues.actionValues.SpyCost} silver. Continue?", a1));
        }

        private static void Spy(SpyData data)
        {
            DialogManager.PopWaitDialog();

            SessionValues.chosenPawnForSpying = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            Caravan caravan = CaravanMaker.MakeCaravan(new Pawn[] { SessionValues.chosenPawnForSpying }, Faction.OfPlayer, data._mapTile, true);

            Map toUse;
            if (data._mapFile == null) toUse = GetOrGenerateMapUtility.GetOrGenerateMap(data._mapTile, null);
            else toUse = MapScriber.StringToMap(data._mapFile, true, true, true, true, true, true, false, false, data._worldObjectMode);

            RimworldManager.HandleMapFactions(toUse, FactionValues.enemyPlayer);

            RimworldManager.PrepareMapLord(toUse, FactionValues.enemyPlayer);

            CaravanEnterMapUtility.Enter(caravan, toUse, CaravanEnterMode.Edge);
        }

        private static void DenySpy()
        {
            DialogManager.PopWaitDialog();
            DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "The current action is not available!" }));
        }
    }
}
