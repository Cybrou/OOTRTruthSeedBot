using System.IO;
using Microsoft.Extensions.Configuration;

namespace OOTRTruthSeedBot.Configuration
{
    public class Config
    {
        public Config(IConfiguration conf)
        {
            Discord = conf.GetSection("Discord").Get<DiscordConfig>() ?? new DiscordConfig();
            Generator = conf.GetSection("Generator").Get<GeneratorConfig>() ?? new GeneratorConfig();
            Restream = conf.GetSection("Restream").Get<RestreamConfig>() ?? new RestreamConfig();
            Web = conf.GetSection("Web").Get<WebConfig>() ?? new WebConfig();
        }

        public DiscordConfig Discord { get; set; }
        public GeneratorConfig Generator { get; set; }
        public RestreamConfig Restream { get; set; }
        public WebConfig Web { get; set; }
    }
}
