using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Managers;
using GameClient.Misc;
using RimWorld;
using Shared;
using System.Collections.Generic;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using Verse;
using static TCPNetwork.Packets.PKT_Transfer;

namespace GameClient.PacketManagers
{
    public class PM_Transfers : PM_Base
    {
        [HandlesPacket(PacketHeader.TransferManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Transfer data = Serializer.ConvertBytesToObject<PKT_Transfer>(bytes);

            switch (data.CurrentStepMode)
            {
                case TransferStepMode.TradeRequest:
                    ReceiveRequest(data, false);
                    break;

                case TransferStepMode.TradeReRequest:
                    DLG_Wait.Instance.Close();
                    ReceiveRequest(data, true);
                    break;

                case TransferStepMode.TradeAccept:
                    FinishRequest(data.CurrentStepMode);
                    break;

                case TransferStepMode.TradeReAccept:
                    DLG_Wait.Instance.Close();
                    FinishRequest(data.CurrentStepMode);
                    break;

                case TransferStepMode.TradeReject:
                    DLG_Wait.Instance.Close();
                    FinishRequest(data.CurrentStepMode);
                    break;

                case TransferStepMode.TradeReReject:
                    DLG_Wait.Instance.Close();
                    FinishRequest(data.CurrentStepMode);
                    break;

                case TransferStepMode.Recover:
                    DLG_Wait.Instance.Close();
                    FinishRequest(data.CurrentStepMode);
                    break;
            }
        }

        public static void SendRequest(TransferLocation transferLocation)
        {
            DLG_Base.PushNewDialog(new DLG_Wait());

            if (transferLocation == TransferLocation.Caravan)
            {
                SessionHandler.OutgoingManifest.CurrentStepMode = TransferStepMode.TradeRequest;
                SessionHandler.OutgoingManifest.ToTile = TradeSession.playerNegotiator.Tile;
                Network.ServerEndpoint.EnqueuePacket(PacketHeader.TransferManager, SessionHandler.OutgoingManifest);
            }

            else if (transferLocation == TransferLocation.Settlement)
            {
                SessionHandler.OutgoingManifest.CurrentStepMode = TransferStepMode.TradeReRequest;
                SessionHandler.OutgoingManifest.ToTile = SessionHandler.IncomingManifest.FromTile;
                Network.ServerEndpoint.EnqueuePacket(PacketHeader.TransferManager, SessionHandler.OutgoingManifest);
            }

            else if (transferLocation == TransferLocation.Pod)
            {
                SessionHandler.OutgoingManifest.CurrentStepMode = TransferStepMode.TradeRequest;
                SessionHandler.OutgoingManifest.ToTile = SessionHandler.ChosenSettlement.Tile;
                Network.ServerEndpoint.EnqueuePacket(PacketHeader.TransferManager, SessionHandler.OutgoingManifest);
            }
        }

        public static void FinishTransfer(bool success)
        {
            if (success) PM_Saves.ForceSave();

            SessionHandler.LastTradeStep = CommonEnumerators.TradeMode.None;
            SessionHandler.IncomingManifest = new PKT_Transfer();
            SessionHandler.OutgoingManifest = new PKT_Transfer();
            SessionHandler.IsInTransfer = false;
        }

        public static void FinishRequest(TransferStepMode mode)
        {
            if (mode == TransferStepMode.TradeAccept)
            {
                FinishTransfer(true);
                RimworldManager.GenerateLetter("Transfer completed", "The transfer was completed", LetterDefOf.PositiveEvent);
            }

            else if (mode == TransferStepMode.TradeReAccept)
            {
                List<Thing> allTransferedItems = GetAllTransferedItems(SessionHandler.IncomingManifest);
                Map map = Find.Maps.Find(x => x.Tile == SessionHandler.IncomingManifest.ToTile);
                IntVec3 location = RimworldManager.GetTransferLocationInMap(map);
                foreach (Thing thing in allTransferedItems) RimworldManager.PlaceThingIntoMap(thing, map, location);

                FinishTransfer(true);
                RimworldManager.GenerateLetter("Transfer completed", "The transfer was completed", LetterDefOf.PositiveEvent);
            }

            else if (mode == TransferStepMode.TradeReject)
            {
                RimworldManager.GenerateLetter("Transfer cancelled", "The transfer was cancelled", LetterDefOf.NeutralEvent);
                RecoverTransferManifest(TransferLocation.Caravan);
                FinishTransfer(false);
            }

            else if (mode == TransferStepMode.TradeReReject)
            {
                RimworldManager.GenerateLetter("Transfer cancelled", "The transfer was cancelled", LetterDefOf.NeutralEvent);
                RecoverTransferManifest(TransferLocation.Settlement);
                FinishTransfer(false);
            }

            else if (mode == TransferStepMode.Recover)
            {
                RimworldManager.GenerateLetter("Transfer cancelled", "Player is not currently available!", LetterDefOf.NeutralEvent);
                RecoverTransferManifest(TransferLocation.Caravan);
                FinishTransfer(false);
            }
        }

        public static void ReceiveRequest(PKT_Transfer transferData, bool isRebound)
        {
            if (DLG_Options.AutorejectTransfersBool || (!isRebound && SessionHandler.IsInTransfer)) RejectRequest(transferData.CurrentTransferMode);
            else
            {
                SessionHandler.IsInTransfer = true;
                SessionHandler.IncomingManifest = transferData;
                DLG_Base.PushNewDialog(new DLG_TradeListing(GetAllTransferedItems(transferData),
                    transferData.CurrentTransferMode));
            }
        }

        public static void RejectRequest(TransferMode transferMode)
        {
            if (transferMode == TransferMode.Trade)
            {
                SessionHandler.IncomingManifest.CurrentStepMode = TransferStepMode.TradeReject;
                Network.ServerEndpoint.EnqueuePacket(PacketHeader.TransferManager, SessionHandler.IncomingManifest);
            }

            else if (transferMode == TransferMode.Rebound)
            {
                SessionHandler.IncomingManifest.CurrentStepMode = TransferStepMode.TradeReReject;
                Network.ServerEndpoint.EnqueuePacket(PacketHeader.TransferManager, SessionHandler.IncomingManifest);
                RecoverTransferManifest(TransferLocation.Caravan);
            }

            FinishTransfer(false);
        }

        public static void AddToTransferManifest(Thing thing, int thingCount)
        {
            if (RimworldManager.CheckIfThingIsCorpse(thing))
            {
                Corpse corpse = thing as Corpse;
                Pawn pawn = corpse.InnerPawn;

                SessionHandler.OutgoingManifest.Pawns.Add(ScribeManager.SerializeToString(pawn, ScribeManager.SerializableType.Pawn));
                RimworldManager.RemovePawnFromGame(pawn);
            }

            else if (RimworldManager.CheckIfThingIsPawn(thing))
            {
                Pawn pawn = thing as Pawn;

                SessionHandler.OutgoingManifest.Pawns.Add(ScribeManager.SerializeToString(pawn, ScribeManager.SerializableType.Pawn));
                RimworldManager.RemovePawnFromGame(pawn);
            }

            else
            {
                SessionHandler.OutgoingManifest.Things.Add(ScribeManager.SerializeToString(thing, ScribeManager.SerializableType.Thing, thingCount));
            }
        }

        public static void RecoverTransferManifest(TransferLocation transferLocation)
        {
            if (transferLocation == TransferLocation.Caravan)
            {
                foreach (Thing thing in GetAllTransferedItems(SessionHandler.OutgoingManifest))
                {
                    try { RimworldManager.PlaceThingIntoCaravan(thing, SessionHandler.ChosenCaravan); }
                    catch { continue; }
                }
            }

            else if (transferLocation == TransferLocation.Settlement)
            {
                IntVec3 location = RimworldManager.GetTransferLocationInMap(Find.AnyPlayerHomeMap);

                foreach (Thing thing in GetAllTransferedItems(SessionHandler.OutgoingManifest))
                {
                    try { RimworldManager.PlaceThingIntoMap(thing, Find.AnyPlayerHomeMap, location); }
                    catch { continue; }
                }
            }
        }

        public static List<Thing> GetAllTransferedItems(PKT_Transfer transferData)
        {
            List<Thing> allTransferedItems = new List<Thing>();

            foreach (string data in transferData.Pawns)
            {
                try { allTransferedItems.Add(ScribeManager.SerializeFromString<Pawn>(data, ScribeManager.SerializableType.Pawn)); }
                catch { continue; }
            }

            foreach (string data in transferData.Things)
            {
                try { allTransferedItems.Add(ScribeManager.SerializeFromString<Thing>(data, ScribeManager.SerializableType.Thing)); }
                catch { continue; }
            }

            return allTransferedItems;
        }
    }
}
