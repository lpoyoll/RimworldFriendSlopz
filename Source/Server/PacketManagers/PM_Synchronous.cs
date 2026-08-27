using RTServer.Hooks.TCPNetwork;
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
        private static readonly SynchronousSessionRegistry Sessions = new();

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
            if (!Sessions.TryGetPartner(client.ID, out int partnerId))
            {
                ResponseShortcutManager.SendUnavailablePacket(client);
                return;
            }

            ServerClient partner = ServerNetwork.GetClientFromIDOrDefault(partnerId);
            if (partner == null)
            {
                Sessions.ClearClient(client.ID);
                ResponseShortcutManager.SendUnavailablePacket(client);
                return;
            }

            client.Listener.EnqueuePacket(header, data);
            partner.Listener.EnqueuePacket(header, data);
        }

        private static void TryStartSynchronousSession(ServerClient client, PKT_Synchronous data)
        {
            FL_Settlement settlement = PM_Settlements.GetSettlementFileFromTile(data.ToTile);
            if (settlement == null)
            {
                ResponseShortcutManager.SendUnavailablePacket(client);
                return;
            }

            ServerClient toFind = ServerNetwork.GetConnectedClientFromUsername(settlement.Username);

            if (toFind == null) ResponseShortcutManager.SendUserUnavailablePacket(client);
            else if (!Sessions.TryRegisterRequest(client.ID, toFind.ID)) ResponseShortcutManager.SendUnavailablePacket(client);
            else
            {
                FL_Settlement requesterSettlement = PM_Settlements.GetSettlementFileFromUsername(client.GetData<FL_Player>().Username);

                PKT_Synchronous _ = new PKT_Synchronous()
                {
                    CurrentStepMode = PKT_Synchronous.StepMode.Ask,
                    // A shared-colony guest intentionally has no second server
                    // settlement record. In that case both sides use the shared tile.
                    FromTile = requesterSettlement?.Tile ?? data.ToTile,
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
            if (!Sessions.TryAccept(client.ID, out int requesterId))
            {
                ResponseShortcutManager.SendUnavailablePacket(client);
                return;
            }

            ServerClient toFind = ServerNetwork.GetClientFromIDOrDefault(requesterId);
            if (toFind == null)
            {
                Sessions.ClearClient(client.ID);
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
            if (!Sessions.TryReject(client.ID, out int requesterId)) return;

            ServerClient toFind = ServerNetwork.GetClientFromIDOrDefault(requesterId);
            if (toFind == null) return;

            PKT_Synchronous _ = new PKT_Synchronous();
            _.CurrentStepMode = PKT_Synchronous.StepMode.Reject;
            _.FromTile = data.FromTile;
            _.ToTile = data.ToTile;

            toFind.Listener.EnqueuePacket(PacketHeader.Synchronous, _);
        }

        private static void StartSynchronousSession(ServerClient client, PKT_Synchronous data)
        {
            if (!Sessions.TryGetPartner(client.ID, out int partnerId))
            {
                ResponseShortcutManager.SendUnavailablePacket(client);
                return;
            }

            ServerClient partner = ServerNetwork.GetClientFromIDOrDefault(partnerId);
            if (partner == null)
            {
                Sessions.ClearClient(client.ID);
                ResponseShortcutManager.SendUserUnavailablePacket(client);
                return;
            }

            PKT_Synchronous _ = new PKT_Synchronous() { CurrentStepMode = PKT_Synchronous.StepMode.Start };
            partner.Listener.EnqueuePacket(PacketHeader.Synchronous, _);
        }

        public static void HandleDisconnect(ServerClient client)
        {
            Sessions.ClearClient(client.ID);
        }
    }
}
