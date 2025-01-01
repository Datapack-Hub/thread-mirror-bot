using System;
using System.Threading.Tasks;
using Discord;

public static class Logger
{
    public static Task Log(LogMessage msg)
    {
        var message = msg.Severity switch
        {
            LogSeverity.Critical or
            LogSeverity.Error => $"\x1b[31m{msg.ToString()}\x1b[0m",

            LogSeverity.Warning => $"\x1b[33m{msg.ToString()}\x1b[0m",

            _ => $"\x1b[90m{msg.ToString()}\x1b[0m"
        };

        Console.WriteLine(message);

        return Task.CompletedTask;
    }

    public static Task Log(string msg, LogSeverity severity) => Log(new LogMessage(severity, "Bot", msg));
}