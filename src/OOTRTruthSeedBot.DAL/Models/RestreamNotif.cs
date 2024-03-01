using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOTRTruthSeedBot.DAL.Models
{
    public class RestreamNotif
    {
        public string Guid { get; set; } = "";

        public DateTime SentDate
        {
            get
            {
                return DateTime.UnixEpoch.AddSeconds(InternalSentDate);
            }
            set
            {
                InternalSentDate = (int)(value - DateTime.UnixEpoch).TotalSeconds;
            }
        }

        public int InternalSentDate { get; set; }
    }
}
