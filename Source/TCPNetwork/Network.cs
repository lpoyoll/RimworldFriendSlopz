using Shared;
using System;
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

        public static int BrowserPort { get; set; } = 7777;

        public static List<ServerClient> ServerClients { get; private set; } = new List<ServerClient>();

        public static readonly TimeSpan BrowserTelemetryInterval = TimeSpan.FromSeconds(60);

        public static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(10);

        public static readonly PacketHeader[] IgnoreLogPackets = { PacketHeader.KeepAliveManager };
    }
}
