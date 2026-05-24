using System;

namespace Shared.Misc
{
    public class Printer
    {
        public static Printer Instance { get; private set; } = null;

        public virtual Action<object, Verbosity> OnMessage { get; set; }

        public virtual Action<object, Verbosity> OnWarning { get; set; }

        public virtual Action<object, Verbosity> OnError { get; set; }

        public virtual Action<object, Verbosity> OnTitle { get; set; }

        public static string SeparatorString { get; set; } = "--------------------------------------------------";
        
        public enum LogMode { Message, Warning, Error, Title, Outsider }

        public enum Verbosity { Normal, Verbose, Extreme }

        public Printer(Action<object, Verbosity> onMessage, Action<object, Verbosity> onWarning, Action<object, 
            Verbosity> onError, Action<object, Verbosity> onTitle)
        {
            Instance = this;

            OnMessage = onMessage;
            OnWarning = onWarning;
            OnError = onError;
            OnTitle = onTitle;
        }

        public static void Message(object toPrint, Verbosity mode = Verbosity.Normal)
        {
            Instance.OnMessage.Invoke(toPrint, mode);
        }

        public static void Warning(object toPrint, Verbosity mode = Verbosity.Normal)
        {
            Instance.OnWarning.Invoke(toPrint, mode);
        }

        public static void Error(object toPrint, Verbosity mode = Verbosity.Normal)
        {
            Instance.OnError.Invoke(toPrint, mode);
        }

        public static void Title(object toPrint, Verbosity mode = Verbosity.Normal)
        {
            Instance.OnTitle.Invoke(toPrint, mode);
        }
    }
}
