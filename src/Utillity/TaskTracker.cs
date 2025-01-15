using System.Collections.Generic;
using System.Threading;
using Discord;

/// <summary>
/// Only supposed to track tasks that are not vital for the programs functionality.
/// </summary>
public static class TaskTracker
{
    private static Dictionary<string, CancellationTokenSource> cancellationTokens = new();

    public static void CancelTask(string taskName)
    {
        var source = cancellationTokens[taskName];

        if (!source.IsCancellationRequested) source.Cancel();
    }

    public static CancellationToken GetNewTrackedCancellationToken(string taskName)
    {
        CancellationTokenSource source = new();
        cancellationTokens.Add(taskName, source);
        _ = Logger.Log($"New Task with the name \"{taskName}\" started.", LogSeverity.Info);
        return source.Token;
    }
}