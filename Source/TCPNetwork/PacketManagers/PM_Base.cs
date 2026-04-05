using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TCPNetwork.Files.Client;

namespace TCPNetwork.PacketManagers
{
    [ManagesPacket]
    public abstract class PM_Base
    {
        public abstract void Receive(ServerClient client, byte[] bytes, PacketHeader header);

        public static Dictionary<PacketHeader, object[]> PacketDictionary { get; set; } = new Dictionary<PacketHeader, object[]>();

        public static void CacheAllPackets()
        {
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(fetch => fetch.GetCustomAttribute<ManagesPacket>() != null))
            {
                MethodInfo method = type.GetMethod("Receive", BindingFlags.Instance | BindingFlags.Public);
                HandlesPacket attribute = method.GetCustomAttribute<HandlesPacket>();
                if (attribute != null) GeneratePacketInstance(attribute.header, type, method);
            }

            foreach (Type type in Assembly.GetCallingAssembly().GetTypes().Where(fetch => fetch.GetCustomAttribute<ManagesPacket>() != null))
            {
                MethodInfo method = type.GetMethod("Receive", BindingFlags.Instance | BindingFlags.Public);
                HandlesPacket attribute = method.GetCustomAttribute<HandlesPacket>();
                if (attribute != null) GeneratePacketInstance(attribute.header, type, method);
            }
        }

        private static void GeneratePacketInstance(PacketHeader header, Type type, MethodInfo method)
        {
            try { PacketDictionary.Add(header, new object[] { Activator.CreateInstance(type), method }); }
            catch (Exception ex) { Printer.Error(ex); }

            // Putting trycatch because the client can freak out during boot and cause issues printing

            try { Printer.Warning($"Added packet '{type.Name}'", Printer.LogImportanceMode.Extreme); }
            catch { }
        }
    }
}
