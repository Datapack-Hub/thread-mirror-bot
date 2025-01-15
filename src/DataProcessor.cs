using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Threading;
using System.Diagnostics;

public class DataProcessor
{
    private readonly string dataPath = Path.Join(AppContext.BaseDirectory, "data");
    public bool isFetchingData = false;

    public DataProcessor()
    {     
        if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
    }

    public async Task UpdateData(DiscordSocketClient client, AppConfig config)
    {
        if (isFetchingData)
        {
            _ = Logger.Log("Already fetching data. Wait till it finishes.", LogSeverity.Warning);
            return;
        }

        isFetchingData = true;

        var cancellationToken = TaskTracker.GetNewTrackedCancellationToken("update");

        try
        {
            _ = Logger.Log("Fetching data.", LogSeverity.Info);
            
            foreach (ulong channelId in config.HelpChannelIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var channel = await client.GetChannelAsync(channelId) as IForumChannel;
                if (channel == null)
                {
                    _ = Logger.Log($"The channel with id '{channelId}' is not a forum channel.", LogSeverity.Warning);
                    continue;
                }

                _ = Logger.Log($"Starting fetching posts from: {channel.Name}", LogSeverity.Info);

                var threads = await GetAllForumPosts(channel, config, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                await Task.Delay(1000);

                var processedThreads = await ProcessThreads(channel, threads, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                var jsonText = JsonSerializer.Serialize(processedThreads);
                using (StreamWriter file = new StreamWriter(Path.Join(dataPath, $"{channelId}.json")))
                {
                    await file.WriteAsync(jsonText);
                }
            }

            _ = Logger.Log("Data fetching complete.", LogSeverity.Info);
        }
        catch (OperationCanceledException)
        {
            _ = Logger.Log("Data fetching canceled.", LogSeverity.Info);
        }
        finally
        {
            isFetchingData = false;
        }
    }

    private async Task<List<HelpThread>> ProcessThreads(IForumChannel channel, IEnumerable<IThreadChannel> threads, CancellationToken cancellationToken)
    {
        const int batchSize = 50;
        Stopwatch sw = new();
        List<HelpThread> processedThreads = new();
        _ = Logger.Log("0 posts processed.", LogSeverity.Info);

        for (int i = 0; i < threads.Count(); i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var nextThreads = threads.Skip(i).Take(batchSize);
            var threadTasks = nextThreads.Select(async thread =>
            {
                var message = await thread.GetMessageAsync(thread.Id);

                return new HelpThread
                (
                    thread.Name,
                    message != null ? message.CleanContent : "No description", // CombineOwnerMessageSequence(thread.OwnerId, message),
                    CombineAppliedTags(channel, thread),
                    message != null ? message.GetJumpUrl() : $"https://discord.com/channels/{channel.GuildId}/{channel.Id}/{thread.Id}"
                );
            });

            sw.Restart();
            var processedTasks = await Task.WhenAll(threadTasks);

            processedThreads.AddRange(processedTasks);
            _ = Logger.Log($"{processedThreads.Count} posts processed.", LogSeverity.Info, true);

            sw.Stop();
            var millisecondDelay = 1010 - sw.Elapsed.Milliseconds;
            await Task.Delay(Math.Max(0, millisecondDelay));
        }

        _ = Logger.Log($"Finished processing posts.", LogSeverity.Info);

        return processedThreads;
    }

    /// <summary>
    /// Fetches all posts from the given forum channel that have been archived 24+ hours ago and are no test threads.
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="config"></param>
    /// <returns>A list of all old enough and non-test threads in the channel</returns>
    private async Task<IEnumerable<IThreadChannel>> GetAllForumPosts(IForumChannel channel, AppConfig config, CancellationToken cancellationToken)
    {
        var before = DateTimeOffset.Now.AddHours(-24);
        List<IThreadChannel> threadList = new();
        _ = Logger.Log("0 posts fetched.", LogSeverity.Info);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var allThreads = await channel.GetPublicArchivedThreadsAsync(before: before);
            if (allThreads.Count == 0) break;
            var threads = allThreads.Where(t =>
                t.AppliedTags.Intersect(config.ResolvedTagIds).Any() &&
                !t.Name.StartsWith("!n")
            );
            if (threads.Count() == 0) break;

            before = threads.Last().ArchiveTimestamp.DateTime;

            threadList.AddRange(threads);
            _ = Logger.Log($"{threadList.Count} posts fetched.", LogSeverity.Info, true);
        }

        _ = Logger.Log($"Finished fetching posts.", LogSeverity.Info);
        return threadList;
    }

    /// <summary>
    /// Combines the messages from the author of the first message in the array they are from the same author.
    /// It stops and returns as soon as it detects a different author.
    /// </summary>
    /// <param name="owner">Owner of the thread where the messages come from</param>
    /// <param name="messages"></param>
    /// <returns>Combined messages string or null if the first message is not from the thread owner</returns>
    private string CombineOwnerMessageSequence(ulong owner, IEnumerable<IMessage> messages)
    {
        if (messages.First().Author.Id != owner) return "No description";
        
        StringBuilder combinedMessage = new();
        var messageSequence = messages.Where(msg => !msg.Author.IsBot).TakeWhile(msg => msg.Author.Id == owner).Select(msg => msg.CleanContent);
        combinedMessage.AppendJoin('\n', messageSequence);
        return combinedMessage.ToString();
    }

    /// <summary>
    /// Combines all applied tags
    /// </summary>
    /// <param name="channel">The forum channel where the tags come from</param>
    /// <param name="thread">The Thread in that forum</param>
    /// <returns>Comma separated list of tags as a sting</returns>
    private string CombineAppliedTags(IForumChannel channel, IThreadChannel thread)
    {
        StringBuilder tagsString = new();
        var tagList = channel.Tags.IntersectBy(thread.AppliedTags, tag => tag.Id).Select(tag => tag.Name);
        tagsString.AppendJoin(", ", tagList);

        return tagsString.ToString();
    }
}