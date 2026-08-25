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
    /// v0.1.9 patched TileFinder but did not promote a clicked world object into
    /// WorldInterface.SelectedTile.
    /// v0.1.10 added that promotion, but incorrectly required ProgramState.Playing.
    /// The starting-site page runs before ProgramState.Playing, so the special
    /// validation never executed and vanilla returned "This tile is occupied".
    /// v0.1.11 scopes the bypass to the new-colony setup state and an explicitly
    /// selected RTSettlement tile instead.
    /// </summary>
    [HarmonyPatch(typeof(TileFinder), nameof(TileFinder.IsValidTileForNewSettlement))]
    public static class SharedTileSettlementPatch
    {
        public static bool Prefix(ref bool __result, PlanetTile tile, StringBuilder reason, bool forGravship)
        {
            try
            {
                // Shared-colony starting sites are only relevant while a new game
                // is being configured. Do NOT require ProgramState.Playing here:
                // Page_SelectStartingSite is shown before the game enters Playing.
                if (forGravship ||
                    Scribe.mode != LoadSaveMode.Inactive ||
                    Find.GameInitData == null ||
                    Find.World == null ||
                    Find.WorldGrid == null ||
                    Find.WorldObjects == null ||
                    Find.WorldInterface == null)
                {
                    return true;
                }

                int tileId = tile.tileId;
                if (tileId < 0 || tileId >= Find.WorldGrid.TilesCount)
                {
                    return true;
                }

                // Never globally legalise occupied tiles. The joining player must
                // have explicitly selected this tile or the RTSettlement on it.
                if (!SharedTileSelectionUtility.IsExplicitlySelectedTile(tile))
                {
                    return true;
                }

                var objectsAtTile = Find.WorldObjects.AllWorldObjects
                    .Where(worldObject => worldObject != null && worldObject.Tile == tile)
                    .ToList();

                var sharedSettlements = objectsAtTile
                    .Where(SharedTileSelectionUtility.IsRemotePlayerSettlement)
                    .ToList();

                // Not a Rimjob player-settlement tile: preserve vanilla rules.
                if (sharedSettlements.Count == 0)
                {
                    return true;
                }

                // Do not make arbitrary occupied content legal. A shared starting
                // tile may contain RTSettlement objects only.
                if (objectsAtTile.Any(worldObject => !SharedTileSelectionUtility.IsRemotePlayerSettlement(worldObject)))
                {
                    return true;
                }

                int capacity = Math.Max(1, SharedColonyState.TileCapacity);
                if (sharedSettlements.Count >= capacity)
                {
                    SetReason(reason, $"Rimjob shared tile is full ({sharedSettlements.Count}/{capacity} settlements)." );
                    __result = false;
                    return false;
                }

                // Preserve the core terrain restrictions from vanilla.
                var worldTile = Find.WorldGrid[tileId];
                var biome = worldTile.PrimaryBiome;
                if (worldTile.WaterCovered ||
                    biome == null ||
                    !biome.canBuildBase ||
                    !biome.implemented ||
                    worldTile.hilliness == Hilliness.Impassable)
                {
                    return true;
                }

                SetReason(reason, $"Rimjob shared tile ({sharedSettlements.Count}/{capacity} occupied)." );
                __result = true;
                Log.Message($"[Rimjob] Allowing first settlement on occupied shared tile {tileId} ({sharedSettlements.Count}/{capacity}).");
                return false;
            }
            catch (Exception exception)
            {
                // Fail closed. If the special-case logic cannot prove this is an
                // eligible Rimjob shared tile, let RimWorld use its normal rules.
                Log.Warning($"[Rimjob] Shared starting-tile validation failed: {exception}");
                return true;
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
        public static void Prefix()
        {
            SharedTileSelectionUtility.PromoteSelectedSharedTile();
        }
    }

    [HarmonyPatch(typeof(Page_SelectStartingSite), "DoNext")]
    public static class SharedTileStartingSiteDoNextPatch
    {
        public static void Prefix()
        {
            SharedTileSelectionUtility.PromoteSelectedSharedTile();
        }
    }
}
