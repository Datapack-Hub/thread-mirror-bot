using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

public static class DiscordSocketClientExt
{
    public static async Task TryConnect(this DiscordSocketClient client, DiscordTokenmanager tokenManager, string token = "")
    {
        string tkn;

        if (token.Length > 0) tkn = await tokenManager.CheckToken(token);
        else
        {
            tkn = await tokenManager.TryFindToken();
            if (token == null)
            {
                _ = Logger.Log("No token provided. Bot now in idle mode.", LogSeverity.Info);
                return;
            }
        }

        var botFinishConnecting = new TaskCompletionSource();
        Task CompleteBotFinishConnecting()
        {
            botFinishConnecting.SetResult();
            return Task.CompletedTask;
        }
        client.Ready += CompleteBotFinishConnecting;

        await client.LoginAsync(TokenType.Bot, tkn);
        await client.StartAsync();
        await botFinishConnecting.Task;
        client.Ready -= CompleteBotFinishConnecting;
    }

    public static async Task Disconnect(this DiscordSocketClient client)
    {
        await client.LogoutAsync();
        await client.StopAsync();
    }

    public static async Task Reconnect(this DiscordSocketClient client, DiscordTokenmanager tokenManager, string token = "")
    {
        await client.Disconnect();
        await client.TryConnect(tokenManager, token);
    }
}