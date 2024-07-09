using Refit;

namespace ClientDiscord.Models;

public class Guild
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public string Region { get; set; }
    public Channel [] Channels { get; set; }
    [AliasAs("afk_timeout")]
    public int AfkTimeout { get; set; }
}

public class CreateGuildRequest
{
    public string Name { get; set; }
    public string Region { get; set; }
    [AliasAs("afk_timeout")]
    public int AfkTimeout { get; set; }
    public string Description { get; set; }
}

public class UpdateGuildRequest
{
    public string Name { get; set; }
    public string Region { get; set; }
    [AliasAs("afk_timeout")]
    public int AfkTimeout { get; set; }
    public string Description { get; set; }
}