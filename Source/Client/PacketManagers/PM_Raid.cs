using DiscordRPC;
using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Managers;
using GameClient.Misc;
using RimWorld;
using RimWorld.Planet;
using RTShared;
using RTShared.Files;
using RTNetwork;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using Verse;
using static RTNetwork.Packets.PKT_Raid;

namespace GameClient.PacketManagers
{
    public class PM_Raid : PM_Base
    {
        [HandlesPacket(PacketHeader.Raid)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Raid data = Serializer.ConvertBytesToObject<PKT_Raid>(bytes);

            switch (data.CurrentStepMode)
            {
                case StepMode.Request:
                    OnAccept(data);
                    break;

                case StepMode.Deny:
                    OnDeny();
                    break;
            }
        }

        public static void RequestRaid(int targetTile)
        {
            if (!SessionHandler.CurrentActionValues.RaidAction.IsEnabled)
            {
                DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                return;
            }

            SendRequest(targetTile);
        }

        private static void SendRequest(int targetTile)
        {
            DLG_Base.PushNewDialog(new DLG_Wait());

            PKT_Raid data = new PKT_Raid();
            data.CurrentStepMode = StepMode.Request;
            data.TargetTile = targetTile;

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Raid, data);
        }

        private static void OnAccept(PKT_Raid data) 
        {
            DLG_Wait.Instance.Close();

            PrepareMap(data.Map); 
        }

        private static void OnDeny()
        {
            DLG_Wait.Instance.Close();

            DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "This map is currently unavailable!" }));
        }

        private static void PrepareMap(FL_Map mapFile)
        {
            Map map = MapSaveLoader.StringToMap(mapFile);

            RimworldManager.SetMapFactions(map, SessionHandler.EnemyFaction);

            RimworldManager.SetMapLord(map, SessionHandler.EnemyFaction);

            CaravanEnterMapUtility.Enter(SessionHandler.ChosenCaravan, SessionHandler.ChosenSettlement.Map,
                    CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, draftColonists: true);

            CameraJumper.TryJump(map.Center, map, CameraJumper.MovementMode.Pan);
        }
    }
}
