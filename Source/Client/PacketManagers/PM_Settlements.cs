using GameClient.Managers;
using GameClient.Misc;
using GameClient.WorldObjects;
using RimWorld;
using RimWorld.Planet;
using Shared;
using Shared.Files;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using TCPNetwork;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient.PacketManagers
{
    public class PM_Settlements : PM_Base
    {
        public static List<WO_Settlement> PlayerSettlements { get; set; } = new List<WO_Settlement>();

        [HandlesPacket(PacketHeader.Settlement)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_PlayerSettlement data = Serializer.ConvertBytesToObject<PKT_PlayerSettlement>(bytes);

            switch (data._stepMode)
            {
                case SettlementStepMode.Add:
                    SpawnSingleSettlement(data._settlementFile);
                    break;

                case SettlementStepMode.Remove:
                    RemoveSingleSettlement(data._settlementFile);
                    break;
            }
        }

        public static void AddSettlements(List<FL_Settlement> settlements)
        {
            foreach (FL_Settlement toAdd in settlements)
            {
                SpawnSingleSettlement(toAdd);
            }
        }

        public static void SpawnSingleSettlement(FL_Settlement toAdd)
        {
            if (!RimworldManager.CheckIfTileIsValid(toAdd.Tile)) return;
            else
            {
                try
                {
                    WorldObjectDef def = DefDatabase<WorldObjectDef>.AllDefs.First(fetch => fetch.defName == "RTSettlement");
                    WO_Settlement settlement = (WO_Settlement)WorldObjectMaker.MakeWorldObject(def);
                    settlement.Tile = toAdd.Tile;
                    settlement.Name = $"{toAdd.Username}'s settlement";
                    settlement.SetFaction(PlanetManagerHelper.GetPlayerFactionFromGoodwill(toAdd.Goodwill));

                    PlayerSettlements.Add(settlement);
                    Find.WorldObjects.Add(settlement);
                }
                catch (Exception e) { Printer.Error($"Failed to spawn settlement at {toAdd.Tile}. Reason: {e}"); }
            }
        }

        public static void RemoveSingleSettlement(FL_Settlement toRemove)
        {
            try
            {
                WO_Settlement toGet = Finder.GetRTSettlementFromTile(toRemove.Tile);
                PlayerSettlements.Remove(toGet); 
                Find.WorldObjects.Remove(toGet);
                toGet.Destroy();
            }
            catch (Exception e) { Printer.Error($"Failed to remove settlement at {toRemove.Tile}. Reason: {e}"); }
        }

        public static void RegenSettlement(WO_Settlement _)
        {
            FL_Settlement file = new FL_Settlement();
            file.Tile = _.Tile;
            file.Username = _.Label.Replace("'s settlement", "");

            if (_.Faction == SessionHandler.EnemyFaction) file.Goodwill = Goodwill.Enemy;
            else if (_.Faction == SessionHandler.AllyFaction) file.Goodwill = Goodwill.Ally;
            else if (_.Faction == SessionHandler.GuildFaction) file.Goodwill = Goodwill.Guild;
            else file.Goodwill = Goodwill.Neutral;

            RemoveSingleSettlement(file);
            SpawnSingleSettlement(file);
        }

        public static void SendNewPlayerSettlement(int settlementTile)
        {
            PKT_PlayerSettlement settlementData = new PKT_PlayerSettlement();
            settlementData._settlementFile.Tile = settlementTile;
            settlementData._stepMode = SettlementStepMode.Add;

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Settlement, settlementData);

            //TODO
            //Find a way to enable this
            //Async map generation will break saving the map at this step
            //PM_Saves.ForceSave();
        }

        public static void AbandonSettlement(int settlementTile)
        {
            PKT_PlayerSettlement settlementData = new PKT_PlayerSettlement();
            settlementData._settlementFile.Tile = settlementTile;
            settlementData._stepMode = SettlementStepMode.Remove;

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Settlement, settlementData);
            PM_Saves.ForceSave();
        }
    }
}
