#if SERVER
using GameServer.Misc;
using Shared;
using System.Net.Sockets;
using static Shared.CommonEnumerators;
using static Shared.CommonValues;

namespace Shared.Network.Server
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

                        try { MethodGatherer.ServerMethodDictionary[header].Invoke(null, new object[] { TargetClient, buffer }); }
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
#endif