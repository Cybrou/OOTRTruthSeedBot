namespace OOTRTruthSeedBot.SeedGenerator
{
    public class SeedResult
    {
        public SeedResult(int seedNumber, byte[] seed, byte[] spoiler)
        {
            SeedNumber = seedNumber;
            Seed = seed;
            Spoiler = spoiler;
        }

        public int SeedNumber { get; set; }
        public byte[] Seed { get; set; }
        public byte[] Spoiler { get; set; }
    }
}
