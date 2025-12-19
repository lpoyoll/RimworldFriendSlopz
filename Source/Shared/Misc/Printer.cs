using System;
using static Shared.CommonEnumerators;

namespace Shared;

public class Printer
{
    private static Printer Instance = null;

    protected virtual Action<object, LogImportanceMode> OnMessage { get; set; }

    protected virtual Action<object, LogImportanceMode> OnWarning { get; set; }

    protected virtual Action<object, LogImportanceMode> OnError { get; set; }

    protected virtual Action<object, LogImportanceMode> OnTitle { get; set; }

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