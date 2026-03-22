using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Shared.CommonEnumerators;

namespace Shared.Misc
{
    public class Printer
    {
        public static Printer Instance { get; private set; } = null;

        public virtual Action<object, LogImportanceMode> OnMessage { get; set; }

        public virtual Action<object, LogImportanceMode> OnWarning { get; set; }

        public virtual Action<object, LogImportanceMode> OnError { get; set; }

        public virtual Action<object, LogImportanceMode> OnTitle { get; set; }

        public static string SeparatorString { get; set; } = "--------------------------------------------------";
        
        public enum LogMode { Message, Warning, Error, Title, Outsider }

        public enum LogImportanceMode { Normal, Verbose, Extreme }

        public Printer(Action<object, LogImportanceMode> onMessage, Action<object, LogImportanceMode> onWarning, Action<object, 
            LogImportanceMode> onError, Action<object, LogImportanceMode> onTitle)
        {
            Instance = this;

            OnMessage = onMessage;
            OnWarning = onWarning;
            OnError = onError;
            OnTitle = onTitle;
        }

        public static void Message(object toPrint, LogImportanceMode mode = LogImportanceMode.Normal)
        {
            Instance.OnMessage.Invoke(toPrint, mode);
        }

        public static void Warning(object toPrint, LogImportanceMode mode = LogImportanceMode.Normal)
        {
            Instance.OnWarning.Invoke(toPrint, mode);
        }

        public static void Error(object toPrint, LogImportanceMode mode = LogImportanceMode.Normal)
        {
            Instance.OnError.Invoke(toPrint, mode);
        }

        public static void Title(object toPrint, LogImportanceMode mode = LogImportanceMode.Normal)
        {
            Instance.OnTitle.Invoke(toPrint, mode);
        }
    }
}
