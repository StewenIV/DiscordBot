using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json;
using ClientDiscord.Api;
using ClientDiscord.Models;
using ClientDiscord.Validators;
using FluentValidation;
using Refit;
using String = System.String;

public class Program
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private string _token = "YOUR_TOKEN_HERE";

    public static Task Main(string[] args) => new Program().MainAsync();

    
    private Program()
    {
        _client = new DiscordSocketClient();
        _interactions = new InteractionService(_client.Rest);
        _services = ConfigureServices().Result;
    }
    private async Task<IServiceProvider> ConfigureServices()
    {
        var services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_interactions)
            .AddSingleton<IDiscordGuildApi>(provider =>
            {
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri("https://discord.com/api/v10/"),
                    DefaultRequestHeaders =
                    {
                        { "Authorization", $"Bot {_token}" }
                    }
                };
                return RestService.For<IDiscordGuildApi>(httpClient);
            })
            .BuildServiceProvider();
        var guildApi = services.GetRequiredService<IDiscordGuildApi>();
        var regions = await guildApi.GetVoiceRegionsAsync();
        var allowedRegions = regions.Select(x => x.Id).ToList();
        services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_interactions)
            .AddSingleton<IDiscordGuildApi>(provider =>
            {
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri("https://discord.com/api/v10/"),
                    DefaultRequestHeaders =
                    {
                        { "Authorization", $"Bot {_token}" }
                    }
                };
                return RestService.For<IDiscordGuildApi>(httpClient);
            })
            .AddSingleton<IDiscordChannelApi>(provider =>
            {
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri("https://discord.com/api/v10/"),
                    DefaultRequestHeaders =
                    {
                        { "Authorization", $"Bot {_token}" }
                    }
                };
                return RestService.For<IDiscordChannelApi>(httpClient);
            })
            .AddSingleton<IValidator<CreateGuildRequest>>(provider => new GuildCreateValidator(allowedRegions))
            .AddSingleton<IValidator<UpdateGuildRequest>>(provider => new GuildUpdateValidator(allowedRegions))
            .AddSingleton<IValidator<CreateChannelRequest>>(provider => new ChannelCreateValidator(allowedRegions))
            .AddSingleton<IValidator<UpdateChannelRequest>>(provider => new ChannelUpdateValidator(allowedRegions))
            .BuildServiceProvider();
        return services;
    }

    
    private async Task MainAsync()
    {
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.InteractionCreated += InteractionCreatedAsync;

        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log);
        return Task.CompletedTask;
    }

    private async Task ReadyAsync()
    {
        Console.WriteLine($"{_client.CurrentUser} is connected!");
        await RegisterCommandsAsync();
    }

    private async Task RegisterCommandsAsync()
    {
        await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        await _interactions.RegisterCommandsGloballyAsync();
    }

    private async Task InteractionCreatedAsync(SocketInteraction interaction)
    {
        var ctx = new SocketInteractionContext(_client, interaction);
        await _interactions.ExecuteCommandAsync(ctx, _services);
    }
}

public class GuildModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IDiscordGuildApi _guildApi;
    private readonly IValidator<UpdateGuildRequest> _updateValidator;
    private readonly IValidator<CreateGuildRequest> _createValidator;

    public GuildModule(IDiscordGuildApi guildApi, IValidator<UpdateGuildRequest> updateValidator,
        IValidator<CreateGuildRequest> createValidator)
    {
        _guildApi = guildApi;
        _updateValidator = updateValidator;
        _createValidator = createValidator;
    }

    [SlashCommand("get-guilds", "List all guilds")]
    public async Task ListGuilds()
    {
        var response = string.Empty;
        await DeferAsync();
        try
        {
            var guilds = await _guildApi.GetUserGuildsAsync();
            foreach (var guild in guilds)
            {
                response += $"{guild.Name} (ID: {guild.Id})\n";
            }

            await FollowupAsync(response);
        }
        catch (HttpRequestException httpEx)
        {
            await Context.Channel.SendMessageAsync($"HTTP Request Error: {httpEx.Source}");
        }
        catch (ApiException apiEx)
        {
            var errorMessage = Rules.ExtractErrorMessage(apiEx.Content);
            await Context.Channel.SendMessageAsync($"API Error: {errorMessage}");
        }
        catch (Exception ex)
        {
            await Context.Channel.SendMessageAsync($"An error occurred: {ex.Message}");
        }
    }

    [SlashCommand("create-guild", "Create a new guild")]
    public async Task CreateGuild(string name, string region, int afkTimeout, string description)
    {
        var request = new CreateGuildRequest
        {
            Name = name,
            Region = region,
            AfkTimeout = afkTimeout,
            Description = description
        };
        var validationResult = await _createValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            await RespondAsync(validationResult.Errors.First().ErrorMessage);
            return;
        }

        await DeferAsync();
        try
        {
            var guild = await _guildApi.CreateGuildAsync(request);
            await FollowupAsync($"Created Guild: {guild.Name} (ID: {guild.Id})");
        }
        catch (HttpRequestException httpEx)
        {
            await Context.Channel.SendMessageAsync($"HTTP Request Error: {httpEx.Source}");
        }
        catch (ApiException apiEx)
        {
            var errorMessage = Rules.ExtractErrorMessage(apiEx.Content);
            await Context.Channel.SendMessageAsync($"API Error: {errorMessage}");
        }
        catch (Exception ex)
        {
            await Context.Channel.SendMessageAsync($"An error occurred: {ex.Message}");
        }
    }

    [SlashCommand("update-guild", "Update a guild")]
    public async Task UpdateGuild(string name, string region, int afkTimeout, string description, string guildId = null)
    {
        if (string.IsNullOrEmpty(guildId))
        {
            if (Context.Guild == null)
            {
                await RespondAsync("Guild not found. Input guildId.");
                return;
            }

            guildId = Context.Guild.Id.ToString();
        }
        else
        {
            var result = await Rules.CheсkGuildId(guildId, _guildApi);

            if (!result)
            {
                await RespondAsync("Guild not found");
                return;
            }
        }

        var request = new UpdateGuildRequest
        {
            Name = name,
            Region = region,
            AfkTimeout = afkTimeout,
            Description = description
        };
        var validationResult = await _updateValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            await RespondAsync(validationResult.Errors.First().ErrorMessage);
            return;
        }

        await DeferAsync();
        try
        {
            var updatedGuild = await _guildApi.UpdateGuildAsync(guildId, request);
            await FollowupAsync($"Updated Guild: {updatedGuild.Name} (ID: {updatedGuild.Id})");
        }
        catch (HttpRequestException httpEx)
        {
            await Context.Channel.SendMessageAsync($"HTTP Request Error: {httpEx.Source}");
        }
        catch (ApiException apiEx)
        {
            var errorMessage = Rules.ExtractErrorMessage(apiEx.Content);
            await Context.Channel.SendMessageAsync($"API Error: {errorMessage}");
        }
        catch (Exception ex)
        {
            await Context.Channel.SendMessageAsync($"An error occurred: {ex.Message}");
        }
    }

    [SlashCommand("delete-guild", "Delete a guild")]
    public async Task DeleteGuild(string guildId = null)

    {
        if (string.IsNullOrEmpty(guildId))
        {
            if (Context.Guild == null)
            {
                await RespondAsync("Guild not found. Input guildId.");
                return;
            }

            guildId = Context.Guild.Id.ToString();
        }
        else
        {
            var result = await Rules.CheсkGuildId(guildId, _guildApi);

            if (!result)
            {
                await RespondAsync("Guild not found");
                return;
            }
        }

        await DeferAsync();
        try
        {
            await _guildApi.DeleteGuildAsync(guildId);
            await FollowupAsync($"Deleted Guild: {guildId}");
        }
        catch (HttpRequestException httpEx)
        {
            await Context.Channel.SendMessageAsync($"HTTP Request Error: {httpEx.Source}");
        }
        catch (ApiException apiEx)
        {
            var errorMessage = Rules.ExtractErrorMessage(apiEx.Content);
            await Context.Channel.SendMessageAsync($"API Error: {errorMessage}");
        }
        catch (Exception ex)
        {
            await Context.Channel.SendMessageAsync($"An error occurred: {ex.Message}");
        }
    }
}

// Channel Module
public class ChannelModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IDiscordChannelApi _channelApi;
    private readonly IDiscordGuildApi _guildApi;
    private readonly IValidator<UpdateChannelRequest> _updateValidator;
    private readonly IValidator<CreateChannelRequest> _createValidator;

    public ChannelModule(IDiscordChannelApi channelApi, IValidator<UpdateChannelRequest> updateValidator,
        IValidator<CreateChannelRequest> createValidator, IDiscordGuildApi guildApi)
    {
        _channelApi = channelApi;
        _updateValidator = updateValidator;
        _createValidator = createValidator;
        _guildApi = guildApi;
    }

    [SlashCommand("get-channels", "List all channels in a guild")]
    public async Task ListChannels(string guildId = null)
    {
        if (string.IsNullOrEmpty(guildId))
        {
            if (Context.Guild == null)
            {
                await RespondAsync("Guild not found. Input guildId.");
                return;
            }

            guildId = Context.Guild.Id.ToString();
        }
        else
        {
            var result = await Rules.CheсkGuildId(guildId, _guildApi);

            if (!result)
            {
                await RespondAsync("Guild not found");
                return;
            }
        }

        var response = string.Empty;
        await DeferAsync();
        try
        {
            var channels = await _channelApi.GetGuildChannelsAsync(guildId);
            foreach (var channel in channels)
            {
                response += $"{channel.Name} (ID: {channel.Id})\n";
            }

            await FollowupAsync(response);
        }
        catch (HttpRequestException httpEx)
        {
            await Context.Channel.SendMessageAsync($"HTTP Request Error: {httpEx.Source}");
        }
        catch (ApiException apiEx)
        {
            var errorMessage = Rules.ExtractErrorMessage(apiEx.Content);
            await Context.Channel.SendMessageAsync($"API Error: {errorMessage}");
        }
        catch (Exception ex)
        {
            await Context.Channel.SendMessageAsync($"An error occurred: {ex.Message}");
        }
    }

    [SlashCommand("create-channel", "Create a new channel")]
    public async Task CreateChannel(string name, string topic, int userLimit, string region, string guildId = null)
    {
        if (string.IsNullOrEmpty(guildId))
        {
            if (Context.Guild == null)
            {
                await RespondAsync("Guild not found. Input guildId.");
                return;
            }

            guildId = Context.Guild.Id.ToString();
        }
        else
        {
            var result = await Rules.CheсkGuildId(guildId, _guildApi);

            if (!result)
            {
                await RespondAsync("Guild not found");
                return;
            }
        }

        var request = new CreateChannelRequest
        {
            Name = name,
            Region = region,
            Topic = topic,
            UserLimit = userLimit
        };
        var validationResult = await _createValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            await RespondAsync(validationResult.Errors.First().ErrorMessage);
            return;
        }

        await DeferAsync();
        try
        {
            var channel = await _channelApi.CreateChannelAsync(guildId, request);
            await FollowupAsync($"Created Channel: {channel.Name} (ID: {channel.Id})");
        }
        catch (HttpRequestException httpEx)
        {
            await Context.Channel.SendMessageAsync($"HTTP Request Error: {httpEx.Source}");
        }
        catch (ApiException apiEx)
        {
            var errorMessage = Rules.ExtractErrorMessage(apiEx.Content);
            await Context.Channel.SendMessageAsync($"API Error: {errorMessage}");
        }
        catch (Exception ex)
        {
            await Context.Channel.SendMessageAsync($"An error occurred: {ex.Message}");
        }
    }

    [SlashCommand("update-channel", "Update a channel")]
    public async Task UpdateChannels(string name, string topic, int userLimit, string region, string guildId = null,
        string channelId = null)
    {
        await DeferAsync();
        if (string.IsNullOrEmpty(channelId))
        {
            channelId = Context.Channel.Id.ToString();
        }

        if (string.IsNullOrEmpty(guildId))
        {
            if (Context.Guild == null)
            {
                await FollowupAsync("Can't get channel object.");
                return;
            }
            else
            {
                guildId = Context.Guild.Id.ToString();
            }
        }

        var resultGuild = await Rules.CheсkGuildId(guildId, _guildApi);
        if (resultGuild)
        {
            var resultChannel = await Rules.CheсkChannelId(channelId, guildId, _channelApi);
            if (!resultChannel)
            {
                await FollowupAsync("Channel not found");
                return;
            }
        }
        else
        {
            await FollowupAsync("Guild not found");
            return;
        }


        var request = new UpdateChannelRequest
        {
            Name = name,
            Topic = topic,
            UserLimit = userLimit,
            Region = region
        };
        var validationResult = await _updateValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            await FollowupAsync(validationResult.Errors.First().ErrorMessage);
            return;
        }

        try
        {
            var channel = await _channelApi.UpdateChannelAsync(channelId, request);
            await FollowupAsync($"Updated Channel: {channel.Name} (ID: {channel.Id})");
        }
        catch (HttpRequestException httpEx)
        {
            await FollowupAsync($"HTTP Request Error: {httpEx.Source}");
        }
        catch (ApiException apiEx)
        {
            var errorMessage = Rules.ExtractErrorMessage(apiEx.Content);
            await FollowupAsync($"API Error: {errorMessage}");
        }
        catch (Exception ex)
        {
            await FollowupAsync($"An error occurred: {ex.Message}");
        }
    }

    [SlashCommand("delete-channel", "Delete a channel")]
    public async Task DeleteChannel(string channelId = null)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Can't get channel object. Input channelId.");
            return;
        }

        if (string.IsNullOrEmpty(channelId))
        {
            channelId = Context.Channel.Id.ToString();
        }

        var guildId = Context.Guild.Id.ToString();
        var result = await Rules.CheсkChannelId(channelId, guildId, _channelApi);
        if (!result)
        {
            await RespondAsync("Channel not found");
            return;
        }

        await DeferAsync();
        try
        {
            await _channelApi.DeleteChannelAsync(channelId);
            await FollowupAsync($"Deleted Channel: {channelId}");
        }
        catch (HttpRequestException httpEx)
        {
            await Context.Channel.SendMessageAsync($"HTTP Request Error: {httpEx.Source}");
        }
        catch (ApiException apiEx)
        {
            var errorMessage = Rules.ExtractErrorMessage(apiEx.Content);
            await Context.Channel.SendMessageAsync($"API Error: {errorMessage}");
        }
        catch (Exception ex)
        {
            await Context.Channel.SendMessageAsync($"An error occurred: {ex.Message}");
        }
    }
}

public class Rules
{
    public static async Task<bool> CheсkGuildId(string guildId, IDiscordGuildApi guildApi)
    {
        if (!string.IsNullOrEmpty(guildId))
        {
            var guilds = await guildApi.GetUserGuildsAsync();
            return guilds.Any(guild => guild.Id == guildId);
        }

        return false;
    }

    public static async Task<bool> CheсkChannelId(string channelId, string guildId, IDiscordChannelApi channelApi)
    {
        if (!string.IsNullOrEmpty(channelId))
        {
            var channels = await channelApi.GetGuildChannelsAsync(guildId);
            return channels.Any(channel => channel.Id == channelId);
        }

        return false;
    }

    public static string ExtractErrorMessage(string jsonContent)
    {
        try
        {
            using (JsonDocument doc = JsonDocument.Parse(jsonContent))
            {
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("message", out JsonElement messageElement))
                {
                    string message = messageElement.GetString();

                    // Check for nested errors
                    if (root.TryGetProperty("errors", out JsonElement errorsElement))
                    {
                        var errors = new List<string>();
                        ExtractNestedErrors(errorsElement, errors);
                        if (errors.Count > 0)
                        {
                            message += ": " + string.Join(", ", errors);
                        }
                    }

                    return message;
                }
            }
        }
        catch (JsonException jsonEx)
        {
            return $"Failed to parse error message: {jsonEx.Message}";
        }

        return "An error occurred, but no message was provided.";
    }

    private static void ExtractNestedErrors(JsonElement element, List<string> errors)
    {
        if (element.TryGetProperty("_errors", out JsonElement errorsElement))
        {
            foreach (var error in errorsElement.EnumerateArray())
            {
                if (error.TryGetProperty("message", out JsonElement messageElement))
                {
                    errors.Add(messageElement.GetString());
                }
            }
        }
        else
        {
            foreach (var property in element.EnumerateObject())
            {
                ExtractNestedErrors(property.Value, errors);
            }
        }
    }
}