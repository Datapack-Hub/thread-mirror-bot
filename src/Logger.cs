using System;
using System.Threading.Tasks;
using Discord;

public class Logger
{
    public static Task Log(LogMessage msg)
    {
        switch (msg.Severity)
        {
            case LogSeverity.Critical:
            case LogSeverity.Error:
                Console.Write($"\x1b[31m{msg.ToString()}\x1b[0m\n");
                break;

            case LogSeverity.Warning:
                Console.Write($"\x1b[33m{msg.ToString()}\x1b[0m\n");
                break;

            default:
                Console.Write($"\x1b[90m{msg.ToString()}\x1b[0m\n");
                break;
        }
        return Task.CompletedTask;
    }

    public static Task Log(string msg, LogSeverity severety) => Log(new LogMessage(severety, "Bot", msg));
}