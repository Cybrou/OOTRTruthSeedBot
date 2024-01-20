using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

namespace OOTRTruthSeedBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Serilog
            builder.Logging.ClearProviders();
            builder.Services.AddLogging(lb =>
            {
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(builder.Configuration)
                    .CreateLogger();

                lb.AddSerilog(dispose: true);
            });

            Config.Services.ConfigureServices(builder.Services);
            var app = builder.Build();
            Config.Services.Provider = app.Services;
            Config.Services.ConfigureApp();

            app.UseForwardedHeaders(new ForwardedHeadersOptions() { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
