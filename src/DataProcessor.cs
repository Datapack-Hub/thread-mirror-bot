using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

public class DataProcessor
{

    public async Task FetchPosts(DiscordSocketClient client, AppConfig config)
    {
        await Logger.Log("Fetching data.", LogSeverity.Info);
        
        foreach (ulong channelId in config.HelpChannelIds)
        {
            StringBuilder threadList = new();
            int threadCounter = 0;

            var channel = await client.GetChannelAsync(channelId) as IForumChannel;
            if (channel == null)
            {
                await Logger.Log($"The channel with id '{channelId}' is not a forum channel.", LogSeverity.Error);
                continue;
            }

            var before = DateTimeOffset.Now.AddHours(-24);
            await Logger.Log($"0 posts from channel {channel.Name} processed.", LogSeverity.Info);
            while (true)
            {
                var allPosts = await channel.GetPublicArchivedThreadsAsync(before: before);
                if (allPosts.Count == 0) break;
                var posts = allPosts.Where(p =>
                    p.AppliedTags.Intersect(config.ResolvedTagIds).Any() &&
                    !p.Name.StartsWith("!n") &&
                    !p.Name.StartsWith("[!]")
                );
                if (posts.Count() == 0) break;

                before = posts.Last().ArchiveTimestamp.DateTime;

                foreach (var post in posts)
                {
                    // var messages = await post.GetMessagesAsync().FlattenAsync();

                    threadList.Append($"POST:  {post.Name}\nTIMESTAMP:  {post.ArchiveTimestamp.DateTime}\n\n");
                    threadCounter++;

                    Console.Write("\x1b[1F\x1b[2K");
                    await Logger.Log($"{threadCounter} posts from channel {channel.Name} processed.", LogSeverity.Info);
                }
            }

            threadList.Insert(0, $"FORUM: {channel.Name}\nCOUNT: {threadCounter}\n\n\n");

            using (StreamWriter file = new StreamWriter(Path.Join(AppContext.BaseDirectory, $"{channelId}.txt")))
            {
                await file.WriteAsync(threadList.ToString());
            }
        }

        await Logger.Log("Data fetching complete.", LogSeverity.Info);
    }
}