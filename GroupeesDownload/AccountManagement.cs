using GroupeesDownload.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupeesDownload
{
    public class AccountManagement
    {
        static readonly Dictionary<string, string[]> CATEGORY_MAP = new Dictionary<string, string[]>
        {
            ["music"] = new[] { "mp3", "flac" },
            ["games"] = new[] { "pc", "os x" },
            ["books"] = new[] { "pdf", "cbz", "epub", "mobi", "cbr" },
            ["movies"] = new[] { "mp4", "mov", "m4v", "scc", "srt" },
            ["android"] = new[] { "apk", "android" },
        };

        Client client;
        Scraper scraper;
        List<Bundle> bundles;
        List<Product> tradeProducts;
        List<Product> thirdPartyKeys;

        public List<Bundle> Bundles => bundles;
        public List<Product> TradeProducts => tradeProducts;
        public List<Product> ThirdPartyKeys => thirdPartyKeys;

        public AccountManagement(Client client, Scraper scraper, List<Bundle> bundles, List<Product> tradeProducts, List<Product> thirdPartyKeys)
        {
            this.client = client;
            this.scraper = scraper;
            this.bundles = bundles;
            this.tradeProducts = tradeProducts;
            this.thirdPartyKeys = thirdPartyKeys;
        }

        public async Task RevealAllProducts(bool thirdParty)
        {
            if (bundles != null && !thirdParty)
            {
                foreach (var bundle in bundles)
                {
                    if (bundle.IsSetForGiveaway || bundle.IsSetForTrade || bundle.IsGiveawayed || bundle.IsTradedOut)
                    {
                        Console.WriteLine($"Bundle {bundle.BundleName} skipped because marked for/has been giveawayed or traded");
                        continue;
                    }

                    bool anyUnrevealed = false;
                    foreach (var product in bundle.Products)
                    {
                        if (product.Id == -1) continue;
                        if (!product.IsRevealed)
                        {
                            if (product.IsSetForGiveaway || product.IsSetForTrade || product.IsGiveawayed || product.IsTradedOut)
                            {
                                Console.WriteLine($"Product {product.ProductName} in {bundle.BundleName} skipped because marked for/has been giveawayed or traded");
                                continue;
                            }

                            anyUnrevealed = true;
                            Console.WriteLine($"Revealing {product.ProductName} from {bundle.BundleName}");
                            await client.RevealProduct(product.Id);
                        }
                    }
                    if (anyUnrevealed)
                    {
                        Console.WriteLine($"Refreshing bundle {bundle.BundleName}");
                        await scraper.UpdateBundleProducts(bundle);
                    }
                }
            }

            List<Product> targetProducts = thirdParty ? thirdPartyKeys : tradeProducts;
            if (targetProducts != null)
            {
                for (int i = 0; i < targetProducts.Count; ++i)
                {
                    var product = targetProducts[i];
                    if (product.Id == -1) continue;
                    if (!product.IsRevealed)
                    {
                        if (product.IsSetForGiveaway || product.IsSetForTrade || product.IsGiveawayed || product.IsTradedOut)
                        {
                            Console.WriteLine($"{(thirdParty ? "Third party key" : "Traded product")} {product.ProductName} skipped because marked for/has been giveawayed or traded");
                            continue;
                        }

                        Console.WriteLine($"Revealing {product.ProductName}");
                        await client.RevealProduct(product.Id);

                        Console.WriteLine($"Refreshing product {product.ProductName}");
                        targetProducts[i] = await scraper.GetUserProduct(product.Id);
                    }
                }
            }
        }

        public async Task UnsetTradeAllProducts(bool thirdParty)
        {
            if (bundles != null && !thirdParty)
            {
                foreach (var bundle in bundles)
                {
                    bool anyTraded = false;
                    foreach (var product in bundle.Products)
                    {
                        if (product.Id == -1) continue;
                        if (product.IsSetForTrade)
                        {
                            anyTraded = true;
                            Console.WriteLine($"Unsetting trade for {product.ProductName} from {bundle.BundleName}");
                            await client.DeleteTradeableItem(product.TradeId.Value);
                        }

                        foreach (var key in product.Keys)
                        {
                            if (key.IsSetForTrade)
                            {
                                anyTraded = true;
                                Console.WriteLine($"Unsetting trade for key {key.PlatformName} for {product.ProductName} from {bundle.BundleName}");
                                await client.DeleteTradeableItem(key.TradeId.Value);
                            }
                        }
                    }
                    if (anyTraded)
                    {
                        Console.WriteLine($"Refreshing bundle {bundle.BundleName}");
                        await scraper.UpdateBundleProducts(bundle);
                    }
                }
            }

            List<Product> targetProducts = thirdParty ? thirdPartyKeys : tradeProducts;
            if (targetProducts != null)
            {
                for (int i = 0; i < targetProducts.Count; ++i)
                {
                    var product = targetProducts[i];
                    if (product.Id == -1) continue;
                    bool anyTraded = false;

                    if (product.IsSetForTrade)
                    {
                        Console.WriteLine($"Unsetting trade for {product.ProductName}");
                        await client.DeleteTradeableItem(product.TradeId.Value);
                        anyTraded = true;
                    }

                    foreach (var key in product.Keys)
                    {
                        if (key.IsSetForTrade)
                        {
                            anyTraded = true;
                            Console.WriteLine($"Unsetting trade for key {key.PlatformName} for {product.ProductName}");
                            await client.DeleteTradeableItem(key.TradeId.Value);
                        }
                    }

                    if (anyTraded)
                    {
                        Console.WriteLine($"Refreshing product {product.ProductName}");
                        targetProducts[i] = await scraper.GetUserProduct(product.Id);
                    }
                }
            }
        }

        public async Task UnsetTradesById(IEnumerable<int> ids)
        {
            foreach (var id in ids)
            {
                Console.WriteLine($"Unsetting trade for {id}");
                await client.DeleteTradeableItem(id);
            }
        }

        public async Task UnsetGiveawayAllProducts(bool thirdParty)
        {
            if (bundles != null && !thirdParty)
            {
                foreach (var bundle in bundles)
                {
                    bool anyGiveawayed = false;
                    foreach (var product in bundle.Products)
                    {
                        if (product.Id == -1) continue;
                        if (product.IsSetForGiveaway)
                        {
                            anyGiveawayed = true;
                            Console.WriteLine($"Unsetting giveway for {product.ProductName} from {bundle.BundleName}");
                            await client.DeleteGiveawayItem(product.GiveawayId.Value);
                        }

                        foreach (var key in product.Keys)
                        {
                            if (key.IsSetForGiveaway)
                            {
                                Console.WriteLine($"Unsetting giveaway for key {key.PlatformName} for {product.ProductName} from {bundle.BundleName}");
                                await client.DeleteGiveawayItem(key.GiveawayId.Value);
                                anyGiveawayed = true;
                            }
                        }
                    }
                    if (anyGiveawayed)
                    {
                        Console.WriteLine($"Refreshing bundle {bundle.BundleName}");
                        await scraper.UpdateBundleProducts(bundle);
                    }
                }
            }

            List<Product> targetProducts = thirdParty ? thirdPartyKeys : tradeProducts;
            if (targetProducts != null)
            {
                for (int i = 0; i < targetProducts.Count; ++i)
                {
                    var product = targetProducts[i];
                    if (product.Id == -1) continue;
                    bool anyGiveawayed = false;

                    if (product.IsSetForGiveaway)
                    {
                        Console.WriteLine($"Unsetting giveaway for {product.ProductName}");
                        await client.DeleteGiveawayItem(product.GiveawayId.Value);
                        anyGiveawayed = true;
                    }

                    foreach (var key in product.Keys)
                    {
                        if (key.IsSetForGiveaway)
                        {
                            Console.WriteLine($"Unsetting giveaway for key {key.PlatformName} for {product.ProductName}");
                            await client.DeleteGiveawayItem(key.GiveawayId.Value);
                            anyGiveawayed = true;
                        }
                    }

                    if (anyGiveawayed)
                    {
                        Console.WriteLine($"Refreshing product {product.ProductName}");
                        targetProducts[i] = await scraper.GetUserProduct(product.Id);
                    }
                }
            }
        }

        public async Task UnsetGiveawaysById(IEnumerable<int> ids)
        {
            foreach (var id in ids)
            {
                Console.WriteLine($"Unsetting giveaway for {id}");
                await client.DeleteGiveawayItem(id);
            }
        }

        public async Task RevealAllKeys(bool thirdParty)
        {
            if (bundles != null && !thirdParty)
            {
                foreach (var bundle in bundles)
                {
                    if (bundle.IsSetForGiveaway || bundle.IsSetForTrade || bundle.IsGiveawayed || bundle.IsTradedOut)
                    {
                        Console.WriteLine($"Bundle {bundle.BundleName} skipped because marked for/has been giveawayed or traded");
                        continue;
                    }

                    bool anyUnrevealed = false;
                    foreach (var product in bundle.Products)
                    {
                        if (product.IsSetForGiveaway || product.IsSetForTrade || product.IsGiveawayed || product.IsTradedOut)
                        {
                            Console.WriteLine($"Product {product.ProductName} in {bundle.BundleName} skipped because marked for/has been giveawayed or traded");
                            continue;
                        }

                        foreach (var key in product.Keys)
                        {
                            if (!key.IsRevealed)
                            {
                                if (key.IsSetForGiveaway || key.IsSetForTrade || key.IsGiveawayed || key.IsTradedOut)
                                {
                                    Console.WriteLine($"Key {key.PlatformName} for {product.ProductName} in {bundle.BundleName} skipped because marked for/has been giveawayed or traded");
                                    continue;
                                }

                                anyUnrevealed = true;
                                Console.WriteLine($"Revealing key {key.PlatformName} for {product.ProductName} from {bundle.BundleName}");
                                await client.RevealKey(key.Id);
                            }
                        }
                    }
                    if (anyUnrevealed)
                    {
                        Console.WriteLine($"Refreshing bundle {bundle.BundleName}");
                        await scraper.UpdateBundleProducts(bundle);
                    }
                }
            }

            List<Product> targetProducts = thirdParty ? thirdPartyKeys : tradeProducts;
            if (targetProducts != null)
            {
                for (int i = 0; i < targetProducts.Count; ++i)
                {
                    bool anyUnrevealed = false;
                    var product = targetProducts[i];

                    if (product.IsSetForGiveaway || product.IsSetForTrade || product.IsGiveawayed || product.IsTradedOut)
                    {
                        Console.WriteLine($"{(thirdParty ? "Third party key" : "Traded product")} {product.ProductName} skipped because marked for/has been giveawayed or traded");
                        continue;
                    }

                    foreach (var key in product.Keys)
                    {
                        if (!key.IsRevealed && !key.IsGiveawayed && !key.IsTradedOut)
                        {
                            if (key.IsSetForGiveaway || key.IsSetForTrade || key.IsGiveawayed || key.IsTradedOut)
                            {
                                Console.WriteLine($"Key {key.PlatformName} for {(thirdParty ? "third party key" : "traded product")} {product.ProductName} skipped because marked for/has been giveawayed or traded");
                                continue;
                            }

                            anyUnrevealed = true;
                            Console.WriteLine($"Revealing key {key.PlatformName} for {product.ProductName}");
                            await client.RevealKey(key.Id);
                        }
                    }
                    if (anyUnrevealed)
                    {
                        Console.WriteLine($"Refreshing product {product.ProductName}");
                        targetProducts[i] = await scraper.GetUserProduct(product.Id);
                    }
                }
            }
        }

        public async Task RevealKeysById(IEnumerable<int> ids)
        {
            foreach (var id in ids)
            {
                Console.WriteLine($"Revealing key {id}");
                await client.RevealKey(id);
            }
        }

        public List<string> GenerateDownloadsList(bool includeCover, DirOrganizationType dirOrgType, DownloadFilterTypes filter, bool dedupe)
        {
            List<string> list = new List<string>();
            HashSet<string> seenUrls = dedupe ? new HashSet<string>() : null;

            if (bundles != null)
            {
                foreach (var bundle in bundles)
                {
                    string dirParent = null;
                    if (dirOrgType != DirOrganizationType.None && !string.IsNullOrWhiteSpace(bundle.BundleName))
                    {
                        if (string.IsNullOrWhiteSpace(bundle.BundleName))
                            dirParent = "Unknown Bundle";
                        else
                            dirParent = SanitizeFilename(bundle.BundleName);
                    }
                    foreach (var product in bundle.Products)
                    {
                        list.AddRange(GenerateDownloadsListForProduct(product, includeCover, dirParent, dirOrgType, filter, seenUrls));
                    }
                }
            }

            if (tradeProducts != null)
            {
                string append = null;
                if (dirOrgType != DirOrganizationType.None)
                {
                    append = "trades";
                }
                foreach (var product in tradeProducts)
                {
                    list.AddRange(GenerateDownloadsListForProduct(product, includeCover, append, dirOrgType, filter, seenUrls));
                }
            }

            return list;
        }

        public List<string> GenerateDownloadsListForProduct(Product product, bool includeCover, string dirParent, DirOrganizationType dirOrgType, DownloadFilterTypes filter, HashSet<string> seenUrls)
        {
            List<string> list = new List<string>();

            if ((filter & DownloadFilterTypes.All) != DownloadFilterTypes.All)
            {
                if (product.Tracks.Count != 0)
                {
                    // Music
                    if ((filter & DownloadFilterTypes.Music) == 0) return list;
                }
                else
                {
                    if (product.Downloads.Count == 1 && product.Downloads[0].Name == null)
                    {
                        // Game, presumably
                        if ((filter & DownloadFilterTypes.Games) == 0) return list;
                    }
                    else
                    {
                        // Other products
                        if ((filter & DownloadFilterTypes.Others) == 0) return list;
                    }
                }
            }

            // Cover
            if (includeCover && product.CoverUrl != null)
            {
                string coverUrl = product.CoverUrl.Replace("plate_square/", string.Empty).Replace("small/", string.Empty).Replace("big/", string.Empty);
                // Handle coin icons
                if (coverUrl.StartsWith("/")) coverUrl = "https://groupees.com" + coverUrl;
                list.Add(coverUrl);
                if (dirParent != null && dirOrgType != DirOrganizationType.None)
                {
                    if (dirOrgType == DirOrganizationType.BundleAndProduct)
                    {
                        list.Add($"\tdir={Path.Combine(dirParent, SanitizeFilename(product.ProductName))}");
                        list.Add($"\tout=cover{Path.GetExtension(coverUrl)}");
                    }
                    else if (dirOrgType == DirOrganizationType.BundleAndType)
                    {
                        list.Add($"\tdir={Path.Combine(dirParent, "covers")}");
                        list.Add($"\tout={SanitizeFilename(product.ProductName + Path.GetExtension(coverUrl))}");
                    }
                }
            }

            // Is this music?
            if (product.Tracks.Count != 0 && (filter & DownloadFilterTypes.MusicDownloadAll) == 0)
            {
                foreach (var download in product.Downloads)
                {
                    // Try to pick FLAC only
                    DownloadFile bestFile = null;
                    foreach (var file in download.Files)
                    {
                        bestFile = file;
                        if (file.PlatformName == "flac") break;
                    }
                    if (bestFile == null)
                    {
                        Console.WriteLine($"!!! No files for music {product.ProductName}");
                    }
                    else
                    {
                        //Console.WriteLine($"Music {product.ProductName}: picked {bestFile.PlatformName} from {string.Join(", ", download.Files.Select(x => x.PlatformName))}");
                        AddUrlToList(bestFile.Url, list, seenUrls, dirParent, dirOrgType, product.ProductName, CategorizeProduct(product, bestFile));
                    }
                }
            }
            else
            {
                foreach (var download in product.Downloads)
                {
                    // Queue everything
                    foreach (var file in download.Files)
                    {
                        AddUrlToList(file.Url, list, seenUrls, dirParent, dirOrgType, product.ProductName, CategorizeProduct(product, file));
                    }
                }
            }

            return list;
        }

        void AddUrlToList(string url, List<string> list, HashSet<string> seenUrls, string dirParent, DirOrganizationType dirOrgType, string productName, string category)
        {
            if (seenUrls == null || !seenUrls.Contains(url))
            {
                list.Add(url);
                if (seenUrls != null) seenUrls.Add(url);
                if (dirParent != null && dirOrgType != DirOrganizationType.None)
                {
                    if (dirOrgType == DirOrganizationType.BundleAndProduct)
                    {
                        list.Add($"\tdir={Path.Combine(dirParent, SanitizeFilename(productName))}");
                    }
                    else if (dirOrgType == DirOrganizationType.BundleAndType)
                    {
                        list.Add($"\tdir={Path.Combine(dirParent, category)}");
                    }
                }
            }
        }

        string CategorizeProduct(Product product, DownloadFile file)
        {
            string lowerPlatform = file.PlatformName?.ToLower();
            if (product.Tracks.Count != 0) return "music";
            foreach (var pair in CATEGORY_MAP)
            {
                foreach (var plat in pair.Value)
                {
                    if (lowerPlatform.Contains(plat)) return pair.Key;
                }
            }
            return "others";
        }

        public List<string> ExportAllKeys()
        {
            List<string> export = new List<string>();
            // Header
            export.Add("Bundle name,Product name,Platform,Code,Used,Third party");

            if (bundles != null)
            {
                foreach (var bundle in bundles)
                {
                    foreach (var product in bundle.Products)
                    {
                        export.AddRange(ExportKeysForProduct(product, bundle));
                    }
                }
            }

            if (tradeProducts != null)
            {
                foreach (var product in tradeProducts)
                {
                    export.AddRange(ExportKeysForProduct(product, null));
                }
            }

            if (thirdPartyKeys != null)
            {
                foreach (var product in thirdPartyKeys)
                {
                    export.AddRange(ExportKeysForProduct(product, null));
                }
            }

            return export;
        }

        public List<string> ExportKeysForProduct(Product product, Bundle bundle)
        {
            List<string> export = new List<string>();
            foreach (var key in product.Keys)
            {
                export.Add($"\"{bundle?.BundleName ?? string.Empty}\",\"{product.ProductName}\",\"{key.PlatformName}\",\"{(key.IsRevealed ? key.Code : $"<{(key.IsPotentiallyNotRevealed ? "potentially " : string.Empty)}not revealed>")}\",{key.IsUsed},{key.IsThirdParty}");
            }
            if (product.IsOwnThirdPartyKey && product.Keys.Count == 0)
            {
                // Own third party keys don't show a key if not revealed, so insert a placeholder entry
                export.Add($"\"\",\"{product.ProductName}\",\"Third Party Key\",\"<not revealed>\",false,true");
            }
            return export;
        }

        static string SanitizeFilename(string s)
        {
            // https://stackoverflow.com/a/13617375/1180879
            var invalids = System.IO.Path.GetInvalidFileNameChars();
            return String.Join("_", s.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }
    }
}
