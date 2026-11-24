using Mintbot;

BotClient mintbot = new();
await mintbot.Start(File.ReadAllText(".env/token.txt"), File.ReadAllText(".env/admin.txt"), File.ReadAllText(".env/logchannel.txt"));

Thread.Sleep(-1);
