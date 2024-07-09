using Refit;

namespace ClientDiscord.Models;

public class Channel
{
    public string Id { get; set; }
    public int Type { get; set; }
    public int Position { get; set; }
    public string Topic { get; set; }
    public bool Nsfw { get; set; }
    [AliasAs("rate_limit_per_user")]
    public int RateLimitPerUser { get; set; }
    [AliasAs("permission_overwrites")]
    public List<object> PermissionOverwrites { get; set; }
    [AliasAs("guild_id")]
    public string GuildId { get; set; }
    public string Name { get; set; }
    [AliasAs("parent_id")]
    public string ParentId { get; set; }
}

public class CreateChannelRequest
{
    public string Name { get; set; }
    public string Topic { get; set; }
    public string Region { get; set; }
    [AliasAs("user_limit")]
    public int UserLimit { get; set; }
}

public class UpdateChannelRequest
{
    public string Name { get; set; }
    public string Topic { get; set; }
    public string Region { get; set; }
    [AliasAs("user_limit")]
    public int UserLimit { get; set; }
}