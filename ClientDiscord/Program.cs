/*using System.Data;
using System.Text;
using ClientDiscord.Api;
using ClientDiscord.Models;
using Refit;

class Program
{
    private static async Task Main(string[] args)
    {
        var token = "YOUR_BOT_TOKEN";

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://discord.com/api/v10/"),
            DefaultRequestHeaders =
            {
                { "Authorization", $"Bot {token}" }
            }
        };
        var guildApi = RestService.For<IDiscordGuildApi>(httpClient);
        var channelApi = RestService.For<IDiscordChannelApi>(httpClient);
        try
        {
            Rules.ShowCommands();
            while (true)
            {
                var request = Console.ReadLine()?.Trim().ToLower();
                if (string.IsNullOrWhiteSpace(request)) continue;
                var rules = new Rules(request, guildApi, channelApi);
                await rules.Start();
            }
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"Error: {ex.StatusCode} - {ex.Message}");
        }
    }
}

class Rules(string request, IDiscordGuildApi guildApi, IDiscordChannelApi channelApi)
{
    private string Request { get; set; } = request;
    private IDiscordGuildApi GuildApi { get; set; } = guildApi;
    private IDiscordChannelApi ChannelApi { get; set; } = channelApi;

    public async Task Start()
    {
        switch (Request)
        {
            case "/getguilds":
                await GetAllGuilds();
                ShowCommands();
                break;
            case "/createguild":
                await CreateGuild();
                ShowCommands();
                break;
            case "/updateguild":
                await UpdateGuild();
                ShowCommands();
                break;
            case "/deleteguild":
                await DeleteGuild();
                ShowCommands();
                break;

            case "/getchannels":
                await GetAllChannels();
                ShowCommands();
                break;
            case "/createchannel":
                await CreateChannel();
                ShowCommands();
                break;
            case "/updatechannel":
                await UpdateChannel();
                ShowCommands();
                break;
            case "/deletechannel":
                await DeleteChannel();
                ShowCommands();
                break;
            default:
                Console.WriteLine("Invalid request");
                break;
        }
    }

    public static void ShowCommands()
    {
        Console.WriteLine("Available Commands:");
        Console.WriteLine("/getGuilds - Get all guilds");
        Console.WriteLine("/createGuild - Create a new guild");
        Console.WriteLine("/updateGuild - Update an existing guild");
        Console.WriteLine("/deleteGuild - Delete a guild");
        Console.WriteLine("/getChannels - Get all channels");
        Console.WriteLine("/createChannel - Create a new channel");
        Console.WriteLine("/updateChannel - Update an existing channel");
        Console.WriteLine("/deleteChannel - Delete a channel");
        Console.WriteLine("Enter a command:");
    }

    private async Task DeleteChannel()
    {
        try
        {
            var channelId = await ChooseChannel();
            await ChannelApi.DeleteChannelAsync(channelId);
            Console.WriteLine($"Deleted Channel with {channelId} ID");
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"Error: {ex.StatusCode} - {ex.Message}");
        }
    }

    private async Task UpdateChannel()
    {
        UpdateChannelRequest request = new UpdateChannelRequest();
        var channelId = await ChooseChannel();
        foreach (var property in request.GetType().GetProperties())
        {
            while (true)
            {
                Console.WriteLine("Input " + $"{property.Name}" + " of the channel to update: ");
                string input = Console.ReadLine();
                try
                {
                    var convertedValue = Convert.ChangeType(input, property.PropertyType);
                    property.SetValue(request, convertedValue);
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(
                        $"Input correct value for {property.Name} (type: {property.PropertyType.Name}): {e.Message}");
                }
            }
        }

        try
        {
            var updatedChannel = await channelApi.UpdateChannelAsync(channelId, request);
            Console.WriteLine($"Updated Channel: {updatedChannel.Name} (ID: {updatedChannel.Id})");
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"Error: {ex.StatusCode} - {ex.Message}");
            Console.WriteLine("Try again");
            await UpdateChannel();
        }
    }

    private async Task<string> ChooseChannel()
    {
        var receivedChannels = await GetAllChannels();
        var isChannelIdCorrect = false;
        var channelId = string.Empty;
        while (true)
        {
            Console.WriteLine("Enter Channel ID:");
            channelId = Console.ReadLine();
            foreach (var receivedChannel in receivedChannels)
            {
                if (receivedChannel.Id == channelId)
                {
                    isChannelIdCorrect = true;
                    break;
                }
                
            }

            if (isChannelIdCorrect) break;
            Console.WriteLine("Invalid Channel ID. Try again:");
        }

        Console.WriteLine("Id was selected correctly");
        return channelId;
    }

    private async Task<string> ChooseGuild()
    {
        Console.WriteLine("Choose Guild ID and enter it:");
        var receivedGuilds = await GetAllGuilds();
        var isGuildIdCorrect = false;
        var guildId = string.Empty;
        while (true)
        {
            Console.WriteLine("Enter Guild ID:");
            guildId = Console.ReadLine()?.Trim();
            foreach (var receivedGuild in receivedGuilds)
            {
                if (receivedGuild.Id == guildId)
                {
                    isGuildIdCorrect = true;
                    break;
                }
            }

            if (isGuildIdCorrect) break;
            Console.WriteLine("Invalid Guild ID. Try again:");
        }

        Console.WriteLine("Id was selected correctly");
        return guildId;
    }

    private async Task CreateChannel()
    {
        CreateChannelRequest request = new CreateChannelRequest();
        var guildId = await ChooseGuild();
        foreach (var property in request.GetType().GetProperties())
        {
            while (true)
            {
                Console.WriteLine("Input " + $"{property.Name}" + " of the channel to create: ");
                string input = Console.ReadLine().Trim();
                try
                {
                    var convertedValue = Convert.ChangeType(input, property.PropertyType);
                    property.SetValue(request, convertedValue);
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(
                        $"Input correct value for {property.Name} (type: {property.PropertyType.Name}): {e.Message}");
                }
            }
        }

        try
        {
            var createdChannel = await channelApi.CreateChannelAsync(guildId, request);
            Console.WriteLine($"Created Channel: {createdChannel.Name} (ID: {createdChannel.Id})");
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"Error: {ex.StatusCode} - {ex.Message}");
        }
    }

    private async Task<List<Channel>> GetAllChannels()
    {
        var guildId = await ChooseGuild();
        var receivedChannels = await ChannelApi.GetGuildChannelsAsync(guildId);
        Console.WriteLine("Getting Channels...");
        Console.WriteLine($"Number of Channels: {receivedChannels.Count}");
        foreach (var channel in receivedChannels)
        {
            Console.WriteLine($"Channel: {channel.Name} (ID: {channel.Id})");
        }

        return receivedChannels;
    }

    private async Task<List<Guild>> GetAllGuilds()
    {
        var receivedGuilds = await GuildApi.GetUserGuildsAsync();
        Console.WriteLine("Getting Guilds...");
        Console.WriteLine($"Number of Guilds: {receivedGuilds.Count}");
        foreach (var guild in receivedGuilds)
        {
            Console.WriteLine($"Guild: {guild.Name} (ID: {guild.Id})");
        }

        return receivedGuilds;
    }

    private async Task DeleteGuild()
    {
        try
        {
            var guildId = await ChooseGuild();
            await GuildApi.DeleteGuildAsync(guildId);
            Console.WriteLine($"Deleted Guild with {guildId} ID");
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"Error: {ex.StatusCode} - {ex.Message}");
        }
    }

    private async Task UpdateGuild()
    {
        UpdateGuildRequest request = new UpdateGuildRequest();
        var guildId = await ChooseGuild();
        foreach (var property in request.GetType().GetProperties())
        {
            while (true)
            {
                Console.WriteLine("Input " + $"{property.Name}" + " of the guild to update: ");
                string input = Console.ReadLine().Trim();
                try
                {
                    var convertedValue = Convert.ChangeType(input, property.PropertyType);
                    property.SetValue(request, convertedValue);
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(
                        $"Input correct value for {property.Name} (type: {property.PropertyType.Name}): {e.Message}");
                }
            }
        }

        try
        {
            var updatedGuild = await guildApi.UpdateGuildAsync(guildId, request);
            Console.WriteLine($"Updated Guild: {updatedGuild.Name} (ID: {updatedGuild.Id})");
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"Error: {ex.StatusCode} - {ex.Message}");
            Console.WriteLine("Try again");
            await UpdateGuild();
        }
    }

    private async Task CreateGuild()
    {
        CreateGuildRequest request = new CreateGuildRequest();
        foreach (var property in request.GetType().GetProperties())
        {
            while (true)
            {
                Console.WriteLine("Input " + $"{property.Name}" + " of the guild: ");
                string input = Console.ReadLine();
                try
                {
                    var convertedValue = Convert.ChangeType(input, property.PropertyType);
                    property.SetValue(request, convertedValue);
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(
                        $"Input correct value for {property.Name} (type: {property.PropertyType.Name}): {e.Message}");
                }
            }
        }

        try
        {
            var createGuild = await guildApi.CreateGuildAsync(request);
            Console.WriteLine($"Created Guild: {createGuild.Name} (ID: {createGuild.Id})");
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"Error: {ex.StatusCode} - {ex.Message}");
            Console.WriteLine("Try again");
            await CreateGuild();
        }
    }
}*/