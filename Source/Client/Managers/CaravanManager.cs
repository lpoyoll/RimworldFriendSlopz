using GameClient.Managers;
using GameClient.Misc;
using GameClient.TCP;
using GameClient.Values;
using GameClient.WorldObjects;
using RimWorld;
using RimWorld.Planet;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;
using static Shared.CommonEnumerators;

namespace GameClient.Managers
{
    [RTManager]
    public static class CaravanManager
    {
        //Variables

        public static WorldObjectDef onlineCaravanDef;

        public static List<Caravan> playerCaravans = new List<Caravan>();

        public static List<CaravanFile> guestCaravans = new List<CaravanFile>();

        public static void ParsePacket(Packet packet)
        {
            CaravanData data = Serializer.ConvertBytesToObject<CaravanData>(packet.contents);

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

        public static void AddCaravan(CaravanFile file)
        {
            if (!ClientValues.isReadyToPlay) return;
            if (file.UID == ClientValues.uid) return;

            try
            {
                if (CaravanManagerH.GetExistingCaravanFromFile(file) != null)
                {
                    Printer.Warning("Caravan to add already existed", LogImportanceMode.Verbose);
                }

                else
                {
                    guestCaravans.Add(file);

                    OnlineCaravan onlineCaravan = (OnlineCaravan)WorldObjectMaker.MakeWorldObject(onlineCaravanDef);
                    onlineCaravan.Tile = file.Tile;
                    onlineCaravan.SetFaction(FactionValues.neutralPlayer);
                    Find.World.worldObjects.AllWorldObjects.Add(onlineCaravan);

                    Printer.Warning("Added");
                }
            }
            catch (Exception e) { Printer.Error(e); }
        }

        private static void RemoveCaravan(CaravanFile file)
        {
            if (!ClientValues.isReadyToPlay) return;
            if (file.UID == ClientValues.uid) return;

            try
            {
                CaravanFile toFind = CaravanManagerH.GetExistingCaravanFromFile(file);
                if (toFind == null) Printer.Warning("Caravan to remove wasn't found", LogImportanceMode.Verbose);
                else
                {
                    OnlineCaravan toRemove = CaravanManagerH.GetAllExistingOnlineCaravans()
                        .FirstOrDefault(fetch => fetch.Tile == toFind.Tile);

                    if (toRemove != null)
                    {
                        Find.World.worldObjects.AllWorldObjects.Remove(toRemove);
                        guestCaravans.Remove(toFind);
                        Printer.Warning("Removed");
                    }
                }
            }
            catch (Exception e) { Printer.Error(e); }
        }

        private static void MoveCaravan(CaravanFile file)
        {
            if (!ClientValues.isReadyToPlay) return;
            if (file.UID == ClientValues.uid) return;

            try
            {
                CaravanFile toFind = CaravanManagerH.GetExistingCaravanFromFile(file);
                if (toFind == null) AddCaravan(file);
                else
                {
                    OnlineCaravan onlineCaravan = CaravanManagerH.GetAllExistingOnlineCaravans()
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
            playerCaravans.Add(caravan);

            CaravanData data = new CaravanData();
            data._stepMode = CaravanStepMode.Add;
            data._caravanFile = new CaravanFile();
            data._caravanFile.Tile = caravan.Tile;
            data._caravanFile.UID = ClientValues.uid;
            data._caravanFile.ID = caravan.ID;

            Packet packet = Packet.CreatePacketFromObject(nameof(CaravanManager), data);
            Network.listener.EnqueuePacket(packet);
        }

        public static void RequestCaravanRemove(Caravan caravan)
        {
            CaravanData data = new CaravanData();
            data._stepMode = CaravanStepMode.Remove;
            data._caravanFile = new CaravanFile();
            data._caravanFile.Tile = caravan.Tile;
            data._caravanFile.UID = ClientValues.uid;
            data._caravanFile.ID = caravan.ID;

            playerCaravans.Remove(caravan);

            Packet packet = Packet.CreatePacketFromObject(nameof(CaravanManager), data);
            Network.listener.EnqueuePacket(packet);
        }

        public static void RequestCaravanUpdate(Caravan caravan)
        {
            CaravanData data = new CaravanData();
            data._stepMode = CaravanStepMode.Move;
            data._caravanFile = new CaravanFile();
            data._caravanFile.Tile = caravan.Tile;
            data._caravanFile.UID = ClientValues.uid;
            data._caravanFile.ID = caravan.ID;

            Packet packet = Packet.CreatePacketFromObject(nameof(CaravanManager), data);
            Network.listener.EnqueuePacket(packet);
        }

        public static void ClearAllCaravans()
        {
            guestCaravans.Clear();
            playerCaravans.Clear();

            foreach (WorldObject worldObject in CaravanManagerH.GetAllExistingOnlineCaravans())
            {
                Find.World.worldObjects.Remove(worldObject);
            }
        }
    }
}

public static class CaravanManagerH
{
    public static OnlineCaravan[] GetAllExistingOnlineCaravans()
    {
        List<OnlineCaravan> onlineCaravans = new List<OnlineCaravan>();
        foreach (WorldObject wo in Find.World.worldObjects.AllWorldObjects)
        {
            if (wo.def == CaravanManager.onlineCaravanDef) onlineCaravans.Add((OnlineCaravan)wo);
        }

        return onlineCaravans.ToArray();
    }

    public static CaravanFile GetExistingCaravanFromFile(CaravanFile file)
    {
        return CaravanManager.guestCaravans.FirstOrDefault(fetch => fetch.UID == file.UID 
            && fetch.ID == file.ID);
    }

    public static void SetCaravanDef()
    {
        CaravanManager.onlineCaravanDef = DefDatabase<WorldObjectDef>.AllDefs.First(fetch => fetch.defName == "RTCaravan");
    }

    public static void SetAllPlayerCaravans()
    {
        Caravan[] playerCaravans = Find.World.worldObjects.Caravans.Where(fetch => fetch.Faction == Faction.OfPlayer).ToArray();
        foreach (Caravan caravan in playerCaravans) CaravanManager.playerCaravans.Add(caravan);
    }
}
