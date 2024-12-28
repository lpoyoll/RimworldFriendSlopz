using GameServer.Core;
using GameServer.Misc;
using Shared;
using System.Reflection;

namespace GameServer.Managers
{
    public static class CompatibilityManager
    {
        public static void LoadAllPatchedAssemblies()
        {
            List<Assembly> toLoad = new List<Assembly>();
            foreach (string compatibility in CompatibilityManagerHelper.GetAllPatchedMods())
            {
                Assembly toAdd = LoadCustomAssembly(compatibility);
                if (toAdd != null) toLoad.Add(toAdd);
            }

            if (toLoad.Count > 0)
            {
                Master.loadedCompatibilityPatches = toLoad.ToArray();
                Printer.Warning($"Loaded > {Master.loadedCompatibilityPatches.Length} patches from '{Master.compatibilityPatchesPath}'");
                Printer.Warning($"CAUTION > Custom patches aren't created by the mod developers, always use them with care");
            }
        }

        private static Assembly LoadCustomAssembly(string assemblyPath)
        {
            try
            {
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                Type[] types = assembly.GetTypes().Where(type => 
                    type.Namespace != null 
                    && !type.Namespace.StartsWith("System") 
                    && !type.Namespace.StartsWith("Microsoft")
                ).ToArray();

                foreach (Type type in types)
                {
                    if (type.GetCustomAttributes(typeof(RTManager), false).Length != 0)
                    {
                        MethodInfo method = type.GetMethod("ParsePacket"); //This will automatically add managers from the loaded assembly using the attribute RTManager
                        if (method != null)
                        {
                            Master.managers[method.Name] = method;
                            Printer.Message($"Found modded manager {type.Name}", CommonEnumerators.LogImportanceMode.Verbose);
                            continue;
                        }
                    }
                    else if (type.GetCustomAttributes(typeof(RTStartupAttribute), false).Length != 0) //Checks for the attribute RTStartup and run the constructor
                    {
                        if (type.IsAbstract && type.IsSealed) //It needs to be static
                        {
                            ConstructorInfo constructor = type.TypeInitializer;
                            if (constructor != null)
                            {
                                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                                return assembly;
                            }
                            else Printer.Error($"Mod {assembly.FullName} has class {type.Name} with attribute 'RTStartup' but no constructor.");
                        }
                        else Printer.Error($"Mod {assembly.FullName} has class {type.Name} with attribute 'RTStartup' but isn't static.");
                    } 
                }
            }
            catch (Exception e) { Printer.Error($"Failed to load patch '{assemblyPath}'\n{e}"); }
            return null;
        }
    }

    public static class CompatibilityManagerHelper
    {
        public static readonly string fileExtension = ".dll";

        public static string[] GetAllPatchedMods()
        {
            return Directory.GetFiles(Master.compatibilityPatchesPath)
                .Where(fetch => fetch.EndsWith(fileExtension)).ToArray();
        }
    }
}
