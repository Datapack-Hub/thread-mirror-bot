using System;
using System.Threading.Tasks;
using Discord;
using Discord.Net;

public class ConnectionGuard
{
    private int maxConnectionAttempts;
    private int disconnections = 0;

    public ConnectionGuard(AppConfig config) => maxConnectionAttempts = config.MaxConnectionAttempts;

    public void Update(AppConfig config) => maxConnectionAttempts = config.MaxConnectionAttempts;

    public Task OnDisconnect(Exception ex)
    {
        if(ex is HttpException)
        {
            if((ex as HttpException).Reason[0..3] == "401")
            {
                FireStopConnecting(ex.Message);
            }
        }
        else
        {
            disconnections++;
            if (disconnections >= maxConnectionAttempts) FireStopConnecting("Too many failed connection attempts");
        }

        return Task.CompletedTask;
    }

    public Task OnConnect()
    {
        disconnections = 0;
        return Task.CompletedTask;
    }

    private void FireStopConnecting(string logMsg)
    {
        Logger.Log(logMsg, LogSeverity.Warning);
        StopConnecting();
    }

    public event Func<Task> StopConnecting; 
}