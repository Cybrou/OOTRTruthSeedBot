using Microsoft.Extensions.Configuration;

namespace OOTRTruthSeedBot.Configuration
{
    public class Config
    {
        public Config(IConfiguration conf)
        {
            Discord = conf.GetSection("Discord").Get<DiscordConfig>() ?? new DiscordConfig();
            Generator = conf.GetSection("Generator").Get<GeneratorConfig>() ?? new GeneratorConfig();
            Web = conf.GetSection("Web").Get<WebConfig>() ?? new WebConfig();
        }

        public DiscordConfig Discord { get; set; }
        public GeneratorConfig Generator { get; set; }
        public WebConfig Web { get; set; }
    }
}
