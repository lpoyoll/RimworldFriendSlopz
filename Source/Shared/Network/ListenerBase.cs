using System.IO;
using System.Threading;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Linq;
using System.Collections.Generic;

namespace Shared
{
    public class ListenerBase
    {
        public TcpClient Connection { get; set; }

        public NetworkStream Stream { get; set; }

        public bool DisconnectFlag { get; set; }

        public ConcurrentQueue<KeyValuePair<byte, byte[]>> PacketQueue { get; private set; } = new ConcurrentQueue<KeyValuePair<byte, byte[]>>();

        public void EnqueuePacket(PacketHeader header, object obj) 
        {
            PacketQueue.Enqueue(new KeyValuePair<byte, byte[]>((byte)header, Serializer.ConvertObjectToBytes(obj)));
        }

        public Action PrintVerboseAction { get; set; }

        public Action PrintExtremeAction { get; set; }

        public string LatestException { get; private set; }

        public void Write()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(1);

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
                    }
                }
            }

            catch (System.IO.IOException e)
            {
                LatestException = e.ToString();
                DisconnectFlag = true;
                PrintExtremeAction();
            }

            catch (Exception e)
            {
                LatestException = e.ToString();
                DisconnectFlag = true;
                PrintVerboseAction();
            }
        }

        public void ReadFullPacket(byte[] content)
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

            catch (Exception e)
            {
                LatestException = e.ToString();
                DisconnectFlag = true;
                PrintVerboseAction();
            }
        }

        public void SendKAFlag()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(CommonValues.KeepAliveCooldown);

                    KeepAliveData keepAliveData = new KeepAliveData();
                    EnqueuePacket(PacketHeader.KeepAliveManager, keepAliveData);
                }
            }

            catch (Exception e)
            {
                LatestException = e.ToString();
                DisconnectFlag = true;
                PrintVerboseAction();
            }
        }

        public void CheckConnectionHealth(Action toDo)
        {
            while (!DisconnectFlag)
            {
                Thread.Sleep(1);
            }

            Thread.Sleep(1000);

            toDo.Invoke();
        }

        public void DestroyConnection() { Connection.Close(); }
    }
}