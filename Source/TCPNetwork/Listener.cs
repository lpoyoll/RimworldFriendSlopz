using Shared;
using Shared.Misc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using static Shared.Misc.Printer;

namespace TCPNetwork
{
    public class Listener
    {
        public ServerClient TargetClient { get; private set; } = null;

        private TcpClient Connection { get; set; } = null;

        private NetworkStream Stream { get; set; } = null;

        public NetworkRuleset Ruleset { get; private set; } = null;

        private ConcurrentQueue<KeyValuePair<byte, PKT_Base>> PacketQueue { get; set; } = new ConcurrentQueue<KeyValuePair<byte, PKT_Base>>();

        private bool IsDisconnecting { get; set; } = false;
        
        private DateTime LastKAReceivedPacket { get; set; } = DateTime.Now;

        private DateTime LastKASentPacket { get; set; } = DateTime.Now;

        public Listener(ServerClient clientToUse, TcpClient connection, NetworkRuleset ruleset)
        {
            this.Connection = connection;
            this.TargetClient = clientToUse;
            this.Stream = connection.GetStream();
            this.Ruleset = ruleset;

            Task.Run(RunAllListenerTasks);
        }

        private void RunAllListenerTasks()
        {
            while (true)
            {
                Thread.Sleep(1);

                try
                {
                    CheckKAFlag();
                    SendKAFlag();

                    Read();
                    Write();

                    if (IsDisconnecting) break;
                }

                catch (Exception ex)
                {
                    Printer.Warning(ex, Verbosity.Extreme);
                    break; 
                }
            }

            Disconnect();
        }

        public void EnqueuePacket(PacketHeader header, object obj)
        {
            if (IsDisconnecting) return;
            else if (!obj.GetType().IsSubclassOf(typeof(PKT_Base))) Printer.Error($"Malformed package {obj.GetType()}");
            else
            {
                PKT_Base packet = new PKT_Base() { Header = header, Contents = Serializer.ConvertObjectToBytes(obj) };
                PacketQueue.Enqueue(new KeyValuePair<byte, PKT_Base>((byte)header, packet));
            }
        }

        private void Read()
        {
            if (Stream.DataAvailable)
            {
                // Read packet header
                byte[] headerBuffer = new byte[sizeof(PacketHeader)];
                Stream.Read(headerBuffer, 0, headerBuffer.Length);
                PacketHeader header = (PacketHeader)headerBuffer[0];

                // Read packet size
                byte[] lengthBuffer = new byte[sizeof(int)];
                Stream.Read(lengthBuffer, 0, lengthBuffer.Length);
                if (!Network.CheckForPacketSize(TargetClient, lengthBuffer)) return;

                // Read packet contents
                byte[] packetBuffer = new byte[sizeof(int)];
                packetBuffer = new byte[BitConverter.ToInt32(lengthBuffer, 0)];
                Network.ReadFullPacket(Stream, packetBuffer);

                // Log packet contents
                if (!Network.IgnoreLogPackets.Contains(header)) Printer.Message($"[Packet] > Received packet '{header}'", Verbosity.Verbose);
                else Printer.Message($"[Packet] > Received packet '{header}'", Verbosity.Extreme);

                // Reset KeepAlive
                LastKAReceivedPacket = DateTime.Now;

                // Execute ruleset action
                if (!TargetClient.IsVerified && header != PacketHeader.Handshake) MarkForDisconnect();
                else Ruleset.OnRead?.Invoke(header, packetBuffer, TargetClient);
            }
        }

        private void Write()
        {
            while (PacketQueue.Count > 0)
            {
                if (!PacketQueue.TryDequeue(out KeyValuePair<byte, PKT_Base> pair)) return;
                else
                {
                    // Write packet header
                    byte[] headerBuffer = new byte[sizeof(PacketHeader)];
                    headerBuffer[0] = (byte)pair.Value.Header;
                    Stream.Write(headerBuffer, 0, headerBuffer.Length);

                    // Write packet size
                    byte[] packetSize = BitConverter.GetBytes(pair.Value.Contents.Length);
                    Stream.Write(packetSize, 0, packetSize.Length);

                    // Write packet data
                    Stream.Write(pair.Value.Contents, 0, pair.Value.Contents.Length);

                    // Log the packet data
                    if (!Network.IgnoreLogPackets.Contains(pair.Value.Header)) Printer.Message($"[Packet] > Sent packet '{pair.Value.Header}'", Verbosity.Verbose);
                    else Printer.Message($"[Packet] > Sent packet '{pair.Value.Header}'", Verbosity.Extreme);

                    // Execute after writing
                    Ruleset.OnWrite?.Invoke(TargetClient);
                }
            }
        }

        private void SendKAFlag()
        {
            if (Ruleset.HandleKeepAlive)
            {
                if (DateTime.Now - LastKASentPacket < TimeSpan.FromSeconds(Network.KeepAliveInterval.TotalSeconds)) return;
                else
                {
                    LastKASentPacket = DateTime.Now;
                    PKT_KeepAlive keepAliveData = new PKT_KeepAlive();
                    EnqueuePacket(PacketHeader.KeepAlive, keepAliveData);
                }
            }
        }

        private void CheckKAFlag()
        {
            if (DateTime.Now - LastKAReceivedPacket < TimeSpan.FromSeconds(Network.KeepAliveInterval.TotalSeconds * 6)) return;
            else MarkForDisconnect();
        }

        public void MarkForDisconnect(bool sendDisconnectPacket = true) 
        {
            if (sendDisconnectPacket)
            {
                try
                {
                    PKT_Disconnect packet = new PKT_Disconnect();
                    EnqueuePacket(PacketHeader.Disconnect, packet);
                }
                catch (Exception ex) { Printer.Error(ex); }
            }

            IsDisconnecting = true;
        }

        private void Disconnect()
        {
            Stream.Dispose();
            Connection.Dispose();
            Ruleset.OnDisconnect?.Invoke(TargetClient);
        }
    }
}