using RTServer.Hooks.TCPNetwork;
using RTServer.Core;
using RTServer.Managers;
using RTShared.Files;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using RTNetwork.Components;
using RTShared.Misc;
using RTShared.Files.Player;

namespace RTServer.PacketManagers
{
    public class PM_Synchronous : PM_Base
    {
        private const int RimjobPawnStateAction = 9022;
        private const int RimjobPawnManifestAction = 9023;
        private const int RimjobHostBuildingAction = 9030;
        private const string RimjobBuildVersion = "0.1.25";
        private const string RimjobPrivateProtocol = "RJ23";

        private static readonly object PrivateRateLock = new object();
        private static readonly Dictionary<string, long> LastPrivatePacketTicks = new Dictionary<string, long>();

        [HandlesPacket(PacketHeader.Synchronous)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Synchronous data = Serializer.ConvertBytesToObject<PKT_Synchronous>(bytes);
            string username = client.GetData<FL_Player>()?.Username ?? "<unknown>";
            int actionCode = Convert.ToInt32(data.CurrentActionType);

            // Private Rimjob state packets are deliberately excluded from the
            // stock RWT action-manager path. Older builds used First(...) to find
            // a registered action handler, which throws for our private values.
            // v0.1.23 consumes them before that can ever happen.
            if (data.CurrentStepMode == PKT_Synchronous.StepMode.Action &&
                IsRimjobPrivateAction(actionCode))
            {
                RouteRimjobPrivateAction(client, data, header, actionCode);
                return;
            }

            Printer.Message($"[SYNC] RX | User={username} | Step={data.CurrentStepMode} | Type={data.CurrentType} | Action={data.CurrentActionType} | From={data.FromTile} | To={data.ToTile} | Target={data.Username ?? "<none>"} | Bytes={(data.Data?.Length ?? 0)}", Printer.Verbosity.Verbose);

            switch (data.CurrentStepMode)
            {
                case PKT_Synchronous.StepMode.Ask: TryStartSynchronousSession(client, data); break;
                case PKT_Synchronous.StepMode.Accept: AcceptSynchronousSession(client, data); break;
                case PKT_Synchronous.StepMode.Reject: RejectSynchronousSession(client, data); break;
                case PKT_Synchronous.StepMode.Start: StartSynchronousSession(client, data); break;
                case PKT_Synchronous.StepMode.Action: RouteToManager(client, data, header); break;
            }
        }

        private static bool IsRimjobPrivateAction(int actionCode) =>
            actionCode == RimjobPawnStateAction ||
            actionCode == RimjobPawnManifestAction ||
            actionCode == RimjobHostBuildingAction;

        private static void RouteRimjobPrivateAction(ServerClient client, PKT_Synchronous data, PacketHeader header, int actionCode)
        {
            FL_Player player = client.GetData<FL_Player>();
            string username = player?.Username ?? "<unknown>";
            int peerId = player?.SynchronousClientID ?? int.MinValue;
            ServerClient peer = ServerNetwork.GetClientFromID(peerId);

            if (peer == null || peer.GetData<FL_Player>()?.SynchronousClientID != client.ID)
            {
                // Do not throw and do not bounce a high-frequency error packet.
                // One concise server line is enough; the client diagnostics will
                // also show that no paired session exists.
                Printer.Message($"[RIMJOB-RELAY] Dropped private action {actionCode}: no valid pair | User={username} | PeerId={peerId}", Printer.Verbosity.Verbose);
                return;
            }

            // The mutual pair was established only after the server validated the
            // requested settlements and target. Repeating a username-to-settlement
            // lookup for every private packet is both redundant and incorrect when
            // a player has historical/multiple settlement records: one direction
            // can resolve a different record and silently lose its pawn stream.
            // The live mutual client-ID pair is the authority for this session.
            data.Username = username;

            int payloadLength = data.Data?.Length ?? 0;
            int maximumPayload = actionCode == RimjobPawnStateAction
                ? 1024 * 1024
                : actionCode == RimjobPawnManifestAction
                    ? 24 * 1024 * 1024
                    : 8 * 1024 * 1024;
            if (payloadLength <= 0 || payloadLength > maximumPayload)
            {
                Printer.Message($"[RIMJOB-RELAY] Dropped private action {actionCode}: invalid payload size {payloadLength}", Printer.Verbosity.Verbose);
                return;
            }

            long now = DateTime.UtcNow.Ticks;
            long minimumInterval = actionCode == RimjobPawnStateAction
                ? TimeSpan.TicksPerMillisecond * 100
                : actionCode == RimjobPawnManifestAction
                    ? TimeSpan.TicksPerSecond * 5
                    : TimeSpan.TicksPerMillisecond * 500;
            string rateKey = client.ID + ":" + actionCode;
            lock (PrivateRateLock)
            {
                if (LastPrivatePacketTicks.TryGetValue(rateKey, out long last) && now - last < minimumInterval)
                    return;
                LastPrivatePacketTicks[rateKey] = now;
            }

            // Private state is peer-only. Echoing it back to its authoritative
            // sender doubles traffic for no useful purpose.
            peer.Listener.EnqueuePacket(header, data);

            if (actionCode != RimjobPawnStateAction)
            {
                Printer.Message($"[RIMJOB-RELAY] Private action relayed | From={username} | To={peer.GetData<FL_Player>().Username} | Action={actionCode} | Bytes={payloadLength}", Printer.Verbosity.Verbose);
            }
        }

        private static void RouteToManager(ServerClient client, PKT_Synchronous data, PacketHeader header)
        {
            string username = client.GetData<FL_Player>().Username;
            int peerId = client.GetData<FL_Player>().SynchronousClientID;
            ServerClient peer = ServerNetwork.GetClientFromID(peerId);
            if (peer == null || peer.GetData<FL_Player>().SynchronousClientID != client.ID)
            {
                string reason = $"Synchronous action '{data.CurrentActionType}' was sent without a valid paired session. LocalPeerId={peerId}; PeerFound={peer != null}.";
                Printer.Message($"[SYNC] Action route rejected | User={username} | {reason}", Printer.Verbosity.Verbose);
                ResponseShortcutManager.SendIllegalPacket(client, reason, context: $"Step=Action; Action={data.CurrentActionType}; From={data.FromTile}; To={data.ToTile}");
                return;
            }

            Printer.Message($"[SYNC] Routing action | From={username} | To={peer.GetData<FL_Player>().Username} | Action={data.CurrentActionType}", Printer.Verbosity.Verbose);
            client.Listener.EnqueuePacket(header, data);
            peer.Listener.EnqueuePacket(header, data);
        }

        private static void TryStartSynchronousSession(ServerClient client, PKT_Synchronous data)
        {
            string requesterUsername = client.GetData<FL_Player>().Username;
            string explicitTarget = string.IsNullOrWhiteSpace(data.Username)
                ? SharedSessionManager.ConsumeNextTarget(client)
                : data.Username;

            Printer.Message($"[SYNC] Session lookup | Requester={requesterUsername} | ToTile={data.ToTile} | ExplicitTarget={explicitTarget ?? "<none>"} | Type={data.CurrentType}", Printer.Verbosity.Verbose);

            FL_Settlement requesterSettlement = PM_Settlements.GetSettlementFileFromUsername(requesterUsername);
            if (requesterSettlement == null)
            {
                string reason = $"Your account '{requesterUsername}' has no registered settlement, so a shared-map session cannot start.";
                Printer.Message($"[SYNC] Session lookup failed | Requester={requesterUsername} | Reason=Requester settlement missing", Printer.Verbosity.Verbose);
                ResponseShortcutManager.SendUnavailablePacket(client, reason, $"SyncAsk; Target={explicitTarget ?? "<none>"}; ToTile={data.ToTile}");
                return;
            }

            FL_Settlement settlement = string.IsNullOrWhiteSpace(explicitTarget)
                ? PM_Settlements.GetSettlementFileFromTile(data.ToTile)
                : PM_Settlements.GetSettlementFileFromTileAndUsername(data.ToTile, explicitTarget);

            if (settlement == null)
            {
                string reason = string.IsNullOrWhiteSpace(explicitTarget)
                    ? $"No player settlement is registered on tile {data.ToTile}."
                    : $"Player '{explicitTarget}' does not have a registered settlement on tile {data.ToTile}.";
                Printer.Message($"[SYNC] Session lookup failed | Requester={requesterUsername} | Target={explicitTarget ?? "<tile default>"} | ToTile={data.ToTile} | Reason=Settlement not found", Printer.Verbosity.Verbose);
                ResponseShortcutManager.SendUserUnavailablePacket(client, reason, $"SyncAsk; RequesterTile={requesterSettlement.Tile}; Target={explicitTarget ?? "<none>"}; ToTile={data.ToTile}");
                return;
            }

            ServerClient targetClient = ServerNetwork.GetConnectedClientFromUsername(settlement.Username);
            Printer.Message($"[SYNC] Target resolved | Requester={requesterUsername} | RequesterTile={requesterSettlement.Tile} | Target={settlement.Username} | TargetTile={settlement.Tile} | TargetOnline={targetClient != null}", Printer.Verbosity.Verbose);

            if (targetClient == null)
            {
                ResponseShortcutManager.SendUserUnavailablePacket(client,
                    $"Player '{settlement.Username}' owns the requested map but is not currently connected. Load the canonical host first, then connect the other shared-tile member.",
                    $"SyncAsk; Requester={requesterUsername}; RequesterTile={requesterSettlement.Tile}; TargetTile={settlement.Tile}");
            }
            else if (targetClient == client)
            {
                ResponseShortcutManager.SendUnavailablePacket(client,
                    $"The shared-map request resolved back to your own account '{requesterUsername}' instead of another tile member.",
                    $"SyncAsk; RequesterTile={requesterSettlement.Tile}; TargetTile={settlement.Tile}; Target={settlement.Username}");
            }
            else if (!InteractionMatchesDiplomacy(client, targetClient, data))
            {
                SharedColonyStance stance = SharedColonyManager.GetEffectiveStance(requesterUsername, targetClient.GetData<FL_Player>().Username);
                string reason = $"Interaction type '{data.CurrentType}' is blocked by the current player-faction relationship ({stance}).";
                PM_Chat.SendServerMessage(client, reason);
                ResponseShortcutManager.SendUnavailablePacket(client, reason,
                    $"SyncAsk; Target={targetClient.GetData<FL_Player>().Username}; ToTile={data.ToTile}; Stance={stance}");
            }
            else if (!SharedSessionManager.TryRegister(client, targetClient))
            {
                ResponseShortcutManager.SendUnavailablePacket(client,
                    $"Player '{targetClient.GetData<FL_Player>().Username}' already has a pending synchronous session request. Wait for it to complete or time out, then retry.",
                    $"SyncAsk; Requester={requesterUsername}; ToTile={data.ToTile}");
            }
            else
            {
                PKT_Synchronous forwarded = new PKT_Synchronous()
                {
                    CurrentStepMode = PKT_Synchronous.StepMode.Ask,
                    FromTile = requesterSettlement.Tile,
                    Username = requesterUsername,
                    ToTile = data.ToTile,
                    Party = data.Party,
                    CurrentType = data.CurrentType
                };

                targetClient.Listener.EnqueuePacket(PacketHeader.Synchronous, forwarded);
                Printer.Message($"[SYNC] Ask forwarded | Requester={requesterUsername} | FromTile={requesterSettlement.Tile} | Target={targetClient.GetData<FL_Player>().Username} | ToTile={data.ToTile}", Printer.Verbosity.Verbose);
            }
        }

        private static void AcceptSynchronousSession(ServerClient client, PKT_Synchronous data)
        {
            string targetUsername = client.GetData<FL_Player>().Username;
            ServerClient requester = SharedSessionManager.ConsumeRequester(client);
            if (requester == null)
            {
                Printer.Message($"[SYNC] Accept rejected | Target={targetUsername} | Reason=No pending requester", Printer.Verbosity.Verbose);
                ResponseShortcutManager.SendUserUnavailablePacket(client,
                    "The synchronous accept arrived after the pending requester disappeared or expired.",
                    $"SyncAccept; Target={targetUsername}");
                return;
            }

            client.GetData<FL_Player>().SynchronousClientID = requester.ID;
            requester.GetData<FL_Player>().SynchronousClientID = client.ID;

            // Private sync is opt-in and versioned. This announcement is queued
            // before Accept reaches the requester, so the client never starts its
            // private state publisher against an incompatible server.
            string buildProtocol = $"{SharedColonyManager.ProtocolPrefix}|BUILD|{RimjobBuildVersion}|{RimjobPrivateProtocol}";
            PM_Chat.SendProtocolMessage(client, buildProtocol);
            PM_Chat.SendProtocolMessage(requester, buildProtocol);

            Printer.Message($"[SYNC] Pair established | Requester={requester.GetData<FL_Player>().Username}#{requester.ID} | Host={targetUsername}#{client.ID} | PrivateProtocol={RimjobPrivateProtocol}", Printer.Verbosity.Verbose);

            data.CurrentStepMode = PKT_Synchronous.StepMode.Accept;
            requester.Listener.EnqueuePacket(PacketHeader.Synchronous, data);
        }

        private static void RejectSynchronousSession(ServerClient client, PKT_Synchronous data)
        {
            ServerClient requester = SharedSessionManager.ConsumeRequester(client);
            if (requester == null)
            {
                Printer.Message($"[SYNC] Reject received with no pending requester | User={client.GetData<FL_Player>().Username}", Printer.Verbosity.Verbose);
                return;
            }

            PKT_Synchronous packet = new PKT_Synchronous();
            packet.CurrentStepMode = PKT_Synchronous.StepMode.Reject;
            packet.FromTile = data.FromTile;
            packet.ToTile = data.ToTile;
            requester.Listener.EnqueuePacket(PacketHeader.Synchronous, packet);
            Printer.Message($"[SYNC] Session rejected by target | Target={client.GetData<FL_Player>().Username} | Requester={requester.GetData<FL_Player>().Username}", Printer.Verbosity.Verbose);
        }

        private static bool InteractionMatchesDiplomacy(ServerClient source, ServerClient target, PKT_Synchronous data)
        {
            if (!SharedColonyManager.Enabled || !Master.ServerConfig.EnforceSharedColonyDiplomacy) return true;

            string sourceUsername = source.GetData<FL_Player>().Username;
            string targetUsername = target.GetData<FL_Player>().Username;
            SharedColonyStance stance = SharedColonyManager.GetEffectiveStance(sourceUsername, targetUsername);
            bool isFriendlyInteraction = Convert.ToInt32(data.CurrentType) == 0;

            if (stance == SharedColonyStance.Hostile) return !isFriendlyInteraction;
            if (stance == SharedColonyStance.Ally) return isFriendlyInteraction;
            return true;
        }

        private static void StartSynchronousSession(ServerClient client, PKT_Synchronous data)
        {
            int peerId = client.GetData<FL_Player>().SynchronousClientID;
            ServerClient peer = ServerNetwork.GetClientFromID(peerId);
            if (peer == null)
            {
                ResponseShortcutManager.SendUnavailablePacket(client,
                    "The shared session start packet has no connected paired client.",
                    $"SyncStart; PeerId={peerId}");
                return;
            }

            PKT_Synchronous packet = new PKT_Synchronous() { CurrentStepMode = PKT_Synchronous.StepMode.Start };
            peer.Listener.EnqueuePacket(PacketHeader.Synchronous, packet);
            Printer.Message($"[SYNC] Session start forwarded | From={client.GetData<FL_Player>().Username} | To={peer.GetData<FL_Player>().Username}", Printer.Verbosity.Verbose);
        }
    }
}
