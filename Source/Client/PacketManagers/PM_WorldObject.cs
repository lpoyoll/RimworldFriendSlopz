using GameClient.Defs;
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
using System.Text;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using UnityEngine.Tilemaps;
using Verse;

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
                    AddWorldObject(packet);
                    break;

                case PKT_WorldObject.StepMode.Remove:
                    RemoveWorldObject(packet);
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

        private static void AddWorldObject(PKT_WorldObject packet)
        {
            SetBypass(true);

            try
            {
                if (packet.WorldObject.MainPartDef == string.Empty)
                {
                    Faction faction = Find.World.factionManager.AllFactions.FirstOrDefault(fetch => fetch.Name == packet.WorldObject.FactionDef);
                    if (faction == null) faction = SessionHandler.NeutralFaction;

                    Settlement settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                    settlement.Name = packet.WorldObject.Name;
                    settlement.Tile = packet.WorldObject.Tile;
                    settlement.SetFaction(faction);

                    Find.WorldObjects.Add(settlement);
                }

                else
                {
                    Faction faction = Find.World.factionManager.AllFactions.FirstOrDefault(fetch => fetch.Name == packet.WorldObject.FactionDef);
                    if (faction == null) faction = SessionHandler.NeutralFaction;

                    SitePartDef partDef = DefDatabase<SitePartDef>.AllDefs.First(fetch => fetch.defName == packet.WorldObject.MainPartDef);
                    //Site site = SiteMaker.MakeSite(partDef, packet.WorldObject.Tile, faction, threatPoints: packet.WorldObject.Points);
                    Site site = SiteMaker.MakeSite(partDef, packet.WorldObject.Tile, faction);

                    Find.WorldObjects.Add(site);
                }
            }
            catch (Exception ex) { Printer.Error(ex, Printer.Verbosity.Verbose); }

            SetBypass(false);
        }

        private static void RemoveWorldObject(PKT_WorldObject packet)
        {
            SetBypass(true);

            try
            {
                WorldObject toFind = Find.World.worldObjects.AllWorldObjects.FirstOrDefault(fetch => fetch.Tile == packet.WorldObject.Tile);
                if (toFind != null) Find.WorldObjects.Remove(toFind);
            }
            catch (Exception ex) { Printer.Error(ex, Printer.Verbosity.Verbose); }

            SetBypass(false);
        }

        public static void SetBypass(bool mode) 
        {
            if (IsBypass && mode) Printer.Error("Bypass shouldn't be enabled!", Printer.Verbosity.Verbose);
            else if (!IsBypass && !mode) Printer.Error("Bypass shouldn't be disabled!", Printer.Verbosity.Verbose);
            else IsBypass = mode; 
        }
    }
}
