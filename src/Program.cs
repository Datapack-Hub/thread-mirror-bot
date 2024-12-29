using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

public class Program
{
    private DiscordSocketClient Client { get; set; }
    private AppConfig Config { get; set; }
    private readonly ConnectionGuard connectionGuard;
    private readonly DataProcessor processor;

    private Program(
        DiscordSocketClient client,
        AppConfig config,
        ConnectionGuard connectionGuard,
        DataProcessor dataProcessor)
    {
        Client = client;
        Config = config;
        this.connectionGuard = connectionGuard;
        processor = dataProcessor;
    }

    public static async Task Main(string[] args)
    {
        string token;

        if (args.Length == 1) token = args[0];
        else
        {
            token = DemandToken();
            if (token == null)
            {
                await Logger.Log("Aborting Startup Process", LogSeverity.Info);
                return;
            }
        }
        var program = await InitProgramAsync(token);
        if (program == null)
        {
            await Logger.Log("Invalid token, aborting startup process", LogSeverity.Error);
            return;
        }
        await program.ConsoleLoop();
    }

    private static string DemandToken()
    {
        Console.Write("Please provide the new token for authentication.\nToken: ");
        var token = Console.ReadLine();
        
        if (token.Length == 0) token = null;
        
        return token;
    }

    private static async Task<Program> InitProgramAsync(string token)
    {
        var config = await AppConfig.InitConfigAsync();

        var connectionGuard = new ConnectionGuard(config);

        var client = new DiscordSocketClient();
        client.Log += Logger.Log;
        client.Disconnected += connectionGuard.OnDisconnect;
        client.Connected += connectionGuard.OnConnect;
        connectionGuard.StopConnecting += async () =>
        {
            await client.LogoutAsync();
            await client.StopAsync();
            await Logger.Log("Bot going into idle mode now.", LogSeverity.Info);
        };
        if (token == null) await Logger.Log("No token provided. Bot is now in idle mode", LogSeverity.Info);
        else
        {
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
        }

        var dataProcessor = new DataProcessor();

        var program = new Program(client, config, connectionGuard, dataProcessor);
        
        return program;
    }

    private async Task ConsoleLoop()
    {
        var dataProcessor = new DataProcessor();

        while (true)
        {
            var input = Console.ReadLine();
            switch(input)
            {
                case "help":
                    Console.WriteLine(helpMessage);
                    break;

                case "clear":
                    Console.Clear();
                    break;

                case "end":
                    Console.Write("\x1b[41mDisconnect and shut down the bot?\x1b[0m\n\"y\"=Yes, else=No: ");

                    var confirmation = Console.ReadLine();

                    if (confirmation == "y")
                    {
                        Console.Write("\x1b[2F\x1b[0J");
                        await Client.LogoutAsync();
                        await Client.StopAsync();
                        goto exit;
                    }
                    else Console.Write("\x1b[2F\x1b[0J\x1b[34mEnding Aborted\x1b[0m\n");
                    break;

                case "reload-config":
                    Config = await AppConfig.InitConfigAsync();
                    connectionGuard.Update(Config);
                    await Logger.Log("Config reload complete", LogSeverity.Info);
                    break;

                case "reauth":
                    await Client.LogoutAsync();
                    await Client.StopAsync();

                    var token = DemandToken();
                    
                    await Client.LoginAsync(TokenType.Bot, token);
                    await Client.StartAsync();
                    break;

                case "idle":
                    await Client.LogoutAsync();
                    await Client.StopAsync();
                    await Logger.Log("Bot in Idle mode now.", LogSeverity.Info);
                    break;

                case "fetch":
                    if (Client.ConnectionState != ConnectionState.Connected)
                    {
                        await Logger.Log("Bot is not connected.", LogSeverity.Warning);
                        break;
                    }
                    await dataProcessor.FetchPosts(Client, Config);
                    break;

                default:
                    Console.Write("\x1b[2F\x1b[0J\x1b[31mInvalid input\x1b[0m\n");
                    break;
            }
        }

        exit:
            await Logger.Log("Console will close in 10 seconds", LogSeverity.Info);
            await Task.Delay(1000 * 10);
    }

    private const string helpMessage = @"
Command List:

help                            Show this list.

clear                           Clear the console.

end                             Disconnect and shut down the bot after a confirmation.

reload-config                   Reads and applies the latest settings from the config file.

reauth                          Reauthenticate the bot with a new token.

idle                            Logs the bot out and sets it to idle mode.

fetch                           Manually fetch help thread data.";
}