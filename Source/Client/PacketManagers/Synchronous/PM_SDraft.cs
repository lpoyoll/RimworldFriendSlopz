using GameClient;
using GameClient.Core;
using GameClient.Hooks.Synchronous;
using GameClient.Hooks.TCPNetwork;
using GameClient.Misc;
using RimWorld;
using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using Verse;

namespace GameClient.PacketManagers.Synchronous
{
    public class PM_SDraft : PM_Base
    {
        private static List<PlayerDraft> PlayerDrafts { get; set; } = new List<PlayerDraft>();

        [OnSessionStart]
        private static void Initialize() { PlayerDrafts = new List<PlayerDraft>(); }

        [OnUpdate]
        private static void Check()
        {
            if (PlayerDrafts.Count > 0)
            {
                PKT_Synchronous packet = new PKT_Synchronous();
                packet.CurrentStepMode = PKT_Synchronous.StepMode.Action;
                packet.CurrentActionType = PKT_Synchronous.ActionType.SPlayerDraft;
                packet.Contents = Serializer.ConvertObjectToBytes(PlayerDrafts);

                Network.ServerEndpoint.EnqueuePacket(PacketHeader.SynchronousManager, packet);

                PlayerDrafts.Clear();
            }
        }

        public static void Ask(Pawn pawn, bool mode) 
        {
            PlayerDraft draft = new PlayerDraft();
            draft.MapTile = pawn.Map.Tile;
            draft.PawnID = pawn.ThingID;
            draft.DraftValue = mode;

            PlayerDrafts.Add(draft); 
        }

        public static void Handle(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PlayerDraft[] drafts = Serializer.ConvertBytesToObject<PlayerDraft[]>(bytes);

            PatchHandler.ExecuteInBypass(delegate
            {
                foreach (PlayerDraft playerDraft in drafts)
                {
                    Map map = Finder.GetMapFromTile(playerDraft.MapTile);
                    Pawn pawn = Finder.GetPawnFromID(map, playerDraft.PawnID);

                    pawn.drafter ??= new Pawn_DraftController(pawn);
                    pawn.drafter.Drafted = playerDraft.DraftValue;
                }
            });
        }

        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            throw new NotImplementedException();
        }
    }
}
