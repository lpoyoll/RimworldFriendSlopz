using GameClient;
using GameClient.Core;
using GameClient.Misc;
using GameClient.Values;
using RimWorld;
using Shared;
using Synchronous.Data;
using Synchronous.Misc;
using Synchronous.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Synchronous.Managers
{
    public static class DraftManager
    {
        private static List<PlayerDraft> PlayerDrafts { get; set; } = new List<PlayerDraft>();

        [ShouldInitializeOnSession]
        private static void Initialize() { PlayerDrafts = new List<PlayerDraft>(); }

        public static void AskForDraft(Pawn pawn, bool mode)
        {
            PlayerDraft playerDraft = new PlayerDraft();
            playerDraft.MapID = pawn.Map.uniqueID;
            playerDraft.PawnID = pawn.ThingID;
            playerDraft.DraftValue = mode;

            PlayerDrafts.Add(playerDraft);
        }

        [ShouldCheckPerFrame]
        private static void CheckForPlayerDrafts()
        {
            if (PlayerDrafts.Count > 0)
            {
                SynchronousData data = new SynchronousData();
                data._uid = ClientValues.Uid;
                data._bytes = Serializer.ConvertObjectToBytes(PlayerDrafts);

                ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.SPlayerDraft, data);

                PlayerDrafts.Clear();
            }
        }

        [HandlesPacket(PacketHeader.SPlayerDraft)]
        private static void ReceiveDrafts(byte[] bytes)
        {
            SynchronousData data = Serializer.ConvertBytesToObject<SynchronousData>(bytes);
            PlayerDraft[] drafts = Serializer.ConvertBytesToObject<PlayerDraft[]>(data._bytes);

            PatchHandler.ExecuteInBypass(delegate
            {
                foreach (PlayerDraft playerDraft in drafts)
                {
                    Map map = Finder.GetMapFromID(playerDraft.MapID);
                    Pawn pawn = Finder.GetPawnFromID(map, playerDraft.PawnID);

                    Printer.Warning(pawn.Label);

                    pawn.drafter ??= new Pawn_DraftController(pawn);
                    pawn.drafter.Drafted = playerDraft.DraftValue;
                }
            });
        }
    }
}
