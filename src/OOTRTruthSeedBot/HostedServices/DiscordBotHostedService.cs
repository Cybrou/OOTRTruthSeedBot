
using OOTRTruthSeedBot.DiscordBot;

namespace OOTRTruthSeedBot.HostedServices
{
    public class DiscordBotHostedService : BackgroundService
    {
        public DiscordBotHostedService(IServiceScopeFactory serviceScopeFactory)
        {
            ServiceScopeFactory = serviceScopeFactory;
        }

        private IServiceScopeFactory ServiceScopeFactory { get; set; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                using(var scope = ServiceScopeFactory.CreateScope())
                {
                    var bot = scope.ServiceProvider.GetRequiredService<Bot>();
                    await bot.Start(stoppingToken);
                }
            }
        }
    }
}
