using RTNetwork.Components;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using RTShared.Misc;

namespace RTServer.PacketManagers
{
    public class PM_Handshake : PM_Base
    {
        [HandlesPacket(PacketHeader.Handshake)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Handshake packet = Serializer.ConvertBytesToObject<PKT_Handshake>(bytes);

            switch (packet.CurrentMode)
            {
                case PKT_Handshake.StepMode.Check:
                    CheckHandshake(client, packet);
                    break;
                
                case PKT_Handshake.StepMode.Accept:
                    OnHandshakeAccept(client, packet);
                    break;

                case PKT_Handshake.StepMode.Deny:
                    OnHandshakeDeny(client, packet);
                    break;
            }
        }

        public static void Send(ServerClient client)
        {
            PKT_Handshake packet = new PKT_Handshake() { IncomingManagers =  GetLocalManagers() };
            client.Listener.EnqueuePacket(PacketHeader.Handshake, packet);
        }

        private static void CheckHandshake(ServerClient client, PKT_Handshake packet)
        {
            bool isValid = true;

            foreach (string str in GetLocalManagers())
            {
                if (!packet.IncomingManagers.Contains(str))
                {
                    Printer.Warning($"Missing existing manager '{str}'", Printer.Verbosity.Verbose);
                    isValid = false;
                }
            }

            if (isValid)
            {
                client.VerifyClient();
                packet.CurrentMode = PKT_Handshake.StepMode.Accept;
                client.Listener.EnqueuePacket(PacketHeader.Handshake, packet);
            }

            else
            {
                packet.CurrentMode = PKT_Handshake.StepMode.Deny;
                client.Listener.EnqueuePacket(PacketHeader.Handshake, packet);

                client.Listener.MarkForDisconnect();
                Printer.Warning($"Handshake with '{client.IP}' was invalid, disconnecting");
            }
        }
        
        private static void OnHandshakeAccept(ServerClient client, PKT_Handshake packet) { client.VerifyClient(); }

        private static void OnHandshakeDeny(ServerClient client, PKT_Handshake packet) { Printer.Error($"Handshake with '{client.IP}' was invalid, disconnecting"); }

        private static List<string> GetLocalManagers() { return PM_Base.PacketDictionary.Values.Select(obj => obj[0].GetType().Name).ToList(); }
    }
}
