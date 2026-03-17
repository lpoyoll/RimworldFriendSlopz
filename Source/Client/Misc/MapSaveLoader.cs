using GameClient.Defs;
using GameClient.Managers;
using RimWorld;
using RimWorld.Planet;
using Shared.Files;
using Shared.Files.Maps;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using Verse;
using Verse.Noise;
using static Shared.CommonEnumerators;
using static UnityEngine.GraphicsBuffer;

namespace GameClient.Misc
{
    public static class MapSaveLoader
    {
        public enum OperationType { Get, Set }

        public static MapFile MapToString(Map map)
        {
            MapFile mapFile = new MapFile();

            mapFile.Tile = map.Tile;

            mapFile.Size = ValueParser.IntVec3ToArray(map.Size);

            mapFile.Wealth = (int)map.wealthWatcher.WealthTotal;

            ToggleWeather(OperationType.Get, mapFile, map);

            GetMapTerrain(mapFile, map);

            GetMapThings(mapFile, map);

            GetMapPawns(mapFile, map);

            return mapFile;
        }

        public static Map StringToMap(MapFile mapFile, bool enforceIDs = false)
        {
            Map map = GetOrGenerateMapUtility.GetOrGenerateMap(mapFile.Tile, ValueParser.ArrayToIntVec3(mapFile.Size), null);

            SetMapTerrain(mapFile, map);

            SetMapThings(mapFile, map, enforceIDs);

            SetMapPawns(mapFile, map, enforceIDs);

            ToggleWeather(OperationType.Set, mapFile, map);

            RegenerateRoofGrid(mapFile, map);

            RegenerateFog(map);

            return map;
        }

        private static void GetMapTerrain(MapFile mapFile, Map map)
        {
            for (int z = 0; z < map.Size.z; ++z)
            {
                for (int x = 0; x < map.Size.x; ++x)
                {
                    MapTile component = new MapTile();
                    IntVec3 vectorToCheck = new IntVec3(x, map.Size.y, z);

                    component.TileString = map.terrainGrid.TerrainAt(vectorToCheck).defName;

                    component.IsPolluted = map.pollutionGrid.IsPolluted(vectorToCheck);

                    try { component.RoofString = map.roofGrid.RoofAt(vectorToCheck).defName; }
                    catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Extreme); }

                    mapFile.Tiles.Add(component);
                }
            }
        }

        private static void GetMapThings(MapFile mapFile, Map map)
        {
            foreach (Thing thing in map.listerThings.AllThings.Where(fetch => !RimworldManager.CheckIfThingIsPawn(fetch)).ToArray())
            {
                try { mapFile.Things.Add(ScribeManager.SerializeToString(thing, ScribeManager.SerializableType.Thing)); }
                catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
            }
        }

        private static void GetMapPawns(MapFile mapFile, Map map)
        {
            foreach (Thing pawn in map.listerThings.AllThings.Where(fetch => RimworldManager.CheckIfThingIsPawn(fetch)).ToArray())
            {
                try { mapFile.Pawns.Add(ScribeManager.SerializeToString(pawn, ScribeManager.SerializableType.Pawn)); }
                catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
            }
        }

        private static void SetMapTerrain(MapFile mapFile, Map map)
        {
            int index = 0;

            for (int z = 0; z < map.Size.z; ++z)
            {
                for (int x = 0; x < map.Size.x; ++x)
                {
                    try
                    {
                        MapTile component = mapFile.Tiles[index];
                        IntVec3 vectorToCheck = new IntVec3(x, map.Size.y, z);
                        map.pollutionGrid.SetPolluted(vectorToCheck, component.IsPolluted);

                        if (component.TileString != null)
                        {
                            TerrainDef terrainToUse = DefDatabase<TerrainDef>.AllDefs.First(fetch => fetch.defName == component.TileString);
                            map.terrainGrid.SetTerrain(vectorToCheck, terrainToUse);
                        }

                        if (component.RoofString != null)
                        {
                            RoofDef roofToUse = DefDatabase<RoofDef>.AllDefs.First(fetch => fetch.defName == component.RoofString);
                            map.roofGrid.SetRoof(vectorToCheck, roofToUse);
                        }
                    }
                    catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }

                    index++;
                }
            }
        }

        private static void SetMapThings(MapFile mapFile, Map map, bool enforceIDs)
        {
            foreach (string str in mapFile.Things)
            {
                try
                {
                    Thing thing = ScribeManager.SerializeFromString<Thing>(str, ScribeManager.SerializableType.Thing, enforceIDs);
                    RimworldManager.PlaceThingIntoMap(thing, map, thing.Position);
                }
                catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
            }
        }

        private static void SetMapPawns(MapFile mapFile, Map map, bool enforceIDs)
        {
            foreach (string str in mapFile.Pawns)
            {
                try
                {
                    Pawn pawn = ScribeManager.SerializeFromString<Pawn>(str, ScribeManager.SerializableType.Pawn, enforceIDs);
                    RimworldManager.PlaceThingIntoMap(pawn, map, pawn.PositionHeld);
                }
                catch (Exception e) { Printer.Warning(e.ToString(), LogImportanceMode.Verbose); }
            }
        }

        private static void RegenerateRoofGrid(MapFile mapFile, Map map)
        {
            map.roofCollapseBuffer.Clear();
            map.roofGrid.Drawer.SetDirty();
        }

        private static void RegenerateFog(Map map)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            Caravan caravan = CaravanMaker.MakeCaravan(new Pawn[] { pawn }, Faction.OfPlayer, map.Tile, true);
            CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Edge);

            FloodFillerFog.FloodUnfog(MapGenerator.PlayerStartSpot, map);

            pawn.Destroy();
        }

        private static void ToggleWeather(OperationType type, MapFile mapFile, Map map)
        {
            if (type == OperationType.Set) map.weatherManager.TransitionTo(DefDatabase<WeatherDef>.AllDefs.ToList()[mapFile.WeatherByte]);
            else mapFile.WeatherByte = (byte)DefDatabase<WeatherDef>.AllDefs.FirstIndexOf(fetch => fetch == map.weatherManager.curWeather);
        }
    }
}
