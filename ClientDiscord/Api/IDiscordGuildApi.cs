using ClientDiscord.Models;
using Discord.Rest;
using Refit;

namespace ClientDiscord.Api;

public interface IDiscordGuildApi
{
    //получить гильдии пользователя
    [Get("/users/@me/guilds")]
    Task<List<Guild>> GetUserGuildsAsync();

    //создать гильдию
    [Post("/guilds")]
    Task<Guild> CreateGuildAsync([Body] CreateGuildRequest request);
    
    //обновить гильдию
    [Patch("/guilds/{guildId}")]
    Task<Guild> UpdateGuildAsync([AliasAs("guildId")] string guildId, [Body] UpdateGuildRequest request);

    //удалить гильдию
    [Delete("/guilds/{guildId}")]
    Task DeleteGuildAsync([AliasAs("guildId")] string guildId);
    
    //получить регионы для гильдии
    [Get("/voice/regions")]
    Task<List<GuildRegions>> GetVoiceRegionsAsync();
}