namespace OOTRTruthSeedBot.Configuration
{
    public class WebConfig
    {
        public string Root { get; set; } = "";

        public string GetSeedUrl(int seedNumber)
        {
            return $"{Root}seed/{seedNumber}";
        }

        public string GetSpoilerUrl(int seedNumber)
        {
            return $"{Root}seed/{seedNumber}/spoiler";
        }
    }
}
