using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

public class ConsoleCommand
{
    public void Help() => Console.WriteLine(@"
Command List:

help                            Show this list.

clear                           Clear the console.

exit                             Disconnect and shut down the bot after a confirmation.

reload-config                   Reads and applies the latest settings from the config file.

reauth dc|gh [<token>]          Reauthenticate the bot (dc) with a new discord bot token | (gh) with new github credentials.

github-deauth                   Removes github credentials and stops uploading generated data.

idle                            Logs the bot out and sets it to idle mode.

update                          Manually updates the help forum data.

push                            Manually pushes help forum data to github.

update-push                     Manually updates and pushes help forum data

stop-task <task name>           Stops the task with the given name.
");

    public void Clear() => Console.Clear();

    public async Task<bool> Exit(DiscordSocketClient client)
    {
        Console.Write("\x1b[41mDisconnect and shut down the bot?\x1b[0m\n\"y\"=Yes, else=No: ");

        var confirmation = Console.ReadLine();

        if (confirmation == "y")
        {
            Console.Write("\x1b[2F\x1b[0J");
            await client.LogoutAsync();
            await client.StopAsync();
            return true;
        }
        else Console.Write("\x1b[2F\x1b[0J\x1b[34mEnding Aborted\x1b[0m\n");
        return false;
    }

    public async Task ReloadConfig(ConnectionGuard connectionGuard)
    {
        await AppConfig.InitNewConfigAsync();
        connectionGuard.Update();
        _ = Logger.Log("Config reload complete", LogSeverity.Info);
    }

    public async Task Reauth(string[] input, DiscordSocketClient client, DiscordTokenmanager tokenManager)
    {
        var token = input.Length == 3 ? input[2] : "";
        await client.Reconnect(tokenManager, token);
    }

    public async Task Reauth(string[] input, Github github)
    {
        var token = input.Length == 3 ? input[2] : "";
        github.AuthenticateNewUser(token);
        await Task.CompletedTask;
    }

    public void GithubDeauthenticate(Github github) => github.DeAuthenticateCurrentUser();

    public async Task Idle(DiscordSocketClient client)
    {
        await client.Disconnect();
        _ = Logger.Log("Now in idle mode", LogSeverity.Info);
    }

    public async Task Update(DiscordSocketClient client, DataProcessor dataProcessor)
    {
        if (client.ConnectionState != ConnectionState.Connected)
        {
            _ = Logger.Log("No connection yet", LogSeverity.Warning);
            return;
        }

        await dataProcessor.UpdateData(client);
    }

    public async Task Push(Github github)
    {
        await github.PushData();
    }

    public async Task UpdatePush(DiscordSocketClient client, DataProcessor dataProcessor, Github github)
    {
        if (client.ConnectionState != ConnectionState.Connected)
        {
            _ = Logger.Log("No connection yet", LogSeverity.Warning);
            return;
        }

        await dataProcessor.UpdateData(client);
        await github.PushData();
    }

    public void StopTask(string[] input) => TaskTracker.CancelTask(input[1]);
}