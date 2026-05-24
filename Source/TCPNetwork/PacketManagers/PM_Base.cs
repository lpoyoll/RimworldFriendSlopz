using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TCPNetwork.PacketManagers
{
    [ManagesPacket]
    public abstract class PM_Base
    {
        public abstract void Receive(ServerClient client, byte[] bytes, PacketHeader header);

        public static Dictionary<PacketHeader, object[]> PacketDictionary { get; set; } = new Dictionary<PacketHeader, object[]>();

        public enum AssemblyType { Client, Server }

        public static void CacheAllPackets(AssemblyType assembly)
        {
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(fetch => fetch.GetCustomAttribute<ManagesPacket>() != null))
            {
                MethodInfo method = type.GetMethod("Receive", BindingFlags.Instance | BindingFlags.Public);
                HandlesPacket attribute = method.GetCustomAttribute<HandlesPacket>();
                if (attribute != null)
                {
                    PacketDictionary.Add(attribute.header, new object[] { Activator.CreateInstance(type), method });
                    if (assembly == AssemblyType.Server) Printer.Warning($"[Base] Added packet '{type.Name}'", Printer.Verbosity.Extreme);
                }
            }

            foreach (Type type in Assembly.GetCallingAssembly().GetTypes().Where(fetch => fetch.GetCustomAttribute<ManagesPacket>() != null))
            {
                MethodInfo method = type.GetMethod("Receive", BindingFlags.Instance | BindingFlags.Public);
                HandlesPacket attribute = method.GetCustomAttribute<HandlesPacket>();
                if (attribute != null)
                {
                    PacketDictionary.Add(attribute.header, new object[] { Activator.CreateInstance(type), method });
                    if (assembly == AssemblyType.Server) Printer.Warning($"[Main] Added packet '{type.Name}'", Printer.Verbosity.Extreme);
                }
            }
        }
    }
}
