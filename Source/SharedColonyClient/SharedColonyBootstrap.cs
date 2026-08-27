using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RTNetwork.Components;
using RTNetwork.Packets;
using RTShared.Misc;
using Verse;

namespace RWTSharedColony
{
    [StaticConstructorOnStartup]
    public static class SharedColonyBootstrap
    {
        static SharedColonyBootstrap()
        {
            Harmony harmony = new Harmony("rwt.shared-colony");
            Assembly assembly = Assembly.GetExecutingAssembly();
            int patchedClasses = 0;
            int failedClasses = 0;

            foreach (Type type in assembly.GetTypes()
                         .Where(type => type.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
                         .OrderBy(type => type.FullName, StringComparer.Ordinal))
            {
                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                    patchedClasses++;
                }
                catch (Exception exception)
                {
                    failedClasses++;
                    Log.Error($"[Rimjob] Harmony patch class failed but remaining patches will continue: {type.FullName} | {exception}");
                    try
                    {
                        RimjobClientDiagnostics.Important($"Harmony patch class failed: {type.FullName} | {exception}");
                    }
                    catch
                    {
                        // Diagnostics must not block patch isolation.
                    }
                }
            }

            Log.Message($"[Rimjob] Isolated Harmony bootstrap complete. PatchedClasses={patchedClasses}; FailedClasses={failedClasses}; MapSize={SharedColonyState.MapSize}.");
        }
    }

    public enum PlayerStance
    {
        Neutral,
        Support,
        Ally,
        Hostile
    }

    public sealed class PlayerRelation
    {
        public PlayerStance Declared { get; set; }

        public PlayerStance Effective { get; set; }
    }

    public static class SharedColonyState
    {
        public const string ProtocolUsername = "RWT_SHARED";

        public const string ProtocolPrefix = "[RWT_SHARED]";

        public static int MapSize { get; private set; } = 500;

        public static int TileCapacity { get; private set; } = 4;

        public static string PendingRemoteUsername { get; set; }

        public static Dictionary<string, PlayerRelation> Relations { get; } =
            new Dictionary<string, PlayerRelation>(StringComparer.OrdinalIgnoreCase);

        public static void HandleProtocol(string message)
        {
            string[] parts = message.Split('|');
            if (parts.Length < 2 || parts[0] != ProtocolPrefix) return;

            if (parts[1] == "CONFIG" && parts.Length >= 4)
            {
                if (int.TryParse(parts[2], out int mapSize)) MapSize = Math.Max(250, Math.Min(750, mapSize));
                if (int.TryParse(parts[3], out int capacity)) TileCapacity = Math.Max(1, Math.Min(8, capacity));
                return;
            }

            if (parts[1] == "REL" && parts.Length >= 5 &&
                Enum.TryParse(parts[3], true, out PlayerStance declared) &&
                Enum.TryParse(parts[4], true, out PlayerStance effective))
            {
                Relations[parts[2]] = new PlayerRelation { Declared = declared, Effective = effective };
                PlayerFactionRegistry.RefreshRelation(parts[2]);
            }
        }

        public static PlayerStance GetEffectiveStance(string username)
        {
            return Relations.TryGetValue(username, out PlayerRelation relation)
                ? relation.Effective
                : PlayerStance.Neutral;
        }

        public static string ResolveChosenSettlementUsername()
        {
            Type managerType = AccessTools.TypeByName("RTClient.Managers.SessionManager");
            PropertyInfo chosenProperty = AccessTools.Property(managerType, "ChosenSettlement");
            object chosen = chosenProperty?.GetValue(null, null);
            if (chosen == null) return null;

            PropertyInfo nameProperty = AccessTools.Property(chosen.GetType(), "Name");
            return nameProperty?.GetValue(chosen, null) as string;
        }

        public static Map GetSynchronousMap()
        {
            Type managerType = AccessTools.TypeByName("RTClient.Managers.SessionManager");
            PropertyInfo mapProperty = AccessTools.Property(managerType, "SynchronousMap");
            return mapProperty?.GetValue(null, null) as Map;
        }

        public static bool IsRwtBypassActive()
        {
            Type patchHandler = AccessTools.TypeByName("RTClient.Misc.PatchHandler");
            PropertyInfo bypassProperty = AccessTools.Property(patchHandler, "BypassFlag");
            return bypassProperty != null && (bool)bypassProperty.GetValue(null, null);
        }
    }

    public static class PlayerFactionRegistry
    {
        private static readonly Dictionary<string, Faction> Factions =
            new Dictionary<string, Faction>(StringComparer.OrdinalIgnoreCase);

        public static Faction GetOrCreate(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            if (Factions.TryGetValue(username, out Faction existing))
            {
                ApplyRelation(existing, SharedColonyState.GetEffectiveStance(username));
                return existing;
            }

            Faction faction = GenerateFaction(username);
            if (faction == null)
            {
                Log.Error($"[RWT Shared Colony] Could not create player faction for {username}");
                return null;
            }

            Factions[username] = faction;
            ApplyRelation(faction, SharedColonyState.GetEffectiveStance(username));
            return faction;
        }

        public static bool IsRemoteFaction(Faction faction)
        {
            return faction != null && Factions.Values.Contains(faction);
        }

        public static void RefreshRelation(string username)
        {
            if (Factions.TryGetValue(username, out Faction faction))
                ApplyRelation(faction, SharedColonyState.GetEffectiveStance(username));
        }

        private static Faction GenerateFaction(string username)
        {
            FactionDef factionDef = DefDatabase<FactionDef>.GetNamedSilentFail("RTNeutralFaction") ??
                                    DefDatabase<FactionDef>.AllDefsListForReading.FirstOrDefault(fetch =>
                                        fetch.humanlikeFaction && !fetch.isPlayer);
            if (factionDef == null) return null;

            foreach (MethodInfo method in typeof(FactionGenerator).GetMethods(AccessTools.all)
                         .Where(fetch => fetch.Name == "NewGeneratedFactionWithRelations"))
            {
                try
                {
                    object[] arguments = BuildGeneratorArguments(method.GetParameters(), factionDef);
                    Faction generated = method.Invoke(null, arguments) as Faction;
                    if (generated == null) continue;

                    FieldInfo nameField = AccessTools.Field(typeof(Faction), "nameInt");
                    nameField?.SetValue(generated, username);
                    return generated;
                }
                catch
                {
                    // RimWorld has changed this factory signature between
                    // releases. Try the next supported overload.
                }
            }

            return null;
        }

        private static object[] BuildGeneratorArguments(ParameterInfo[] parameters, FactionDef factionDef)
        {
            object[] arguments = new object[parameters.Length];
            for (int index = 0; index < parameters.Length; index++)
            {
                Type type = parameters[index].ParameterType;
                if (type == typeof(FactionDef)) arguments[index] = factionDef;
                else if (type.Name == "FactionGeneratorParms") arguments[index] = BuildGeneratorParms(type, factionDef);
                else if (parameters[index].HasDefaultValue) arguments[index] = parameters[index].DefaultValue;
                else arguments[index] = type.IsValueType ? Activator.CreateInstance(type) : null;
            }

            return arguments;
        }

        private static object BuildGeneratorParms(Type type, FactionDef factionDef)
        {
            foreach (ConstructorInfo constructor in type.GetConstructors(AccessTools.all))
            {
                try
                {
                    ParameterInfo[] parameters = constructor.GetParameters();
                    object[] arguments = parameters.Select(parameter =>
                        parameter.ParameterType == typeof(FactionDef)
                            ? (object)factionDef
                            : parameter.HasDefaultValue
                                ? parameter.DefaultValue
                                : parameter.ParameterType.IsValueType
                                    ? Activator.CreateInstance(parameter.ParameterType)
                                    : null).ToArray();
                    object result = constructor.Invoke(arguments);
                    SetFactionDef(result, type, factionDef);
                    return result;
                }
                catch
                {
                    // Try the next constructor.
                }
            }

            object fallback = Activator.CreateInstance(type, true);
            SetFactionDef(fallback, type, factionDef);
            return fallback;
        }

        private static void SetFactionDef(object instance, Type type, FactionDef factionDef)
        {
            AccessTools.Field(type, "factionDef")?.SetValue(instance, factionDef);
            AccessTools.Field(type, "def")?.SetValue(instance, factionDef);
            AccessTools.Property(type, "FactionDef")?.SetValue(instance, factionDef, null);
        }

        private static void ApplyRelation(Faction faction, PlayerStance stance)
        {
            if (faction == null || Faction.OfPlayer == null) return;

            FactionRelationKind kind = stance == PlayerStance.Ally
                ? FactionRelationKind.Ally
                : stance == PlayerStance.Hostile
                    ? FactionRelationKind.Hostile
                    : FactionRelationKind.Neutral;

            SetRelation(faction, Faction.OfPlayer, kind);
            SetRelation(Faction.OfPlayer, faction, kind);
        }

        private static void SetRelation(Faction source, Faction target, FactionRelationKind kind)
        {
            MethodInfo method = typeof(Faction).GetMethods(AccessTools.all)
                .FirstOrDefault(fetch => fetch.Name == "TrySetRelationKind" &&
                                         fetch.GetParameters().Length >= 2 &&
                                         fetch.GetParameters()[0].ParameterType == typeof(Faction));
            if (method == null) return;

            ParameterInfo[] parameters = method.GetParameters();
            object[] arguments = new object[parameters.Length];
            arguments[0] = target;
            arguments[1] = kind;
            for (int index = 2; index < arguments.Length; index++)
                arguments[index] = parameters[index].HasDefaultValue
                    ? parameters[index].DefaultValue
                    : parameters[index].ParameterType.IsValueType
                        ? Activator.CreateInstance(parameters[index].ParameterType)
                        : null;
            method.Invoke(source, arguments);
        }
    }

    [HarmonyPatch]
    public static class ProtocolChatPatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("RTClient.PacketManagers.PM_Chat"), "Receive");

        public static bool Prefix(object[] __args)
        {
            byte[] bytes = __args.OfType<byte[]>().FirstOrDefault();
            if (bytes == null) return true;

            PKT_Chat packet = Serializer.ConvertBytesToObject<PKT_Chat>(bytes);
            if (packet.Username != SharedColonyState.ProtocolUsername ||
                !packet.Message.StartsWith(SharedColonyState.ProtocolPrefix, StringComparison.Ordinal)) return true;

            SharedColonyState.HandleProtocol(packet.Message);
            return false;
        }
    }

    [HarmonyPatch]
    public static class ExplicitSynchronousTargetPatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("RTClient.PacketManagers.Synchronous.PM_Synchronous"), "Ask");

        public static void Prefix()
        {
            string target = SharedColonyState.ResolveChosenSettlementUsername();
            if (string.IsNullOrWhiteSpace(target)) return;

            SharedColonyState.PendingRemoteUsername = target;
            PKT_Chat command = new PKT_Chat
            {
                IsCommand = true,
                Message = $"/colony target @{target}"
            };
            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Chat, command);
        }
    }

    [HarmonyPatch]
    public static class IncomingSynchronousIdentityPatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("RTClient.PacketManagers.Synchronous.PM_Synchronous"), "OnAsk");

        public static void Prefix(object[] __args)
        {
            PKT_Synchronous packet = __args.OfType<PKT_Synchronous>().FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(packet?.Username)) SharedColonyState.PendingRemoteUsername = packet.Username;
        }
    }

    [HarmonyPatch]
    public static class LoadedMapFactionPatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("RTClient.PacketManagers.Synchronous.PM_Synchronous"), "SetMap");

        public static void Postfix(object[] __args)
        {
            bool visitorSide = __args.OfType<bool>().FirstOrDefault();
            if (!visitorSide) return;
            AssignAggregatePawns(SharedColonyState.GetSynchronousMap(), SharedColonyState.PendingRemoteUsername);
        }

        internal static void AssignAggregatePawns(Map map, string username)
        {
            Faction remoteFaction = PlayerFactionRegistry.GetOrCreate(username);
            if (map?.mapPawns == null || remoteFaction == null) return;

            foreach (Pawn pawn in map.mapPawns.AllPawns.ToArray())
            {
                if (pawn.Faction == Faction.OfPlayer) continue;
                if (pawn.Faction?.def?.defName?.StartsWith("RT", StringComparison.Ordinal) != true) continue;
                pawn.SetFactionDirect(remoteFaction);
            }
        }
    }

    [HarmonyPatch]
    public static class SpawnedPartyFactionPatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("RTClient.PacketManagers.Synchronous.PM_Synchronous"), "SpawnOtherPawns");

        public static void Postfix(object[] __args)
        {
            PKT_Synchronous packet = __args.OfType<PKT_Synchronous>().FirstOrDefault();
            string username = packet?.Username ?? SharedColonyState.PendingRemoteUsername;
            LoadedMapFactionPatch.AssignAggregatePawns(SharedColonyState.GetSynchronousMap(), username);
        }
    }

    [HarmonyPatch]
    public static class SettlementFactionPatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("RTClient.PacketManagers.PM_Settlements"), "SpawnSingleSettlement");

        public static void Postfix(object[] __args)
        {
            object settlementFile = __args.FirstOrDefault(argument =>
                argument?.GetType().FullName == "RTShared.Files.FL_Settlement");
            if (settlementFile == null || Find.WorldObjects == null) return;

            string username = AccessTools.Property(settlementFile.GetType(), "Username")?.GetValue(settlementFile, null) as string;
            object tileValue = AccessTools.Property(settlementFile.GetType(), "Tile")?.GetValue(settlementFile, null);
            if (string.IsNullOrWhiteSpace(username) || tileValue == null) return;

            int tile = Convert.ToInt32(tileValue);
            Faction faction = PlayerFactionRegistry.GetOrCreate(username);
            if (faction == null) return;

            foreach (WorldObject worldObject in Find.WorldObjects.AllWorldObjects.Where(fetch => fetch.Tile == tile))
            {
                PropertyInfo nameProperty = AccessTools.Property(worldObject.GetType(), "Name");
                if ((nameProperty?.GetValue(worldObject, null) as string) != username) continue;
                AccessTools.Method(worldObject.GetType(), "SetFaction")?.Invoke(worldObject, new object[] { faction });
            }
        }
    }

    [HarmonyPatch]
    public static class FourTimesAreaMapPatch
    {
        public static IEnumerable<MethodBase> TargetMethods() =>
            typeof(MapGenerator).GetMethods(AccessTools.all).Where(fetch => fetch.Name == "GenerateMap");

        public static void Prefix(object[] __args)
        {
            MapParent parent = __args.OfType<MapParent>().FirstOrDefault();
            if (parent != null && parent.Faction != null && parent.Faction != Faction.OfPlayer) return;

            for (int index = 0; index < __args.Length; index++)
            {
                if (!(__args[index] is IntVec3 size)) continue;
                if (size.x >= SharedColonyState.MapSize && size.z >= SharedColonyState.MapSize) return;

                IntVec3 forcedSize = new IntVec3(SharedColonyState.MapSize, size.y, SharedColonyState.MapSize);
                __args[index] = forcedSize;
                Log.Message($"[Rimjob] Forcing player settlement map from {size.x}x{size.z} to {forcedSize.x}x{forcedSize.z}.");
                try
                {
                    RimjobClientDiagnostics.Important($"Map generation forced to {forcedSize.x}x{forcedSize.z}; previous={size.x}x{size.z}; tile={(parent?.Tile.tileId ?? -1)}.");
                }
                catch
                {
                    // Map generation must not depend on diagnostics.
                }
                return;
            }
        }
    }

    [HarmonyPatch]
    public static class RemotePawnGizmoPatch
    {
        public static MethodBase TargetMethod() => AccessTools.Method(typeof(Pawn), "GetGizmos");

        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (PlayerFactionRegistry.IsRemoteFaction(__instance.Faction)) __result = Enumerable.Empty<Gizmo>();
        }
    }

    [HarmonyPatch]
    public static class RemotePawnDraftPatch
    {
        public static MethodBase TargetMethod() => AccessTools.PropertySetter(typeof(Pawn_DraftController), "Drafted");

        public static bool Prefix(Pawn_DraftController __instance)
        {
            Pawn pawn = AccessTools.Field(typeof(Pawn_DraftController), "pawn")?.GetValue(__instance) as Pawn;
            if (pawn == null || !PlayerFactionRegistry.IsRemoteFaction(pawn.Faction)) return true;
            return SharedColonyState.IsRwtBypassActive();
        }
    }
}
