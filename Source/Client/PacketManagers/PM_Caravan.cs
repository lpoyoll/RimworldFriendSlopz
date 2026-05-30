using GameClient.Defs;
using GameClient.PacketManagers;
using GameClient.WorldObjects;
using RimWorld;
using RimWorld.Planet;
using RTShared;
using RTShared.Files;
using RTShared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using RTNetwork;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using Verse;
using static RTShared.Misc.Printer;
using static RTNetwork.Packets.PKT_Caravan;
using RTNetwork.Components;
using GameClient.Managers;

namespace GameClient.PacketManagers
{
    public class PM_Caravan : PM_Base
    {
        public static List<Caravan> PlayerCaravans { get; private set; } = new List<Caravan>();

        public static List<FL_Caravan> GuestCaravans { get; private set; } = new List<FL_Caravan>();

        [HandlesPacket(PacketHeader.Caravan)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Caravan data = Serializer.ConvertBytesToObject<PKT_Caravan>(bytes);

            switch (data._stepMode)
            {
                case CaravanStepMode.Add:
                    AddCaravan(data._caravanFile);
                    break;

                case CaravanStepMode.Remove:
                    RemoveCaravan(data._caravanFile);
                    break;

                case CaravanStepMode.Move:
                    MoveCaravan(data._caravanFile);
                    break;
            }
        }

        public static void AddCaravan(FL_Caravan file)
        {
            try
            {
                if (GetExistingCaravanFromFile(file) != null)
                {
                    Printer.Warning("Caravan to add already existed", Verbosity.Verbose);
                }

                else
                {
                    GuestCaravans.Add(file);

                    WO_Caravan onlineCaravan = (WO_Caravan)WorldObjectMaker.MakeWorldObject(RTWorldObjectDefOf.RTCaravan);
                    onlineCaravan.Tile = file.Tile;
                    onlineCaravan.SetFaction(SessionManager.NeutralFaction);
                    Find.World.worldObjects.AllWorldObjects.Add(onlineCaravan);
                }
            }
            catch (Exception e) { Printer.Error(e); }
        }

        private static void RemoveCaravan(FL_Caravan file)
        {
            try
            {
                FL_Caravan toFind = GetExistingCaravanFromFile(file);
                if (toFind == null) Printer.Warning("Caravan to remove wasn't found", Verbosity.Verbose);
                else
                {
                    WO_Caravan toRemove = GetAllExistingOnlineCaravans()
                        .FirstOrDefault(fetch => fetch.Tile == toFind.Tile);

                    if (toRemove != null)
                    {
                        Find.World.worldObjects.AllWorldObjects.Remove(toRemove);
                        GuestCaravans.Remove(toFind);
                    }
                }
            }
            catch (Exception e) { Printer.Error(e); }
        }

        private static void MoveCaravan(FL_Caravan file)
        {
            try
            {
                FL_Caravan toFind = GetExistingCaravanFromFile(file);
                if (toFind == null) AddCaravan(file);
                else
                {
                    WO_Caravan onlineCaravan = GetAllExistingOnlineCaravans()
                        .FirstOrDefault(fetch => fetch.Tile == toFind.Tile);

                    if (onlineCaravan != null)
                    {
                        onlineCaravan.Tile = file.Tile;
                        toFind.Tile = file.Tile;
                    }
                }
            }
            catch (Exception e) { Printer.Error(e); }
        }

        public static void RequestCaravanAdd(Caravan caravan)
        {
            PlayerCaravans.Add(caravan);

            PKT_Caravan data = new PKT_Caravan();
            data._stepMode = CaravanStepMode.Add;
            data._caravanFile = new FL_Caravan();
            data._caravanFile.Tile = caravan.Tile;
            data._caravanFile.Username = SessionManager.Username;
            data._caravanFile.ID = caravan.ID;

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Caravan, data);
        }

        public static void RequestCaravanRemove(Caravan caravan)
        {
            PKT_Caravan data = new PKT_Caravan();
            data._stepMode = CaravanStepMode.Remove;
            data._caravanFile = new FL_Caravan();
            data._caravanFile.Tile = caravan.Tile;
            data._caravanFile.Username = SessionManager.Username;
            data._caravanFile.ID = caravan.ID;

            PlayerCaravans.Remove(caravan);

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Caravan, data);
        }

        public static void RequestCaravanUpdate(Caravan caravan)
        {
            PKT_Caravan data = new PKT_Caravan();
            data._stepMode = CaravanStepMode.Move;
            data._caravanFile = new FL_Caravan();
            data._caravanFile.Tile = caravan.Tile;
            data._caravanFile.Username = SessionManager.Username;
            data._caravanFile.ID = caravan.ID;

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Caravan, data);
        }

        [OnSessionEnd]
        private static void CleanValues()
        {
            GuestCaravans.Clear();
            PlayerCaravans.Clear();

            foreach (WorldObject worldObject in GetAllExistingOnlineCaravans())
            {
                Find.World.worldObjects.Remove(worldObject);
            }
        }

        public static WO_Caravan[] GetAllExistingOnlineCaravans()
        {
            List<WO_Caravan> onlineCaravans = new List<WO_Caravan>();
            foreach (WorldObject wo in Find.World.worldObjects.AllWorldObjects)
            {
                if (wo.def == RTWorldObjectDefOf.RTCaravan) onlineCaravans.Add((WO_Caravan)wo);
            }

            return onlineCaravans.ToArray();
        }

        public static FL_Caravan GetExistingCaravanFromFile(FL_Caravan file)
        {
            return PM_Caravan.GuestCaravans.FirstOrDefault(fetch => fetch.Username == file.Username
                && fetch.ID == file.ID);
        }

        public static void AddCaravans()
        {
            Caravan[] playerCaravans = Find.World.worldObjects.Caravans.Where(fetch => fetch.Faction == Faction.OfPlayer).ToArray();
            foreach (Caravan caravan in playerCaravans) PM_Caravan.PlayerCaravans.Add(caravan);
        }
    }
}
