using System.Collections.Generic;
using System.Linq;
using GameClient.Managers;
using GameClient.WorldObjects;
using RimWorld;
using Shared;
using Verse;

namespace GameClient.Values
{
    public static class ClientValues
    {
        public static bool IsGeneratingFreshWorld { get; private set; } = false;

        public static bool IsReadyToPlay { get; private set; } = false;

        public static bool IsSavingGame { get; private set; } = false;

        public static bool IsInTransfer { get; private set; } = false;

        public static bool IsUsingScriber { get; private set; } = false;

        public static string Username { get; set; } = string.Empty;

        public static string Uid { get; set; } = string.Empty;

        public static bool IsAdmin { get; set; } = false;

        public static bool HasFaction { get; set; } = false;

        public static List<Faction> PlayerFactions { get; set; } = new List<Faction>();

        public static Faction NeutralPlayer { get; set; } = null;

        public static Faction AllyPlayer { get; set; } = null;

        public static Faction EnemyPlayer { get; set; } = null;

        public static Faction YourOnlineFaction { get; set; } = null;

        public enum VerboseMode { None, Verbose, Extreme }

        public static void SetValues(ServerGlobalData serverGlobalData)
        {
            ToggleAdmin(serverGlobalData._isClientAdmin);
            ToggleFaction(serverGlobalData._isClientFactionMember);
        }

        public static void FindPlayerFactionsInWorld()
        {
            Faction[] factions = Find.FactionManager.AllFactions.ToArray();
            NeutralPlayer = factions.First(fetch => fetch.def.defName == RTFactionDefOf.RTNeutral.defName);
            AllyPlayer = factions.First(fetch => fetch.def.defName == RTFactionDefOf.RTAlly.defName);
            EnemyPlayer = factions.First(fetch => fetch.def.defName == RTFactionDefOf.RTEnemy.defName);
            YourOnlineFaction = factions.First(fetch => fetch.def.defName == RTFactionDefOf.RTFaction.defName);

            PlayerFactions.Clear();
            PlayerFactions.Add(NeutralPlayer);
            PlayerFactions.Add(AllyPlayer);
            PlayerFactions.Add(EnemyPlayer);
            PlayerFactions.Add(YourOnlineFaction);
        }

        public static void ForcePermadeath() { Current.Game.Info.permadeathMode = true; }

        public static void ManageDevOptions()
        {
            if (IsAdmin) return;
            else Prefs.DevMode = false;
        }

        public static void ToggleGenerateWorld(bool mode) { IsGeneratingFreshWorld = mode; }

        public static void SetIntentionalDisconnect(bool mode, DisconnectionManager.DCReason reason = DisconnectionManager.DCReason.None)
        {
            DisconnectionManager.IsIntentionalDisconnect = mode;
            DisconnectionManager.IntentionalDisconnectReason = reason;
        }

        public static void ToggleReadyToPlay(bool mode) { IsReadyToPlay = mode; }

        public static void ToggleTransfer(bool mode) { IsInTransfer = mode; }

        public static void ToggleChatScroll(bool mode) { ChatManager.ShouldScrollChat = mode; }

        public static void ToggleSavingGame(bool mode) { IsSavingGame = mode; }

        public static void ToggleUsingScriber(bool mode) { IsUsingScriber = mode; }

        public static void ToggleAdmin(bool mode) 
        { 
            IsAdmin = mode;
            ClientValues.ManageDevOptions();
        }

        public static void ToggleFaction(bool mode) { HasFaction = mode; }

        public static void CleanValues()
        {
            ToggleGenerateWorld(false);
            SetIntentionalDisconnect(false);
            ToggleReadyToPlay(false);
            ToggleTransfer(false);
            ToggleSavingGame(false);
            ToggleUsingScriber(false);
            ToggleAdmin(false);
            ToggleFaction(false);
        }
    }
}