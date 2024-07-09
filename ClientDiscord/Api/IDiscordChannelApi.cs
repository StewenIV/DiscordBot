using ClientDiscord.Models;
using Discord.Rest;
using Refit;

namespace ClientDiscord.Api;

public interface IDiscordChannelApi
{
    //получить канал гильдии 
    [Get("/guilds/{guildId}/channels")]
    Task<List<Channel>> GetGuildChannelsAsync([AliasAs("guildId")] string guildId);

    //создать канал гильдии 
    [Post("/guilds/{guildId}/channels")]
    Task<Channel> CreateChannelAsync([AliasAs("guildId")] string guildId, [Body] CreateChannelRequest request);

    //Обновить(изменить) позицию канала
    [Patch("/channels/{channelId}")]
    Task<Channel> UpdateChannelAsync([AliasAs("channelId")] string channelId , [Body] UpdateChannelRequest request);

    //удалить канал
    [Delete("/channels/{channelId}")]
    Task DeleteChannelAsync([AliasAs("channelId")] string channelId);
    
    
}