using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOTRTruthSeedBot.Configuration
{
    public class RestreamConfig
    {
        public string SheetUri { get; set; } = "https://docs.google.com/spreadsheets/d/1r0ryKJNe6gfsPosjyAUlM0-hWWvpPvjoH0SE3X5ULHY/export?format=csv&gid=1431677730";
        public DateTime MinDate { get; set; } = new DateTime(2024, 3, 1);
    }
}
