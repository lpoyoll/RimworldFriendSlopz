using Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using static Shared.CommonEnumerators;
using static Shared.CommonValues;

namespace TCPNetwork
{
    public class Listener
    {
        public enum ListenerMode { Client, Server }

        private ServerClient TargetClient { get; set; } = null;

        public TcpClient Connection { get; set; } = null;

        public NetworkStream Stream { get; set; } = null;

        public bool DisconnectFlag { get; set; } = false;

        public bool KeepAliveFlag { get; set; } = false;

        private Action<PacketHeader, byte[], ServerClient> OnReadPacket { get; set; } = null;

        private Action<bool> OnWritePacket { get; set; } = null;

        public Action<ServerClient> OnDisconnect { get; set; } = null;

        private Action<ServerClient> OnSendFlag { get; set; } = null;

        private Action<object, LogImportanceMode> OnMessage { get; set; } = null;

        private Action<object, LogImportanceMode> OnWarning { get; set; } = null;

        private Action<object, LogImportanceMode> OnError { get; set; } = null;

        private ConcurrentQueue<KeyValuePair<byte, byte[]>> PacketQueue { get; set; } = new ConcurrentQueue<KeyValuePair<byte, byte[]>>();

        public static readonly string DefaultParserMethodName = "ParsePacket";

        public static readonly int KeepAliveCooldown = 30000;

        public static readonly PacketHeader[] IgnoreLogPackets = { PacketHeader.KeepAliveManager };

        public static readonly PacketHeader[] BypassReadyPackets =
        {
            PacketHeader.LoginManager,
            PacketHeader.KeepAliveManager,
            PacketHeader.VersionManager,
            PacketHeader.SaveManager,
            PacketHeader.WorldManager,
            PacketHeader.GlobalDataManager,
            PacketHeader.RecountManager,
            PacketHeader.ChatManager,
            PacketHeader.ConsoleManager
        };

        public Listener(ServerClient clientToUse, TcpClient connection, Action<PacketHeader, byte[], ServerClient> onReadPacket, Action<bool> onWritePacket, 
            Action<ServerClient> onDisconnect, Action<ServerClient> onSendFlag, Action<object, LogImportanceMode> onMessage, Action<object, LogImportanceMode> onWarning, 
            Action<object, LogImportanceMode> onError, ListenerMode mode)
        {
            this.Connection = connection;
            this.TargetClient = clientToUse;
            this.Stream = connection.GetStream();

            this.OnMessage = onMessage;
            this.OnWarning = onWarning;
            this.OnError = onError;
            this.OnSendFlag = onSendFlag;

            this.OnReadPacket = onReadPacket;
            this.OnWritePacket = onWritePacket;
            this.OnDisconnect = onDisconnect;

            Task.Run(() => Read());
            Task.Run(() => Write());
            Task.Run(() => SendKAFlag());
            Task.Run(() => CheckKAFlag());
        }

        public void EnqueuePacket(PacketHeader header, object obj)
        {
            PacketQueue.Enqueue(new KeyValuePair<byte, byte[]>((byte)header, Serializer.ConvertObjectToBytes(obj)));
        }

        private void Read()
        {
            try
            {
                while (!DisconnectFlag)
                {
                    Thread.Sleep(1);

                    if (Stream.DataAvailable)
                    {
                        // Read packet header
                        byte[] buffer = new byte[1];
                        Stream.Read(buffer, 0, buffer.Length);
                        PacketHeader header = (PacketHeader)buffer[0];

                        // Read packet size
                        buffer = new byte[4];
                        Stream.Read(buffer, 0, buffer.Length);

                        // Read packet contents
                        buffer = new byte[BitConverter.ToInt32(buffer, 0)];
                        ReadFullPacket(buffer);

                        if (!IgnoreLogPackets.Contains(header)) OnMessage($"[Packet] > Received packet {header}", LogImportanceMode.Verbose);
                        else OnMessage($"[Packet] > Received packet {header}", LogImportanceMode.Extreme);

                        try { OnReadPacket(header, buffer, TargetClient); }
                        catch (Exception ex) { OnHandleError(ex); }

                        void OnHandleError(Exception e)
                        {
                            OnError($"Error while trying to execute method from type '{header}'", LogImportanceMode.Verbose);
                            OnError("Forcefully disconnecting due to MethodManager exception", LogImportanceMode.Verbose);
                            OnError(e, LogImportanceMode.Verbose);
                            DisconnectFlag = true;
                        }
                    }
                }
            }
            catch (Exception e) { OnWarning(e, LogImportanceMode.Extreme); }

            DisconnectFlag = true;
        }

        private void Write()
        {
            try
            {
                while (!DisconnectFlag)
                {
                    Thread.Sleep(1);

                    OnWritePacket(true);

                    if (PacketQueue.Count > 0)
                    {
                        if (!PacketQueue.TryDequeue(out KeyValuePair<byte, byte[]> packetData)) return;
                        byte[] packetSize = BitConverter.GetBytes(packetData.Value.Length);

                        // Write packet header
                        Stream.Write(new byte[] { packetData.Key }, 0, 1);

                        // Write packet size
                        Stream.Write(packetSize, 0, packetSize.Length);

                        // Write packet data
                        Stream.Write(packetData.Value, 0, packetData.Value.Length);

                        //Log the packet data
                        if (!IgnoreLogPackets.Contains((PacketHeader)(packetData.Key))) OnMessage($"[Packet] Sent packet > {(PacketHeader)(packetData.Key)}", LogImportanceMode.Verbose);
                        else OnMessage($"[Packet] > Sent packet {(PacketHeader)(packetData.Key)}", LogImportanceMode.Extreme);
                    }

                    OnWritePacket(false);
                }
            }
            catch (Exception e) { OnWarning(e, LogImportanceMode.Extreme); }

            DisconnectFlag = true;
        }

        private void SendKAFlag()
        {
            try
            {
                while (!DisconnectFlag)
                {
                    this.OnSendFlag.Invoke(TargetClient);

                    Thread.Sleep(1000);
                    KeepAliveData keepAliveData = new KeepAliveData();
                    EnqueuePacket(PacketHeader.KeepAliveManager, keepAliveData);
                }
            }
            catch (Exception e) { OnWarning(e, LogImportanceMode.Verbose); }
        }

        private void CheckKAFlag()
        {
            try
            {
                while (!DisconnectFlag)
                {
                    KeepAliveFlag = true;
                    Thread.Sleep(KeepAliveCooldown);

                    if (KeepAliveFlag) break;
                    else continue;
                }
            }
            catch (Exception e) { OnWarning(e, LogImportanceMode.Verbose); }

            DisconnectFlag = true;
            this.OnDisconnect(TargetClient);
        }

        private void ReadFullPacket(byte[] content)
        {
            int readBytes = 0;

            try
            {
                while (readBytes < content.Length)
                {
                    int read = Stream.Read(content, readBytes, content.Length - readBytes);
                    if (read == 0) throw new ArgumentOutOfRangeException();
                    readBytes += read;
                }
            }
            catch (Exception e) { OnWarning(e, LogImportanceMode.Verbose); }
        }

        public void DestroyConnection() 
        { 
            Connection.Dispose();
            Stream.Dispose();
        }
    }
}