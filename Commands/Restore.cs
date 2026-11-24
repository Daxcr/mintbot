using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Mintbot;

public static partial class ContextCommands
{
    public static async Task Restore(SocketUserMessage msg, string[] parameters)
    {
        if (msg.Author.Id == BotClient.AdminID && msg.Channel is SocketGuildChannel guildChannel && msg.Author is SocketGuildUser guildUser)
        {
            SocketGuildChannel logchannel = guildChannel.Guild.GetChannel(BotClient.LogChannelID);

            await logchannel.AddPermissionOverwriteAsync(
                guildUser,
                new OverwritePermissions(
                    viewChannel: PermValue.Allow,
                    sendMessages: PermValue.Allow,
                    readMessageHistory: PermValue.Allow));

            await msg.AddReactionAsync(new Emoji("👍"));
            await BotClient.Log($"**{msg.Author.Username}** has restored their log-viewing permissions.", null);
        }
    }
}
