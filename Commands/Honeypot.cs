using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Mintbot;

public static partial class ContextCommands
{
    private static Embed HoneypotHelp = new EmbedBuilder()
        .WithTitle("Honeypot Help")
        .WithDescription("""
            `$honeypot up` - Initialises the honeypot
            `$honeypot down` - Disables the honeypot
            """)
        .Build();

    private static Embed HoneypotInfo = new EmbedBuilder()
        .WithTitle("Honeypot")
        .WithDescription("""
            This channel is used to catch automated spam bots.
            Posting in this channel will result in a softban.

            -# [Bot made by Dax](<https://github.com/Daxcr/mintbot>)
            """)
        .Build();

    public static async Task Honeypot(SocketUserMessage msg, string[] parameters)
    {
        if (parameters.Length == 1)
        {
            await msg.ReplyAsync(embed: HoneypotHelp);
            return;
        }

        SocketGuild? guild = (msg.Author as SocketGuildUser)?.Guild;

        switch (parameters[1])
        {
            case "up":
                SocketGuildChannel? channel2 = guild?.GetChannel(BotClient.HoneypotChannelID);

                if (channel2 != null)
                {
                    await msg.ReplyAsync("Honeypot is already up.");
                    return;
                }

                RestTextChannel textChannel = await guild!.CreateTextChannelAsync("honeypot", properties =>
                {
                    properties.Topic = "Ban all cats!! >:(";
                });

                await textChannel.SendMessageAsync(embed: HoneypotInfo);

                await BotClient.Log($"**{msg.Author.Username}** enabled the honeypot.", Color.Green);

                File.WriteAllText("honeypot.txt", textChannel.Id.ToString());
                await msg.AddReactionAsync(new Emoji("👍"));
                break;

            case "down":
                SocketGuildChannel? channel = guild?.GetChannel(BotClient.HoneypotChannelID);

                if (channel == null)
                {
                    await msg.ReplyAsync("Honeypot is already down.");
                    return;
                }

                await channel.DeleteAsync();

                await BotClient.Log($"**{msg.Author.Username}** disabled the honeypot.", null);

                await msg.AddReactionAsync(new Emoji("👍"));
                break;
        }
    }
}
