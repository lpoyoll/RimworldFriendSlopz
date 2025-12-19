namespace Shared;

public static class StringChecker
{
    private static string[] IllegalChars { get; } =
    [
        "<", ">", ":", "\"", "/", "|", "?", "*", "CON", "PRN", "AUX", "NUL", "COM1",
        "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7",
        "LPT8", "LPT9", " "
    ];

    public static bool CheckIfStringValid(string toCheck)
    {
        Printer.Warning("t");
        if (string.IsNullOrEmpty(toCheck)) return false;
        else if (string.IsNullOrWhiteSpace(toCheck)) return false;
        Printer.Warning("t2");
        foreach (string str in IllegalChars)
        {
            if (toCheck.Contains(str)) return false;
        }
        
        return true;
    }
}