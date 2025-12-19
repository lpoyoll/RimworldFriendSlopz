using Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using static Shared.CommonEnumerators;

namespace TCPNetwork;

public class Listener
{
    public enum ListenerMode { Client, Server }

    private readonly ServerClient TargetClient;

    private readonly TcpClient Connection;

    private readonly NetworkStream Stream ;

    private readonly Action<PacketHeader, byte[], ServerClient> OnReadPacket;

    private readonly Action<bool> OnWritePacket;

    private readonly Action<ServerClient> OnDisconnect;

    private readonly Action<object, LogImportanceMode> OnMessage;

    private readonly Action<object, LogImportanceMode> OnWarning;

    private readonly Action<object, LogImportanceMode> OnError;

    private ConcurrentQueue<KeyValuePair<byte, byte[]>> PacketQueue { get; set; } = new ConcurrentQueue<KeyValuePair<byte, byte[]>>();

    private bool DisconnectFlag { get; set; } = false;

    public int CurrentKeepAliveTime { get; set; } = 0;

    public const int KeepAliveMaxTime = 30000;

    public static readonly string DefaultParserMethodName = "ParsePacket";

    public static readonly PacketHeader[] IgnoreLogPackets = [PacketHeader.KeepAliveManager];

    public static readonly PacketHeader[] BypassReadyPackets =
    [
        PacketHeader.LoginManager,
        PacketHeader.KeepAliveManager,
        PacketHeader.VersionManager,
        PacketHeader.SaveManager,
        PacketHeader.WorldManager,
        PacketHeader.GlobalDataManager,
        PacketHeader.RecountManager,
        PacketHeader.ChatManager,
        PacketHeader.ConsoleManager,
        PacketHeader.ServerBrowserReachability
    ];

    public Listener(ServerClient clientToUse, TcpClient connection, Action<PacketHeader, byte[], ServerClient> onReadPacket, Action<bool> onWritePacket, 
        Action<ServerClient> onDisconnect, Action<object, LogImportanceMode> onMessage, Action<object, LogImportanceMode> onWarning, 
        Action<object, LogImportanceMode> onError, ListenerMode mode)
    {
        Connection = connection;
        TargetClient = clientToUse;
        Stream = connection.GetStream();

        OnMessage = onMessage;
        OnWarning = onWarning;
        OnError = onError;

        OnReadPacket = onReadPacket;
        OnWritePacket = onWritePacket;
        OnDisconnect = onDisconnect;

        Task.Run(() => Read());
        Task.Run(() => Write());
        Task.Run(() => SendKAFlag());
        Task.Run(() => CheckKAFlag());
    }

    public void EnqueuePacket(PacketHeader header, object obj)
    {
        PacketQueue.Enqueue(new KeyValuePair<byte, byte[]>((byte)header, Serializer.ConvertObjectToBytes(obj)));
    }
    public void EnqueueBytes(PacketHeader header, byte[] bytes)
    {
        PacketQueue.Enqueue(new KeyValuePair<byte, byte[]>((byte)header, bytes));
    }

    private void Read()
    {
        try
        {
            byte[] headerBuffer = new byte[sizeof(PacketHeader)];
            byte[] lengthBuffer = new byte[Network.PacketLengthSizeInBytes];
            while (!DisconnectFlag)
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
                    var packetBuffer = new byte[BitConverter.ToInt32(lengthBuffer, 0)];
                    ReadFullPacket(packetBuffer);

                    if (!IgnoreLogPackets.Contains(header)) OnMessage($"[Packet] > Received packet {header}", LogImportanceMode.Verbose);
                    else OnMessage($"[Packet] > Received packet {header}", LogImportanceMode.Extreme);

                    try { OnReadPacket(header, packetBuffer, TargetClient); }
                    catch (Exception e) { OnWarning(e, LogImportanceMode.Extreme); }
                }
            }
        }
        catch (Exception e) { OnWarning(e, LogImportanceMode.Extreme); }

        Disconnect();
    }

    private void Write()
    {
        try
        {
            byte[] headerBuffer = new byte[sizeof(PacketHeader)];
            while (!DisconnectFlag)
            {
                Thread.Sleep(1);

                OnWritePacket(true);

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
                    if (!IgnoreLogPackets.Contains((PacketHeader)(packetData.Key))) OnMessage($"[Packet] Sent packet > {(PacketHeader)(packetData.Key)}", LogImportanceMode.Verbose);
                    else OnMessage($"[Packet] > Sent packet {(PacketHeader)(packetData.Key)}", LogImportanceMode.Extreme);
                }

                OnWritePacket(false);
            }
        }
        catch (Exception e) { OnWarning(e, LogImportanceMode.Extreme); }

        Disconnect();
    }

    private void SendKAFlag()
    {
        try
        {
            while (!DisconnectFlag)
            {
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
                Thread.Sleep(1);

                if (CurrentKeepAliveTime < KeepAliveMaxTime) CurrentKeepAliveTime++;
                else break;
            }
        }
        catch (Exception e) { OnWarning(e, LogImportanceMode.Verbose); }

        Disconnect();
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

    public void Disconnect()
    {
        if (DisconnectFlag) return;
        else
        {       
            Printer.Warning(new StackTrace());
            DisconnectFlag = true;
            Connection.Dispose();
            Stream.Dispose();

            OnDisconnect(TargetClient);
        }
    }
}