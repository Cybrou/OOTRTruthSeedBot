
using OOTRTruthSeedBot.DiscordBot;

namespace OOTRTruthSeedBot.HostedServices
{
    public class DiscordBotHostedService : BackgroundService
    {
        public DiscordBotHostedService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<DiscordBotHostedService> logger
            )
        {
            ServiceScopeFactory = serviceScopeFactory;
            Logger = logger;
        }

        private IServiceScopeFactory ServiceScopeFactory { get; set; }
        private ILogger<DiscordBotHostedService> Logger { get; set; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                using(var scope = ServiceScopeFactory.CreateScope())
                {
                    var bot = scope.ServiceProvider.GetRequiredService<Bot>();

                    try
                    {
                        await bot.Start(stoppingToken);
                    }
                    catch(Exception ex)
                    {
                        Logger.LogError(ex, "Bot stopped working.");
                        await bot.Reset();
                    }

                    await Task.Delay(500, stoppingToken);
                }
            }
        }
    }
}
