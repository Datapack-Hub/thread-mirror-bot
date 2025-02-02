using System;
using System.IO;
using System.Threading.Tasks;
using Discord;

public class DiscordTokenManager
{
    private readonly string secretsDir = Path.Join(AppContext.BaseDirectory, "secrets");

    /// <summary>
    /// Parse the token source (raw token or file path)
    /// </summary>
    /// <param name="source"></param>
    /// <returns>valid token or an empty string</returns>
    public async Task<string> ParseTokenSource(string source = "bot_token")
    {
        string src = source;
        string token = null;

        while (token == null)
        {
            if (File.Exists(Path.Join(secretsDir, src))) src = await TryFindToken(src);
            
            src = CheckToken(src);
            
            if (src == null) src = DemandTokenSource();
            else token = src;
        }

        return token;
    }

    /// <summary>
    /// Tries to find a token file and read it
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns>token from the file</returns>
    private async Task<string> TryFindToken(string fileName)
    {
        var token = await File.ReadAllTextAsync(Path.Join(secretsDir, fileName));
        return token.Trim();
    }

    /// <summary>
    /// Checks if the given token is a valid.
    /// </summary>
    /// <param name="input"></param>
    /// <returns>A valid token or an null</returns>
    private string CheckToken(string inToken)
    {
        string outToken = null;
        
        try
        {
            TokenUtils.ValidateToken(TokenType.Bot, inToken);
            outToken = inToken;
        }
        catch
        {
            Console.Write("\x1b[31mToken invalid!\x1b[0m\n");
        }

        return outToken;
    }

    /// <summary>
    /// Requests an input from the user. Either a token, the path to a token text file, or nothing.
    /// </summary>
    private string DemandTokenSource()
    {
        Console.Write("Please provide a token for Discord authentication or leave empty to go into idle mode without connecting.\nToken: ");
        var input = Console.ReadLine().Trim();
        return input;
    }
}