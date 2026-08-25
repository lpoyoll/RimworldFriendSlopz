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
        [HandlesPacket(PacketHeader.Synchronous)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Synchronous data = Serializer.ConvertBytesToObject<PKT_Synchronous>(bytes);

            switch (data.CurrentStepMode)
            {
                case PKT_Synchronous.StepMode.Ask:
                    TryStartSynchronousSession(client, data);
                    break;

                case PKT_Synchronous.StepMode.Accept:
                    AcceptSynchronousSession(client, data);
                    break;

                case PKT_Synchronous.StepMode.Reject:
                    RejectSynchronousSession(client, data);
                    break;

                case PKT_Synchronous.StepMode.Start:
                    StartSynchronousSession(client, data);
                    break;

                case PKT_Synchronous.StepMode.Action:
                    RouteToManager(client, data, header);
                    break;
            }
        }

        private static void RouteToManager(ServerClient client, PKT_Synchronous data, PacketHeader header)
        {
            ServerClient peer = ServerNetwork.GetClientFromID(client.GetData<FL_Player>().SynchronousClientID);
            if (peer == null || peer.GetData<FL_Player>().SynchronousClientID != client.ID)
            {
                ResponseShortcutManager.SendIllegalPacket(client, "Synchronous action was sent without a valid paired session");
                return;
            }

            client.Listener.EnqueuePacket(header, data);
            peer.Listener.EnqueuePacket(header, data);
        }

        private static void TryStartSynchronousSession(ServerClient client, PKT_Synchronous data)
        {
            string explicitTarget = string.IsNullOrWhiteSpace(data.Username)
                ? SharedSessionManager.ConsumeNextTarget(client)
                : data.Username;
            FL_Settlement settlement = string.IsNullOrWhiteSpace(explicitTarget)
                ? PM_Settlements.GetSettlementFileFromTile(data.ToTile)
                : PM_Settlements.GetSettlementFileFromTileAndUsername(data.ToTile, explicitTarget);

            if (settlement == null)
            {
                ResponseShortcutManager.SendUserUnavailablePacket(client);
                return;
            }

            ServerClient toFind = ServerNetwork.GetConnectedClientFromUsername(settlement.Username);

            if (toFind == null) ResponseShortcutManager.SendUserUnavailablePacket(client);
            else if (toFind == client) ResponseShortcutManager.SendUnavailablePacket(client);
            else if (!InteractionMatchesDiplomacy(client, toFind, data))
            {
                PM_Chat.SendServerMessage(client, "That interaction conflicts with the current player-faction relationship.");
                ResponseShortcutManager.SendUnavailablePacket(client);
            }
            else if (!SharedSessionManager.TryRegister(client, toFind)) ResponseShortcutManager.SendUnavailablePacket(client);
            else
            {
                PKT_Synchronous _ = new PKT_Synchronous()
                {
                    CurrentStepMode = PKT_Synchronous.StepMode.Ask,
                    FromTile = PM_Settlements.GetSettlementFileFromUsername(client.GetData<FL_Player>().Username).Tile,
                    Username = client.GetData<FL_Player>().Username,
                    ToTile = data.ToTile,
                    Party = data.Party,
                    CurrentType = data.CurrentType
                };

                toFind.Listener.EnqueuePacket(PacketHeader.Synchronous, _);
            }
        }

        private static void AcceptSynchronousSession(ServerClient client, PKT_Synchronous data)
        {
            ServerClient toFind = SharedSessionManager.ConsumeRequester(client);
            if (toFind == null)
            {
                ResponseShortcutManager.SendUserUnavailablePacket(client);
                return;
            }

            client.GetData<FL_Player>().SynchronousClientID = toFind.ID;
            toFind.GetData<FL_Player>().SynchronousClientID = client.ID;

            data.CurrentStepMode = PKT_Synchronous.StepMode.Accept;
            toFind.Listener.EnqueuePacket(PacketHeader.Synchronous, data);
        }

        private static void RejectSynchronousSession(ServerClient client, PKT_Synchronous data)
        {
            ServerClient toFind = SharedSessionManager.ConsumeRequester(client);
            if (toFind == null) return;

            PKT_Synchronous _ = new PKT_Synchronous();
            _.CurrentStepMode = PKT_Synchronous.StepMode.Reject;
            _.FromTile = data.FromTile;
            _.ToTile = data.ToTile;

            toFind.Listener.EnqueuePacket(PacketHeader.Synchronous, _);
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
            PKT_Synchronous _ = new PKT_Synchronous() { CurrentStepMode = PKT_Synchronous.StepMode.Start };
            ServerNetwork.GetClientFromID(client.GetData<FL_Player>().SynchronousClientID).Listener.EnqueuePacket(PacketHeader.Synchronous, _);
        }
    }
}
