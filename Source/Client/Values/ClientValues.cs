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
        public static bool IsGeneratingFreshWorld = false;

        public static bool IsReadyToPlay = false;

        public static bool IsSavingGame = false;

        public static bool IsInTransfer = false;

        public static bool IsUsingScriber = false;

        public static string Username = string.Empty;

        public static string Uid = string.Empty;

        public static bool IsAdmin = false;

        public static bool HasFaction = false;

        public static List<Faction> playerFactions = new List<Faction>();

        public static Faction neutralPlayer = null;

        public static Faction allyPlayer = null;

        public static Faction enemyPlayer = null;

        public static Faction yourOnlineFaction = null;

        public enum VerboseMode { None, Verbose, Extreme }

        public static void SetValues(ServerGlobalData serverGlobalData)
        {
            ToggleAdmin(serverGlobalData._isClientAdmin);
            ToggleFaction(serverGlobalData._isClientFactionMember);
        }

        public static void FindPlayerFactionsInWorld()
        {
            Faction[] factions = Find.FactionManager.AllFactions.ToArray();
            neutralPlayer = factions.First(fetch => fetch.def.defName == RTFactionDefOf.RTNeutral.defName);
            allyPlayer = factions.First(fetch => fetch.def.defName == RTFactionDefOf.RTAlly.defName);
            enemyPlayer = factions.First(fetch => fetch.def.defName == RTFactionDefOf.RTEnemy.defName);
            yourOnlineFaction = factions.First(fetch => fetch.def.defName == RTFactionDefOf.RTFaction.defName);

            playerFactions.Clear();
            playerFactions.Add(neutralPlayer);
            playerFactions.Add(allyPlayer);
            playerFactions.Add(enemyPlayer);
            playerFactions.Add(yourOnlineFaction);
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
            DisconnectionManager.isIntentionalDisconnect = mode;
            DisconnectionManager.intentionalDisconnectReason = reason;
        }

        public static void ToggleReadyToPlay(bool mode) { IsReadyToPlay = mode; }

        public static void ToggleTransfer(bool mode) { IsInTransfer = mode; }

        public static void ToggleChatScroll(bool mode) { ChatManager.shouldScrollChat = mode; }

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