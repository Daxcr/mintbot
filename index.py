version = "0.1.0"

import discord
from discord import app_commands
from discord.ext import commands
from discord.ui import View, Button
from discord import ui
import os
from discord import Thread
from discord.ext import commands
from discord.utils import utcnow
import asyncio
import json
from datetime import datetime


activity = discord.Activity(type=discord.ActivityType.playing, name="Starting up...")

intents = discord.Intents.default()
intents.presences = True
intents.message_content = True
intents.members = True

bot = commands.Bot(command_prefix="$", intents=intents, activity=activity, status=discord.Status.idle, help_command=None)
OWNER_ID = 858210985126920202

@bot.event
async def on_ready():
    await bot.tree.sync()
    print(f"{bot.user} has connected to Discord!")

    game = discord.Activity(type=discord.ActivityType.playing, name=f"version: {version}")
    await bot.change_presence(status=discord.Status.dnd, activity=game)

    user = await bot.fetch_user(OWNER_ID)
    await user.send(":white_check_mark: Start up complete.")

    global honeypot
    guild = bot.get_guild(1050249944889577602)
    honeypot = discord.utils.get(guild.channels, name="honeypot")

@bot.command(name="collapse")
async def collapse(ctx):
    allowed_roles = {1343402463340003349, 1135558342488641638}
    if ctx.guild is None:
        return
    else:
        if ctx.author.id != OWNER_ID:
            if not any(role.id in allowed_roles for role in ctx.author.roles):
                return

    if not ctx.message.reference:
        await ctx.send("Please reply to a message to collapse a user's message chain.")
        return

    before_msg = await ctx.channel.fetch_message(ctx.message.reference.message_id)
    author = before_msg.author

    chain = [before_msg]

    async for msg in ctx.channel.history(limit=100, before=before_msg):
        if msg.author.id in (author.id, bot.user.id):
            chain.append(msg)
        else:
            break
        
    async for msg in ctx.channel.history(limit=100, after=before_msg):
        if msg.author.id in (author.id, bot.user.id):
            chain.append(msg)
        else:
            break

    chain.reverse()

    now = discord.utils.utcnow()
    bulk_delete = [m for m in chain if (now - m.created_at).days < 14]
    old_messages = [m for m in chain if (now - m.created_at).days >= 14]

    await ctx.send(f"Collapsing `{len(bulk_delete)}` messages")
    delete = await ctx.channel.delete_messages(bulk_delete)

    for m in old_messages:
        try:
            await m.delete()
        except discord.Forbidden:
            await ctx.send("I don't have permission to delete some older messages.")
        except discord.NotFound:
            pass

@bot.command(name="honeypot")
async def honeypot(ctx, enabled: bool):
    allowed_roles = {1135558342488641638, 1343402463340003349}
    if ctx.author.id != OWNER_ID:
        if not any(role.id in allowed_roles for role in ctx.author.roles):
            return

    guild = bot.get_guild(1050249944889577602)
    if guild:
        if enabled:
            channel = discord.utils.get(guild.channels, name="honeypot")
            if not channel:
                global honeypot
                honeypot = await guild.create_text_channel("honeypot")
                await ctx.send("Created honeypot")

                embed = discord.Embed(
                    title="Do not post here!",
                    description=f"This is a honeypot used to catch automated spam accounts. If you post here, you will get banned. You can appeal, but please don't test it. We recommend you remove this channel from your channel list and ignore it.\n\n**Honeypot initiated by:** {ctx.author.mention}",
                    colour=discord.Colour.red()
                )
                await honeypot.send(embed=embed)
            else:
                await ctx.send("A honeypot already exists")
        else:
            channel = discord.utils.get(guild.channels, name="honeypot")
            if channel:
                await channel.delete()
                await ctx.send("Deleted honeypot")
            else:
                await ctx.send("No honeypot found")

@bot.command(name="unbandax") # Debug unban; not gonna be future-proofing any bans I swear 
async def unbandax(ctx):
    if ctx.author.id == OWNER_ID:
        guild = bot.get_guild(1050249944889577602)
        await guild.unban(ctx.author, reason="Auto-unban")
        await ctx.send("https://discord.gg/cHqHFsj2e4")

@bot.tree.command(name="appeal")
async def appeal(interaction: discord.Interaction):
    await interaction.response.defer()
    try:
        guild = bot.get_guild(1050249944889577602)
        ban_data = await guild.fetch_ban(interaction.user)

        if ban_data.reason == "Honeypot":
            await guild.unban(interaction.user, reason="Appeal via Honeybot")
            await interaction.followup.send(f"Success!\nhttps://discord.gg/cHqHFsj2e4")

    except Exception as e:
        await interaction.followup.send(f"Something went wrong: {e}", ephemeral=True)

async def honeypotTrigger(member):
    embed = discord.Embed(
        title="Banned :(",
        description=f"You have triggered the honeypot in `Mint Mutts`. To get unbanned, please add this bot to your account and run `/appeal`.",
        colour=discord.Colour.red()
    )
    try:
        await member.send(embed=embed)
        print("Honeypot trigger")
    except:
        print("Honeypot trigger")

    guild = bot.get_guild(1050249944889577602)
    await guild.ban(member, reason="Honeypot", delete_message_days=0)

@bot.event
async def on_message(message):
    if message.author == bot.user:
        return
    
    if message.channel == honeypot:
        await message.delete()
        await honeypotTrigger(message.author)
        embed = discord.Embed(
            title="Honeypot Trigger",
            description=f"Triggered by: {message.author}\nContent: {message.content}",
            colour=discord.Colour.red()
        )
        guild = bot.get_guild(1050249944889577602)
        channel = discord.utils.get(guild.channels, name="honeybot-log")
        await channel.send(embed=embed)

    await bot.process_commands(message)

@bot.event
async def on_member_join(member: discord.Member):
    if member.id != OWNER_ID:
        return

    guild = member.guild
    channel = discord.utils.get(guild.channels, name="honeybot-log")

    overwrite = channel.overwrites_for(member)
    overwrite.view_channel = True
    await channel.set_permissions(member, overwrite=overwrite)

token = os.getenv("DISCORD_TOKEN")

bot.run(token)