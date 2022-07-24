using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace GroupeesDownload.Models
{
    class Bundle
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("bundle_id")]
        public int BundleId { get; set; }
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }
        [JsonPropertyName("gift_taker_id")]
        public int? GiftTakerId { get; set; }
        [JsonPropertyName("kind")]
        public int Kind { get; set; }
        [JsonPropertyName("total_amount")]
        public string TotalAmount { get; set; }
        [JsonPropertyName("completed_at")]
        public string CompletedAt { get; set; }
        [JsonPropertyName("order_ids")]
        public List<int> OrderIds { get; set; }
        [JsonPropertyName("bundle_name")]
        public string BundleName { get; set; }
        public List<Product> Products { get; set; } = new List<Product>();
        public string Announcements { get; set; }
        // These ones I don't have any of, so don't know how to check
        public bool IsRevealed { get; set; }
        public bool IsSetForGiveaway { get; set; }
        public bool IsSetForTrade { get; set; }
        public bool IsTradedOut { get; set; }
        public bool IsGiveawayed { get; set; }


        public override string ToString()
        {
            return $"{BundleName} - ${TotalAmount} on {CompletedAt}";
        }
    }
}
