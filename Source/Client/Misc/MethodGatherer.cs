using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Shared.CommonEnumerators;

namespace Shared
{
    public static class MethodGatherer
    {
        public static MethodInfo[] OnStartMethods { get; private set; } = null;

        public static MethodInfo[] OnEndMethods { get; private set; } = null;

        public static MethodInfo[] PerFrameMethods { get; private set; } = null;

        public static MethodInfo[] OnSynchronousStartMethods { get; private set; } = null;

        public static MethodInfo[] OnSynchronousEndMethods { get; private set; } = null;

        public static void CacheAllMethods()
        {
            OnStartMethods = GetSessionStartMethods(Assembly.GetCallingAssembly().GetTypes());
            OnEndMethods = GetSessionEndMethods(Assembly.GetCallingAssembly().GetTypes());
            PerFrameMethods = GetPerFrameMethods(Assembly.GetCallingAssembly().GetTypes());
            OnSynchronousStartMethods = GetSynchronousStartMethods(Assembly.GetCallingAssembly().GetTypes());
            OnSynchronousEndMethods = GetSynchronousEndMethods(Assembly.GetCallingAssembly().GetTypes());
        }

        private static MethodInfo[] GetSessionStartMethods(Type[] types)
        {
            List<MethodInfo> toAdd = new List<MethodInfo>();
            for (int x = 0; x < types.Length; x++)
            {
                toAdd.AddRange(types[x].GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                    .Where(fetch => fetch.GetCustomAttribute<OnSessionStart>() != null).ToList());
            }

            return toAdd.ToArray();
        }

        private static MethodInfo[] GetSessionEndMethods(Type[] types)
        {
            List<MethodInfo> toAdd = new List<MethodInfo>();
            for (int x = 0; x < types.Length; x++)
            {
                toAdd.AddRange(types[x].GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                    .Where(fetch => fetch.GetCustomAttribute<OnSessionEnd>() != null).ToList());
            }

            return toAdd.ToArray();
        }

        private static MethodInfo[] GetPerFrameMethods(Type[] types)
        {
            List<MethodInfo> toAdd = new List<MethodInfo>();
            for (int x = 0; x < types.Length; x++)
            {
                toAdd.AddRange(types[x].GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                    .Where(fetch => fetch.GetCustomAttribute<OnUpdate>() != null).ToList());
            }

            return toAdd.ToArray();
        }

        private static MethodInfo[] GetSynchronousStartMethods(Type[] types)
        {
            List<MethodInfo> toAdd = new List<MethodInfo>();
            for (int x = 0; x < types.Length; x++)
            {
                toAdd.AddRange(types[x].GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                    .Where(fetch => fetch.GetCustomAttribute<OnSynchronousStart>() != null).ToList());
            }

            return toAdd.ToArray();
        }

        private static MethodInfo[] GetSynchronousEndMethods(Type[] types)
        {
            List<MethodInfo> toAdd = new List<MethodInfo>();
            for (int x = 0; x < types.Length; x++)
            {
                toAdd.AddRange(types[x].GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                    .Where(fetch => fetch.GetCustomAttribute<OnSynchronousEnd>() != null).ToList());
            }

            return toAdd.ToArray();
        }
    }
}