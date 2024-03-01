using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OOTRTruthSeedBot.DAL.Models;
using OOTRTruthSeedBot.DiscordBot;
using OOTRTruthSeedBot.HostedServices;

namespace OOTRTruthSeedBot.Config
{
    public static class Services
    {
#pragma warning disable CS8618 // Always set in Program.cs
        public static IServiceProvider Provider { get; set; }
#pragma warning restore CS8618

        public static IServiceCollection ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                    .AddJsonOptions(jo =>
                    {
                        jo.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    });

            services.AddDbContext<Context>(o =>
                o.UseSqlite("name=OOTRTruthSeedBotDatabase")
            );

            services.AddSingleton<Configuration.Config>();
            services.AddSingleton<Bot>();
            services.AddScoped<SeedGenerator.Generator>();

            services.AddHostedService<DiscordBotHostedService>();
            services.AddHostedService<PurgeSeedHostedService>();
            services.AddHostedService<RestreamNotifHostedService>();

            return services;
        }

        public static void ConfigureApp()
        {
            using (var scope = Provider.CreateScope())
            {
                // Generator max concurrency
                int generatorMaxConcurrency = scope.ServiceProvider.GetRequiredService<Configuration.Config>().Generator.MaximumConcurrency;
                SeedGenerator.Generator.InitConcurrency(generatorMaxConcurrency);
            }
        }
    }
}
