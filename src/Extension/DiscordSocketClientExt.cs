using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

public static class DiscordSocketClientExt
{
    public static async Task TryConnect(this DiscordSocketClient client, DiscordTokenManager tokenManager, string tokenSource = "")
    {
        string token = await tokenManager.ParseTokenSource(tokenSource);

        if (token == "")
        {
            _ = Logger.Log("No token provided. Bot now in idle mode.", LogSeverity.Info);
            return;
        }
        
        var botFinishConnecting = new TaskCompletionSource();
        Task CompleteBotFinishConnecting()
        {
            botFinishConnecting.SetResult();
            return Task.CompletedTask;
        }
        client.Ready += CompleteBotFinishConnecting;

        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();
        await botFinishConnecting.Task;
        client.Ready -= CompleteBotFinishConnecting;
    }

    public static async Task Disconnect(this DiscordSocketClient client)
    {
        await client.LogoutAsync();
        await client.StopAsync();
    }

    public static async Task Reconnect(this DiscordSocketClient client, DiscordTokenManager tokenManager, string token = "")
    {
        await client.Disconnect();
        await client.TryConnect(tokenManager, token);
    }
}