using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using OOTRTruthSeedBot.Configuration;

namespace OOTRTruthSeedBot.SeedGenerator
{
    public class Generator
    {
        public Generator(Config config)
        {
            Config = config.Generator;
        }

        private GeneratorConfig Config { get; set; }

        public static int MaximumConcurrency { get; private set; }

        private static SemaphoreSlim? ConcurrencySem { get; set; }

        public static void InitConcurrency(int maximumConcurrency)
        {
            MaximumConcurrency = maximumConcurrency;
            ConcurrencySem = new SemaphoreSlim(MaximumConcurrency);
        }

        public async Task<SeedResult?> GenerateSeedAsync(int seedNumber)
        {
            return await Task.Run(async () =>
            {
                bool semUsed = false;

                if (ConcurrencySem != null)
                {
                    semUsed = true;
                    await ConcurrencySem.WaitAsync(10000);
                }

                string tempFolder = Path.GetTempFileName();
                File.Delete(tempFolder);
                Directory.CreateDirectory(tempFolder);

                string tempSettingsPath = Path.Combine(tempFolder, "settings.sav");

                // Prepare settings.sav
                JsonNode? json;
                using (var fs = new FileStream(Config.DefaultSettingsPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    json = await JsonNode.ParseAsync(fs);
                }

                if (json == null)
                {
                    return null;
                }

                json["output_dir"]?.ReplaceWith(tempFolder);
                json["user_message"]?.ReplaceWith($"Truth (#{seedNumber})");
                using (var fs = new FileStream(tempSettingsPath, FileMode.Create))
                using (var jw = new Utf8JsonWriter(fs))
                {
                    json.WriteTo(jw);
                }

                // Generate seed
                Process generatorProcess = new Process();
                generatorProcess.StartInfo = new ProcessStartInfo();
                generatorProcess.StartInfo.WorkingDirectory = Config.RandomizerPath;
                generatorProcess.StartInfo.FileName = Config.PythonPath;
                generatorProcess.StartInfo.ArgumentList.Add("OoTRandomizer.py");
                generatorProcess.StartInfo.ArgumentList.Add("--loglevel");
                generatorProcess.StartInfo.ArgumentList.Add("error");
                generatorProcess.StartInfo.ArgumentList.Add("--no_log");
                generatorProcess.StartInfo.ArgumentList.Add("--settings");
                generatorProcess.StartInfo.ArgumentList.Add(tempSettingsPath);
                generatorProcess.Start();
                await generatorProcess.WaitForExitAsync();
                if (generatorProcess.ExitCode != 0)
                {
                    return null;
                }

                // Read generated seed
                byte[]? seed = null;
                byte[]? spoiler = null;
                foreach (var file in Directory.GetFiles(tempFolder))
                {
                    if (file.EndsWith(".zpf"))
                    {
                        seed = await File.ReadAllBytesAsync(file);
                    }
                    else if (file.EndsWith("_Spoiler.json"))
                    {
                        spoiler = await File.ReadAllBytesAsync(file);
                    }
                }

                SeedResult? result = null;
                if (seed != null && spoiler != null)
                {
                    result = new SeedResult(seedNumber, seed, spoiler);
                }

                Directory.Delete(tempFolder, true);

                if (semUsed)
                {
                    ConcurrencySem?.Release();
                }

                return result;
            });
        }

        public async Task WriteSeedOnDiskAsync(SeedResult seed)
        {
            string seedFolder = Path.Combine(Config.SeedOutputPath, seed.SeedNumber.ToString());
            string seedFile = Path.Combine(seedFolder, GetSeedFileName(seed.SeedNumber));
            string spoilerLog = Path.Combine(seedFolder, GetSpoilerCompressedFileName(seed.SeedNumber));

            if (Directory.Exists(seedFolder))
            {
                Directory.Delete(seedFolder, true);
            }

            Directory.CreateDirectory(seedFolder);

            // Seed
            await File.WriteAllBytesAsync(seedFile, seed.Seed);

            // Compressed Spoiler
            using (var fs = new FileStream(spoilerLog, FileMode.Create, FileAccess.ReadWrite))
            using (var gz = new GZipStream(fs, CompressionLevel.SmallestSize))
            {
                await gz.WriteAsync(seed.Spoiler, 0, seed.Spoiler.Length);
                gz.Close();
            }
        }

        public static string GetSeedFileName(int seedNumber)
        {
            return $"OoTR_ToT_{seedNumber:000000}.zpf";
        }

        public static string GetSpoilerFileName(int seedNumber)
        {
            return $"OoTR_ToT_{seedNumber:000000}_Spoiler.json";
        }

        public static string GetSpoilerCompressedFileName(int seedNumber)
        {
            return $"{GetSpoilerFileName(seedNumber)}.gz";
        }

        public async Task<string?> GetHash(int seedNumber)
        {
            string spoilerLogPath = Path.Combine(Config.SeedOutputPath, seedNumber.ToString(), GetSpoilerCompressedFileName(seedNumber));
            if (!File.Exists(spoilerLogPath))
            {
                return null;
            }

            // Read spoiler
            JsonNode? json;
            string? hash = null;
            using (var fs = new FileStream(spoilerLogPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var gz = new GZipStream(fs, CompressionMode.Decompress)) 
            {
                json = await JsonNode.ParseAsync(gz);
                hash = json?["file_hash"]?.ToJsonString()?.Replace("[", "")
                                                         ?.Replace("]", "")
                                                         ?.Replace("\"", "")
                                                         ?.Replace(",", " - ");
            }

            return hash;
        }
    }
}
