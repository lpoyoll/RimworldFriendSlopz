using GameClient.Hooks.Synchronous;
using GameClient.Misc;
using Shared;
using System;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using Verse;

namespace GameClient.PacketManagers.Synchronous
{
    public class PM_SDestroy : PM_Base
    {
        public static void Ask(Thing thing)
        {
            PlayerDestroy destroy = new PlayerDestroy();
            destroy.ThingID = thing.ThingID;

            PKT_Synchronous packet = new PKT_Synchronous();
            packet.CurrentStepMode = PKT_Synchronous.StepMode.Action;
            packet.CurrentActionType = PKT_Synchronous.ActionType.SPlayerDestroy;
            packet.Contents = Serializer.ConvertObjectToBytes(destroy, false);

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.SynchronousManager, packet);
        }

        public static void Handle(ServerClient client, PKT_Synchronous data)
        {
            PlayerDestroy destroy = Serializer.ConvertBytesToObject<PlayerDestroy>(data.Contents, false);

            PatchHandler.ExecuteInBypass(delegate
            {
                Thing thing = Finder.GetThingFromID(SessionHandler.SynchronousMap, destroy.ThingID);
                thing.Destroy();
            });
        }

        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            throw new NotImplementedException();
        }
    }
}
