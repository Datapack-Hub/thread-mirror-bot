using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

public class Program
{
    private static readonly DiscordTokenmanager tokenManager = new();
    private readonly DiscordSocketClient client;
    private readonly ConnectionGuard connectionGuard;
    private readonly Github github;
    private readonly DataProcessor dataProcessor;

    private Program(
        DiscordSocketClient client,
        ConnectionGuard connectionGuard,
        DataProcessor dataProcessor,
        Github github)
    {
        this.client = client;
        this.connectionGuard = connectionGuard;
        this.dataProcessor = dataProcessor;
        this.github = github;
    }

    public static async Task Main(string[] args)
    {
        string token;

        token = args.Length > 0 ? await tokenManager.ParseTokenSource(args[0]) : await tokenManager.ParseTokenSource();

        var program = await InitProgramAsync(token);
        await program.RunAsync();
    }

    private static async Task<Program> InitProgramAsync(string token)
    {
        await AppConfig.InitNewConfigAsync();

        ConnectionGuard connectionGuard = new();

        DiscordSocketClient client = new();
        client.Log += Logger.Log;
        client.Disconnected += connectionGuard.OnDisconnect;
        client.Connected += connectionGuard.OnConnect;
        connectionGuard.StopConnecting += async () =>
        {
            _ = Logger.Log("Going into idle mode now.", LogSeverity.Info);
            await client.Disconnect();
        };
        
        if (token == "") _ = Logger.Log("No token provided. Bot will start in idle mode.", LogSeverity.Warning);
        else await client.TryConnect(tokenManager, token);

        var github = await Github.InitGithubAsync();

        DataProcessor dataProcessor = new();

        Program program = new(client, connectionGuard, dataProcessor, github);

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

            if(await EvaluateInputAsync(input)) break;
        }

        _ = Logger.Log("Bot shutting down.", LogSeverity.Info);
        await Task.Delay(1000 * 1);
    }

    /// <summary>
    /// Evaluates the commandline input.
    /// </summary>
    /// <param name="input"></param>
    /// <returns>Should exit console loop</returns>
    private async Task<bool> EvaluateInputAsync(string input)
    {
        ConsoleCommand cmd = new();
        string[] processedInput = input.Split(' ');
        bool shouldConsoleLoopExit = false;

        switch (processedInput[0])
        {
            case "help":
                if (processedInput.Length > 1)
                {
                    _ = Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                }
                cmd.Help();
                break;

            case "clear":
                if (processedInput.Length > 1)
                {
                    _ = Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                }
                cmd.Clear();
                break;

            case "exit":
                if (processedInput.Length > 1)
                {
                    _ = Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                }
                shouldConsoleLoopExit = await cmd.Exit( client);
                break;

            case "reload-config":
                if (processedInput.Length > 1)
                {
                    _ = Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                }
                await cmd.ReloadConfig(connectionGuard);
                break;

            case "reauth":
                if (processedInput.Length > 3)
                {
                    _ = Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                } 
                else if(processedInput.Length < 2)
                {
                    _ = Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                }
                if (processedInput[1] == "dc") await cmd.Reauth(processedInput, client, tokenManager);
                else if (processedInput[1] == "gh") await cmd.Reauth(processedInput, github);
                else goto invalid_input;
                break;

            case "github-deauth":
                cmd.GithubDeauthenticate(github);
                break;

            case "idle":
                if (processedInput.Length > 1)
                {
                    _ = Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                }
                await cmd.Idle(client);
                break;

            case "update":
                if (processedInput.Length > 1)
                {
                    _ = Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                }
                _ = cmd.Update(client, dataProcessor);
                break;

            case "push":
                if (processedInput.Length > 1)
                {
                    _ = Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                }
                _ = cmd.Push(github);
                break;


            case "update-push":
                if (processedInput.Length > 1)
                {
                    _ = Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                }
                _ = cmd.UpdatePush(client, dataProcessor, github);
                break;

            case "stop-task":
                if (processedInput.Length > 2)
                {
                    _ = Logger.Log("Too many arguments for this command!", LogSeverity.Error);
                    break;
                }
                cmd.StopTask(processedInput);
                break;

            default:
                invalid_input:
                Console.Write($"\x1b[1F\x1b[K\x1b[31m'{input}' is not a valid command.\x1b[0m\n");
                break;
        }

        return shouldConsoleLoopExit;
    }
}