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
        
        token = args.Length == 1 ? CheckToken(args[0]) : DemandToken();

        var program = await InitProgramAsync(token);
        await program.RunAsync();
    }

    /// <summary>
    /// Checks if the given "token" is a file path to a token file, a real token or an invalid token.
    /// </summary>
    /// <param name="input"></param>
    /// <returns>A valid token or null if no token (empty token string) is provided</returns>
    private static string CheckToken(string input)
    {
        string token = null;

        if (File.Exists(input)) token = CheckToken(File.ReadAllText(input));
        else
        {
            try
            {
                TokenUtils.ValidateToken(TokenType.Bot, input);
                token = input;
            }
            catch
            {
                if (input.Length == 0) token = null;
                else
                {
                    Console.Write("\x1b[31mInput token invalid!\x1b[0m\n");
                    DemandToken();
                }
            }
        }

        return token;
    }

    /// <summary>
    /// Requests an input from the user. Either a token, the path to a token text file, or nothing.
    /// </summary>
    /// <returns>User input</returns>
    private static string DemandToken()
    {
        Console.Write("Please provide a token for authentication or leave empty to go into idle mode without connecting.\nToken: ");
        var input = Console.ReadLine();

        return CheckToken(input);
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
            Logger.Log("Going into idle mode now.", LogSeverity.Info);
            await client.LogoutAsync();
            await client.StopAsync();
        };

        if (token == null) Logger.Log("No token provided. Bot will start in idle mode.", LogSeverity.Warning);
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
    /// Starts the console loop and background tasks of the program
    /// </summary>
    /// <returns></returns>
    private async Task RunAsync()
    {
        Task[] tasks =
        [
            ConsoleLoopAsync()
        ];

        await Task.WhenAll(tasks);
    } 

    private async Task ConsoleLoopAsync()
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

            var continueLoop = await EvaluateInputAsync(input);
            if (!continueLoop) break;
        }
            Logger.Log("Bot shutting down.", LogSeverity.Info);
            await Task.Delay(1000 * 1);
    }

    private async Task<bool> EvaluateInputAsync(string input)
    {
        string[] processedInput = input.Split(' ');

        switch (processedInput[0])
        {
            case "help":
                if (processedInput.Length > 1)
                {
                    Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                }
                Console.WriteLine(helpMessage);
                break;

            case "clear":
                if (processedInput.Length > 1)
                {
                    Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                        break;
                }
                Console.Clear();
                break;

            case "end":
                if (processedInput.Length > 1)
                {
                    Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                }

                Console.Write("\x1b[41mDisconnect and shut down the bot?\x1b[0m\n\"y\"=Yes, else=No: ");

                var confirmation = Console.ReadLine();

                if (confirmation == "y")
                {
                    Console.Write("\x1b[2F\x1b[0J");
                    await Client.LogoutAsync();
                    await Client.StopAsync();
                    return false;
                }
                else Console.Write("\x1b[2F\x1b[0J\x1b[34mEnding Aborted\x1b[0m\n");
                break;

            case "reload-config":
                if (processedInput.Length > 1)
                {
                    Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                }

                Config = await AppConfig.InitConfigAsync();
                connectionGuard.Update(Config);
                Logger.Log("Config reload complete", LogSeverity.Info);
                break;

            case "reauth":
                if (processedInput.Length > 2)
                {
                    Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                }

                await Client.LogoutAsync();
                await Client.StopAsync();

                string token;

                if (processedInput.Length == 2) token = CheckToken(processedInput[1]);
                else
                {
                    token = DemandToken();
                    if (token == null)
                    {
                        Logger.Log("No token provided. Bot now in idle mode.", LogSeverity.Info);
                        break;
                    }
                }

                await Client.LoginAsync(TokenType.Bot, token);
                await Client.StartAsync();
                break;

            case "idle":
                if (processedInput.Length > 1)
                {
                    Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                }

                await Client.LogoutAsync();
                await Client.StopAsync();
                Logger.Log("Now in idle mode", LogSeverity.Info);
                break;

            case "update":
                if (processedInput.Length > 1)
                {
                    Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                }

                if (Client.ConnectionState != ConnectionState.Connected)
                {
                    Logger.Log("No connection yet", LogSeverity.Warning);
                    break;
                }
                
                _ = dataProcessor.UpdateData(Client, Config);
                break;
            
            case "stop":
                if (processedInput.Length > 2)
                {
                    Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                }
                
                TaskTracker.CancelTask(processedInput[1]);
                break;

            default:
                Console.Write($"\x1b[1F\x1b[K\x1b[31m'{input}' is not a valid command.\x1b[0m\n");
                break;
        }

        return true;
    }

    private const string helpMessage = @"
Command List:

help                            Show this list.

clear                           Clear the console.

end                             Disconnect and shut down the bot after a confirmation.

reload-config                   Reads and applies the latest settings from the config file.

reauth                          Reauthenticate the bot with a new token.

idle                            Logs the bot out and sets it to idle mode.

update                          Manually updates the help forum data.

stop <task name>                Stops the task with the given name.
";
}