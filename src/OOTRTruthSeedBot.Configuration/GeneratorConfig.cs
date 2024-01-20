namespace OOTRTruthSeedBot.Configuration
{
    public class GeneratorConfig
    {
        public string PythonPath { get; set; } = "";
        public string RandomizerPath { get; set; } = "";
        public string DefaultSettingsPath { get; set; } = "";
        public string SeedOutputPath { get; set; } = "";
        public int MaximumConcurrency { get; set; } = 3;
    }
}
