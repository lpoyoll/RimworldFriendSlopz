using GameClient.Defs;
using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Misc;
using GameClient.PacketManagers;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace GameClient.WorldObjects
{
    public class WO_Site : MapParent
    {
        private Material cachedMat;

        public override string Label => base.Label;

        public SitePartDef MainSitePartDef => RTSitePartDefOf.RTBase;

        public List<RTSitePart> parts = new List<RTSitePart>();

        public override Texture2D ExpandingIcon => MainSitePartDef.ExpandingIconTexture;

        public override Material Material
        {
            get
            {
                if (cachedMat == null)
                {
                    cachedMat = MaterialPool.MatFrom(color: (!MainSitePartDef.applyFactionColorToSiteTexture || base.Faction == null) ? 
                        Color.white : base.Faction.Color, texPath: MainSitePartDef.siteTexture, shader: ShaderDatabase.WorldOverlayTransparentLit, renderQueue: 3550);
                }

                return cachedMat;
            }
        }

        public void AddPart(RTSitePart part)
        {
            if (!part.def.forceMutators.NullOrEmpty())
            {
                foreach (TileMutatorDef forceMutator in part.def.forceMutators)
                {
                    base.Tile.Tile.AddMutator(forceMutator);
                }
            }

            parts.Add(part);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            List<Gizmo> gizmoList = new List<Gizmo>();
            return gizmoList;
        }

        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
        {
            List<Gizmo> gizmoList = new List<Gizmo>();

            Command_Action command_Worker = new Command_Action
            {
                defaultLabel = "Assign worker",
                defaultDesc = "Assigns a worker to work at this site",
                icon = ContentFinder<Texture2D>.Get("Commands/Worker"),
                action = delegate
                {
                    SessionHandler.ChosenCaravan = caravan;
                    SessionHandler.ChosenSite = this;

                    DLG_Base.PushNewDialog(new DLG_Wait());
                    PM_Sites.RequestWorkerInfo();
                }
            };

            Command_Action command_DestroySite = new Command_Action
            {
                defaultLabel = "Destroy site",
                defaultDesc = "Destroy this site",
                icon = ContentFinder<Texture2D>.Get("Commands/Site"),
                action = delegate
                {
                    SessionHandler.ChosenSite = this;
                    PM_Sites.RequestDestroySite();
                }
            };

            if (Faction == Find.FactionManager.OfPlayer || Faction == SessionHandler.GuildFaction) gizmoList.Add(command_Worker);
            if (Faction == Find.FactionManager.OfPlayer || Faction == SessionHandler.GuildFaction) gizmoList.Add(command_DestroySite);

            return gizmoList;
        }
    }
}
