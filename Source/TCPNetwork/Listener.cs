using Shared;
using Shared.Misc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using static Shared.CommonEnumerators;
using static Shared.CommonValues;
using static Shared.Misc.Printer;

namespace TCPNetwork
{
    public class Listener
    {
        private ServerClient TargetClient { get; set; } = null;

        private TcpClient Connection { get; set; } = null;

        private NetworkStream Stream { get; set; } = null;

        private NetworkRuleset Ruleset { get; set; } = null;

        private ConcurrentQueue<KeyValuePair<byte, byte[]>> PacketQueue { get; set; } = new ConcurrentQueue<KeyValuePair<byte, byte[]>>();

        private bool IsDisconnecting { get; set; } = false;

        private bool SeveredConnection { get; set; } = false;
        
        private DateTime LastKAReceivedPacket { get; set; } = DateTime.Now;

        private DateTime LastKASentPacket { get; set; } = DateTime.Now;

        private Semaphore Semaphore { get; set; } = new Semaphore(1, 1);

        public Listener(ServerClient clientToUse, TcpClient connection, NetworkRuleset ruleset)
        {
            this.Connection = connection;
            this.TargetClient = clientToUse;
            this.Stream = connection.GetStream();
            this.Ruleset = ruleset;

            Ruleset.OnConnect?.Invoke(clientToUse);
            Task.Run(RunAllListenerTasks);
        }

        private void RunAllListenerTasks()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(1);

                    CheckKAFlag();
                    SendKAFlag();

                    Read();
                    Write();

                    if (IsDisconnecting) break;
                }

                catch (Exception ex)
                {
                    Printer.Warning(ex, LogImportanceMode.Ludicrous);
                    break; 
                }
            }

            Disconnect();
        }

        public void EnqueuePacket(PacketHeader header, object obj)
        {
            if (IsDisconnecting) return;
            else if (!obj.GetType().IsSubclassOf(typeof(PKT_Base))) Printer.Error($"Malformed package {obj.GetType()}");
            else PacketQueue.Enqueue(new KeyValuePair<byte, byte[]>((byte)header, Serializer.ConvertObjectToBytes(obj)));
        }

        public void EnqueuePacket(PacketHeader header, byte[] bytes)
        {
            if (IsDisconnecting) return;
            else PacketQueue.Enqueue(new KeyValuePair<byte, byte[]>((byte)header, bytes));
        }

        private void Read()
        {
            int readBytes = 0;
            byte[] headerBuffer = new byte[sizeof(PacketHeader)];
            byte[] lengthBuffer = new byte[Network.PacketLengthSizeInBytes];
            byte[] packetBuffer = new byte[Network.PacketLengthSizeInBytes];

            if (Stream.DataAvailable)
            {
                // Read packet header
                Stream.Read(headerBuffer, 0, sizeof(PacketHeader));
                PacketHeader header = (PacketHeader)headerBuffer[0];

                // Read packet size
                Stream.Read(lengthBuffer, 0, Network.PacketLengthSizeInBytes);

                // Read packet contents
                readBytes = 0;
                packetBuffer = new byte[BitConverter.ToInt32(lengthBuffer, 0)];
                while (readBytes < packetBuffer.Length) readBytes += Stream.Read(packetBuffer, readBytes, packetBuffer.Length - readBytes);

                // Reset KeepAlive
                LastKAReceivedPacket = DateTime.Now;

                // Log packet contents
                if (!Network.IgnoreLogPackets.Contains(header)) Printer.Message($"[Packet] > Received packet {header}", LogImportanceMode.Verbose);
                else Printer.Message($"[Packet] > Received packet {header}", LogImportanceMode.Extreme);

                // Execute ruleset action
                Ruleset.OnRead?.Invoke(header, packetBuffer, TargetClient);
            }
        }

        private void Write()
        {
            byte[] headerBuffer = new byte[sizeof(PacketHeader)];

            while (PacketQueue.Count > 0)
            {
                if (!PacketQueue.TryDequeue(out KeyValuePair<byte, byte[]> packetData)) return;
                byte[] packetSize = BitConverter.GetBytes(packetData.Value.Length);

                // Write packet header
                headerBuffer[0] = packetData.Key;
                Stream.Write(headerBuffer, 0, sizeof(PacketHeader));

                // Write packet size
                Stream.Write(packetSize, 0, packetSize.Length);

                // Write packet data
                Stream.Write(packetData.Value, 0, packetData.Value.Length);

                //Log the packet data
                if (!Network.IgnoreLogPackets.Contains((PacketHeader)(packetData.Key))) Printer.Message($"[Packet] > Sent packet {(PacketHeader)(packetData.Key)}", LogImportanceMode.Verbose);
                else Printer.Message($"[Packet] > Sent packet {(PacketHeader)(packetData.Key)}", LogImportanceMode.Extreme);

                //Execute after writing
                Ruleset.OnWrite?.Invoke(TargetClient);
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
                    EnqueuePacket(PacketHeader.KeepAliveManager, keepAliveData);
                }
            }
        }

        private void CheckKAFlag()
        {
            if (DateTime.Now - LastKAReceivedPacket > Network.KeepAliveMaxTime) MarkForDisconnect();
            else return;
        }

        public void MarkForDisconnect() 
        {
            PKT_Command packet = new PKT_Command();
            packet._commandMode = CommandMode.Disconnect;
            EnqueuePacket(PacketHeader.ConsoleManager, packet);

            IsDisconnecting = true; 
        }

        private void Disconnect()
        {
            Semaphore.WaitOne();

            if (SeveredConnection) return;
            else
            {
                MarkForDisconnect();
                Connection.Dispose();
                Stream.Dispose();

                Ruleset.OnDisconnect?.Invoke(TargetClient);
                SeveredConnection = true;
            }

            Semaphore.Release();
        }
    }
}