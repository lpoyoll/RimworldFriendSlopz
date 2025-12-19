using GameClient.Core.Configs;
using GameClient.Dialogs;
using GameClient.Misc;
using TCPNetwork.Packets;
using RimWorld;
using RimWorld.Planet;
using Shared;
using Shared.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Verse;
using Verse.Sound;
using static TCPNetwork.Packets.TransferData;

namespace GameClient.Managers;
//Class that handles all the thing transfers between clients in the mod

public static class TransferManager
{
    [HandlesPacket(PacketHeader.TransferManager)]
    private static void ParsePacket(byte[] bytes)
    {
        TransferData data = Serializer.ConvertBytesToObject<TransferData>(bytes);

        switch (data._stepMode)
        {
            case TransferStepMode.TradeRequest:
                ReceiveTransferRequest(data);
                break;

            case TransferStepMode.TradeAccept:
                RT_Dialog_Wait.Instance.Close();
                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("MESSAGE", ["Transfer was a success!"]));
                if (data._transferMode == TransferMode.Pod) LaunchDropPods();
                FinishTransfer(true);
                break;

            case TransferStepMode.TradeReject:
                RT_Dialog_Wait.Instance.Close();
                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", ["Player rejected the trade!"]));
                RecoverTradeItems(TransferLocation.Caravan);
                break;

            case TransferStepMode.TradeReRequest:
                RT_Dialog_Wait.Instance.Close();
                ReceiveReboundRequest(data);
                break;

            case TransferStepMode.TradeReAccept:
                RT_Dialog_Wait.Instance.Close();
                GetTransferedItemsToSettlement(TransferManagerHelper.GetAllTransferredItems(SessionHandler.IncomingManifest));
                break;

            case TransferStepMode.TradeReReject:
                RT_Dialog_Wait.Instance.Close();
                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", ["Player rejected the trade!"]));
                RecoverTradeItems(TransferLocation.Settlement);
                break;

            case TransferStepMode.Recover:
                RT_Dialog_Wait.Instance.Close();
                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", ["Player is not currently available!"]));
                RecoverTradeItems(TransferLocation.Caravan);
                break;
            
            default:
                Printer.Error($"Received invalid step mode {data._stepMode}");
                return;
        }
    }

    public static void TakeTransferItems(TransferLocation transferLocation)
    {
        if (TradeSession.deal.TryExecute(out bool actuallyTraded))
        {
            SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();

            if (transferLocation == TransferLocation.Caravan)
            {
                TradeSession.playerNegotiator.GetCaravan().RecacheInventory();
            }
        }
    }

    public static void TakeTransferItemsFromPods(IEnumerable<IThingHolder> pods)
    {
        SessionHandler.OutgoingManifest._transferMode = TransferMode.Pod;

        foreach (IThingHolder pod in pods)
        {
            try
            {
                ThingOwner directlyHeldThings = pod.GetDirectlyHeldThings();

                for (int i = 0; i < directlyHeldThings.Count(); i++)
                {
                    TransferManagerHelper.AddThingToTransferManifest(directlyHeldThings[i], directlyHeldThings[i].stackCount);
                }
            }
            catch { continue; }
        }
    }

    public static void SendTransferRequestToServer(TransferLocation transferLocation)
    {
        RT_Dialog_Base.PushNewDialog(new RT_Dialog_Wait("Waiting for transfer response"));

        if (transferLocation == TransferLocation.Caravan)
        {
            SessionHandler.ChosenCaravan = TradeSession.playerNegotiator.GetCaravan();

            SessionHandler.OutgoingManifest._stepMode = TransferStepMode.TradeRequest;
            SessionHandler.OutgoingManifest._fromTile = Find.AnyPlayerHomeMap.Tile;
            SessionHandler.OutgoingManifest._toTile = TradeSession.playerNegotiator.Tile;

            ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.TransferManager, SessionHandler.OutgoingManifest);
        }

        else if (transferLocation == TransferLocation.Settlement)
        {
            RT_Dialog_ItemListing.Instance.Close();

            SessionHandler.OutgoingManifest._stepMode = TransferStepMode.TradeReRequest;
            SessionHandler.OutgoingManifest._fromTile = Find.AnyPlayerHomeMap.Tile;
            SessionHandler.OutgoingManifest._toTile = SessionHandler.IncomingManifest._fromTile;

            ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.TransferManager, SessionHandler.OutgoingManifest);
        }

        else if (transferLocation == TransferLocation.Pod)
        {
            SessionHandler.OutgoingManifest._stepMode = TransferStepMode.TradeRequest;
            SessionHandler.OutgoingManifest._fromTile = Find.AnyPlayerHomeMap.Tile;
            SessionHandler.OutgoingManifest._toTile = SessionHandler.ChosenSettlement.Tile;

            ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.TransferManager, SessionHandler.OutgoingManifest);
        }
    }

    public static void RecoverTradeItems(TransferLocation transferLocation)
    {
        try
        {
            Thing[] toRecover = TransferManagerHelper.GetAllTransferredItems(SessionHandler.OutgoingManifest);

            if (transferLocation == TransferLocation.Caravan) GetTransferredItemsToCaravan(toRecover, false);
            else if (transferLocation == TransferLocation.Settlement) GetTransferedItemsToSettlement(toRecover, false);
        }

        catch
        {
            Printer.Warning("Rethrowing transfer items, might be RimWorld's fault");

            Thread.Sleep(100);

            RecoverTradeItems(transferLocation);
        }
    }

    public static void GetTransferedItemsToSettlement(Thing[] things, bool success = true, bool customMap = true, bool invokeMessage = true)
    {
        Action r1 = delegate
        {
            Map map = null;
            if (customMap) map = Find.Maps.Find(x => x.Tile == SessionHandler.IncomingManifest._toTile);
            else map = Find.AnyPlayerHomeMap;

            foreach (Thing thing in things)
            {
                if (thing.def.CanHaveFaction) thing.SetFactionDirect(Faction.OfPlayer);
                RimworldManager.PlaceThingIntoMap(thing, map, ThingPlaceMode.Near, true, success);
            }

            FinishTransfer(success);
        };

        if (invokeMessage)
        {
            if (success) RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("MESSAGE", ["Transfer was a success!"], r1));
            else RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", ["Transfer was cancelled!"], r1));
        }
        else r1.Invoke();
    }
    public static void GetTransferredItemsToCaravan(Thing[] things, bool success = true, bool invokeMessage = true)
    {
        Action r1 = delegate
        {
            foreach (Thing thing in things) RimworldManager.PlaceThingIntoCaravan(thing, SessionHandler.ChosenCaravan);

            FinishTransfer(success);
        };

        if (invokeMessage)
        {
            if (success) RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", ["Transfer was a success!"], r1));
            else RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", ["Transfer was cancelled!"], r1));
        }
        else r1.Invoke();
    }

    public static void FinishTransfer(bool success)
    {
        SessionHandler.LastTradeStep = CommonEnumerators.TradeMode.None;

        if (success) SaveManager.ForceSave();

        SessionHandler.IncomingManifest = new TransferData();
        SessionHandler.OutgoingManifest = new TransferData();

        SessionHandler.IsInTransfer = false;
    }

    public static void ReceiveTransferRequest(TransferData transferData)
    {
        try
        {
            SessionHandler.IncomingManifest = transferData;

            if (SessionHandler.IsInTransfer || ModConfigGetter.RejectTransfersBool) RejectRequest(transferData._transferMode, false);
            else
            {
                Action r1 = delegate
                {
                    RT_Dialog_ItemListing d1 = new RT_Dialog_ItemListing(TransferManagerHelper.GetAllTransferredItems(transferData), 
                        transferData._transferMode);

                    RT_Dialog_Base.PushNewDialog(d1);
                };

                string description = string.Empty;
                if (transferData._transferMode == TransferMode.Trade) description = "You are receiving a trade request";
                else description = "You are receiving a gift request";

                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("MESSAGE", [description], r1));
            }
        }

        catch
        {
            Printer.Warning("Rethrowing transfer items, might be RimWorld's fault");

            Thread.Sleep(100);

            ReceiveTransferRequest(transferData);
        }
    }

    public static void ReceiveReboundRequest(TransferData transferData)
    {
        try
        {
            SessionHandler.IncomingManifest = transferData;

            RT_Dialog_ItemListing d1 = new RT_Dialog_ItemListing(TransferManagerHelper.GetAllTransferredItems(transferData), TransferMode.Rebound);
            RT_Dialog_Base.PushNewDialog(d1);
        }

        catch
        {
            Printer.Warning("Rethrowing transfer items, might be RimWorld's fault");

            Thread.Sleep(100);

            ReceiveReboundRequest(transferData);
        }
    }

    public static void RejectRequest(TransferMode transferMode, bool finishTransfer = true)
    {
        if (transferMode == TransferMode.Gift)
        {
            //Nothing should happen here
        }

        else if (transferMode == TransferMode.Trade)
        {
            SessionHandler.IncomingManifest._stepMode = TransferStepMode.TradeReject;

            ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.TransferManager, SessionHandler.IncomingManifest);
        }

        else if (transferMode == TransferMode.Pod)
        {
            //Nothing should happen here
        }

        else if (transferMode == TransferMode.Rebound)
        {
            SessionHandler.IncomingManifest._stepMode = TransferStepMode.TradeReReject;

            ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.TransferManager, SessionHandler.IncomingManifest);

            RecoverTradeItems(TransferLocation.Caravan);
        }

        if (finishTransfer) FinishTransfer(false);
    }

    public static void LaunchDropPods()
    {
        foreach (IThingHolder holder in SessionHandler.ChosenPods.ToArray())
        {
            holder.GetDirectlyHeldThings().ClearAndDestroyContents();
        }
    }
}

public static class TransferManagerHelper
{
    public static void AddThingToTransferManifest(Thing thing, int thingCount)
    {
        if (ScriberH.CheckIfThingIsHuman(thing))
        {
            Pawn pawn = thing as Pawn;

            SessionHandler.OutgoingManifest._humans.Add(ScribeManager.HumanToString(pawn));

            RimworldManager.RemovePawnFromGame(pawn);
        }

        else if (ScriberH.CheckIfThingIsAnimal(thing))
        {
            Pawn pawn = thing as Pawn;

            SessionHandler.OutgoingManifest._animals.Add(ScribeManager.SerializeToString(pawn, ScribeManager.SerializableType.Thing));

            RimworldManager.RemovePawnFromGame(pawn);
        }

        else SessionHandler.OutgoingManifest._things.Add(ScribeManager.SerializeToString(thing, ScribeManager.SerializableType.Thing, thingCount));
    }

    public static IntVec3 GetTransferLocationInMap(Map map)
    {
        Thing tradingSpot = map.listerThings.AllThings.Find(x => x.def.defName == "RTTransferSpot");
        if (tradingSpot != null) return tradingSpot.Position;
        else
        {
            RT_Dialog_Message d1 = new RT_Dialog_Message("MESSAGE", [
                "You are missing a transfer spot!",
                "Received things will appear in the center of the map",
                "Build a trading spot to change the drop location!"
            ]);

            RT_Dialog_Base.PushNewDialog(d1);

            return new IntVec3(map.Center.x, map.Center.y, map.Center.z);
        }
    }

    public static Thing[] GetAllTransferredItems(TransferData transferData)
    {
        List<Thing> allTransferredItems = [];

        foreach (HumanFile file in transferData._humans)
        {
            allTransferredItems.Add(ScribeManager.StringtoHuman(file));
        }

        foreach (string data in transferData._animals)
        {
            allTransferredItems.Add((Pawn)ScribeManager.SerializeFromString<Pawn>(data));
        }

        foreach (string data in transferData._things)
        {
            allTransferredItems.Add((Thing)ScribeManager.SerializeFromString<Thing>(data));
        }

        return allTransferredItems.ToArray();
    }
}