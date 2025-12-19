using System;
using System.Collections.Generic;
using System.Linq;
using GameClient.Defs;
using GameClient.Misc;
using GameClient.WorldObjects;
using RimWorld;
using RimWorld.Planet;
using Shared;
using Shared.Files;
using TCPNetwork.Packets;
using Verse;

namespace GameClient.Managers
{
    public static class CaravanManager
    {
        public static List<CaravanFile> GuestCaravans { get; private set; } = new List<CaravanFile>();

        [HandlesPacket(PacketHeader.CaravanManager)]
        private static void ParsePacket(byte[] bytes)
        {
            CaravanData data = Serializer.ConvertBytesToObject<CaravanData>(bytes);

            switch (data._stepMode)
            {
                case CommonEnumerators.CaravanStepMode.Add:
                    AddCaravan(data._caravanFile);
                    break;

                case CommonEnumerators.CaravanStepMode.Remove:
                    RemoveCaravan(data._caravanFile);
                    break;

                case CommonEnumerators.CaravanStepMode.Move:
                    MoveCaravan(data._caravanFile);
                    break;
                default:
                    Printer.Error($"Received invalid step mode {data._stepMode}");
                    return;
            }
        }

        public static void AddCaravan(CaravanFile file)
        {
            try
            {
                if (CaravanManagerH.GetExistingCaravanFromFile(file) != null)
                {
                    Printer.Warning("Caravan to add already existed", CommonEnumerators.LogImportanceMode.Verbose);
                }

                else
                {
                    GuestCaravans.Add(file);

                    RTCaravan onlineCaravan = (RTCaravan)WorldObjectMaker.MakeWorldObject(RTWorldObjectDefOf.RTCaravan);
                    onlineCaravan.Tile = file.Tile;
                    onlineCaravan.SetFaction(SessionHandler.NeutralFaction);
                    Find.World.worldObjects.AllWorldObjects.Add(onlineCaravan);
                }
            }
            catch (Exception e) { Printer.Error(e); }
        }

        private static void RemoveCaravan(CaravanFile file)
        {
            try
            {
                CaravanFile toFind = CaravanManagerH.GetExistingCaravanFromFile(file);
                if (toFind == null) Printer.Warning("Caravan to remove wasn't found", CommonEnumerators.LogImportanceMode.Verbose);
                else
                {
                    RTCaravan toRemove = CaravanManagerH.GetAllExistingOnlineCaravans()
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

        private static void MoveCaravan(CaravanFile file)
        {
            Printer.Warning("Moving caravan");
            try
            {
                CaravanFile toFind = CaravanManagerH.GetExistingCaravanFromFile(file);
                if (toFind == null)
                {
                    AddCaravan(file);
                    Printer.Warning("Adding caravan");
                }
                else
                {
                    RTCaravan onlineCaravan = CaravanManagerH.GetAllExistingOnlineCaravans()
                        .FirstOrDefault(fetch => fetch.Tile == toFind.Tile);
                    Printer.Warning(onlineCaravan);
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
            CaravanData data = new CaravanData();
            data._stepMode = CommonEnumerators.CaravanStepMode.Add;
            data._caravanFile = new CaravanFile();
            data._caravanFile.Tile = caravan.Tile;
            data._caravanFile.Username = SessionHandler.Username;
            data._caravanFile.ID = caravan.ID;

            ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.CaravanManager, data);
        }

        public static void RequestCaravanRemove(Caravan caravan)
        {
            CaravanData data = new CaravanData();
            data._stepMode = CommonEnumerators.CaravanStepMode.Remove;
            data._caravanFile = new CaravanFile();
            data._caravanFile.Tile = caravan.Tile;
            data._caravanFile.Username = SessionHandler.Username;
            data._caravanFile.ID = caravan.ID;

            ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.CaravanManager, data);
        }

        public static void RequestCaravanUpdate(Caravan caravan)
        {
            CaravanData data = new CaravanData();
            data._stepMode = CommonEnumerators.CaravanStepMode.Move;
            data._caravanFile = new CaravanFile();
            data._caravanFile.Tile = caravan.Tile;
            data._caravanFile.Username = SessionHandler.Username;
            data._caravanFile.ID = caravan.ID;

            ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.CaravanManager, data);
        }

        public static void ClearAllCaravans()
        {
            GuestCaravans.Clear();

            foreach (WorldObject worldObject in CaravanManagerH.GetAllExistingOnlineCaravans())
            {
                Find.World.worldObjects.Remove(worldObject);
            }
        }
    }

    public static class CaravanManagerH
    {
        public static RTCaravan[] GetAllExistingOnlineCaravans()
        {
            List<RTCaravan> onlineCaravans = new List<RTCaravan>();
            foreach (WorldObject wo in Find.World.worldObjects.AllWorldObjects)
            {
                if (wo.def == RTWorldObjectDefOf.RTCaravan) onlineCaravans.Add((RTCaravan)wo);
            }

            return onlineCaravans.ToArray();
        }

        public static CaravanFile GetExistingCaravanFromFile(CaravanFile file)
        {
            return CaravanManager.GuestCaravans.FirstOrDefault(fetch => fetch.Username == file.Username
                                                                        && fetch.ID == file.ID);
        }
    }
}