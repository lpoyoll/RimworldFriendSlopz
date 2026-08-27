using System.Text;
using RTNetwork.Components;
using RTNetwork.Packets;
using RTServer.Hooks.TCPNetwork;
using RTServer.PacketManagers;
using RTShared.Files.Player;
using RTShared.Misc;
using static RTNetwork.Packets.PKT_ResponseShortcut;

namespace RTServer.Managers
{
    public static class ResponseShortcutManager
    {
        public static void SendIllegalPacket(ServerClient client, string message = "", bool shouldBroadcast = true, string context = null)
        {
            SendDiagnostic(client, ResponseStepMode.IllegalAction,
                string.IsNullOrWhiteSpace(message) ? "The server rejected this packet as an illegal action." : message,
                context);

            PKT_ResponseShortcut data = new PKT_ResponseShortcut();
            data._stepMode = ResponseStepMode.IllegalAction;

            client.Listener.EnqueuePacket(PacketHeader.ResponseShortcut, data);
            client.Listener.MarkForDisconnect();

            if (shouldBroadcast)
            {
                Printer.Warning($"[Illegal action] > {SafeUsername(client)} > {client.IP}");
                Printer.Warning($"[Illegal reason] > {message}");
            }
        }

        public static void SendUserUnavailablePacket(ServerClient client, string reason = null, string context = null)
        {
            SendDiagnostic(client, ResponseStepMode.UserUnavailable,
                string.IsNullOrWhiteSpace(reason) ? "The requested player or player-owned settlement is not currently available." : reason,
                context);

            PKT_ResponseShortcut data = new PKT_ResponseShortcut();
            data._stepMode = ResponseStepMode.UserUnavailable;
            client.Listener.EnqueuePacket(PacketHeader.ResponseShortcut, data);
        }

        public static void SendUnavailablePacket(ServerClient client, string reason = null, string context = null)
        {
            SendDiagnostic(client, ResponseStepMode.Unavailable,
                string.IsNullOrWhiteSpace(reason) ? "The server rejected this action because the current session state does not permit it." : reason,
                context);

            PKT_ResponseShortcut data = new PKT_ResponseShortcut();
            data._stepMode = ResponseStepMode.Unavailable;
            client.Listener.EnqueuePacket(PacketHeader.ResponseShortcut, data);
        }

        public static void SendBreakPacket(ServerClient client)
        {
            PKT_ResponseShortcut data = new PKT_ResponseShortcut();
            data._stepMode = ResponseStepMode.Pop;
            client.Listener.EnqueuePacket(PacketHeader.ResponseShortcut, data);
        }

        public static void SendNoPowerPacket(ServerClient client, string reason = null, string context = null)
        {
            SendDiagnostic(client, ResponseStepMode.NoPower,
                string.IsNullOrWhiteSpace(reason) ? "The server does not have enough action power available for this request." : reason,
                context);

            PKT_ResponseShortcut data = new PKT_ResponseShortcut();
            data._stepMode = ResponseStepMode.NoPower;
            client.Listener.EnqueuePacket(PacketHeader.ResponseShortcut, data);
        }

        private static void SendDiagnostic(ServerClient client, ResponseStepMode stepMode, string reason, string context)
        {
            if (client == null) return;

            string username = SafeUsername(client);
            int synchronousPeerId = -1;
            try { synchronousPeerId = client.GetData<FL_Player>().SynchronousClientID; }
            catch { }

            string serverContext = $"User={username}; IP={client.IP}; SyncPeerId={synchronousPeerId}";
            if (!string.IsNullOrWhiteSpace(context)) serverContext += $"; Context={context}";

            string encodedReason = Convert.ToBase64String(Encoding.UTF8.GetBytes(reason ?? string.Empty));
            string encodedContext = Convert.ToBase64String(Encoding.UTF8.GetBytes(serverContext));

            try
            {
                PM_Chat.SendProtocolMessage(client,
                    $"{SharedColonyManager.ProtocolPrefix}|ERROR|{stepMode}|{encodedReason}|{encodedContext}");
            }
            catch (Exception ex)
            {
                Printer.Warning($"[ACTION-DENY] Unable to send client diagnostic | User={username} | Step={stepMode} | Error={ex.Message}");
            }

            Printer.Message($"[ACTION-DENY] User={username} | IP={client.IP} | Step={stepMode} | Reason={reason} | {serverContext}",
                Printer.Verbosity.Verbose);
        }

        private static string SafeUsername(ServerClient client)
        {
            try
            {
                string username = client.GetData<FL_Player>()?.Username;
                return string.IsNullOrWhiteSpace(username) ? "<unknown>" : username;
            }
            catch
            {
                return "<unknown>";
            }
        }
    }
}
