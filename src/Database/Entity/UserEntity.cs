using System.Collections.Generic;

public class UserEntity
{
    public int Id { get; set; }
    public ulong DiscordId { get; set; }
    public string Username { get; set; }

    public int QuestionsAsked { get; set; }
    public int AnswersGiven { get; set; }
    public int ThreadsParticipatedIn { get; set; }

    public ICollection<MessageEntity> Messages { get; set; }
}