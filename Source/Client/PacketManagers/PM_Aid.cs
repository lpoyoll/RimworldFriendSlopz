using GameClient.Dialogs;
using GameClient.Misc;
using RimWorld;
using Shared;
using System;
using Verse;
using TCPNetwork.Packets;
using TCPNetwork;
using GameClient.Managers;
using static TCPNetwork.Packets.PKT_Aid;
using GameClient.Dialogs.Default;
using TCPNetwork.PacketManagers;

namespace GameClient.PacketManagers
{
    public class PM_Aid : PM_Base
    {
        [HandlesPacket(PacketHeader.AidManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Aid data = Serializer.ConvertBytesToObject<PKT_Aid>(bytes);

            switch (data._stepMode)
            {
                case AidStepMode.Receive:
                    ReceiveAidRequest(data);
                    break;

                case AidStepMode.Accept:
                    OnAidAccept();
                    break;

                case AidStepMode.Reject:
                    OnAidReject(data);
                    break;
            }
        }

        private static void ReceiveAidRequest(PKT_Aid data)
        {
            Action toDoYes = delegate { AcceptAid(data); };
            Action toDoNo = delegate { RejectAid(data); };

            DLG_Base.PushNewDialog(new DLG_YesNo("You are receiving aid, accept?", toDoYes, toDoNo));
        }

        public static void SendAidRequest()
        {
            PKT_Aid aidData = new PKT_Aid();
            aidData._stepMode = AidStepMode.Send;
            aidData._fromTile = Find.AnyPlayerHomeMap.Tile;
            aidData._toTile = SessionHandler.ChosenSettlement.Tile;

            Pawn toGet = RimworldManager.GetAllSettlementsPawns(Faction.OfPlayer, false)[DLG_ListingWithButton.ResultInt];
            aidData._humanData = ScribeManager.SerializeToString(toGet, ScribeManager.SerializableType.Thing);
            RimworldManager.RemovePawnFromGame(toGet);

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.AidManager, aidData);

            DLG_Base.PushNewDialog(new DLG_Wait());
        }

        private static void OnAidAccept()
        {
            DLG_Wait.Instance.Close();

            RimworldManager.GenerateLetter("Sent aid", "You have sent aid towards a settlement! The owner will receive the news soon",
                LetterDefOf.PositiveEvent);

            PM_Saves.ForceSave();
        }

        private static void OnAidReject(PKT_Aid data)
        {
            DLG_Wait.Instance.Close();
            Map map = Find.World.worldObjects.SettlementAt(data._fromTile).Map;
            Pawn pawn = ScribeManager.SerializeFromString<Pawn>(data._humanData, ScribeManager.SerializableType.Pawn);

            RimworldManager.PlaceThingIntoMap(pawn, map, RimworldManager.GetTransferLocationInMap(map));
            RimworldManager.GenerateLetter("Aid rejected", "The aid was rejected", LetterDefOf.NeutralEvent);
        }

        private static void AcceptAid(PKT_Aid data)
        {
            Map map = Find.World.worldObjects.SettlementAt(data._toTile).Map;
            Pawn pawn = ScribeManager.SerializeFromString<Pawn>(data._humanData, ScribeManager.SerializableType.Pawn);
            RimworldManager.PlaceThingIntoMap(pawn, map, RimworldManager.GetTransferLocationInMap(map), true);

            data._stepMode = AidStepMode.Accept;
            Network.ServerEndpoint.EnqueuePacket(PacketHeader.AidManager, data);

            RimworldManager.GenerateLetter("Received aid", "You have received aid from a player! The pawn should come to help soon",
                LetterDefOf.PositiveEvent);

            PM_Saves.ForceSave();
        }

        private static void RejectAid(PKT_Aid data)
        {
            data._stepMode = AidStepMode.Reject;
            Network.ServerEndpoint.EnqueuePacket(PacketHeader.AidManager, data);
        }
    }
}
