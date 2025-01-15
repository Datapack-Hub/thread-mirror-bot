using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Octokit;

public class Github
{
    private readonly string authDataPath = Path.Join("secrets", "github_auth");
    private GitHubClient githubClient = new(new ProductHeaderValue("datapack-hub-help-thread-mirror"));
    private bool hasValidCredentials = false;

    public static async Task<Github> InitGithubAsync()
    {
        Github github = new();
        await github.TryFindCredentials();

        return github;
    }

    public void DeAuthenticate()
    {
        githubClient.Credentials = null;
        hasValidCredentials = false;

        _ = Logger.Log("Removed github authentication.", LogSeverity.Info);
    }

    // Changes Credentials to the new user
    public async Task Authenticate(string token = "")
    {
        if (token.Length == 0) await TryFindCredentials();
        else await CheckCredentials(new Credentials(token));
    }

    public async Task TryFindCredentials()
    {
        Credentials credentials;
        if(File.Exists(authDataPath))
        {
            var token = (await File.ReadAllTextAsync(authDataPath)).Trim();
            credentials = new(token);
        }
        else credentials = await DemandCredentials();

        await CheckCredentials(credentials);

        if (hasValidCredentials) _ = Logger.Log("Set github credentials.", Discord.LogSeverity.Info);
        else _ = Logger.Log("No valid github credentials available.", Discord.LogSeverity.Info);
    }

    public async Task<Credentials> CheckCredentials(Credentials credentials)
    {
        if (credentials == null)
        {
            hasValidCredentials = false;
            return null;
        }

        githubClient.Credentials = credentials;
        
        try
        {
            await githubClient.User.Current();
            hasValidCredentials = true;
            return credentials;
        }
        catch
        {
            hasValidCredentials = false;
            return await DemandCredentials();
        }
    }

    public async Task<Credentials> DemandCredentials()
    {
        Console.Write("Please provide an auth token for Github authentication or leave empty if you don't want to upload generated data to a remote repository.\nToken: ");
        var token = Console.ReadLine();
        if (token.Length == 0) return null;
        
        return await CheckCredentials(new Credentials(token));
    }
}