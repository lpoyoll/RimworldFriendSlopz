using GameClient.Managers;
using GameClient.Patches;
using RimWorld;
using RimWorld.Planet;
using Shared;
using Shared.Details.Planet;
using Shared.Misc;
using System;
using System.Collections.Generic;
using TCPNetwork;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using Verse;
namespace GameClient.PacketManagers
{
    public class PM_Pollution : PM_Base
    {
        [HandlesPacket(PacketHeader.Pollution)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            if (ModsConfig.BiotechActive)
            {
                PKT_Pollution data = Serializer.ConvertBytesToObject<PKT_Pollution>(bytes);

                AddPollutedTileOrganic(data._pollutionData);
            }
        }

        public static void AddPollutedTiles(List<PollutionDetail> details, bool forceRefresh)
        {
            if (details == null) return;

            foreach (PollutionDetail detail in details)
            {
                AddPollutedTileSimple(detail, forceRefresh);
            }

            //If we don't want to force refresh we wait for all and then refresh the layer
            if (!forceRefresh) ForcePollutionLayerRefresh();
        }

        public static void AddPollutedTileOrganic(PollutionDetail details)
        {
            PatchAddPollution.addedByServer = true;
            WorldPollutionUtility.PolluteWorldAtTile(details.Tile, details.Quantity);
        }

        public static void AddPollutedTileSimple(PollutionDetail details, bool forceRefresh)
        {
            if (!RimworldManager.CheckIfTileIsValid(details.Tile)) return;
            else
            {
                SurfaceTile toPollute = Find.WorldGrid[details.Tile];
                toPollute.pollution = details.Quantity;

                if (forceRefresh) ForcePollutionLayerRefresh();
            }
        }

        public static void ClearAllPollution()
        {
            PlanetLayer layer = Find.World.grid.FirstLayerOfDef(PlanetLayerDefOf.Surface);

            foreach (SurfaceTile tile in layer.Tiles)
            {
                if (tile.pollution != 0) tile.pollution = 0;
            }

            ForcePollutionLayerRefresh();
        }

        public static List<PollutionDetail> GetPlanetPollutedTiles()
        {
            List<PollutionDetail> toGet = new List<PollutionDetail>();
            foreach (SurfaceTile tile in Find.WorldGrid.Tiles)
            {
                if (tile.pollution != 0)
                {
                    PollutionDetail details = new PollutionDetail();
                    details.Tile = tile.tile;
                    details.Quantity = tile.pollution;

                    toGet.Add(details);
                }
            }

            return toGet;
        }

        public static void ForcePollutionLayerRefresh()
        {
            PlanetLayer toRefresh = Find.World.grid.FirstLayerOfDef(PlanetLayerDefOf.Surface);
            Find.World.renderer.SetDirty<WorldDrawLayer>(toRefresh);
        }
    }
}
