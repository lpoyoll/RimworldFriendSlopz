using Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using TCPNetwork.Files.Client;

namespace TCPNetwork
{
    public class Network
    {
        public const int PacketLengthSizeInBytes = sizeof(int);

        public static string Ip { get; set; } = string.Empty;

        public static int Port { get; set; } = int.MaxValue;

        public static Listener ServerEndpoint { get; set; } = null;

        public static Listener BrowserEndpoint { get; set; } = null;

        public static TcpListener ServerListener { get; set; } = null;

        public static string BrowserIp { get; set; } = "66.29.129.72";

        public static int BrowserServerPort { get; set; } = 7777;

        public static int BrowserClientPort { get; set; } = 7778;

        public static ConcurrentDictionary<ServerClient, int> ServerClients { get; private set; } = new ConcurrentDictionary<ServerClient, int>();

        public static readonly TimeSpan BrowserTelemetryInterval = TimeSpan.FromSeconds(300);

        public static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(10);

        public static readonly PacketHeader[] IgnoreLogPackets = { PacketHeader.KeepAliveManager };
    }
}
