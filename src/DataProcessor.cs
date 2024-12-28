using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

public class DataProcessor
{

    // BROKEN
    public async Task FetchPosts(DiscordSocketClient client, AppConfig config)
    {
        foreach (ulong channelId in config.HelpChannelIds)
        {
            var channel = await client.GetChannelAsync(channelId) as IForumChannel;
            if (channel == null)
            {
                await Logger.Log($"The channel with id '{channelId}' is not a forum channel.", LogSeverity.Error);
                continue;
            }
            var allPosts = await channel.GetPublicArchivedThreadsAsync(limit: 10);
            var posts = allPosts.Where(p =>
                DateTime.Now.AddHours(-24).CompareTo(p.ArchiveTimestamp.DateTime) > 0
            );

            Console.WriteLine($"FORUM: {channel.Name}");
            Console.WriteLine($"POSTCOUNT: {posts.Count()}");

            foreach (var post in posts)
            {
                // var messages = await post.GetMessagesAsync().FlattenAsync();

                Console.WriteLine($"POST:  {post.Name}");
                Console.WriteLine($"TIMESTAMP:  {post.ArchiveTimestamp.DateTime}");
                // foreach (var msg in messages.Reverse())
                // {
                //     if (msg.Author.IsBot) continue;
                //     Console.WriteLine($"{msg.Author}:\n\t{msg.Content}");
                // }
                Console.WriteLine("\n\n\n");
            }
        }

        await Logger.Log("Data fetching complete", LogSeverity.Info);
    }
}