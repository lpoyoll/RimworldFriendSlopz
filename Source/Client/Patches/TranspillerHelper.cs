using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GameClient.TCP;
using HarmonyLib;
using Shared;

namespace GameClient.Patches
{
    public static class TranspilerHelper
    {
        private static readonly FieldInfo NetworkState = AccessTools.Field(typeof(Network), nameof(Network.State));

        public static CodeInstruction[] CheckIfConnected(ILGenerator generator)
        {
            Label skipLabel = generator.DefineLabel();
            CodeInstruction[] instructions = new CodeInstruction[]
            {
                new(OpCodes.Ldsfld, NetworkState),
                new(OpCodes.Ldc_I4, (int)CommonEnumerators.ClientNetworkState.Connected),
                new(OpCodes.Ceq),
                new(OpCodes.Brtrue_S, skipLabel),
                new(OpCodes.Ret),
                new CodeInstruction(OpCodes.Nop).WithLabels(skipLabel)
            };
            return instructions;
        }
    }
}