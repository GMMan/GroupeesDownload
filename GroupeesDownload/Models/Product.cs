using System;
using System.Collections.Generic;
using System.Text;

namespace GroupeesDownload.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string CoverUrl { get; set; }
        public bool IsRevealed { get; set; }
        public bool IsSetForGiveaway { get; set; }
        public bool IsSetForTrade { get; set; }
        public bool IsTradedOut { get; set; }
        public bool IsGiveawayed { get; set; }
        public bool IsFavorite { get; set; }
        public string ProductName { get; set; }
        public string ProductInfoHtml { get; set; }
        public string ProductInfoExtendedHtml { get; set; }
        public List<DownloadableProduct> Downloads { get; set; } = new List<DownloadableProduct>();
        public List<Key> Keys { get; set; } = new List<Key>();
        public List<Track> Tracks { get; set; } = new List<Track>();
        public int? TradeId { get; set; }
        public int? GiveawayId { get; set; }


        public override string ToString()
        {
            return ProductName;
        }
    }
}
