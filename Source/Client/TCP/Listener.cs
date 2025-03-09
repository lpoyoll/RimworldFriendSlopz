using GameClient.Core;
using GameClient.Managers;
using GameClient.Misc;
using Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static Shared.CommonEnumerators;
using static Shared.CommonValues;

namespace GameClient.TCP
{
    public class Listener : ListenerBase
    {
        public Listener(TcpClient connection)
        {
            this.Connection = connection;
            this.Stream = connection.GetStream();

            PrintVerboseAction = delegate { Printer.Warning(LatestException, LogImportanceMode.Verbose); };
            PrintExtremeAction = delegate { Printer.Warning(LatestException, LogImportanceMode.Extreme); };

            Task.Run(() => Read());
            Task.Run(() => Write());
            Task.Run(() => SendKAFlag());
            Task.Run(() => CheckConnectionHealth(delegate { Network.DisconnectFromServer(); }));
        }

        public void Read()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(1);

                    if (Stream.DataAvailable)
                    {
                        byte[] buffer = new byte[Packet.DefaultPacketSizeInBytes];
                        Stream.Read(buffer, 0, buffer.Length);
                        Packet.SetPacketSize(BitConverter.ToInt32(buffer, 0));

                        buffer = new byte[Packet.CurrentPacketSizeInBytes];
                        ReadFullPacket(buffer);
                        Packet packet = Packet.DecompressPacket(buffer);

                        if (!IgnoredLogPackets.Contains(packet.Header)) Printer.Message($"[Packet] > {packet.Header}", LogImportanceMode.Verbose);
                        else Printer.Message($"[Packet] > {packet.Header}", LogImportanceMode.Extreme);

                        try 
                        {
                            MainThreadHandler.Instance.Enqueue(delegate
                            {
                                Master.managerDictionary[packet.Header].Invoke(null, new object[] { packet });
                            });
                        }
                        catch (Exception ex) { OnHandleError(ex); }

                        void OnHandleError(Exception ex)
                        {
                            Printer.Error($"Error while trying to execute method from type '{packet.Header}'");
                            Printer.Error("Forcefully disconnecting due to MethodManager exception");
                            Printer.Error(ex.ToString());
                            DisconnectFlag = true;
                        }
                    }
                }
            }

            catch (System.ObjectDisposedException e)
            {
                Printer.Warning(e, LogImportanceMode.Extreme);
                DisconnectFlag = true;
            }

            catch (Exception e)
            {
                Printer.Warning(e.ToString(), LogImportanceMode.Verbose);
                DisconnectFlag = true;
            }
        }
    }
}
