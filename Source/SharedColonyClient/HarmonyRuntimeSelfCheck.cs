using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RWTSharedColony
{
    [StaticConstructorOnStartup]
    public static class HarmonyRuntimeSelfCheck
    {
        static HarmonyRuntimeSelfCheck()
        {
            try
            {
                MethodInfo choices = typeof(FloatMenuMakerMap).GetMethods(AccessTools.all)
                    .FirstOrDefault(method => method.Name == "ChoicesAtFor" &&
                                              method.GetParameters().Any(parameter => parameter.ParameterType == typeof(Pawn)));
                if (choices == null)
                {
                    Log.Error("[Rimjob] Harmony self-check: FloatMenuMakerMap.ChoicesAtFor not found.");
                    return;
                }

                if (choices.ReturnType != typeof(List<FloatMenuOption>))
                {
                    Log.Error($"[Rimjob] Harmony self-check: ChoicesAtFor return type changed to {choices.ReturnType.FullName}; remote-pawn RMB suppression was not assumed safe.");
                    return;
                }

                Log.Message("[Rimjob] Harmony self-check passed for RimWorld 1.6 FloatMenuMakerMap.ChoicesAtFor.");
            }
            catch (Exception exception)
            {
                Log.Warning("[Rimjob] Harmony runtime self-check failed: " + exception);
            }
        }
    }
}
