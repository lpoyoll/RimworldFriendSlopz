using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.TCP;
using HarmonyLib;
using RimWorld;
using Shared;
using Verse;

namespace GameClient.Patches
{
    public static class MoveColonyUtilityPatches
    {
        [HarmonyPatch(typeof(MoveColonyUtility), nameof(MoveColonyUtility.MoveColonyAndReset))]
        public static class MoveColonyAndResetPatch
        {
            private static readonly FieldInfo PlayerSettlementsRemoved;

            static MoveColonyAndResetPatch()
            {
                PlayerSettlementsRemoved = AccessTools.Field(typeof(MoveColonyUtility), "playerSettlementsRemoved"); //caching, saves some performance
            }

            /// <summary>
            /// Catches the removed player settlements and notifies the server about them
            /// </summary>
            /// 

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
                MethodInfo method = AccessTools.Method(typeof(MoveColonyAndResetPatch), nameof(RemovePreviousSettlements));
                MethodInfo methodToCheck = AccessTools.PropertyGetter(typeof(ModsConfig), nameof(ModsConfig.IdeologyActive));
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Call && (MethodInfo)codes[i].operand == methodToCheck)
                    {
                        codes.InsertRange(i, new CodeInstruction[]
                        {
                            new CodeInstruction(OpCodes.Ldsfld,  PlayerSettlementsRemoved),
                            new CodeInstruction(OpCodes.Call, method)
                        });
                        break;
                    }
                }
                return codes;
            }

            [HarmonyPostfix]
            public static void Postfix(int tile)
            {
                SettlementManager.SendNewPlayerSettlement(tile);
            }

            private static void RemovePreviousSettlements(List<int> settlementsToRemove)
            {
                foreach (int settlement in settlementsToRemove)
                {
                    PlayerSettlementData settlementData = new PlayerSettlementData();
                    settlementData._settlementFile.Tile = settlement;
                    settlementData._stepMode = CommonEnumerators.SettlementStepMode.Remove;

                    Network.Listener.EnqueuePacket(PacketHeader.SettlementManager, settlementData);
                }
            }
        }
    }
}