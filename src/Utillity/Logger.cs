using System;
using System.Threading.Tasks;
using Discord;

public static class Logger
{
    private static async Task FormatAndLogMessage(LogMessage msg, bool overwriteLastMessage = false)
    {
        var message = msg.Severity switch
        {
            LogSeverity.Critical or
            LogSeverity.Error => $"\x1b[31m{msg.ToString()}\x1b[0m",

            LogSeverity.Warning => $"\x1b[33m{msg.ToString()}\x1b[0m",

            _ => $"\x1b[90m{msg.ToString()}\x1b[0m"
        };

        if (overwriteLastMessage) Console.WriteLine($"\x1b[s\x1b[1F\x1b[2K{message}\x1b[s"); //BROKEN but idc rn
        else Console.WriteLine(message);
        await Task.CompletedTask;
    }

    public static async Task Log(LogMessage msg) => await FormatAndLogMessage(msg);

    public static async Task Log(string msg, LogSeverity severity, bool overwriteLastMessage = false) => await FormatAndLogMessage(new LogMessage(severity, "Bot", msg), overwriteLastMessage);

    public static void RemoveLastLogMessage() => Console.Write("\x1b[1F\x1b[2K");
}