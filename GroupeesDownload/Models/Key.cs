using System;
using System.Collections.Generic;
using System.Text;

namespace GroupeesDownload.Models
{
    class Key
    {
        public int Id { get; set; }
        public string PlatformName { get; set; }
        public string Code { get; set; }
        public bool IsUsed { get; set; }
        public bool IsRevealed { get; set; }
        public bool IsSetForGiveaway { get; set; }
        public bool IsSetForTrade { get; set; }
        public bool IsTradedOut { get; set; }
        public bool IsGiveawayed { get; set; }
        public bool IsPotentiallyNotRevealed { get; set; }
        public int? TradeId { get; set; }
        public int? GiveawayId { get; set; }

        public override string ToString()
        {
            return PlatformName;
        }
    }
}
