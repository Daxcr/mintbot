using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Mintbot;

public class BotClient
{
    public static DiscordSocketClient Client = new DiscordSocketClient(new DiscordSocketConfig { GatewayIntents = GatewayIntents.All });
    InteractionService Interactions = new InteractionService(Client);
    public const string CommandPrefix = "$";
    public static ulong AdminID = 0;
    public static ulong HoneypotChannelID = 0;
    public static ulong LogChannelID = 0;
    public Dictionary<string, Func<SocketUserMessage, string[], Task>> Commands = new()
    {
        { "test", Test },
        { "honeypot", ContextCommands.Honeypot },
        { "restore", ContextCommands.Restore },
    };

    public static async Task Test(SocketUserMessage msg, string[] parameters) =>
        await msg.AddReactionAsync(new Emoji("👍"));

    public BotClient()
    {
        Client.Log += message =>
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        };

        Client.Ready += async () =>
        {
            await Interactions.RegisterCommandsGloballyAsync();
        };

        Client.InteractionCreated += async interaction =>
        {
            SocketInteractionContext ctx = new SocketInteractionContext(Client, interaction);
            await Interactions.ExecuteCommandAsync(ctx, null);
        };

        Client.MessageReceived += async message =>
        {
            if (message.Channel.Id == HoneypotChannelID && !message.Author.IsBot) // Ideally we don't want this to trigger when bots message.
            {
                if (message.Author is not SocketGuildUser guildUser2)
                    return;

                SocketGuild guild = guildUser2.Guild; // Keep these in case the user ever expires
                ulong userId = guildUser2.Id;

                await Log($"{message.Author.Username} triggered the honeypot.", Color.Red);

                await guildUser2.BanAsync(pruneDays: 0, reason: "Honeypot");
                await guild.RemoveBanAsync(userId);
            }

            if (message.Author is not SocketGuildUser guildUser)
                return;

            if (
                message.Content.StartsWith(CommandPrefix) &&
                (message.Author.Id == AdminID || guildUser.GuildPermissions.ManageMessages)
            )
            {
                string[] split = message.Content
                    .Substring(CommandPrefix.Length)
                    .Split(' ');
                if (Commands.TryGetValue(split[0], out var func))
                    _ = func.Invoke((SocketUserMessage)message, split);
            }
        };

        Client.ButtonExecuted += async component =>
        {
            string[] buttondata = component.Data.CustomId.Split("/");
#pragma warning disable CS1522 // Empty switch block
            switch (buttondata[0])
            {

            }
#pragma warning restore CS1522 // Empty switch block
        };
    }

    public async Task Start(string token, string admin, string logchannel)
    {
        // In case we ever want interaction commands
        // await interactions.AddModuleAsync< ... >(null);

        await Client.LoginAsync(TokenType.Bot, token.Trim());
        await Client.StartAsync();

        AdminID = ulong.Parse(admin);
        LogChannelID = ulong.Parse(logchannel);
        HoneypotChannelID = ulong.Parse(File.ReadAllText("honeypot.txt"));

        await Task.Delay(-1);
    }

    public static async Task Log(string message, Color? colour)
    {
        Console.WriteLine($"{DateTime.Now} | {message}");
        IChannel logchannel = await Client.GetChannelAsync(LogChannelID);
        if (logchannel is IMessageChannel msglogchannel)
        {
            Embed embed;

            if (colour is null)
                embed = new EmbedBuilder()
                    .WithDescription(message)
                    .Build();
            else
                embed = new EmbedBuilder()
                    .WithDescription(message)
                    .WithColor((Color)colour)
                    .Build();

            await msglogchannel.SendMessageAsync(embed: embed);
        }
    }
}
