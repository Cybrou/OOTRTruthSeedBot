using System.Text;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OOTRTruthSeedBot.DAL.Models;
using OOTRTruthSeedBot.SeedGenerator;

namespace OOTRTruthSeedBot.DiscordBot
{
    public class Bot
    {
        public Bot(
                Configuration.Config config,
                IServiceScopeFactory scopeFactory,
                ILogger<Bot> logger
            )
        {
            Config = config;
            ScopeFactory = scopeFactory;
            DiscordSocketConfig socketConf = new() { GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers };
            Client = new DiscordSocketClient(socketConf);
            Logger = logger;
        }

        private Configuration.Config Config { get; set; }
        private DiscordSocketClient Client { get; set; }
        private IServiceScopeFactory ScopeFactory { get; set; }
        private ILogger<Bot> Logger { get; set; }

        public async Task Start()
        {
            await Start(CancellationToken.None);
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            await Client.LoginAsync(TokenType.Bot, Config.Discord.BotToken);
            Client.Ready += Client_Ready;
            Client.SlashCommandExecuted += Client_SlashCommandHandler;
            Client.Log += Client_Log;

            await Client.StartAsync();
            await Task.Delay(-1).WaitAsync(cancellationToken);
            await Client.StopAsync();
            await Client.LogoutAsync();
        }

        public async Task Reset()
        {
            try
            {
                await Client.StopAsync();
            }
            catch (Exception) { }

            try
            {
                await Client.LogoutAsync();
                Client.Ready -= Client_Ready;
                Client.SlashCommandExecuted -= Client_SlashCommandHandler;
                Client.Log -= Client_Log;
            }
            catch (Exception) { }

            DiscordSocketConfig socketConf = new() { GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers };
            Client = new DiscordSocketClient(socketConf);
        }

        private async Task Client_Ready()
        {
            SocketGuild? guild = Client.GetGuild(Config.Discord.BotServer);
            if (guild != null)
            {
                await guild.DownloadUsersAsync();
            }
        }

        private Task Client_Log(LogMessage log)
        {
            if (log.Exception != null)
            {
                Logger.LogError(log.Exception, log.Message);
            }

            return Task.CompletedTask;
        }

        public async Task RegisterCommands()
        {
            SocketGuild? guild = Client.GetGuild(Config.Discord.BotServer);
            if (guild != null)
            {
                SlashCommandBuilder seedCmd = new();
                seedCmd.WithName("seed");
                seedCmd.WithDescription("Generate a new seed or get information of a previously generated one.");
                SlashCommandOptionBuilder seedCmdSeedOption = new();
                seedCmdSeedOption.WithName("seed-truth-number");
                seedCmdSeedOption.WithDescription("The seed number given by the /seed command.");
                seedCmdSeedOption.WithType(ApplicationCommandOptionType.Integer);
                seedCmdSeedOption.WithRequired(false);
                seedCmd.AddOption(seedCmdSeedOption);

                await guild.CreateApplicationCommandAsync(seedCmd.Build());

                SlashCommandBuilder unlockCmd = new();
                unlockCmd.WithName("unlock");
                unlockCmd.WithDescription("Unlock the spoiler log of a previously generated seed.");
                SlashCommandOptionBuilder unlockCmdSeedOption = new();
                unlockCmdSeedOption.WithName("seed-truth-number");
                unlockCmdSeedOption.WithDescription("The seed number given by the /seed command.");
                unlockCmdSeedOption.WithType(ApplicationCommandOptionType.Integer);
                unlockCmdSeedOption.WithRequired(true);
                unlockCmd.AddOption(unlockCmdSeedOption);

                await guild.CreateApplicationCommandAsync(unlockCmd.Build());
            }
        }

        private async Task Client_SlashCommandHandler(SocketSlashCommand cmd)
        {
            await cmd.DeferAsync();

            // Check channel
            if (cmd.ChannelId != Config.Discord.BotChannel)
            {
                EmbedBuilder eb = new();
                eb.WithTitle("Error")
                  .WithDescription($"Commands can only be used in the <#{Config.Discord.BotChannel}> channel.")
                  .WithColor(new Color(0xFF, 0, 0));

                await cmd.FollowupAsync(embed: eb.Build(), ephemeral: true);
                return;
            }

            try
            {
                switch (cmd.Data.Name)
                {
                    case "seed":
                        await SeedCommandHandler(cmd);
                        break;
                    case "unlock":
                        await UnlockCommandHandler(cmd);
                        break;
                }
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, "Error while processing bot slash command");

                EmbedBuilder eb = new();
                eb.WithTitle("Error")
                  .WithDescription($"Sorry, something went wrong :-(, I crashed.")
                  .WithColor(new Color(0xFF, 0, 0));

                await cmd.FollowupAsync(embed: eb.Build(), ephemeral: true);
            }
        }

        private async Task SeedCommandHandler(SocketSlashCommand cmd)
        {
            long? seedNumber = (long?)cmd.Data.Options.FirstOrDefault()?.Value;

            using (var scope = ScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<Context>();
                var generator = scope.ServiceProvider.GetRequiredService<Generator>();

                if (seedNumber == null) // New seed
                {
                    // Prepare db entry
                    Seed newSeed = new Seed() { CreationDate = DateTime.UtcNow, CreatorId = cmd.User.Id };
                    db.Seeds.Add(newSeed);
                    await db.SaveChangesAsync();

                    // Try to generate
                    SeedResult? res = await generator.GenerateSeedAsync(newSeed.Id);
                    if (res != null)
                    {
                        string userName = Client.GetGuild(cmd.GuildId.Value).GetUser(cmd.User.Id)?.DisplayName ?? cmd.User.Username;

                        await generator.WriteSeedOnDiskAsync(res);
                        newSeed.IsGenerated = true;
                        await db.SaveChangesAsync();
                        string? seedHash = await generator.GetHash(newSeed.Id);


                        StringBuilder sb = new();
                        sb.Append($"Seed number: {newSeed.Id}\n");
                        if (seedHash != null)
                        {
                            sb.Append($"Seed hash: {seedHash}\n");
                        }
                        sb.Append($"[Click here to download the zpf]({Config.Web.GetSeedUrl(newSeed.Id)})\n");
                        sb.Append($"Use the following command to unlock the spoiler log: `/unlock {newSeed.Id}`");

                        EmbedBuilder eb = new();
                        eb.WithTitle($"Your seed is ready {userName}")
                          .WithDescription(sb.ToString())
                          .WithColor(new Color(0x0, 0xff, 0x8))
                          .WithFooter($"zpf copy/past: {Config.Web.GetSeedUrl(newSeed.Id)}");

                        await cmd.FollowupAsync(embed: eb.Build());
                        return;
                    }
                    else
                    {
                        EmbedBuilder eb = new();
                        eb.WithTitle("Error")
                          .WithDescription("The seed generation failed. Please try again.")
                          .WithColor(new Color(0xff, 0x0, 0x0));

                        await cmd.FollowupAsync(embed: eb.Build(), ephemeral: true);
                        return;
                    }
                }
                else // Get seed info
                {
                    Seed? seed = db.Seeds.Where(s => s.Id == seedNumber.Value).FirstOrDefault();
                    if (seed == null || !seed.IsGenerated)
                    {
                        EmbedBuilder eb = new();
                        eb.WithTitle("Error")
                          .WithDescription("That seed does not seem to exist.")
                          .WithColor(new Color(0xff, 0x0, 0x0));

                        await cmd.FollowupAsync(embed: eb.Build(), ephemeral: true);
                        return;
                    }
                    else
                    {
                        StringBuilder sb = new();
                        StringBuilder fSb = new();
                        string? userName = Client.GetGuild(cmd.GuildId.Value).GetUser(seed.CreatorId)?.DisplayName;
                        string? seedHash = await generator.GetHash(seed.Id);

                        sb.Append($"Creator: {userName ?? "unknown"} (at <t:{seed.InternalCreationDate}>)\n");
                        if (!seed.IsDeleted && seedHash != null)
                        {
                            sb.Append($"Seed hash: {seedHash}\n");
                        }

                        if (seed.IsDeleted)
                        {
                            sb.Append("State: purged\n");
                        }
                        else if (seed.IsUnlocked)
                        {
                            sb.Append($"State: unlocked (at <t:{seed.InternalUnlockedDate}>)\n");
                        }
                        else
                        {
                            sb.Append("State: locked\n");
                        }

                        if (!seed.IsDeleted)
                        {
                            sb.Append($"[Click here to download the zpf]({Config.Web.GetSeedUrl(seed.Id)})\n");
                            fSb.Append($"zpf copy/past: {Config.Web.GetSeedUrl(seed.Id)}");

                            if (seed.IsUnlocked)
                            {
                                sb.Append($"[Click here to download the spoiler log]({Config.Web.GetSpoilerUrl(seed.Id)})\n");
                                fSb.Append($"\nspoiler copy/past: {Config.Web.GetSpoilerUrl(seed.Id)}");
                            }
                        }

                        EmbedBuilder eb = new();
                        eb.WithTitle($"Seed #{seed.Id}")
                          .WithDescription(sb.ToString())
                          .WithColor(new Color(0x22, 0x00, 0xff))
                          .WithFooter(fSb.ToString());

                        await cmd.FollowupAsync(embed: eb.Build());
                        return;
                    }
                }
            }
        }

        private async Task UnlockCommandHandler(SocketSlashCommand cmd)
        {
            long seedNumber = (long)cmd.Data.Options.First().Value;

            using (var scope = ScopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<Context>();
                Seed? seed = db.Seeds.Where(s => s.Id == seedNumber).FirstOrDefault();

                if (seed == null || !seed.IsGenerated)
                {
                    EmbedBuilder eb = new();
                    eb.WithTitle("Error")
                      .WithDescription("That seed does not seem to exist.")
                      .WithColor(new Color(0xff, 0x0, 0x0));

                    await cmd.FollowupAsync(embed: eb.Build(), ephemeral: true);
                    return;
                }
                else if (seed.IsDeleted)
                {
                    EmbedBuilder eb = new();
                    eb.WithTitle("Error")
                      .WithDescription("That seed is too old and had been purged.")
                      .WithColor(new Color(0xff, 0x0, 0x0));

                    await cmd.FollowupAsync(embed: eb.Build(), ephemeral: true);
                    return;
                }
                else if (seed.IsUnlocked)
                {
                    EmbedBuilder eb = new();
                    eb.WithTitle("Error")
                      .WithDescription("That seed is already unlocked.")
                      .WithColor(new Color(0xff, 0x0, 0x0));

                    await cmd.FollowupAsync(embed: eb.Build(), ephemeral: true);
                    return;
                }
                else if (seed.CreatorId != cmd.User.Id)
                {
                    string creatorUserName = Client.GetGuild(cmd.GuildId.Value).GetUser(seed.CreatorId)?.DisplayName ?? "unknown";

                    EmbedBuilder eb = new();
                    eb.WithTitle("Error")
                      .WithDescription($"Only the seed creator ({creatorUserName}) can unlock that seed.")
                      .WithColor(new Color(0xff, 0x0, 0x0));

                    await cmd.FollowupAsync(embed: eb.Build(), ephemeral: true);
                    return;
                }
                else
                {
                    // Unlock the seed
                    seed.UnlockedDate = DateTime.UtcNow;
                    seed.IsUnlocked = true;
                    await db.SaveChangesAsync();

                    EmbedBuilder eb = new();
                    eb.WithTitle($"The seed #{seed.Id} is now unlocked")
                      .WithDescription($"[Click here to download the spoiler log]({Config.Web.GetSpoilerUrl(seed.Id)})")
                      .WithColor(new Color(0xff, 0x0, 0xea))
                      .WithFooter($"spoiler copy/past: {Config.Web.GetSpoilerUrl(seed.Id)}");

                    await cmd.FollowupAsync(embed: eb.Build());
                }
            }
        }
    }
}
