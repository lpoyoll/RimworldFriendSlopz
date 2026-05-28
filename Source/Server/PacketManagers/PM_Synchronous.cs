using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using GameServer.PacketManager;
using Shared;
using Shared.Files;
using TCPNetwork;
using Shared.Files.ServerClient;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;

namespace GameServer.PacketManagers
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
            client.Listener.EnqueuePacket(header, data);
            ServerNetwork.GetClientFromID(client.GetData<UserFile>().SynchronousClientID).Listener.EnqueuePacket(header, data);
        }

        private static void TryStartSynchronousSession(ServerClient client, PKT_Synchronous data)
        {
            FL_Settlement settlement = PM_Settlements.GetSettlementFileFromTile(data.ToTile);
            ServerClient toFind = ServerNetwork.GetConnectedClientFromUsername(settlement.Username);

            if (toFind == null) ResponseShortcutManager.SendUserUnavailablePacket(client);
            else
            {
                PKT_Synchronous _ = new PKT_Synchronous();
                _.CurrentStepMode = PKT_Synchronous.StepMode.Ask;
                _.FromTile = PM_Settlements.GetSettlementFileFromUsername(client.GetData<UserFile>().Username).Tile;
                _.Username = client.GetData<UserFile>().Username;
                _.ToTile = data.ToTile;
                _.Party = data.Party;
                _.CurrentType = data.CurrentType;

                toFind.Listener.EnqueuePacket(PacketHeader.Synchronous, _);
            }
        }

        private static void AcceptSynchronousSession(ServerClient client, PKT_Synchronous data)
        {
            FL_Settlement settlement = PM_Settlements.GetSettlementFileFromTile(data.ToTile);
            ServerClient toFind = ServerNetwork.GetConnectedClientFromUsername(settlement.Username);

            client.GetData<UserFile>().SynchronousClientID = toFind.ID;
            toFind.GetData<UserFile>().SynchronousClientID = client.ID;

            data.CurrentStepMode = PKT_Synchronous.StepMode.Accept;
            toFind.Listener.EnqueuePacket(PacketHeader.Synchronous, data);
        }

        private static void RejectSynchronousSession(ServerClient client, PKT_Synchronous data)
        {
            FL_Settlement settlement = PM_Settlements.GetSettlementFileFromTile(data.ToTile);
            ServerClient toFind = ServerNetwork.GetConnectedClientFromUsername(settlement.Username);

            PKT_Synchronous _ = new PKT_Synchronous();
            _.CurrentStepMode = PKT_Synchronous.StepMode.Reject;
            _.FromTile = data.FromTile;
            _.ToTile = data.ToTile;

            toFind.Listener.EnqueuePacket(PacketHeader.Synchronous, _);
        }

        private static void StartSynchronousSession(ServerClient client, PKT_Synchronous data)
        {
            PKT_Synchronous _ = new PKT_Synchronous() { CurrentStepMode = PKT_Synchronous.StepMode.Start };
            ServerNetwork.GetClientFromID(client.GetData<UserFile>().SynchronousClientID).Listener.EnqueuePacket(PacketHeader.Synchronous, _);
        }
    }
}
