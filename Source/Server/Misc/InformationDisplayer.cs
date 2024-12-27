using GameServer.TCP;
using static Shared.CommonEnumerators;

namespace GameServer.Misc
{
    public static class InformationDisplayer
    {
        public static void DisplayConnect(ServerClient client) { Printer.Error($"[Connect] > {client.userFile.SavedIP}"); }

        public static void DisplayDisconnect(ServerClient client) { Printer.Error($"[Disconnect] > {client.userFile.Label}"); }

        public static void DisplayLogin(ServerClient client) { Printer.Error($"[Log in] > {client.userFile.Label}"); }

        public static void DisplayRegister(ServerClient client) { Printer.Error($"[Register] > {client.userFile.Label}"); }

        public static void DisplaySaveGame(ServerClient client) { Printer.Error($"[Save game] > {client.userFile.Label}"); }

        public static void DisplayLoadGame(ServerClient client) { Printer.Error($"[Load game] > {client.userFile.Label}"); }

        public static void DisplaySaveMap(ServerClient client) { Printer.Error($"[Save map] > {client.userFile.Label}"); }

        public static void DisplaySetMods(ServerClient client) { Printer.Error($"[Set mods] > {client.userFile.Label}"); }

        public static void DisplayRemoveMap(string value) { Printer.Error($"[Remove map] > {value}"); }

        public static void DisplayChatMap(string label, string message) { Printer.Error($"[Chat - {label}] > {message}"); }

        public static void DisplaySiteTick() { Printer.Error($"[Tick] > Sites", LogImportanceMode.Verbose); }

        public static void DisplayCaravanTick() { Printer.Error($"[Tick] > Caravans", LogImportanceMode.Verbose); }

        public static void DisplayAddSettlement(string value) { Printer.Error($"[Add settlement] > {value}"); }

        public static void DisplayRemoveSettlement(string value) { Printer.Error($"[Remove settlement] > {value}"); }

        public static void DisplayAddSite(string value) { Printer.Error($"[Add site] > {value}"); }

        public static void DisplayRemoveSite(string value) { Printer.Error($"[Remove site] > {value}"); }

        public static void DisplayAddFaction(string value) { Printer.Error($"[Add faction] > {value}"); }

        public static void DisplayRemoveFaction(string value) { Printer.Error($"[Remove faction] > {value}"); }

        public static void DisplayAddRoad(string value, string value2) { Printer.Error($"[Add road] > {value} - {value2}"); }

        public static void DisplayRemoveRoad(string value, string value2) { Printer.Error($"[Remove road] > {value} - {value2}"); }

        public static void DisplayAddCaravan(string value) { Printer.Error($"[Add caravan] > {value}"); }

        public static void DisplayRemoveCaravan(string value) { Printer.Error($"[Remove caravan] > {value}"); }

        public static void DisplayMoveCaravan(string value) { Printer.Error($"[Move caravan] > {value}", LogImportanceMode.Verbose); }

        public static void DisplaySaveFile(string value) { Printer.Error($"[Save file] > {value}"); }

        public static void DisplayLoadFile(string value) { Printer.Error($"[Load file] > {value}"); }

        public static void DisplayServerBackup(string value) { Printer.Error($"[Server Backup] > {value}"); }

        public static void DisplayUserBackup(string value) { Printer.Error($"[User Backup] > {value}"); }

        public static void DisplayResetPlayer(string value) { Printer.Error($"[Reset player] > {value}"); }

        public static void DisplayLoadEvents(string value) { Printer.Error($"[Load events] > {value}"); }

        public static void DisplayModBypass(string value) { Printer.Error($"[Mod bypass] > {value}"); }

        public static void DisplayModMismatch(string value) { Printer.Error($"[Mod mismatch] > {value}"); }

        public static void DisplayVersionMismatch(string value) { Printer.Error($"[Version mismatch] > {value}"); }

        public static void DisplayReceivePacket(string value, LogImportanceMode mode) { Printer.Error($"[Packet] > {value}", mode); }
    }
}
