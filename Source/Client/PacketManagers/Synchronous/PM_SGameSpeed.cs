using GameClient;
using GameClient.Hooks.Synchronous;
using GameClient.Hooks.TCPNetwork;
using GameClient.Misc;
using RimWorld;
using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using Verse;

namespace GameClient.PacketManagers.Synchronous
{
    public class PM_SGameSpeed : PM_Base
    {
        public static TimeSpeed LatestGameSpeed { get; private set; } = TimeSpeed.Paused;

        [OnSynchronousStart]
        private static void SendFirstSpeed()
        {
            if (SessionHandler.IsSynchronousHost)
            {
                PlayerGameSpeed data = new PlayerGameSpeed();
                data.CurrentGameSpeed = (int)TimeSpeed.Paused;
                data.TimeTicks = Find.TickManager.TicksSinceSettle;

                PKT_Synchronous packet = new PKT_Synchronous();
                packet.CurrentStepMode = PKT_Synchronous.StepMode.Action;
                packet.CurrentActionType = PKT_Synchronous.ActionType.SPlayerGameSpeed;
                packet.Contents = Serializer.ConvertObjectToBytes(data);

                Network.ServerEndpoint.EnqueuePacket(PacketHeader.SynchronousManager, packet);
            }
        }

        public static void Ask(TimeSpeed speed)
        {
            if (speed == LatestGameSpeed) return;
            else
            {
                PlayerGameSpeed data = new PlayerGameSpeed();
                data.CurrentGameSpeed = (int)speed;
                data.TimeTicks = Find.TickManager.TicksSinceSettle;

                PKT_Synchronous packet = new PKT_Synchronous();
                packet.CurrentStepMode = PKT_Synchronous.StepMode.Action;
                packet.CurrentActionType = PKT_Synchronous.ActionType.SPlayerGameSpeed;
                packet.Contents = Serializer.ConvertObjectToBytes(data);

                Network.ServerEndpoint.EnqueuePacket(PacketHeader.SynchronousManager, packet);
            }
        }

        public static void Handle(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PlayerGameSpeed data = Serializer.ConvertBytesToObject<PlayerGameSpeed>(bytes);

            PatchHandler.ExecuteInBypass(delegate
            {
                Find.TickManager.CurTimeSpeed = (TimeSpeed)data.CurrentGameSpeed;
                Find.TickManager.DebugSetTicksGame(data.TimeTicks);
                LatestGameSpeed = Find.TickManager.CurTimeSpeed;
            });
        }

        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            throw new NotImplementedException();
        }
    }
}
