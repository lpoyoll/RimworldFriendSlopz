using GameServer.Core;
using GameServer.Managers;
using GameServer.Misc;
using Shared;
using System.Collections.Concurrent;
using System.Net.Sockets;
using static Shared.CommonEnumerators;
using static Shared.CommonValues;

namespace GameServer.TCP
{
    public class Listener : ListenerBase
    {
        private ServerClient TargetClient { get; set; }

        public Listener(ServerClient clientToUse, TcpClient connection)
        {
            this.TargetClient = clientToUse;

            this.Connection = connection;
            this.Stream = connection.GetStream();

            PrintVerboseAction = delegate { Printer.Warning(LatestException, LogImportanceMode.Verbose); };
            PrintExtremeAction = delegate { Printer.Warning(LatestException, LogImportanceMode.Extreme); };

            Task.Run(() => Read());
            Task.Run(() => Write());
            Task.Run(() => SendKAFlag());
            Task.Run(() => CheckConnectionHealth(delegate { Network.KickClient(TargetClient); }));
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

                        try { Master.managerDictionary[packet.Header].Invoke(null, new object[] { TargetClient, packet }); }
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
