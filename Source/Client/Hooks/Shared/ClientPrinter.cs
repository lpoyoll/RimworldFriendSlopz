using GameClient.Core.Configs;
using GameClient.Misc;
using Shared.Misc;
using System;
using UnityEngine;
using Verse;
using static Shared.Misc.Printer;

namespace GameClient.Hooks.Shared
{
    public static class ClientPrinter
    {
        private static readonly Color RTColor = new Color(140f, 0f, 255f);
        
        public static void CreateLogger()
        {
            Action<object, Verbosity> onMessage = delegate (object value, Verbosity importance)
            {
                if (CheckIfShouldPrint(importance)) MainThreadHandler.Instance.Enqueue(() => { WriteToConsole(value.ToString(), LogMode.Message, importance); });
            };

            Action<object, Verbosity> onWarning = delegate (object value, Verbosity importance)
            {
                if (CheckIfShouldPrint(importance)) MainThreadHandler.Instance.Enqueue(() => { WriteToConsole(value.ToString(), LogMode.Warning, importance); });
            };

            Action<object, Verbosity> onError = delegate (object value, Verbosity importance)
            {
                if (CheckIfShouldPrint(importance)) MainThreadHandler.Instance.Enqueue(() => { WriteToConsole(value.ToString(), LogMode.Error, importance); });
            };

            Printer printer = new Printer(onMessage, onWarning, onError, null);
        }

        private static void WriteToConsole(string text, LogMode mode, Verbosity importance)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            if (CheckIfShouldPrint(importance))
            {
                string toWrite = $"[RT] > {text}";


                switch (mode)
                {
                    case LogMode.Message:
                        Log.Message(toWrite.Colorize(RTColor));
                        break;

                    case LogMode.Warning:
                        Log.Warning(toWrite.Colorize(RTColor));
                        break;

                    case LogMode.Error:
                        Log.Error(toWrite);
                        break;

                    default:
                        throw new Exception($"[RT] > Logger was passed invalid arguments");
                }
            }
        }

        private static bool CheckIfShouldPrint(Verbosity importance)
        {
            if (importance == Verbosity.Normal) return true;
            else if (importance == Verbosity.Verbose && ModConfigGetter.CurrentVerboseMode >= Printer.Verbosity.Verbose) return true;
            else if (importance == Verbosity.Extreme && ModConfigGetter.CurrentVerboseMode >= Printer.Verbosity.Extreme) return true;
            else if (importance == Verbosity.Extreme && ModConfigGetter.CurrentVerboseMode >= Printer.Verbosity.Extreme) return true;
            else return false;
        }
    }
}
