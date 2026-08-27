using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RTClient.Dialogs.Default;
using RTClient.Managers;
using RTNetwork.Packets;
using RTShared.Misc;
using UnityEngine;
using Verse;

namespace RWTSharedColony
{
    public sealed class RimjobServerError
    {
        public string Step { get; set; }
        public string Reason { get; set; }
        public string ServerContext { get; set; }
        public DateTime ReceivedUtc { get; set; }
    }

    /// <summary>
    /// Client-side diagnostic sink used by the F9 diagnostics window and by
    /// verbose action/session errors. Important/errors are always persisted;
    /// high-volume packet/session traces are controlled by VerboseEnabled.
    /// </summary>
    public static class RimjobClientDiagnostics
    {
        private const int MaxLines = 2000;
        private static readonly object Sync = new object();
        private static readonly List<string> Lines = new List<string>();
        private static readonly string LogDirectory;
        private static readonly string ConfigPath;
        private static readonly string LogPathValue;

        public static bool VerboseEnabled { get; private set; }
        public static RimjobServerError PendingServerError { get; private set; }
        public static RimjobServerError LastServerError { get; private set; }
        public static string LogPath => LogPathValue;
        public static string LogsFolder => LogDirectory;

        static RimjobClientDiagnostics()
        {
            try
            {
                LogDirectory = Path.Combine(GenFilePaths.SaveDataFolderPath, "Rimjob", "Logs");
                Directory.CreateDirectory(LogDirectory);
                ConfigPath = Path.Combine(LogDirectory, "client-diagnostics.cfg");
                LogPathValue = Path.Combine(LogDirectory, $"Client-{DateTime.Now:yyyyMMdd-HHmmss}.log");
                VerboseEnabled = File.Exists(ConfigPath) &&
                                 File.ReadAllText(ConfigPath).Trim().Equals("verbose=1", StringComparison.OrdinalIgnoreCase);
                Important($"Client diagnostics initialised. Verbose={VerboseEnabled}. Log={LogPathValue}");
            }
            catch (Exception exception)
            {
                LogDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rimjob", "Logs");
                Directory.CreateDirectory(LogDirectory);
                ConfigPath = Path.Combine(LogDirectory, "client-diagnostics.cfg");
                LogPathValue = Path.Combine(LogDirectory, $"Client-{DateTime.Now:yyyyMMdd-HHmmss}.log");
                Write("ERROR", $"Diagnostics fallback path used: {exception.Message}", true);
            }
        }

        public static void SetVerbose(bool enabled)
        {
            VerboseEnabled = enabled;
            try { File.WriteAllText(ConfigPath, enabled ? "verbose=1" : "verbose=0"); }
            catch (Exception exception) { Log.Warning($"[Rimjob] Could not persist verbose logging setting: {exception.Message}"); }
            Important($"Verbose client logging {(enabled ? "enabled" : "disabled")}.");
        }

        public static void ToggleVerbose() => SetVerbose(!VerboseEnabled);

        public static void Important(string message) => Write("INFO", message, true);
        public static void Error(string message) => Write("ERROR", message, true);
        public static void Verbose(string message)
        {
            if (VerboseEnabled) Write("VERBOSE", message, true);
        }

        public static string[] SnapshotLines()
        {
            lock (Sync) return Lines.ToArray();
        }

        public static void ClearVisibleLog()
        {
            lock (Sync) Lines.Clear();
            Important("Visible diagnostic buffer cleared.");
        }

        public static void CopyVisibleLog()
        {
            GUIUtility.systemCopyBuffer = string.Join(Environment.NewLine, SnapshotLines());
        }

        public static void OpenLogsFolder()
        {
            try { Process.Start(LogDirectory); }
            catch (Exception exception) { Error($"Could not open logs folder: {exception.Message}"); }
        }

        public static bool HandleProtocolPacket(PKT_Chat packet)
        {
            if (packet == null || packet.Username != SharedColonyState.ProtocolUsername ||
                string.IsNullOrWhiteSpace(packet.Message) ||
                !packet.Message.StartsWith(SharedColonyState.ProtocolPrefix, StringComparison.Ordinal))
                return false;

            string[] parts = packet.Message.Split('|');
            string kind = parts.Length > 1 ? parts[1] : "UNKNOWN";
            Verbose($"Protocol RX kind={kind} raw={packet.Message}");

            if (kind == "ERROR" && parts.Length >= 5)
            {
                RimjobServerError error = new RimjobServerError
                {
                    Step = parts[2],
                    Reason = Decode(parts[3]),
                    ServerContext = Decode(parts[4]),
                    ReceivedUtc = DateTime.UtcNow
                };
                PendingServerError = error;
                LastServerError = error;
                Error($"Server action rejection received. Step={error.Step}; Reason={error.Reason}; Context={error.ServerContext}");
                return true;
            }

            if (kind == "TILE" || kind == "SETTLED")
                Important($"Shared-session protocol received: {packet.Message}");

            return false;
        }

        public static RimjobServerError ConsumePendingServerError(string expectedStep)
        {
            RimjobServerError error = PendingServerError;
            if (error == null) return null;
            if (DateTime.UtcNow - error.ReceivedUtc > TimeSpan.FromSeconds(10))
            {
                PendingServerError = null;
                return null;
            }

            // ERROR is sent immediately before ResponseShortcut over the same TCP
            // stream. Prefer a matching step but still consume the latest detail
            // when an older server uses a slightly different enum spelling.
            PendingServerError = null;
            return error;
        }

        public static string BuildClientState()
        {
            try
            {
                int tile = Current.Game?.CurrentMap?.Tile.tileId ?? -1;
                return $"User={SessionManager.Username ?? "<unknown>"}; " +
                       $"Network={SessionManager.CurrentNetworkState}; Ready={SessionManager.IsReadyToPlay}; " +
                       $"Admin={SessionManager.IsAdmin}; CurrentTile={tile}; " +
                       $"PendingTile={SharedTileLiveSync.PendingTile}; PendingHost={SharedTileLiveSync.PendingHostUsername ?? "<none>"}; " +
                       $"AwaitingAccept={SharedTileLiveSync.AwaitingAccept}; GuestActive={SharedTileLiveSync.SharedGuestActive}; " +
                       $"HostActive={SharedTileLiveSync.SharedHostActiveOrPending}; SyncHost={SessionManager.IsSynchronousHost}; " +
                       $"SyncMapTile={(SessionManager.SynchronousMap?.Tile.tileId ?? -1)}";
            }
            catch (Exception exception)
            {
                return "Unable to collect client state: " + exception.Message;
            }
        }

        private static string Decode(string value)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
            catch { return value ?? string.Empty; }
        }

        private static void Write(string level, string message, bool persist)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}";
            lock (Sync)
            {
                Lines.Add(line);
                if (Lines.Count > MaxLines) Lines.RemoveRange(0, Lines.Count - MaxLines);
                if (persist)
                {
                    try { File.AppendAllText(LogPathValue, line + Environment.NewLine); }
                    catch { }
                }
            }

            if (level == "ERROR") Log.Error("[Rimjob] " + message);
            else if (level == "INFO" && VerboseEnabled) Log.Message("[Rimjob] " + message);
        }
    }

    public sealed class RimjobActionErrorWindow : Window
    {
        private readonly string Body;
        private Vector2 ScrollPosition;

        public override Vector2 InitialSize => new Vector2(760f, 520f);

        public RimjobActionErrorWindow(string body)
        {
            Body = body ?? "Unknown Rimjob action error.";
            doCloseX = true;
            closeOnAccept = true;
            closeOnCancel = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 36f), "Rimjob - Action rejected");
            Text.Font = GameFont.Small;

            Rect outer = new Rect(0f, 44f, inRect.width, inRect.height - 92f);
            float textHeight = Math.Max(outer.height, Text.CalcHeight(Body, outer.width - 24f) + 20f);
            Rect view = new Rect(0f, 0f, outer.width - 18f, textHeight);
            Widgets.BeginScrollView(outer, ref ScrollPosition, view);
            Widgets.Label(new Rect(6f, 4f, view.width - 12f, textHeight), Body);
            Widgets.EndScrollView();

            if (Widgets.ButtonText(new Rect(0f, inRect.height - 40f, 150f, 34f), "Copy details"))
                GUIUtility.systemCopyBuffer = Body;
            if (Widgets.ButtonText(new Rect(160f, inRect.height - 40f, 150f, 34f), "Open log folder"))
                RimjobClientDiagnostics.OpenLogsFolder();
            if (Widgets.ButtonText(new Rect(inRect.width - 120f, inRect.height - 40f, 120f, 34f), "Close"))
                Close();
        }
    }

    public sealed class RimjobDiagnosticsWindow : Window
    {
        private enum Tab
        {
            Session,
            Network,
            VerboseLog
        }

        private static RimjobDiagnosticsWindow Instance;
        private Tab CurrentTab = Tab.Session;
        private Vector2 ScrollPosition;

        public override Vector2 InitialSize => new Vector2(900f, 650f);

        public RimjobDiagnosticsWindow()
        {
            Instance = this;
            doCloseX = true;
            closeOnCancel = true;
            draggable = true;
            resizeable = true;
        }

        public override void PostClose()
        {
            if (ReferenceEquals(Instance, this)) Instance = null;
            base.PostClose();
        }

        public static void Toggle()
        {
            if (Instance != null)
            {
                Instance.Close();
                return;
            }
            Find.WindowStack.Add(new RimjobDiagnosticsWindow());
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 32f), "Rimjob Diagnostics");
            Text.Font = GameFont.Small;

            float tabY = 38f;
            float tabWidth = 160f;
            DrawTab(new Rect(0f, tabY, tabWidth, 34f), "Session", Tab.Session);
            DrawTab(new Rect(tabWidth + 8f, tabY, tabWidth, 34f), "Network", Tab.Network);
            DrawTab(new Rect((tabWidth + 8f) * 2f, tabY, tabWidth, 34f), "Verbose Log", Tab.VerboseLog);

            Rect body = new Rect(0f, 82f, inRect.width, inRect.height - 82f);
            Widgets.DrawMenuSection(body);
            body = body.ContractedBy(12f);

            switch (CurrentTab)
            {
                case Tab.Session:
                    DrawSession(body);
                    break;
                case Tab.Network:
                    DrawNetwork(body);
                    break;
                case Tab.VerboseLog:
                    DrawVerboseLog(body);
                    break;
            }
        }

        private void DrawTab(Rect rect, string label, Tab tab)
        {
            if (Widgets.ButtonText(rect, (CurrentTab == tab ? "• " : "") + label)) CurrentTab = tab;
        }

        private static void DrawSession(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);
            listing.Label("Client/session state");
            listing.GapLine();
            listing.Label("Username: " + (SessionManager.Username ?? "<not logged in>"));
            listing.Label("Network state: " + SessionManager.CurrentNetworkState);
            listing.Label("Ready to play: " + SessionManager.IsReadyToPlay);
            listing.Label("Admin: " + SessionManager.IsAdmin);
            listing.Label("Current map tile: " + (Current.Game?.CurrentMap?.Tile.tileId ?? -1));
            listing.Label("Shared pending tile: " + SharedTileLiveSync.PendingTile);
            listing.Label("Shared canonical host: " + (SharedTileLiveSync.PendingHostUsername ?? "<none>"));
            listing.Label("Awaiting host accept: " + SharedTileLiveSync.AwaitingAccept);
            listing.Label("Shared guest active: " + SharedTileLiveSync.SharedGuestActive);
            listing.Label("Shared host active/pending: " + SharedTileLiveSync.SharedHostActiveOrPending);
            listing.Label("Synchronous host: " + SessionManager.IsSynchronousHost);
            listing.Label("Synchronous map tile: " + (SessionManager.SynchronousMap?.Tile.tileId ?? -1));
            listing.Gap();
            listing.Label("F9 toggles this diagnostics window.");
            listing.End();
        }

        private static void DrawNetwork(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);
            listing.Label("Network / last rejection");
            listing.GapLine();
            listing.Label("Server endpoint present: " + (Network.ServerEndpoint != null));
            listing.Label("Network state: " + SessionManager.CurrentNetworkState);
            listing.Label("Server player count: " + SessionManager.CurrentServerPlayers);
            listing.Label("Verbose logging: " + RimjobClientDiagnostics.VerboseEnabled);
            listing.Gap();

            RimjobServerError error = RimjobClientDiagnostics.LastServerError;
            if (error == null)
            {
                listing.Label("Last server rejection: <none received this session>");
            }
            else
            {
                listing.Label("Last server rejection: " + error.Step);
                listing.Label("Reason: " + error.Reason);
                listing.Label("Server context: " + error.ServerContext);
                listing.Label("Received UTC: " + error.ReceivedUtc.ToString("u"));
            }
            listing.Gap();
            listing.Label("Current client state:");
            listing.Label(RimjobClientDiagnostics.BuildClientState());
            listing.End();
        }

        private void DrawVerboseLog(Rect rect)
        {
            bool enabled = RimjobClientDiagnostics.VerboseEnabled;
            string buttonLabel = enabled ? "Disable verbose logging" : "Enable verbose logging";
            if (Widgets.ButtonText(new Rect(0f, 0f, 190f, 34f), buttonLabel))
                RimjobClientDiagnostics.ToggleVerbose();
            if (Widgets.ButtonText(new Rect(200f, 0f, 120f, 34f), "Copy log"))
                RimjobClientDiagnostics.CopyVisibleLog();
            if (Widgets.ButtonText(new Rect(330f, 0f, 120f, 34f), "Clear view"))
                RimjobClientDiagnostics.ClearVisibleLog();
            if (Widgets.ButtonText(new Rect(460f, 0f, 150f, 34f), "Open log folder"))
                RimjobClientDiagnostics.OpenLogsFolder();

            Widgets.Label(new Rect(0f, 40f, rect.width, 28f), "Log file: " + RimjobClientDiagnostics.LogPath);
            Widgets.Label(new Rect(0f, 66f, rect.width, 28f),
                "Errors and shared-session milestones are always logged. Verbose mode also records protocol and synchronous packet decisions.");

            string[] lines = RimjobClientDiagnostics.SnapshotLines();
            string text = string.Join(Environment.NewLine, lines);
            Rect outer = new Rect(0f, 100f, rect.width, rect.height - 100f);
            float height = Math.Max(outer.height, Text.CalcHeight(text, outer.width - 24f) + 20f);
            Rect view = new Rect(0f, 0f, outer.width - 18f, height);
            Widgets.BeginScrollView(outer, ref ScrollPosition, view);
            Widgets.Label(new Rect(4f, 4f, view.width - 8f, height), text);
            Widgets.EndScrollView();
        }
    }

    [HarmonyPatch]
    public static class RimjobF9DiagnosticsPatch
    {
        public static MethodBase TargetMethod() => AccessTools.Method(AccessTools.TypeByName("Verse.Root_Play"), "Update");

        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Input.GetKeyDown(KeyCode.F9)) RimjobDiagnosticsWindow.Toggle();
        }
    }

    [HarmonyPatch]
    public static class RimjobProtocolDiagnosticsPatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("RTClient.PacketManagers.PM_Chat"), "Receive");

        [HarmonyPriority(Priority.First)]
        public static void Prefix(object[] __args)
        {
            try
            {
                byte[] bytes = __args.OfType<byte[]>().FirstOrDefault();
                if (bytes == null) return;
                PKT_Chat packet = Serializer.ConvertBytesToObject<PKT_Chat>(bytes);
                RimjobClientDiagnostics.HandleProtocolPacket(packet);
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Error("Failed to inspect protocol packet: " + exception.Message);
            }
        }
    }

    [HarmonyPatch]
    public static class RimjobSynchronousDiagnosticsPatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("RTClient.PacketManagers.Synchronous.PM_Synchronous"), "Receive");

        [HarmonyPriority(Priority.First)]
        public static void Prefix(object[] __args)
        {
            if (!RimjobClientDiagnostics.VerboseEnabled) return;
            try
            {
                byte[] bytes = __args.OfType<byte[]>().FirstOrDefault();
                if (bytes == null) return;
                PKT_Synchronous packet = Serializer.ConvertBytesToObject<PKT_Synchronous>(bytes);
                RimjobClientDiagnostics.Verbose(
                    $"Synchronous RX step={packet.CurrentStepMode}; type={packet.CurrentType}; action={packet.CurrentActionType}; " +
                    $"from={packet.FromTile}; to={packet.ToTile}; user={packet.Username ?? "<none>"}; data={(packet.Data?.Length ?? 0)} bytes");
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Error("Failed to inspect synchronous packet: " + exception.Message);
            }
        }
    }

    [HarmonyPatch]
    public static class RimjobVerboseResponseShortcutPatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("RTClient.PacketManagers.PM_ResponseShortcuts"), "Receive");

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(object[] __args)
        {
            try
            {
                byte[] bytes = __args.OfType<byte[]>().FirstOrDefault();
                if (bytes == null) return true;
                PKT_ResponseShortcut packet = Serializer.ConvertBytesToObject<PKT_ResponseShortcut>(bytes);
                if (packet._stepMode == PKT_ResponseShortcut.ResponseStepMode.Pop) return true;

                RimjobServerError serverError = RimjobClientDiagnostics.ConsumePendingServerError(packet._stepMode.ToString());
                string fallback;
                switch (packet._stepMode)
                {
                    case PKT_ResponseShortcut.ResponseStepMode.IllegalAction:
                        fallback = "The server classified the request as an illegal action and disconnected the client.";
                        break;
                    case PKT_ResponseShortcut.ResponseStepMode.UserUnavailable:
                        fallback = "The requested player or settlement is not currently available.";
                        break;
                    case PKT_ResponseShortcut.ResponseStepMode.Unavailable:
                        fallback = "The requested action is not available in the current server/session state.";
                        break;
                    case PKT_ResponseShortcut.ResponseStepMode.NoPower:
                        fallback = "There is not enough action power available for this request.";
                        break;
                    default:
                        return true;
                }

                string reason = string.IsNullOrWhiteSpace(serverError?.Reason) ? fallback : serverError.Reason;
                string serverContext = string.IsNullOrWhiteSpace(serverError?.ServerContext)
                    ? "No detailed server context was supplied. Enable server verbosity 2 and client verbose logging for the next attempt."
                    : serverError.ServerContext;
                string clientState = RimjobClientDiagnostics.BuildClientState();

                string body =
                    $"Response: {packet._stepMode}\n\n" +
                    $"Reason: {reason}\n\n" +
                    $"Server context: {serverContext}\n\n" +
                    $"Client state: {clientState}\n\n" +
                    $"Client log: {RimjobClientDiagnostics.LogPath}";

                RimjobClientDiagnostics.Error($"Action rejected. Response={packet._stepMode}; Reason={reason}; Server={serverContext}; Client={clientState}");
                try { DLG_Wait.Instance?.Close(true); } catch { }
                Find.WindowStack.Add(new RimjobActionErrorWindow(body));
                return false;
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Error("Verbose action-error handler failed: " + exception);
                return true;
            }
        }
    }
}
