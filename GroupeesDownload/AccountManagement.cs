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

        public List<Bundle> Bundles => bundles;

        public AccountManagement(Client client, Scraper scraper, List<Bundle> bundles)
        {
            this.client = client;
            this.scraper = scraper;
            this.bundles = bundles;
        }

        public async Task RevealAllProducts()
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

        public async Task UnsetTradeAllProducts()
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

        public async Task RevealAllKeys()
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

        public List<string> GenerateDownloadsList()
        {
            List<string> list = new List<string>();

            foreach (var bundle in bundles)
            {
                foreach (var product in bundle.Products)
                {
                    list.AddRange(GenerateDownloadsListForProduct(product));
                }
            }

            return list;
        }

        public List<string> GenerateDownloadsListForProduct(Product product)
        {
            List<string> list = new List<string>();

            // Cover
            if (product.CoverUrl != null)
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
