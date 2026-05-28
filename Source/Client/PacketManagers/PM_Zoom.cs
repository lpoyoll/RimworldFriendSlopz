using DiscordRPC;
using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Managers;
using GameClient.Misc;
using RimWorld;
using RimWorld.Planet;
using RTShared;
using RTShared.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RTNetwork;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using Verse;
using static RTNetwork.Packets.PKT_Zoom;

namespace GameClient.PacketManagers
{
    public class PM_Zoom : PM_Base
    {
        [HandlesPacket(PacketHeader.Zoom)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Zoom data = Serializer.ConvertBytesToObject<PKT_Zoom>(bytes);

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

        public static void RequestZoom(int targetTile)
        {
            if (!SessionHandler.CurrentActionValues.ZoomAction.IsEnabled)
            {
                DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "This feature has been disabled in this server!" }));
                return;
            }

            SendRequest(targetTile);
        }

        private static void SendRequest(int targetTile)
        {
            DLG_Base.PushNewDialog(new DLG_Wait());

            PKT_Zoom data = new PKT_Zoom();
            data.CurrentStepMode = StepMode.Request;
            data.TargetTile = targetTile;

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Zoom, data);
        }

        private static void OnAccept(PKT_Zoom data)
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

            RimworldManager.SetMapFactions(map, SessionHandler.NeutralFaction);

            RimworldManager.SetMapLord(map, SessionHandler.NeutralFaction);

            CameraJumper.TryJump(map.Center, map, CameraJumper.MovementMode.Pan);
        }
    }
}
