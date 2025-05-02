using RimWorld.Planet;
using RimWorld;
using System;
using System.Linq;
using Verse;
using Shared;
using static Shared.CommonEnumerators;
using System.Collections.Generic;
using GameClient.Misc;
using GameClient.Values;
using GameClient.TCP;
using UnityEngine.Tilemaps;

namespace GameClient.Managers
{

    public static class NPCManager
    {
        [HandlesPacket(PacketHeader.NPCManager)]
        private static void ParsePacket(byte[] bytes)
        {
            NPCSettlementData data = Serializer.ConvertBytesToObject<NPCSettlementData>(bytes);

            switch (data._stepMode)
            {
                case SettlementStepMode.Add:
                    break;

                case SettlementStepMode.Remove:
                    RemoveNPCSettlementFromPacket(data._settlementData);
                    break;
            }
        }

        public static void AddSettlements(PlanetNPCSettlementDetails[] settlements)
        {
            foreach (PlanetNPCSettlementDetails settlement in settlements)
            {
                SpawnSingleSettlement(settlement);
            }
        }

        public static void SpawnSingleSettlement(PlanetNPCSettlementDetails toAdd)
        {
            if (Find.WorldObjects.Settlements.FirstOrDefault(fetch => fetch.Tile == toAdd.tile) != null) return;
            else
            {
                Settlement settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                settlement.Tile = toAdd.tile;
                settlement.Name = toAdd.name;

                List<Faction> factions = PlanetManagerHelper.GetNPCFactionFromDefName(toAdd.defName);

                if (factions.Count == 0)
                {
                    Printer.Warning($"Could not find faction for settlement at tile {toAdd.tile} with faction {toAdd.defName}");
                    return;
                }

                else if (factions.Count == 1)
                {
                    settlement.SetFaction(factions.First());
                }

                else if (factions.Count > 1)
                {
                    foreach (Faction faction in factions)
                    {
                        if (faction.Name == toAdd.factionName) settlement.SetFaction(faction);
                    }

                    if (settlement.Faction == null) settlement.SetFaction(factions.First());
                }

                Find.WorldObjects.Add(settlement);

                NPCSettlementManagerHelper.TryRelinkQuest(settlement);
            }
        }

        public static void ClearAllSettlements()
        {
            Settlement[] settlements = Find.WorldObjects.Settlements.Where(fetch => !ClientValues.playerFactions.Contains(fetch.Faction) &&
                fetch.Faction != Faction.OfPlayer).ToArray();

            foreach (Settlement settlement in settlements) RemoveSingleSettlement(settlement, null);

            DestroyedSettlement[] destroyedSettlements = Find.WorldObjects.DestroyedSettlements.Where(fetch => !ClientValues.playerFactions.Contains(fetch.Faction) &&
                fetch.Faction != Faction.OfPlayer).ToArray();

            foreach (DestroyedSettlement settlement in destroyedSettlements) RemoveSingleSettlement(null, settlement);
        }

        public static void RemoveNPCSettlementFromPacket(PlanetNPCSettlementDetails data)
        {
            Settlement toRemove = Find.World.worldObjects.Settlements.FirstOrDefault(fetch => fetch.Tile == data.tile &&
                fetch.Faction != Faction.OfPlayer);

            if (toRemove != null) RemoveSingleSettlement(toRemove, null);
        }

        public static void RemoveSingleSettlement(Settlement settlement, DestroyedSettlement destroyedSettlement)
        {
            if (settlement != null)
            {
                try
                {
                    if (!RimworldManager.CheckIfMapHasPlayerPawns(settlement.Map))
                    {
                        NPCSettlementManagerHelper.lastRemovedSettlement = settlement;
                        Find.WorldObjects.Remove(settlement);
                    }
                    else Printer.Warning($"Ignored removal of settlement at {settlement.Tile} because player was inside");
                }
                catch (Exception e) { Printer.Warning($"Failed to remove NPC settlement at {settlement.Tile}. Reason: {e}"); }
            }

            else if (destroyedSettlement != null)
            {
                try
                {
                    if (!RimworldManager.CheckIfMapHasPlayerPawns(destroyedSettlement.Map))
                    {
                        Find.WorldObjects.Remove(destroyedSettlement);
                    }
                    else Printer.Warning($"Ignored removal of settlement at {destroyedSettlement.Tile} because player was inside");
                }
                catch (Exception e) { Printer.Warning($"Failed to remove NPC settlement at {destroyedSettlement.Tile}. Reason: {e}"); }
            }
        }

        public static void RequestSettlementRemoval(Settlement settlement)
        {
            NPCSettlementData data = new NPCSettlementData();
            data._stepMode = SettlementStepMode.Remove;
            data._settlementData.tile = settlement.Tile;

            Network.listener.EnqueuePacket(PacketHeader.NPCManager, data);
        }
    }

    public static class NPCSettlementManagerHelper
    {
        public static PlanetNPCSettlementDetails[] tempNPCSettlements;

        public static Settlement lastRemovedSettlement;

        private static Dictionary<int, List<QuestPart>> questToFixTemp = new Dictionary<int, List<QuestPart>>();

        public static void SetValues(ServerGlobalData serverGlobalData)
        {
            tempNPCSettlements = serverGlobalData._npcSettlements;
        }

        public static void SaveAllQuests() 
        {
            foreach(var quest in Find.QuestManager.QuestsListForReading.Where(x => !x.Historical)) 
            {
                TrySaveQuest(quest);
            }
        }

        public static void CleanupQuests() 
        {
            questToFixTemp.Clear();
        }

        private static void TrySaveQuest(Quest quest)
        {
            Printer.Warning($"Trying to save quest with id {quest.id}");
            var questPart = quest.PartsListForReading.Where(x => x is QuestPart_SpawnWorldObject
            || x is QuestPart_DisableTradeRequest
            || x is QuestPart_TradeRequestInactive
            || x is QuestPart_InitiateTradeRequest);
            foreach (var part in questPart) 
            {
                int tile = -1;

                if(part is QuestPart_SpawnWorldObject part2) 
                {
                    Printer.Warning($"Found {typeof(QuestPart_SpawnWorldObject).Name}!", LogImportanceMode.Verbose);
                    tile = part2.worldObject?.Tile ?? -1;
                }
                else if(part is QuestPart_DisableTradeRequest part3) 
                {
                    Printer.Warning($"Found {typeof(QuestPart_DisableTradeRequest).Name}!", LogImportanceMode.Verbose);
                    tile = part3.settlement?.Tile ?? -1;
                }
                else if(part is QuestPart_TradeRequestInactive part4) 
                {
                    Printer.Warning($"Found {typeof(QuestPart_TradeRequestInactive).Name}!", LogImportanceMode.Verbose);
                    tile = part4.settlement?.Tile ?? -1;
                }
                else if (part is QuestPart_InitiateTradeRequest part5)
                {
                    Printer.Warning($"Found {typeof(QuestPart_InitiateTradeRequest).Name}!", LogImportanceMode.Verbose);
                    tile = part5.settlement?.Tile ?? -1;
                }

                if (tile != -1)
                {
                    Printer.Warning($"Saved quest {quest.id}", LogImportanceMode.Verbose);
                    if (questToFixTemp.ContainsKey(tile))
                    {
                        questToFixTemp[tile].Add(part);
                    }
                    else
                    {
                        questToFixTemp[tile] = new List<QuestPart>() { part };
                    }
                }
            }
        }

        public static void TryRelinkQuest(WorldObject obj) 
        {
            try
            {
                if (questToFixTemp.TryGetValue(obj.Tile, out var parts))
                {
                    Printer.Warning($"Found quest with id {parts.First().quest.id}");
                    foreach (var part in parts)
                    {
                        if (part is QuestPart_SpawnWorldObject part2)
                        {
                            Printer.Warning($"Found {typeof(QuestPart_SpawnWorldObject).Name}!", LogImportanceMode.Verbose);
                            part2.worldObject = obj;
                        }
                        else if (part is QuestPart_DisableTradeRequest part3)
                        {
                            Printer.Warning($"Found {typeof(QuestPart_DisableTradeRequest).Name}!", LogImportanceMode.Verbose);
                            part3.settlement = (Settlement)obj;
                        }
                        else if (part is QuestPart_TradeRequestInactive part4)
                        {
                            Printer.Warning($"Found {typeof(QuestPart_TradeRequestInactive).Name}!", LogImportanceMode.Verbose);
                            part4.settlement = (Settlement)obj;
                        }
                        else if (part is QuestPart_InitiateTradeRequest part5)
                        {
                            Printer.Warning($"Found {typeof(QuestPart_InitiateTradeRequest).Name}!", LogImportanceMode.Verbose);
                            part5.settlement = (Settlement)obj;
                        }
                        
                    }
                    Printer.Warning($"Loaded quest with id {parts.First().quest.id} on tile {obj.Tile}.", LogImportanceMode.Verbose);
                }
            }
            catch (Exception ex)
            {
                Printer.Warning($"Error while trying to relink quests\n{ex}");
            }
        }
    }
}
