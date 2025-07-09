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
        /// <summary>
        /// Checks if the player is online, with an if - else statement
        /// </summary>
        /// <param name="generator"> IlGenerate provided by Harmony</param>
        /// <param name="baseMethod"> The original's method Opcodes</param>
        /// <param name="codeToExecuteIfConnected"> The CodeInstructions to be executed if Connected</param>
        /// <param name="index"> Current index, where the IF will begin</param>
        /// <param name="codeToSKipIfConnected"> Amount of instructions to skip after the IF branch</param>
        public static void CheckIfConnected(ILGenerator generator, List<CodeInstruction> baseMethod, CodeInstruction[] codeToExecuteIfConnected, ref int index, int codeToSKipIfConnected = 0)
        {
            Label skipLabel = generator.DefineLabel();

            baseMethod[index].WithLabels(skipLabel);
            List<CodeInstruction> instructions = new List<CodeInstruction>
            {
                new(OpCodes.Ldsfld, NetworkState),
                new(OpCodes.Ldc_I4, (int)CommonEnumerators.ClientNetworkState.Connected),
                new(OpCodes.Ceq),
                new(OpCodes.Brfalse_S, skipLabel),
            };
            instructions.AddRange(codeToExecuteIfConnected);
            if (codeToSKipIfConnected > 0)
            {
                Label elseLabel = generator.DefineLabel();
                instructions.Add(new CodeInstruction(OpCodes.Br, elseLabel));
                index += codeToSKipIfConnected;
                baseMethod[index].WithLabels(elseLabel);
                index -= codeToSKipIfConnected;
            }
            baseMethod.InsertRange(index, instructions);
            index += instructions.Count;
            baseMethod[index].WithLabels(skipLabel);
        }
    }
}