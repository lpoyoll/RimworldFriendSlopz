using GameClient.Hooks.Synchronous;
using GameClient.Misc;
using RTShared;
using System;
using RTNetwork;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using Verse;
using RTNetwork.Components;
using GameClient.Managers;

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

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Synchronous, packet);
        }

        public static void Handle(ServerClient client, PKT_Synchronous data)
        {
            PlayerDestroy destroy = Serializer.ConvertBytesToObject<PlayerDestroy>(data.Contents, false);

            PatchHandler.ExecuteInBypass(delegate
            {
                Thing thing = Finder.GetThingFromID(SessionManager.SynchronousMap, destroy.ThingID);
                thing.Destroy();
            });
        }

        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            throw new NotImplementedException();
        }
    }
}
