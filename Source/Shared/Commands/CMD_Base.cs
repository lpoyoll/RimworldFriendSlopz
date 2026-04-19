using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using static Shared.Misc.Printer;

namespace Shared.Commands
{
    public abstract class CMD_Base
    {
        public string Prefix { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int ParameterCount { get; set; } = 0;

        public bool IsChatCommand { get; set; } = false;

        public abstract void Action();

        public static string[] CommandParameters { get; set; } = null;

        public static List<CMD_Base> Commands { get; set; } = new List<CMD_Base>();

        public static List<CMD_Base> ChatCommands { get; set; } = new List<CMD_Base>();

        private static Semaphore Semaphore { get; set; } = new Semaphore(1, 1);

        public static void GetAllCommands()
        {
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(fetch => fetch.IsSubclassOf(typeof(CMD_Base))))
            {
                CMD_Base command = (CMD_Base)Activator.CreateInstance(type);
                if (command.IsChatCommand) ChatCommands.Add(command);
                else
                {
                    Commands.Add(command);
                    Printer.Warning($"[Base] Added command '{type.Name}'", LogImportanceMode.Extreme);
                }
            }

            foreach (Type type in Assembly.GetCallingAssembly().GetTypes().Where(fetch => fetch.IsSubclassOf(typeof(CMD_Base))))
            {
                CMD_Base command = (CMD_Base)Activator.CreateInstance(type);
                if (command.IsChatCommand) ChatCommands.Add(command);
                else
                {
                    Commands.Add(command);
                    Printer.Warning($"[Main] Added command '{type.Name}'", LogImportanceMode.Extreme);
                }
            }
        }

        public static void ListenForCommands()
        {
            if (CheckIfConsoleIsInteractive())
            {
                while (true)
                {
                    ParseCommand(Console.ReadLine());
                }
            }

            else Thread.Sleep(1);
        }

        private static bool CheckIfConsoleIsInteractive()
        {
            try { return Console.In.Peek() != -1 ? true : false; }
            catch 
            { 
                Printer.Warning($"Couldn't find interactive console, disabling commands");
                return false;
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
