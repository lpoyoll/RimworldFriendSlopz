using GameClient.Defs;
using GameClient.Managers;
using GameClient.WorldObjects;
using RimWorld;
using RimWorld.Planet;
using RTShared;
using RTShared.Files;
using RTShared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using RTNetwork;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using UnityEngine.Tilemaps;
using Verse;
using RTNetwork.Components;

namespace GameClient.PacketManagers
{
    public class PM_WorldObject : PM_Base
    {
        public static bool IsBypass { get; private set; } = false;

        [HandlesPacket(PacketHeader.WorldObject)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_WorldObject packet = Serializer.ConvertBytesToObject<PKT_WorldObject>(bytes);

            switch(packet.CurrentStepMode)
            {
                case PKT_WorldObject.StepMode.Add:
                    AddWorldObject(packet.WorldObject);
                    break;

                case PKT_WorldObject.StepMode.Remove:
                    RemoveWorldObject(packet.WorldObject);
                    break;
            }
        }

        public static void Send(WorldObject wo, PKT_WorldObject.StepMode mode, PKT_WorldObject.WorldObjectMode type)
        {
            PKT_WorldObject packet = new PKT_WorldObject();
            packet.CurrentStepMode = mode;
            packet.WorldObject.Tile = wo.Tile.tileId;

            if (type == PKT_WorldObject.WorldObjectMode.Settlement)
            {
                Settlement settlement = wo as Settlement;
                packet.WorldObject.Name = settlement.Name;
                packet.WorldObject.FactionDef = settlement.Faction.def.defName;
            }

            else
            {
                Site site = wo as Site;
                packet.WorldObject.Points = site.ActualThreatPoints;
                packet.WorldObject.MainPartDef = site.MainSitePartDef.defName;
                foreach (SitePart part in site.parts) packet.WorldObject.PartDefNames.Add(part.def.defName);
            }

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.WorldObject, packet);
        }

        public static void SendAllWorldObjects()
        {
            List<FL_WorldObject> worldObjects = new List<FL_WorldObject>();
            foreach (WorldObject wo in Find.World.worldObjects.AllWorldObjects)
            {
                try
                {
                    if (wo.Faction == Faction.OfPlayer) continue;
                    else
                    {
                        FL_WorldObject file = new FL_WorldObject();
                        file.Tile = wo.Tile.tileId;

                        if (wo.def == WorldObjectDefOf.Settlement)
                        {
                            Settlement settlement = wo as Settlement;
                            file.Name = settlement.Name;
                            file.FactionDef = settlement.Faction.def.defName;
                        }

                        else
                        {
                            Site site = wo as Site;
                            file.Points = site.ActualThreatPoints;
                            file.MainPartDef = site.MainSitePartDef.defName;
                            foreach (SitePart part in site.parts) file.PartDefNames.Add(part.def.defName);
                        }

                        worldObjects.Add(file);
                    }
                }
                catch (Exception ex) { Printer.Warning(ex); }
            }

            PKT_WorldObject packet = new PKT_WorldObject();
            packet.CurrentStepMode = PKT_WorldObject.StepMode.Bulk;
            packet.Bulk = worldObjects;

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.WorldObject, packet);
        }

        private static void AddWorldObject(FL_WorldObject wo)
        {
            if (SessionManager.IsGeneratingFreshWorld) return;
            else
            {
                SetBypass(true);

                try
                {
                    if (wo.MainPartDef == string.Empty)
                    {
                        Faction faction = Find.World.factionManager.AllFactions.FirstOrDefault(fetch => fetch.def.defName == wo.FactionDef);
                        if (faction == Faction.OfPlayer || faction == null) faction = SessionManager.NeutralFaction;

                        Settlement settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                        settlement.Name = wo.Name;
                        settlement.Tile = wo.Tile;
                        settlement.SetFaction(faction);

                        Find.WorldObjects.Add(settlement);
                    }

                    else
                    {
                        Faction faction = Find.World.factionManager.AllFactions.FirstOrDefault(fetch => fetch.def.defName == wo.FactionDef);
                        if (faction == Faction.OfPlayer || faction == null) faction = SessionManager.NeutralFaction;

                        SitePartDef partDef = DefDatabase<SitePartDef>.AllDefs.First(fetch => fetch.defName == wo.MainPartDef);
                        Site site = SiteMaker.MakeSite(partDef, wo.Tile, faction, threatPoints: wo.Points);

                        Find.WorldObjects.Add(site);
                    }
                }
                catch (Exception ex) { Printer.Error(ex, Printer.Verbosity.Verbose); }

                SetBypass(false);
            }
        }

        private static void RemoveWorldObject(FL_WorldObject wo)
        {
            if (SessionManager.IsGeneratingFreshWorld) return;
            else
            {
                SetBypass(true);

                try
                {
                    WorldObject toFind = Find.World.worldObjects.AllWorldObjects.FirstOrDefault(fetch => fetch.Tile == wo.Tile);
                    if (toFind != null) Find.WorldObjects.Remove(toFind);
                }
                catch (Exception ex) { Printer.Error(ex, Printer.Verbosity.Verbose); }

                SetBypass(false);
            }
        }

        public static void ClearAllObjects()
        {
            foreach (WorldObject wo in Find.WorldObjects.AllWorldObjects.ToList())
            {
                if (wo.def == WorldObjectDefOf.Settlement && wo.Faction == Faction.OfPlayer) continue;
                else if (wo.def == WorldObjectDefOf.Site && wo.Faction == Faction.OfPlayer) continue;
                else if (wo.def == WorldObjectDefOf.Caravan && wo.Faction == Faction.OfPlayer) continue;
                else Find.WorldObjects.Remove(wo);
            }

            PM_Roads.ClearAllRoads();
            if (ModLister.BiotechInstalled) PM_Pollution.ClearAllPollution();
        }

        public static void AddWorldObjects(List<FL_WorldObject> worldObjects)
        {
            foreach (FL_WorldObject wo in worldObjects) { AddWorldObject(wo); }
        }

        public static void SetBypass(bool mode) 
        {
            if (IsBypass && mode) Printer.Error("Bypass shouldn't be enabled!", Printer.Verbosity.Verbose);
            else if (!IsBypass && !mode) Printer.Error("Bypass shouldn't be disabled!", Printer.Verbosity.Verbose);
            else IsBypass = mode; 
        }
    }
}
