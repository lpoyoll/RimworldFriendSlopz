using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shared
{
    public static class MethodGatherer
    {
        public static Dictionary<PacketHeader, MethodInfo> ClientMethodDictionary { get; private set; }

        public static Dictionary<PacketHeader, MethodInfo> ServerMethodDictionary { get; private set; }

        public enum AssemblyType { Client, Server }

        public static void CacheAllMethods(AssemblyType type)
        {
            if (type == AssemblyType.Client)
            {
                MethodInfo[] clientMethods = GetPacketHandlerAttributes((Type[])Assembly.GetExecutingAssembly().GetTypes().ToArray());
                ClientMethodDictionary = new Dictionary<PacketHeader, MethodInfo>();
                for (int i = 0; i < clientMethods.Length; i++)
                {
                    ClientMethodDictionary.Add(clientMethods[i].GetCustomAttribute<HandlesPacket>().header,
                        clientMethods[i]);
                }
            }

            else
            {
                MethodInfo[] serverMethods = GetPacketHandlerAttributes((Type[])Assembly.GetExecutingAssembly().GetTypes().ToArray());
                ServerMethodDictionary = new Dictionary<PacketHeader, MethodInfo>();
                for (int i = 0; i < serverMethods.Length; i++)
                {
                    ServerMethodDictionary.Add(serverMethods[i].GetCustomAttribute<HandlesPacket>().header,
                        serverMethods[i]);
                }
            }
        }

        private static MethodInfo[] GetPacketHandlerAttributes(Type[] types)
        {
            List<MethodInfo> toAdd = new List<MethodInfo>();
            for (int x = 0; x < types.Length; x++)
            {
                toAdd.AddRange(types[x].GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                    .Where(fetch => fetch.GetCustomAttribute<HandlesPacket>() != null).ToList());
            }
            return toAdd.ToArray();
        }
    }
}