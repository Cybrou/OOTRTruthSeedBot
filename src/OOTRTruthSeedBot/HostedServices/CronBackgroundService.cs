using Cronos;

namespace OOTRTruthSeedBot.HostedServices
{
    public abstract class CronBackgroundService : BackgroundService
    {
        public CronBackgroundService()
            : base()
        {
            _cronExpression = CronExpression.Parse(GetCronExpression());
        }

        protected abstract string GetCronExpression();

        private CronExpression _cronExpression;

        protected CronExpression CronExpression => _cronExpression;

        protected async Task WaitNextOccurrence(CancellationToken stoppingToken)
        {
            DateTime now = DateTime.UtcNow;
            DateTime? nextOccurence = _cronExpression.GetNextOccurrence(now);

            if (nextOccurence != null)
            {
                await Task.Delay((int)(nextOccurence.Value - now).TotalMilliseconds + 650, stoppingToken);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await WaitNextOccurrence(stoppingToken);
                if (!stoppingToken.IsCancellationRequested)
                {
                    await CronExecuteAsync(stoppingToken);
                }
            }
        }

        /// <summary>
        /// Called when cron trigger.
        /// </summary>
        protected abstract Task CronExecuteAsync(CancellationToken stoppingToken);
    }
}
