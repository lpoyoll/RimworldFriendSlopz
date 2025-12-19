using Shared;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using TCPNetwork.Files.Client;
using static Shared.CommonEnumerators;

namespace TCPNetwork;

public class Network
{
    public const int PacketLengthSizeInBytes = sizeof(int);

    public static string Ip { get; set; } = string.Empty;

    public static string Port { get; set; } = string.Empty;

    protected virtual Action<object, LogImportanceMode> OnMessage { get; set; }

    protected virtual Action<object, LogImportanceMode> OnWarning { get; set; }

    protected virtual Action<object, LogImportanceMode> OnError { get; set; }

    protected virtual Action<PacketHeader, byte[], ServerClient> OnReadPacket { get; set; }

    protected virtual Action<bool> OnWritePacket { get; set; }

    protected virtual Action<ServerClient> OnConnect { get; set; }

    protected virtual Action<ServerClient> OnDisconnect { get; set; }

    public Listener ClientListener { get; protected set; } = null;

    public TcpListener ServerListener { get; set; }

    public List<ServerClient> ServerClients { get; private set; } = [];
}