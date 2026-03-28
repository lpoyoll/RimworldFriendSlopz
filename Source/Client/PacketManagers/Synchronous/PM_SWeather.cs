using GameClient.Hooks.Synchronous;
using GameClient.Misc;
using Shared;
using System;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using Verse;

namespace GameClient.PacketManagers.Synchronous
{
    public class PM_SWeather : PM_Base
    {
        public static void Ask(byte value)
        {
            PlayerWeather weather = new PlayerWeather();
            weather.MapTile = Find.CurrentMap.Tile;
            weather.WeatherByte = value;

            PKT_Synchronous packet = new PKT_Synchronous();
            packet.CurrentStepMode = PKT_Synchronous.StepMode.Action;
            packet.CurrentActionType = PKT_Synchronous.ActionType.SPlayerWeather;
            packet.Contents = Serializer.ConvertObjectToBytes(weather);

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.SynchronousManager, packet);
        }

        public static void Handle(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PlayerWeather data = Serializer.ConvertBytesToObject<PlayerWeather>(bytes);

            PatchHandler.ExecuteInBypass(delegate
            {
                WeatherDef def = Finder.GetWeatherDefFromByte(data.WeatherByte);
                Finder.GetMapFromTile(data.MapTile).weatherManager.TransitionTo(def);
            });
        }

        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            throw new NotImplementedException();
        }
    }
}
