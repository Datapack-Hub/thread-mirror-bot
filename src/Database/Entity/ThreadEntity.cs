using System.Collections.Generic;

public class ThreadEntity
{
    public int Id { get; set; }
    public ulong DiscordThreadId { get; set; }
    public string Title { get; set; }
    public int MessageCount { get; set; }

    public ICollection<MessageEntity> Messages { get; set; }
}
