using GroupeesDownload.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupeesDownload
{
    class AccountManagement
    {
        Client client;
        Scraper scraper;
        List<Bundle> bundles;
        List<Product> tradeProducts;

        public List<Bundle> Bundles => bundles;
        public List<Product> TradeProducts => tradeProducts;

        public AccountManagement(Client client, Scraper scraper, List<Bundle> bundles, List<Product> tradeProducts)
        {
            this.client = client;
            this.scraper = scraper;
            this.bundles = bundles;
            this.tradeProducts = tradeProducts;
        }

        public async Task RevealAllProducts()
        {
            if (bundles != null)
            {
                foreach (var bundle in bundles)
                {
                    bool anyUnrevealed = false;
                    foreach (var product in bundle.Products)
                    {
                        if (!product.IsRevealed && !product.IsGiveawayed && !product.IsSetForTrade)
                        {
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

            if (tradeProducts != null)
            {
                for (int i = 0; i < tradeProducts.Count; ++i)
                {
                    var product = tradeProducts[i];
                    if (!product.IsRevealed && !product.IsGiveawayed && !product.IsSetForTrade)
                    {
                        Console.WriteLine($"Revealing {product.ProductName}");
                        await client.RevealProduct(product.Id);
                        
                        Console.WriteLine($"Refreshing product {product.ProductName}");
                        tradeProducts[i] = await scraper.GetProfileSingleProduct(product.Id);
                    }
                }
            }
        }

        public async Task UnsetTradeAllProducts()
        {
            if (bundles != null)
            {
                foreach (var bundle in bundles)
                {
                    bool anyTraded = false;
                    foreach (var product in bundle.Products)
                    {
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

            if (tradeProducts != null)
            {
                for (int i = 0; i < tradeProducts.Count; ++i)
                {
                    var product = tradeProducts[i];
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
                        tradeProducts[i] = await scraper.GetProfileSingleProduct(product.Id);
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

        public async Task UnsetGiveawayAllProducts()
        {
            if (bundles != null)
            {
                foreach (var bundle in bundles)
                {
                    bool anyGiveawayed = false;
                    foreach (var product in bundle.Products)
                    {
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

            if (tradeProducts != null)
            {
                for (int i = 0; i < tradeProducts.Count; ++i)
                {
                    var product = tradeProducts[i];
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
                        tradeProducts[i] = await scraper.GetProfileSingleProduct(product.Id);
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

        public async Task RevealAllKeys()
        {
            if (bundles != null)
            {
                foreach (var bundle in bundles)
                {
                    bool anyUnrevealed = false;
                    foreach (var product in bundle.Products)
                    {
                        foreach (var key in product.Keys)
                        {
                            if (!key.IsRevealed && !key.IsGiveawayed && !key.IsTradedOut)
                            {
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

            if (tradeProducts != null)
            {
                for (int i = 0; i < tradeProducts.Count; ++i)
                {
                    bool anyUnrevealed = false;
                    var product = tradeProducts[i];

                    foreach (var key in product.Keys)
                    {
                        if (!key.IsRevealed && !key.IsGiveawayed && !key.IsTradedOut)
                        {
                            anyUnrevealed = true;
                            Console.WriteLine($"Revealing key {key.PlatformName} for {product.ProductName}");
                            await client.RevealKey(key.Id);
                        }
                    }
                    if (anyUnrevealed)
                    {
                        Console.WriteLine($"Refreshing product {product.ProductName}");
                        tradeProducts[i] = await scraper.GetProfileSingleProduct(product.Id);
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

        public List<string> GenerateDownloadsList(bool includeCover)
        {
            List<string> list = new List<string>();

            if (bundles != null)
            {
                foreach (var bundle in bundles)
                {
                    foreach (var product in bundle.Products)
                    {
                        list.AddRange(GenerateDownloadsListForProduct(product, includeCover));
                    }
                }
            }

            return list;
        }

        public List<string> GenerateDownloadsListForProduct(Product product, bool includeCover)
        {
            List<string> list = new List<string>();

            // Cover
            if (includeCover && product.CoverUrl != null)
                list.Add(product.CoverUrl.Replace("plate_square/", string.Empty).Replace("small/", string.Empty).Replace("big/", string.Empty));

            // Is this music?
            if (product.Tracks.Count != 0)
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
                        list.Add(bestFile.Url);
                    }
                }
            }
            else
            {
                foreach (var download in product.Downloads)
                {
                    // Queue everything
                    foreach (var file in download.Files)
                        list.Add(file.Url);
                }
            }

            return list;
        }
    }
}
