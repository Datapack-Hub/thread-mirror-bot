using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

public class Program
{
    private DiscordSocketClient Client { get; set; }
    private AppConfig Config { get; set; }
    private readonly ConnectionGuard connectionGuard;
    private readonly DataProcessor dataProcessor;

    private Program(
        DiscordSocketClient client,
        AppConfig config,
        ConnectionGuard connectionGuard,
        DataProcessor dataProcessor)
    {
        Client = client;
        Config = config;
        this.connectionGuard = connectionGuard;
        this.dataProcessor = dataProcessor;
    }

    public static async Task Main(string[] args)
    {
        string token;

        if (File.Exists(args[0])) token = File.ReadAllText(args[0]);
        else
        {
            try
            {
                TokenUtils.ValidateToken(TokenType.Bot, args[0]);
                token = args[0];
            }
            catch
            {
                token = args.Length == 0 ? DemandToken() : args[0];
            }
        }

        var program = await InitProgramAsync(token);
        await program.RunAsync();
    }

    private static string DemandToken()
    {
        Console.Write("Please provide the new token for authentication or nothing to start without token.\nToken: ");
        var token = Console.ReadLine();
        
        return token.Length == 0 ? null : token;
    }

    private static async Task<Program> InitProgramAsync(string token)
    {
        var config = await AppConfig.InitConfigAsync();

        ConnectionGuard connectionGuard = new(config);

        DiscordSocketClient client = new();
        client.Log += Logger.Log;
        client.Disconnected += connectionGuard.OnDisconnect;
        client.Connected += connectionGuard.OnConnect;
        connectionGuard.StopConnecting += async () =>
        {
            await Logger.Log("Going into idle mode now.", LogSeverity.Info);
            await client.LogoutAsync();
            await client.StopAsync();
        };

        if (token == null) await Logger.Log("No token provided. Bot will start in idle mode.", LogSeverity.Warning);
        else
        {
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
        }

        DataProcessor dataProcessor = new();

        Program program = new(client, config, connectionGuard, dataProcessor);

        return program;
    }

    /// <summary>
    /// Starts the Main loop and background tasks of the program
    /// </summary>
    /// <returns></returns>
    private async Task RunAsync()
    {
        await ConsoleLoop();
    } 

    private async Task ConsoleLoop()
    {
        while (true)
        {
            #region Experimental Console
            // string input = "";
            // while (true)
            // {
            //     var keyInfo = Console.ReadKey();

            //     switch(keyInfo.Key)
            //     {
            //         case ConsoleKey.Enter:
            //             goto break_input_loop;
            //             break;
                    
            //         case ConsoleKey.Backspace:
            //             if (input.Length < 1) break;
            //             input = input[0..^1];
            //             Console.Write("\b\b");
            //             break;

            //         case ConsoleKey.LeftArrow:
            //             Console.
            //             break;

            //         case ConsoleKey.RightArrow:

            //         default:
            //             input += keyInfo.KeyChar;
            //             // Console.Write(keyInfo.KeyChar);
            //             break;
            //     }
            // }
            // break_input_loop: {}
            #endregion

            var input = Console.ReadLine();

            switch (input)
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
                        goto exit_console_loop;
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
                    if (token == null)
            
                    await Client.LoginAsync(TokenType.Bot, token);
                    await Client.StartAsync();
                    break;

                case "idle":
                    await Client.LogoutAsync();
                    await Client.StopAsync();
                    await Logger.Log("Now in idle mode", LogSeverity.Info);
                    break;

                case "fetch":
                    if (Client.ConnectionState != ConnectionState.Connected)
                    {
                        await Logger.Log("No connection yet", LogSeverity.Warning);
                        break;
                    }
                    await dataProcessor.FetchPosts(Client, Config);
                    break;

                default:
                    Console.Write($"\x1b[1F\x1b[K\x1b[31m'{input}' is not a valid command.\x1b[0m\n");
                    break;
            }
        }

        exit_console_loop:
            await Logger.Log("Console will close in 10 seconds.", LogSeverity.Info);
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