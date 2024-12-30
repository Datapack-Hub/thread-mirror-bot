using System;
using System.Threading.Tasks;
using Discord;

public class Logger
{
    public static Task Log(LogMessage msg)
    {
        var message = msg.Severity switch
        {
            LogSeverity.Critical or
            LogSeverity.Error => $"\x1b[31m{msg.ToString()}\x1b[0m\n",

            LogSeverity.Warning => $"\x1b[33m{msg.ToString()}\x1b[0m\n",
            
            _ => $"\x1b[90m{msg.ToString()}\x1b[0m\n"
        };

        Console.WriteLine(message);
        return Task.CompletedTask;
    }

    public static Task Log(string msg, LogSeverity severety) => Log(new LogMessage(severety, "Bot", msg));
}