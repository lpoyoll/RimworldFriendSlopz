using GameClient.Managers;
using RimWorld;
using Shared.Files;
using Shared.Files.Maps;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient.Misc
{
    public static class MapSaveLoader
    {
        public static MapFile MapToString(Map map)
        {
            MapFile mapFile = new MapFile();

            mapFile.Tile = map.Tile;

            mapFile.Size = ValueParser.IntVec3ToArray(map.Size);

            mapFile.Wealth = (int)map.wealthWatcher.WealthTotal;

            mapFile.WeatherByte = (byte)DefDatabase<WeatherDef>.AllDefs.FirstIndexOf(fetch => fetch == map.weatherManager.curWeather);

            mapFile.Mods = ModManagerH.GetRunningModList();

            GetMapTerrain(mapFile, map);

            GetMapThings(mapFile, map);

            GetMapHumans(mapFile, map);

            GetMapAnimals(mapFile, map);

            return mapFile;
        }

        public static Map StringToMap(MapFile mapFile, bool factionThings, bool nonFactionThings, bool factionHumans, bool nonFactionHumans, 
            bool factionAnimals, bool nonFactionAnimals, bool lessLoot = false, bool enforceIDs = false)
        {
            Map map = SetEmptyMap(mapFile, mapFile.Tile);

            SetMapTerrain(mapFile, map);

            if (factionThings || nonFactionThings) SetMapThings(mapFile, map, factionThings, nonFactionThings, lessLoot, enforceIDs);

            if (factionHumans || nonFactionHumans) SetMapHumans(mapFile, map, factionHumans, nonFactionHumans, enforceIDs);

            if (factionAnimals || nonFactionAnimals) SetMapAnimals(mapFile, map, factionAnimals, nonFactionAnimals, enforceIDs);

            SetWeather(mapFile, map);

            SetFog(map);

            SetRoofs(map);

            return map;
        }

        private static void GetMapTerrain(MapFile mapFile, Map map)
        {
            try
            {
                for (int z = 0; z < map.Size.z; ++z)
                {
                    for (int x = 0; x < map.Size.x; ++x)
                    {
                        IntVec3 vectorToCheck = new IntVec3(x, map.Size.y, z);

                        MapTile component = new MapTile();

                        TerrainDef terrainDef = map.terrainGrid.TerrainAt(vectorToCheck);
                        if (terrainDef != null) component.TileByte = (byte)DefDatabase<TerrainDef>.AllDefs.FirstIndexOf(fetch => fetch == terrainDef);

                        component.IsPolluted = map.pollutionGrid.IsPolluted(vectorToCheck);

                        RoofDef roofDef = map.roofGrid.RoofAt(vectorToCheck);
                        if (roofDef != null) component.RoofByte = (byte)DefDatabase<RoofDef>.AllDefs.FirstIndexOf(fetch => fetch == roofDef);

                        mapFile.Tiles.Add(component);
                    }
                }
            }
            catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
        }

        private static void GetMapThings(MapFile mapFile, Map map)
        {
            Thing[] toList = map.listerThings.AllThings.Where(fetch => !ScriberH.CheckIfThingIsHuman(fetch) && !ScriberH.CheckIfThingIsAnimal(fetch)).ToArray();
            foreach (Thing thing in toList)
            {
                try
                {
                    string data = ScribeManager.SerializeToString(thing, ScribeManager.SerializableType.Thing);
                    if (thing.def.alwaysHaulable) mapFile.FactionThings.Add(data);
                    else if (!thing.def.alwaysHaulable) mapFile.NonFactionThings.Add(data);
                }
                catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
            }
        }

        private static void GetMapHumans(MapFile mapFile, Map map)
        {
            Thing[] toList = map.listerThings.AllThings.Where(fetch => ScriberH.CheckIfThingIsHuman(fetch)).ToArray();
            foreach (Thing thing in toList)
            {
                try
                {
                    string humanData = ScribeManager.SerializeToString(thing as Pawn, ScribeManager.SerializableType.Thing);
                    if (thing.Faction == Faction.OfPlayer) mapFile.FactionHumans.Add(humanData);
                    else if (thing.Faction != Faction.OfPlayer) mapFile.NonFactionHumans.Add(humanData);
                }
                catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
            }
        }

        private static void GetMapAnimals(MapFile mapFile, Map map)
        {
            Thing[] toList = map.listerThings.AllThings.Where(fetch => ScriberH.CheckIfThingIsAnimal(fetch)).ToArray();
            foreach (Thing thing in toList)
            {
                try
                {
                    string animalData = ScribeManager.SerializeToString(thing as Pawn, ScribeManager.SerializableType.Thing);
                    if (thing.Faction == Faction.OfPlayer) mapFile.FactionAnimals.Add(animalData);
                    else if (thing.Faction != Faction.OfPlayer) mapFile.NonFactionAnimals.Add(animalData);
                }
                catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
            }
        }

        private static Map SetEmptyMap(MapFile mapFile, int tileToUse)
        {
            try
            {
                PlanetManagerHelper.SetOverrideGenerators();
                Map toReturn = GetOrGenerateMapUtility.GetOrGenerateMap(tileToUse, ValueParser.ArrayToIntVec3(mapFile.Size), null);
                PlanetManagerHelper.SetDefaultGenerators();

                return toReturn;
            }

            catch (Exception e) 
            { 
                Printer.Error(e.ToString(), LogImportanceMode.Verbose);
                return null;
            }
        }

        private static void SetMapTerrain(MapFile mapFile, Map map)
        {
            int index = 0;

            try
            {
                for (int z = 0; z < map.Size.z; ++z)
                {
                    for (int x = 0; x < map.Size.x; ++x)
                    {
                        MapTile component = mapFile.Tiles[index];
                        IntVec3 vectorToCheck = new IntVec3(x, map.Size.y, z);

                        try
                        {
                            TerrainDef terrainToUse = DefDatabase<TerrainDef>.AllDefs.ToList()[component.TileByte];
                            map.terrainGrid.SetTerrain(vectorToCheck, terrainToUse);
                            map.pollutionGrid.SetPolluted(vectorToCheck, component.IsPolluted);
                        }
                        catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }

                        try
                        {
                            RoofDef roofToUse = DefDatabase<RoofDef>.AllDefs.ToList()[component.RoofByte];
                            map.roofGrid.SetRoof(vectorToCheck, roofToUse);
                        }
                        catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }

                        index++;
                    }
                }
            }
            catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
        }

        private static void SetMapThings(MapFile mapFile, Map map, bool factionThings, bool nonFactionThings, bool lessLoot, bool enforceIDs)
        {
            List<Thing> tileThings = new List<Thing>();
            Random rnd = new Random();

            if (factionThings)
            {
                foreach (string str in mapFile.FactionThings)
                {
                    try
                    {
                        if (lessLoot && rnd.Next(1, 100) <= 70) continue;
                        else
                        {
                            Thing thing = ScribeManager.SerializeFromString<Thing>(str, ScribeManager.SerializableType.Thing, enforceIDs);
                            if (thing.def.CanHaveFaction) thing.SetFaction(SessionHandler.NeutralFaction);
                            RimworldManager.PlaceThingIntoMap(thing, map, thing.Position);
                        }
                    }
                    catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
                }
            }

            if (nonFactionThings)
            {
                foreach (string str in mapFile.NonFactionThings)
                {
                    try 
                    {
                        Thing thing = ScribeManager.SerializeFromString<Thing>(str, ScribeManager.SerializableType.Thing, enforceIDs);
                        if (thing.def.CanHaveFaction) thing.SetFaction(SessionHandler.NeutralFaction);
                        RimworldManager.PlaceThingIntoMap(thing, map, thing.Position);
                    }
                    catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
                }
            }
        }

        private static void SetMapHumans(MapFile mapFile, Map map, bool factionHumans, bool nonFactionHumans, bool enforceIDs)
        {
            if (factionHumans)
            {
                foreach (string str in mapFile.FactionHumans)
                {
                    try
                    {
                        Pawn pawn = ScribeManager.SerializeFromString<Pawn>(str, ScribeManager.SerializableType.Pawn, enforceIDs);
                        pawn.SetFaction(SessionHandler.NeutralFaction);
                        RimworldManager.PlaceThingIntoMap(pawn, map, pawn.PositionHeld);
                    }
                    catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
                }
            }

            if (nonFactionHumans)
            {
                foreach (string str in mapFile.NonFactionHumans)
                {
                    try
                    {
                        Pawn pawn = ScribeManager.SerializeFromString<Pawn>(str, ScribeManager.SerializableType.Pawn, enforceIDs);
                        RimworldManager.PlaceThingIntoMap(pawn, map, pawn.PositionHeld);
                    }
                    catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
                }
            }
        }

        private static void SetMapAnimals(MapFile mapFile, Map map, bool factionAnimals, bool nonFactionAnimals, bool enforceIDs)
        {
            if (factionAnimals)
            {
                foreach (string str in mapFile.FactionAnimals)
                {
                    try
                    {
                        Pawn pawn = ScribeManager.SerializeFromString<Pawn>(str, ScribeManager.SerializableType.Pawn, enforceIDs);
                        pawn.SetFaction(SessionHandler.NeutralFaction);
                        RimworldManager.PlaceThingIntoMap(pawn, map, pawn.PositionHeld);
                    }
                    catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
                }
            }

            if (nonFactionAnimals)
            {
                foreach (string str in mapFile.NonFactionAnimals)
                {
                    try
                    {
                        Pawn pawn = ScribeManager.SerializeFromString<Pawn>(str, ScribeManager.SerializableType.Pawn, enforceIDs);
                        RimworldManager.PlaceThingIntoMap(pawn, map, pawn.PositionHeld);
                    }
                    catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
                }
            }
        }

        private static void SetWeather(MapFile mapFile, Map map)
        {
            try
            {
                WeatherDef weatherDef = DefDatabase<WeatherDef>.AllDefs.ToList()[mapFile.WeatherByte];
                map.weatherManager.TransitionTo(weatherDef);
            }
            catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
        }

        private static void SetFog(Map map)
        {
            try { FloodFillerFog.FloodUnfog(MapGenerator.PlayerStartSpot, map); }
            catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
        }

        private static void SetRoofs(Map map)
        {
            try
            {
                map.roofCollapseBuffer.Clear();
                map.roofGrid.Drawer.SetDirty();
            }
            catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
        }
    }
}
