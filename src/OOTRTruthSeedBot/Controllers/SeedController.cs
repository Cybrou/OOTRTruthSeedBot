using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OOTRTruthSeedBot.DAL.Models;
using OOTRTruthSeedBot.SeedGenerator;

namespace OOTRTruthSeedBot.Controllers
{
    [Route("seed")]
    public class SeedController : BaseController
    {
        public SeedController(Context database, Configuration.Config config)
        {
            Database = database;
            Config = config;
        }

        private Configuration.Config Config { get; set; }
        private Context Database { get; set; }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetSeed(int id)
        {
            Seed? seed = await Database.Seeds.Where(s => s.Id == id).FirstOrDefaultAsync();
            if (seed == null || !seed.IsGenerated)
            {
                var result = Content("That seed does not seem to exist.");
                result.StatusCode = 404;

                return result;
            }

            if (seed.IsDeleted)
            {
                var result = Content("That seed is too old and had been purged.");
                result.StatusCode = 410;

                return result;
            }

            string fileName = Generator.GetSeedFileName(seed.Id);
            var fs = new FileStream(Path.Combine(Config.Generator.SeedOutputPath, seed.Id.ToString(), fileName), FileMode.Open, FileAccess.Read, FileShare.Read);
            
            return File(fs, "application/octet-stream", fileName);
        }

        [HttpGet]
        [Route("{id}/spoiler")]
        public async Task<IActionResult> GetSpoiler(int id)
        {
            Seed? seed = await Database.Seeds.Where(s => s.Id == id).FirstOrDefaultAsync();
            if (seed == null || !seed.IsGenerated)
            {
                var result = Content("That seed does not seem to exist.");
                result.StatusCode = 404;

                return result;
            }

            if (seed.IsDeleted)
            {
                var result = Content("That seed is too old and had been purged.");
                result.StatusCode = 410;

                return result;
            }

            if (!seed.IsUnlocked)
            {
                var result = Content("The creator of the seed has not unlocked the spoiler log.");
                result.StatusCode = 401;

                return result;
            }

            string fileName = Generator.GetSpoilerFileName(seed.Id);
            string compFileName = Generator.GetSpoilerCompressedFileName(seed.Id);

            var fs = new FileStream(Path.Combine(Config.Generator.SeedOutputPath, seed.Id.ToString(), compFileName), FileMode.Open, FileAccess.Read, FileShare.Read);
            var gz = new GZipStream(fs, CompressionMode.Decompress);

            return File(gz, "application/json", fileName);
        }
    }
}
