using System;
using System.IO;
using System.Threading.Tasks;
using Discord;

public class DiscordTokenmanager
{
    public async Task<string> TryFindToken()
    {
        var token = File.ReadAllText(Path.Join("secrets", "bot_token"));
        return await CheckToken(token);
    }

    /// <summary>
    /// Checks if the given "token" is a file path to a token file, a real token or an invalid token.
    /// </summary>
    /// <param name="input"></param>
    /// <returns>A valid token or null if no token (empty token string) is provided</returns>
    public async Task<string> CheckToken(string input)
    {
        string token;

        if (File.Exists(input)) token = await CheckToken((await File.ReadAllTextAsync(Path.Join("secrets", input))).Trim());
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
                    Console.Write("\x1b[31mToken invalid!\x1b[0m\n");
                    token = await DemandToken();
                }
            }
        }

        return token;
    }

    /// <summary>
    /// Requests an input from the user. Either a token, the path to a token text file, or nothing.
    /// </summary>
    /// <returns>User input</returns>
    public async Task<string> DemandToken()
    {
        Console.Write("Please provide a token for Discord authentication or leave empty to go into idle mode without connecting.\nToken: ");
        var input = Console.ReadLine();

        return await CheckToken(input);
    }
}