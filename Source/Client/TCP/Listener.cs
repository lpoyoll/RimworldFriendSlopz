using GameClient.Misc;
using Shared;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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

                        if (!IgnoredLogPackets.Contains(header)) Printer.Message($"[Packet] > {header}", LogImportanceMode.Verbose);
                        else Printer.Message($"[Packet] > {header}", LogImportanceMode.Extreme);

                        try 
                        {
                            MainThreadHandler.Instance.Enqueue(delegate
                            {
                                MethodGatherer.ClientMethodDictionary[header].Invoke(null, new object[] { buffer });
                            });
                        }
                        catch (Exception ex) { OnHandleError(ex); }

                        void OnHandleError(Exception ex)
                        {
                            Printer.Error($"Error while trying to execute method from type '{header}'");
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
