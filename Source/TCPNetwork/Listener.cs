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
        
        private DateTime LastKAPacket { get; set; } = DateTime.Now;

        private Semaphore Semaphore { get; set; } = new Semaphore(1, 1);

        public Listener(ServerClient clientToUse, TcpClient connection, NetworkRuleset ruleset)
        {
            this.Connection = connection;
            this.TargetClient = clientToUse;
            this.Stream = connection.GetStream();
            this.Ruleset = ruleset;

            Ruleset.OnConnect?.Invoke(clientToUse);

            Task.Run(() => Read());
            Task.Run(() => Write());
            Task.Run(() => SendKAFlag());
            Task.Run(() => CheckKAFlag());
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
            try
            {
                int readBytes = 0;
                byte[] headerBuffer = new byte[sizeof(PacketHeader)];
                byte[] lengthBuffer = new byte[Network.PacketLengthSizeInBytes];
                byte[] packetBuffer = new byte[Network.PacketLengthSizeInBytes];

                while (!IsDisconnecting)
                {
                    Thread.Sleep(1);

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
                        LastKAPacket = DateTime.Now;

                        // Log packet contents
                        if (!Network.IgnoreLogPackets.Contains(header)) Printer.Message($"[Packet] > Received packet {header}", LogImportanceMode.Verbose);
                        else Printer.Message($"[Packet] > Received packet {header}", LogImportanceMode.Extreme);

                        // Execute ruleset action
                        try { Ruleset.OnRead?.Invoke(header, packetBuffer, TargetClient); }
                        catch (Exception e) { Printer.Warning(e, LogImportanceMode.Normal); }
                    }
                }
            }
            catch (ObjectDisposedException _) { Printer.Warning("Disposed of connection", LogImportanceMode.Extreme); }
            catch (System.IO.IOException _) { Printer.Warning("Disposed of connection", LogImportanceMode.Extreme); }
            catch (Exception e) { Printer.Warning(e); }

            Disconnect();
        }

        private void Write()
        {
            try
            {
                byte[] headerBuffer = new byte[sizeof(PacketHeader)];

                while (Connection.Client != null)
                {
                    Thread.Sleep(1);

                    if (PacketQueue.Count > 0)
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
            }
            catch (ObjectDisposedException _) { Printer.Warning("Disposed of connection", LogImportanceMode.Extreme); }
            catch (System.IO.IOException _) { Printer.Warning("Disposed of connection", LogImportanceMode.Extreme); }
            catch (Exception e) { Printer.Warning(e); }

            Disconnect();
        }

        private void SendKAFlag()
        {
            try
            {
                while (!IsDisconnecting)
                {
                    Thread.Sleep(Network.KeepAliveInterval);
                    PKT_KeepAlive keepAliveData = new PKT_KeepAlive();
                    EnqueuePacket(PacketHeader.KeepAliveManager, keepAliveData);
                }
            }
            catch (Exception e) { Printer.Warning(e, LogImportanceMode.Verbose); }
        }

        private void CheckKAFlag()
        {
            try
            {
                while (!IsDisconnecting)
                {
                    Thread.Sleep(Network.KeepAliveInterval);
                    DateTime current = DateTime.Now;
                    if (current - LastKAPacket > Network.KeepAliveMaxTime) break;
                }
            }
            catch (Exception e) { Printer.Warning(e, LogImportanceMode.Verbose); }

            Disconnect();
        }

        public void MarkForDisconnect() { IsDisconnecting = true; }

        private void Disconnect()
        {
            Semaphore.WaitOne();

            while (PacketQueue.Count > 0) Thread.Sleep(1);
            Thread.Sleep(1000);

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