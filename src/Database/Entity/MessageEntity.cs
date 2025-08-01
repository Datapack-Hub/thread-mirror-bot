using System;

public class MessageEntity
{
    public int Id { get; set; }
    public ulong MessageId { get; set; }
    public ulong ThreadId { get; set; }
    public DateTime Timestamp { get; set; }

    public int UserId { get; set; }
    public UserEntity User { get; set; }
}