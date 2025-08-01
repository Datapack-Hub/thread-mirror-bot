using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Octokit;
using FileMode = Octokit.FileMode;

public class Github
{
    private readonly string secretsDir = Path.Join(AppContext.BaseDirectory, "secrets");
    private readonly string pushDataDir = Path.Join(AppContext.BaseDirectory, "data");
    private GitHubClient githubClient = new(new ProductHeaderValue("datapack-hub-help-thread-mirror"));
    private bool hasValidCredentials = false;

    public static async Task<Github> InitGithubAsync()
    {
        Github github = new();
        await github.ParseTokenSource();

        return github;
    }

    public void DeAuthenticateCurrentUser()
    {
        githubClient.Credentials = null;
        hasValidCredentials = false;

        _ = Logger.Log("Removed github authentication.", LogSeverity.Info);
    }

    public void AuthenticateNewUser(string tokenSource = "github_auth") => _ = ParseTokenSource(tokenSource);

    /// <summary>
    /// Parse the token source (raw token or file path) and set credentials if valid or do nothing if it's an empty string.
    /// </summary>
    /// <param name="source"></param>
    private async Task ParseTokenSource(string source = "github_auth")
    {
        string src = source;

        while (!hasValidCredentials)
        {
            if (File.Exists(Path.Join(secretsDir, src))) src = await TryFindToken(src);

            await CheckToken(src);

            if (!hasValidCredentials) src = DemandCredentials();
        }
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

    private async Task CheckToken(string token)
    {
        githubClient.Credentials = new Credentials(token);
        
        try
        {
            await githubClient.User.Current();
            hasValidCredentials = true;
        }
        catch
        {
            hasValidCredentials = false;
        }
    }

    public string DemandCredentials()
    {
        Console.Write("Please provide an auth token for Github authentication or leave empty if you don't want to upload generated data to a remote repository.\nToken: ");
        var token = Console.ReadLine().Trim();
        
        return token;
    }

    public async Task PushData()
    {
        if (!hasValidCredentials)
        {
            _ = Logger.Log("Cannot push data to github. No valid credentials.", LogSeverity.Warning);
            return;
        }

        if (AppConfig.Data.RepositoryOwner == null)
        {
            _ = Logger.Log("RepositoryOwner configuration variable is not set.", LogSeverity.Warning);
            return;
        }

        if (AppConfig.Data.RepositoryName == null)
        {
            _ = Logger.Log("RepositoryName configuration variable is not set.", LogSeverity.Warning);
            return;
        }

        if (AppConfig.Data.RepositoryBranch == null)
        {
            _ = Logger.Log("RepositoryBranch configuration variable is not set.", LogSeverity.Warning);
            return;
        }

        _ = Logger.Log("Starting data push.", LogSeverity.Info);

        Reference branchRef;
        try
        {
            branchRef = await githubClient.Git.Reference.Get(AppConfig.Data.RepositoryOwner, AppConfig.Data.RepositoryName, $"heads/{AppConfig.Data.RepositoryBranch}");
        }
        catch
        {
            _ = Logger.Log("Repository or branch not found.", LogSeverity.Warning);
            return;
        }
        var latestCommitSha = branchRef.Object.Sha;

        var latestCommit = await githubClient.Git.Commit.Get(AppConfig.Data.RepositoryOwner, AppConfig.Data.RepositoryName, latestCommitSha);

        var filePaths = Directory.GetFiles(pushDataDir);

        var newTree = new NewTree { BaseTree = latestCommit.Tree.Sha };

        foreach (var path in filePaths)
        {
            var newBlob = new NewBlob
            {
                Content = await File.ReadAllTextAsync(path),
                Encoding = EncodingType.Utf8
            };

            var blob = await githubClient.Git.Blob.Create(AppConfig.Data.RepositoryOwner, AppConfig.Data.RepositoryName, newBlob);

            newTree.Tree.Add(new NewTreeItem 
            {
                Path = $"{AppConfig.Data.RepositoryTargetPath}/{path.Split(Path.DirectorySeparatorChar)[^1]}",
                Mode = FileMode.File,
                Type = TreeType.Blob,
                Sha = blob.Sha
            });
        }

        var tree = await githubClient.Git.Tree.Create(AppConfig.Data.RepositoryOwner, AppConfig.Data.RepositoryName, newTree);

        var newCommit = new NewCommit("updating help channel posts", tree.Sha, latestCommitSha);
        var commit = await githubClient.Git.Commit.Create(AppConfig.Data.RepositoryOwner, AppConfig.Data.RepositoryName, newCommit);

        await githubClient.Git.Reference.Update(AppConfig.Data.RepositoryOwner, AppConfig.Data.RepositoryName, $"heads/{AppConfig.Data.RepositoryBranch}", new ReferenceUpdate(commit.Sha));

        _ = Logger.Log("Code pushed to github successfully", LogSeverity.Info);
    }
}