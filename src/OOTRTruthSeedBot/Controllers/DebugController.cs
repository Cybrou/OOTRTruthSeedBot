using Microsoft.AspNetCore.Mvc;
using OOTRTruthSeedBot.DiscordBot;
using OOTRTruthSeedBot.SeedGenerator;

namespace OOTRTruthSeedBot.Controllers
{
    [Route("debug")]
    public class DebugController : BaseController
    {
        public DebugController(Generator generator, Bot bot) {
            Generator = generator;
            Bot = bot;
        }

        private Generator Generator { get; set; }
        private Bot Bot { get; set; }

        [HttpGet]
        [Route("register")]
        public async Task<int> Register()
        {
            await Bot.RegisterCommands();
            return 0;
        }
    }
}
