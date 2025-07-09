using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using GameClient.Managers;
using GameClient.TCP;
using GameClient.Values;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.SceneManagement;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient.Patches.Pages
{
    public class SelectStartingSitePatches
    {
        [HarmonyPatch(typeof(Page_SelectStartingSite), "DoCustomBottomButtons")]
        public static class PathSelectStartingSitePage
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
            {
                const string disconnectText = "Disconnect";
                List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
                codes.InsertRange(0, TranspilerHelper.CheckIfConnected(ilGenerator));
                MethodInfo helper = AccessTools.Method(typeof(PathSelectStartingSitePage), nameof(Helper));
                int index = 0;
                for (; index < codes.Count; index++)
                {
                    if (codes[index].operand is string str && str == "Back")
                    {
                        codes.RemoveAt(index + 1);
                        codes.RemoveAt(index + 1);
                        codes[index].operand = disconnectText;
                        break;
                    }
                }
                bool flag = false;
                for (; index < codes.Count; index++)
                {
                    if (codes[index].opcode == OpCodes.Ldarg_0)
                    {
                        if (!flag)
                        {
                            flag = true;
                            continue;
                        }
                        codes.InsertRange(index, new CodeInstruction[]
                        {
                            new(OpCodes.Call, helper),
                            new(OpCodes.Ret)
                        });
                        break;
                    }
                }
                return codes;
            }

            private static void Helper()
            {
                SceneManager.LoadScene(0);
                DisconnectionManager.SetIntentionalDisconnect(true, DisconnectionManager.DCReason.QuitToMenu);
                Network.Listener.DisconnectFlag = true;
            }
        }

        [HarmonyPatch(typeof(Page_SelectStartingSite), "PreOpen")]
        public static class PatchSettlements
        {
            [HarmonyPostfix]
            public static void DoPost()
            {
                if (Network.State == ClientNetworkState.Disconnected) return;

                if (!ClientValues.IsGeneratingFreshWorld)
                {
                    WorldManager.SetPlanetFeatures();
                    WorldManager.SetPlanetFactions();
                    RiverManager.SetPlanetRivers();
                }

                PlanetManager.BuildPlanet();
            }
        }
    }
}
