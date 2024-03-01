
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using OOTRTruthSeedBot.DAL.Models;
using OOTRTruthSeedBot.DiscordBot;

namespace OOTRTruthSeedBot.HostedServices
{
    public class RestreamNotifHostedService : CronBackgroundService
    {
        public RestreamNotifHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<RestreamNotifHostedService> logger) 
        {
            ScopeFactory = scopeFactory;
            Logger = logger;
        }

        private IServiceScopeFactory ScopeFactory { get; set; }
        private ILogger<RestreamNotifHostedService> Logger { get; set; }

        protected override string GetCronExpression() => "*/5 * * * *";

        protected override async Task CronExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                Logger.LogInformation("Check restream notifications");

                using (var scope = ScopeFactory.CreateScope())
                {
                    var config = scope.ServiceProvider.GetRequiredService<Configuration.Config>();
                    var db = scope.ServiceProvider.GetRequiredService<Context>();
                    var bot = scope.ServiceProvider.GetRequiredService<Bot>();

                    // Download sheet data
                    HttpClient http = new HttpClient();
                    using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, config.Restream.SheetUri);
                    using HttpResponseMessage resp = await http.SendAsync(req);

                    if (!resp.IsSuccessStatusCode)
                    {
                        Logger.LogError("Error while downloading sheet data.");
                        return;
                    }

                    using StreamReader sr = new StreamReader(await resp.Content.ReadAsStreamAsync(), System.Text.Encoding.UTF8);
                    CsvConfiguration csvConfig = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture) { Delimiter = ",", Escape = '"', NewLine = "\r\n" };
                    using CsvReader csv = new CsvReader(sr, csvConfig);
                    while(await csv.ReadAsync())
                    {
                        string guid = csv.GetField(0);
                        bool isRestream = csv.GetField(8)?.ToLower() == "true";
                        string strDatetime = csv.GetField(16);

                        if (!string.IsNullOrWhiteSpace(guid)
                            && isRestream
                            && DateTime.TryParseExact(strDatetime, "dd/MM/yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime dateTime)
                            && dateTime >= config.Restream.MinDate
                            && !(await db.RestreamNotifs.AnyAsync(r => r.Guid == guid)))
                        {
                            string type = csv.GetField(2);
                            string matchtup = csv.GetField(3);
                            string round = csv.GetField(4);
                            string host = csv.GetField(14);
                            string cohost = csv.GetField(15);

                            // Send notif
                            if (await bot.SendRestreamNotif(type, round, matchtup, host, cohost, dateTime))
                            {
                                // Save in bdd
                                RestreamNotif newNotif = new RestreamNotif() { Guid = guid, SentDate = DateTime.UtcNow };
                                await db.RestreamNotifs.AddAsync(newNotif);
                                await db.SaveChangesAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while processing restream norifications.");
            }
        }
    }
}
