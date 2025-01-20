using System.Text.Json.Serialization;

public struct HelpThread
{
    public HelpThread(ulong id, string name, string description, string tags, string url)
    {
        Id = id;
        Name = name;
        Description = description;
        Tags = tags;
        Url = url;
    }

    /// <summary>
    /// Post id
    /// </summary>
    [JsonInclude] public ulong Id;

    /// <summary>
    /// Title of the help post
    /// </summary>
    [JsonInclude] public string Name;

    /// <summary>
    /// The inital message sent by the OP
    /// </summary>
    [JsonInclude] public string Description;

    /// <summary>
    /// A comma separated list of tags as a string
    /// </summary>
    [JsonInclude] public string Tags;

    /// <summary>
    /// The Url to the post
    /// </summary>
    [JsonInclude] public string Url;
};