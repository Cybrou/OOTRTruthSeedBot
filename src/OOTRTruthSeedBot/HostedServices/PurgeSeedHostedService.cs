using OOTRTruthSeedBot.DAL.Models;

namespace OOTRTruthSeedBot.HostedServices
{
    public class PurgeSeedHostedService : CronBackgroundService
    {
        private IServiceScopeFactory ScopeFactory { get; set; }
        private ILogger<PurgeSeedHostedService> Logger { get; set; }

        public PurgeSeedHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<PurgeSeedHostedService> logger)
        {
            ScopeFactory = scopeFactory;
            Logger = logger;
        }

        protected override string GetCronExpression() => "0 4 * * *";

        protected override async Task CronExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogInformation("Purge task");

                using (var scope = ScopeFactory.CreateScope())
                {
                    var config = scope.ServiceProvider.GetRequiredService<Configuration.Config>();
                    var db = scope.ServiceProvider.GetRequiredService<Context>();

                    // Get old seeds
                    foreach(var seed in db.Seeds.Where(s => s.InternalState < 0x4).OrderByDescending(s => s.Id).Skip(config.Generator.MaximumSeeds))
                    {
                        try
                        {
                            string seedPath = Path.Combine(config.Generator.SeedOutputPath, seed.Id.ToString());
                            if (Directory.Exists(seedPath))
                            {
                                Directory.Delete(seedPath, true);
                            }

                            seed.IsDeleted = true;
                        }
                        catch(Exception ex)
                        {
                            Logger.LogError(ex, "Error while deleting seed #{seedId}", seed.Id);
                        }
                    }

                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while purgeing seeds.");
            }
        }
    }
}
