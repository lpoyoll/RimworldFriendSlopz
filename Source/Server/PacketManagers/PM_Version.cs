using GameServer.Misc;
using RTShared;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using static RTNetwork.Packets.PKT_Login;
using RTNetwork.Components;

namespace GameServer.PacketManager
{
    public class PM_Version : PM_Base
    {
        [HandlesPacket(PacketHeader.Version)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Version packet = Serializer.ConvertBytesToObject<PKT_Version>(bytes);

            if (packet.Version == CommonValues.ExecutableVersion) client.Listener.EnqueuePacket(PacketHeader.Version, packet);
            else
            {
                PM_Login.DenyConnectionWithReason(client, LoginResponse.Version);
                InformationDisplayer.DisplayVersionMismatch(client);
            }
        }
    }
}
