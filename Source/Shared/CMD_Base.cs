using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Shared.Misc.Printer;

namespace Shared
{
    public abstract class CMD_Base
    {
        public string Prefix { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int ParameterCount { get; set; } = 0;

        public abstract void Action();

        public static string[] CommandParameters { get; set; } = null;

        public static List<CMD_Base> Commands { get; set; } = new List<CMD_Base>();

        private static Semaphore Semaphore { get; set; } = new Semaphore(1, 1);

        private static bool InteractiveConsole { get; set; } = false;

        public static void GetAllCommands()
        {
            foreach (Type type in Assembly.GetCallingAssembly().GetTypes().Where(fetch => fetch.IsSubclassOf(typeof(CMD_Base))))
            {
                Commands.Add((CMD_Base)Activator.CreateInstance(type));
                Printer.Warning($"Added command '{type.Name}' to server", LogImportanceMode.Extreme);
            }
        }

        public static void ListenForCommands()
        {
            try { InteractiveConsole = Console.In.Peek() != -1 ? true : false; }
            catch { Printer.Warning($"Couldn't find interactive console, disabling commands"); }

            if (InteractiveConsole)
            {
                while (true)
                {
                    ParseCommand(Console.ReadLine());
                }
            }
        }

        private static void ParseCommand(string input)
        {
            Semaphore.WaitOne();

            try
            {
                int parameterCount = input.Split(' ').Length - 1;
                string parsedPrefix = input.Split(' ')[0].ToLower();
                CommandParameters = input.Replace(parsedPrefix + " ", "").Split(' ');

                CMD_Base toFetch = Commands.FirstOrDefault(x => x.Prefix == parsedPrefix);
                if (toFetch == null) Printer.Warning($"Command '{parsedPrefix}' was not found");
                else
                {
                    if (toFetch.ParameterCount == parameterCount || toFetch.ParameterCount == 0 || toFetch.ParameterCount == -1) toFetch.Action();
                    else Printer.Warning($"Wrong parameter count for '{toFetch.Prefix}'");
                }
            }
            catch (Exception ex) { Printer.Error(ex); }

            Semaphore.Release();
        }
    }
}
