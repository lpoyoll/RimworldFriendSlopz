using System;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RWTSharedColony
{
    /// <summary>
    /// Allows a joining Rimjob player to choose an existing RTSettlement tile
    /// as their first settlement, while leaving ordinary RimWorld occupancy
    /// rules unchanged everywhere else.
    ///
    /// v0.1.10 incorrectly required ProgramState.Playing. The starting-site page
    /// runs before the game enters Playing, so vanilla returned "This tile is occupied".
    /// v0.1.11 uses a final postfix override scoped to new-colony setup and an
    /// explicitly selected RTSettlement tile. This remains effective even if the
    /// original method, or another Harmony prefix, rejects the tile first.
    /// </summary>
    [HarmonyPatch(typeof(TileFinder), nameof(TileFinder.IsValidTileForNewSettlement))]
    public static class SharedTileSettlementPatch
    {
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref bool __result, PlanetTile tile, StringBuilder reason, bool forGravship)
        {
            try
            {
                // If vanilla already accepts it, there is nothing for Rimjob to do.
                if (__result) return;

                // The shared-colony special case is for a joining player's new
                // colony. Do NOT require ProgramState.Playing: this page is shown
                // before the game enters the Playing state.
                if (forGravship ||
                    Scribe.mode != LoadSaveMode.Inactive ||
                    Find.GameInitData == null ||
                    Find.World == null ||
                    Find.WorldGrid == null ||
                    Find.WorldObjects == null ||
                    Find.WorldInterface == null)
                {
                    return;
                }

                int tileId = tile.tileId;
                if (tileId < 0 || tileId >= Find.WorldGrid.TilesCount)
                {
                    return;
                }

                // Never globally legalise occupied tiles. The joining player must
                // have explicitly selected this exact tile or the RTSettlement on it.
                if (!SharedTileSelectionUtility.IsExplicitlySelectedTile(tile))
                {
                    return;
                }

                var objectsAtTile = Find.WorldObjects.AllWorldObjects
                    .Where(worldObject => worldObject != null && worldObject.Tile == tile)
                    .ToList();

                var sharedSettlements = objectsAtTile
                    .Where(SharedTileSelectionUtility.IsRemotePlayerSettlement)
                    .ToList();

                // Not a Rimjob player settlement tile: preserve vanilla rejection.
                if (sharedSettlements.Count == 0)
                {
                    return;
                }

                // Do not make quest sites, NPC settlements, camps, etc. shareable.
                if (objectsAtTile.Any(worldObject => !SharedTileSelectionUtility.IsRemotePlayerSettlement(worldObject)))
                {
                    return;
                }

                int capacity = Math.Max(1, SharedColonyState.TileCapacity);
                if (sharedSettlements.Count >= capacity)
                {
                    SetReason(reason, $"Rimjob shared tile is full ({sharedSettlements.Count}/{capacity} settlements)." );
                    __result = false;
                    return;
                }

                // Preserve the important vanilla terrain restrictions while only
                // overriding the occupancy/adjacency restriction for this shared tile.
                var worldTile = Find.WorldGrid[tileId];
                var biome = worldTile.PrimaryBiome;
                if (worldTile.WaterCovered ||
                    biome == null ||
                    !biome.canBuildBase ||
                    !biome.implemented ||
                    worldTile.hilliness == Hilliness.Impassable)
                {
                    return;
                }

                __result = true;
                SetReason(reason, $"Rimjob shared tile ({sharedSettlements.Count}/{capacity} occupied)." );
                Log.Message($"[Rimjob] Overrode vanilla occupied-tile rejection for shared first settlement on tile {tileId} ({sharedSettlements.Count}/{capacity}).");
            }
            catch (Exception exception)
            {
                // Fail closed: leave vanilla's rejection intact if our special-case
                // validation cannot prove this is a legitimate Rimjob shared tile.
                Log.Warning($"[Rimjob] Shared starting-tile validation failed: {exception}");
            }
        }

        private static void SetReason(StringBuilder reason, string value)
        {
            if (reason == null) return;
            reason.Clear();
            reason.Append(value);
        }
    }

    internal static class SharedTileSelectionUtility
    {
        public static bool IsRemotePlayerSettlement(WorldObject worldObject)
        {
            return worldObject?.def?.defName == "RTSettlement";
        }

        public static bool IsExplicitlySelectedTile(PlanetTile tile)
        {
            if (!tile.Valid || Find.WorldInterface == null) return false;

            PlanetTile selectedTile = Find.WorldInterface.SelectedTile;
            if (selectedTile.Valid && selectedTile == tile)
            {
                return true;
            }

            WorldObject selectedObject = Find.WorldSelector?.FirstSelectedObject;
            return IsRemotePlayerSettlement(selectedObject) &&
                   selectedObject.Tile.Valid &&
                   selectedObject.Tile == tile;
        }

        /// <summary>
        /// Clicking an occupied RTSettlement selects the WorldObject rather than
        /// necessarily setting WorldInterface.SelectedTile. Page_SelectStartingSite
        /// validates SelectedTile, so promote the selected settlement's tile first.
        /// </summary>
        public static void PromoteSelectedSharedTile()
        {
            try
            {
                if (Find.WorldInterface == null || Find.WorldSelector == null) return;

                WorldObject selectedObject = Find.WorldSelector.FirstSelectedObject;
                if (!IsRemotePlayerSettlement(selectedObject)) return;
                if (!selectedObject.Tile.Valid) return;

                if (!Find.WorldInterface.SelectedTile.Valid ||
                    Find.WorldInterface.SelectedTile != selectedObject.Tile)
                {
                    Find.WorldInterface.SelectedTile = selectedObject.Tile;
                }

                if (Find.GameInitData != null)
                {
                    Find.GameInitData.startingTile = selectedObject.Tile;
                }

                Log.Message($"[Rimjob] Selected occupied player tile {selectedObject.Tile.tileId} for shared first settlement.");
            }
            catch (Exception exception)
            {
                Log.Warning($"[Rimjob] Could not promote selected shared tile: {exception}");
            }
        }
    }

    [HarmonyPatch(typeof(Page_SelectStartingSite), "CanDoNext")]
    public static class SharedTileStartingSiteCanNextPatch
    {
        [HarmonyPriority(Priority.First)]
        public static void Prefix()
        {
            SharedTileSelectionUtility.PromoteSelectedSharedTile();
        }
    }

    [HarmonyPatch(typeof(Page_SelectStartingSite), "DoNext")]
    public static class SharedTileStartingSiteDoNextPatch
    {
        [HarmonyPriority(Priority.First)]
        public static void Prefix()
        {
            SharedTileSelectionUtility.PromoteSelectedSharedTile();
        }
    }
}
