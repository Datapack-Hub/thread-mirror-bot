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
            var channel = await client.GetChannelAsync(channelId) as IThreadChannel;
            if (channel == null)
            {
                await Logger.Log($"The channel with id '{channelId}' is not a forum channel.", LogSeverity.Error);
                continue;
            }
            var threads = await channel.GetActiveThreadsAsync();
            Console.WriteLine($"Thread NUM: {threads.Count}");

            foreach (var thread in threads)
            {
                var messages = await thread.GetMessagesAsync().FlattenAsync();

                Console.Write($"THREAD:  {thread.Name} - {(DateTime.Now - thread.ArchiveTimestamp.DateTime).CompareTo(TimeSpan.FromHours(24))}\n\n");
                foreach (var msg in messages.Reverse())
                {
                    if (msg.Author.IsBot) continue;
                    Console.WriteLine($"{msg.Author}:\n\t{msg.Content}");
                }
                Console.WriteLine("#############################################");
            }
        }
    }
}