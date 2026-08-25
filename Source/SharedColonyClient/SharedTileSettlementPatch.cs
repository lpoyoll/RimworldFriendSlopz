using System;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace RWTSharedColony
{
    /// <summary>
    /// RimWorld normally refuses to use a world tile that already contains a
    /// world object. For Rimjob, an RTSettlement is deliberately shareable:
    /// several independently owned player settlements may occupy the same
    /// server tile up to the configured Shared Colony capacity.
    ///
    /// This patch is intentionally narrow. It only bypasses vanilla starting
    /// site validation when the exact tile contains RTSettlement objects and
    /// no other world object type, the biome is buildable, and capacity is
    /// still available. NPC settlements, quest sites and other occupied tiles
    /// remain under vanilla rules.
    /// </summary>
    [HarmonyPatch(typeof(TileFinder), nameof(TileFinder.IsValidTileForNewSettlement))]
    public static class SharedTileSettlementPatch
    {
        public static bool Prefix(ref bool __result, PlanetTile tile, StringBuilder reason, bool forGravship)
        {
            try
            {
                // Never interfere with gravship landing checks or game/load caches.
                if (forGravship ||
                    Scribe.mode != LoadSaveMode.Inactive ||
                    Current.ProgramState != ProgramState.Playing ||
                    Current.Game == null ||
                    Find.World == null ||
                    Find.WorldGrid == null ||
                    Find.WorldObjects == null)
                {
                    return true;
                }

                int tileId = tile.tileId;
                if (tileId < 0 || tileId >= Find.WorldGrid.TilesCount)
                {
                    return true;
                }

                var objectsAtTile = Find.WorldObjects.AllWorldObjects
                    .Where(worldObject => worldObject != null && worldObject.Tile == tile)
                    .ToList();

                var sharedSettlements = objectsAtTile
                    .Where(IsRemotePlayerSettlement)
                    .ToList();

                // Not a Rimjob shared-colony tile: use ordinary RimWorld rules.
                if (sharedSettlements.Count == 0)
                {
                    return true;
                }

                // Do not make arbitrary occupied tiles legal. If anything other
                // than RTSettlement exists on this exact tile, vanilla decides.
                if (objectsAtTile.Any(worldObject => !IsRemotePlayerSettlement(worldObject)))
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

                // Keep the important vanilla terrain rule even though we are
                // deliberately bypassing the occupied-world-object rule.
                var worldTile = Find.WorldGrid[tileId];
                var biome = worldTile.PrimaryBiome;
                if (worldTile.WaterCovered || biome == null || !biome.canBuildBase)
                {
                    return true;
                }

                SetReason(reason, $"Rimjob shared tile ({sharedSettlements.Count}/{capacity} occupied)." );
                __result = true;
                return false;
            }
            catch (Exception exception)
            {
                // Fail closed: if our special-case logic cannot prove this is a
                // safe shared tile, fall back to RimWorld's original validator.
                Log.Warning($"[RWT Shared Colony] shared starting-tile check failed: {exception.Message}");
                return true;
            }
        }

        private static bool IsRemotePlayerSettlement(WorldObject worldObject)
        {
            return worldObject?.def?.defName == "RTSettlement";
        }

        private static void SetReason(StringBuilder reason, string value)
        {
            if (reason == null) return;
            reason.Clear();
            reason.Append(value);
        }
    }
}
