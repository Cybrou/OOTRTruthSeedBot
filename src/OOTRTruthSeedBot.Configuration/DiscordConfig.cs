namespace OOTRTruthSeedBot.Configuration
{
    public class DiscordConfig
    {
        public string BotToken { get; set; } = "";
        public ulong BotServer { get; set; }
        public ulong BotChannel { get; set; }
        public ulong BotRestreamChannel { get; set; }
    }
}
