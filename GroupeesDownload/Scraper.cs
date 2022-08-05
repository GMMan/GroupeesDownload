using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using GroupeesDownload.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GroupeesDownload
{
    public class Scraper
    {
        Client client;
        IBrowsingContext asContext;

        public Scraper(Client client)
        {
            this.client = client;
            asContext = BrowsingContext.New(null);
        }

        public async Task Test()
        {
            ;
        }

        public async Task AutoSetTokens()
        {
            Console.WriteLine("Auto-obtaining user ID and CSRF token...");

            string html = await client.GetHome();
            using IDocument doc = await asContext.OpenAsync(req => req.Content(html));

            var nCsrfToken = doc.Head.QuerySelector("meta[name='csrf-token']");
            string csrfToken = nCsrfToken.GetAttribute("content");

            var nFayepub = doc.Body.QuerySelector(".fayepub");
            int userId = int.Parse(nFayepub.GetAttribute("data-user"));

            client.SetOtherTokens(userId, csrfToken);
        }

        public async Task<List<Product>> GetAllTradesCompletedProducts()
        {
            List<Product> products = new List<Product>();
            var ids = await GetAllTradesCompletedProductIds();
            int num = 0;
            foreach (var id in ids)
            {
                ++num;
                Console.WriteLine($"Getting trade product {num}/{ids.Count}");
                products.Add(await GetProfileSingleProduct(id));
            }
            return products;
        }

        public async Task<List<Bundle>> GetAllBundles()
        {
            var bundles = await GetAllBundlesList();
            int num = 0;
            foreach (var bundle in bundles)
            {
                ++num;
                Console.WriteLine($"Getting bundle {num}/{bundles.Count}: {bundle.BundleName}");
                var tup = await GetBundleProducts(bundle.Id);
                bundle.Products = tup.Item1;
                bundle.Announcements = tup.Item2;
            }
            return bundles;
        }

        public async Task UpdateBundleProducts(Bundle bundle)
        {
            var tup = await GetBundleProducts(bundle.Id);
            bundle.Products = tup.Item1;
            bundle.Announcements = tup.Item2;
        }

        public async Task<Tuple<List<Product>, string>> GetBundleProducts(int orderId)
        {
            var html = await GetOrderHtml(orderId);
            return await ParseProducts(html);
        }

        async Task<List<Bundle>> GetAllBundlesList()
        {
            List<Bundle> bundles = new List<Bundle>();
            for (int i = 1; ; ++i)
            {
                Console.WriteLine($"Getting bundles page {i}");
                var currPageBundles = await client.GetBundles(i);
                if (currPageBundles.Count == 0) break;
                bundles.AddRange(currPageBundles);
            }
            return bundles;
        }

        async Task<string> GetOrderHtml(int id)
        {
            // Assume HTML is on its own line
            var js = await client.GetOrder(id);
            foreach (var line in js.Split("\n"))
            {
                if (line.StartsWith("var html "))
                {
                    int startIndex = line.IndexOf("'");
                    int endIndex = line.LastIndexOf("'");
                    if (startIndex < 0) continue;
                    return Regex.Unescape(line.Substring(startIndex + 1, endIndex - startIndex - 1));
                }
            }

            throw new ParsingException("Could not find html line.", js);
        }

        public async Task<List<int>> GetAllTradesCompletedProductIds()
        {
            List<int> ids = new List<int>();
            for (int i = 1; ; ++i)
            {
                Console.WriteLine($"Getting trades page {i}");
                string html = await GetTradesCompletedPageHtml(i);
                var pageIds = await ParseTradesCompletedPageForProductIds(html);
                if (pageIds.Count == 0) break;
                ids.AddRange(pageIds);
            }
            return ids;
        }

        public async Task<Product> GetProfileSingleProduct(int id)
        {
            string html = await client.GetProfileProduct(id);
            Product p = await ParseProfileSingleProduct(html, id);
            html = await GetProfileSingleProductHtml(id);
            await ParseProfileSingleProductDetails(html, p);
            return p;
        }

        async Task<Product> ParseProfileSingleProduct(string html, int id)
        {
            using IDocument doc = await asContext.OpenAsync(req => req.Content(html));
            var nCell = doc.QuerySelector($".product-cell[data-id={id}]");
            Product p = new Product();
            p.CoverUrl = "https://groupees.com" + nCell.GetSingleByClassName("product-cover").GetAttribute("src");
            p.ProductName = nCell.QuerySelector("h4").GetAttribute("title");
            var nTransmittedInfo = nCell.GetSingleOrDefaultByClassName("transmitted-info");
            if (nTransmittedInfo != null && !nTransmittedInfo.HasClassName("hidden"))
            {
                var text = nTransmittedInfo.GetSingleByClassName("h4").TextContent;
                if (text.Contains("Givenaway"))
                    p.IsGiveawayed = true;
                else if (text.Contains("Traded out"))
                    p.IsTradedOut = true;
                else if (!text.Contains("Set for Trade") && !text.Contains("Set for Giveaway"))
                    throw new ParsingException($"Product transmitted info contains unknown text: {text}", nTransmittedInfo.OuterHtml);

                var revokeButton = nTransmittedInfo.GetSingleByClassName("btn-revoke-product");
                if (!revokeButton.HasClassName("disabled"))
                {
                    if (revokeButton.GetAttribute("data-trade") == "true")
                    {
                        p.IsSetForTrade = true;
                        p.TradeId = int.Parse(revokeButton.GetAttribute("data-id"));
                    }
                    else
                    {
                        p.IsSetForGiveaway = true;
                        p.GiveawayId = int.Parse(revokeButton.GetAttribute("data-id"));
                    }
                }
            }
            return p;
        }

        async Task<string> GetProfileSingleProductHtml(int id)
        {
            // Assume HTML is on its own line
            var js = await client.GetProfileProductDetails(id);
            foreach (var line in js.Split("\n"))
            {
                if (line.Contains("var expandedPart "))
                {
                    int startIndex = line.IndexOf("'");
                    int endIndex = line.LastIndexOf("'");
                    if (startIndex < 0) continue;
                    return Regex.Unescape(line.Substring(startIndex + 1, endIndex - startIndex - 1));
                }
            }

            throw new ParsingException("Could not find html line.", js);
        }

        async Task ParseProfileSingleProductDetails(string html, Product p)
        {
            using IDocument doc = await asContext.OpenNewAsync();

            var root = asContext.GetService<IHtmlParser>().ParseFragment(html, doc.Body).Where(x => x.NodeType == NodeType.Element).Single() as IElement;
            p.IsRevealed = root.GetAttribute("data-revealed") == "true";
            var nDetails = root.GetSingleByClassName("product-index").GetSingleByClassName("row");
            p.ProductInfoHtml = nDetails.QuerySelector(".desc-info").InnerHtml;
            p.ProductInfoExtendedHtml = nDetails.QuerySelector(".product-full-info").InnerHtml;

            // Keys
            foreach (var nKey in nDetails.QuerySelectorAll(".key-block"))
            {
                Key k = new Key();
                var keyP = nKey.QuerySelector(":scope > p");
                k.PlatformName = Regex.Replace(keyP.ChildNodes[0].TextContent.Trim(), " Key$", string.Empty);
                // Assume if third party, there's only one key in the product so the whole product represents the key
                k.IsThirdParty = p.ProductInfoHtml.Contains("This product was created with third party key.");
                
                var nStatus = nKey.QuerySelector(".key-status");
                if (nStatus.TextContent.Length == 0)
                {
                    k.IsRevealed = true;
                }
                else
                {
                    var text = nStatus.TextContent;
                    if (text.Contains("(used)"))
                        k.IsRevealed = true;
                    else if (text.Contains("(givenaway"))
                        k.IsGiveawayed = true;
                    else if (text.Contains("(traded out)")) // Not verified
                        k.IsTradedOut = true;
                    else if (!text.Contains("(Not revealed)") && !text.Contains("(set for trade)") && !text.Contains("(available in Giveaways)"))
                        throw new ParsingException($"Key status contains unknown text: {nStatus.TextContent}", nKey.OuterHtml);
                }

                if (k.IsRevealed)
                {
                    k.Code = nKey.QuerySelector(".code").GetAttribute("value");
                    k.IsUsed = nKey.QuerySelector(".key-usage-toggler").GetAttribute("data-state") == "true";
                }
                else
                {
                    var giveawayButton = nKey.QuerySelector(".btn-giveaway-key");
                    if (giveawayButton != null && giveawayButton.HasClassName("active"))
                    {
                        k.IsSetForGiveaway = true;
                        k.GiveawayId = int.Parse(giveawayButton.GetAttribute("data-id"));
                    }

                    var tradeButton = nKey.QuerySelector(".btn-trade-key");
                    if (tradeButton != null && tradeButton.HasClassName("active"))
                    {
                        k.IsSetForTrade = true;
                        k.TradeId = int.Parse(giveawayButton.GetAttribute("data-id"));
                    }
                }

                p.Keys.Add(k);
            }

            // Downloads
            DownloadableProduct dp = new DownloadableProduct();
            foreach (var nDownload in nDetails.QuerySelectorAll(".btn-download"))
            {
                DownloadFile df = new DownloadFile();
                df.PlatformName = Regex.Replace(nDownload.ChildNodes.Where(x => x.NodeType == NodeType.Text && x.TextContent.Trim().Length != 0).First().TextContent.Trim(),
                    "^Download (for )?", string.Empty);
                df.Url = nDownload.QuerySelector(":scope > .btn-menu > li > a").GetAttribute("href");

                // Check if preceeding element is <h3>, as this indicates new downloadable product
                var predecessor = nDownload.PreviousElementSibling;
                if (predecessor != null && predecessor.TagName == "H3")
                {
                    if (dp.Files.Count != 0) p.Downloads.Add(dp);
                    dp = new DownloadableProduct();
                    dp.Name = predecessor.TextContent;
                }

                dp.Files.Add(df);
            }

            if (dp.Files.Count != 0) p.Downloads.Add(dp);

            // Music
            var nTrackList = nDetails.QuerySelector(".track-list");
            if (nTrackList != null)
            {
                foreach (IElement nTrack in nTrackList.ChildNodes.Where(x => x.NodeType == NodeType.Element))
                {
                    if (nTrack.TagName != "LI") throw new ParsingException("Non-list item found in track list.", nTrackList.OuterHtml);
                    Track t = new Track();

                    var nFavorite = nTrack.QuerySelector(":scope > .track-actions > .favorite-star");
                    t.Id = int.Parse(nFavorite.GetAttribute("data-track-id"));
                    t.IsFavorite = nFavorite.HasClassName("favorite");
                    t.Name = nTrack.GetSingleByClassName("track-title").TextContent;

                    p.Tracks.Add(t);
                }
            }
        }

        async Task<string> GetTradesCompletedPageHtml(int page)
        {
            // Assume HTML is on its own line
            var js = await client.GetTrades(page);
            foreach (var line in js.Split("\n"))
            {
                if (line.Contains("var newItems "))
                {
                    int startIndex = line.IndexOf("'");
                    int endIndex = line.LastIndexOf("'");
                    if (startIndex < 0) continue;
                    return Regex.Unescape(line.Substring(startIndex + 1, endIndex - startIndex - 1));
                }
            }

            throw new ParsingException("Could not find html line.", js);
        }

        async Task<List<int>> ParseTradesCompletedPageForProductIds(string html)
        {
            List<int> ids = new List<int>();
            using IDocument doc = await asContext.OpenNewAsync();
            var table = doc.CreateElement("table");
            doc.Body.AppendChild(table);
            var tbody = doc.CreateElement("tbody");
            table.AppendChild(tbody);

            foreach (IElement row in asContext.GetService<IHtmlParser>().ParseFragment(html, tbody).Where(x => x.NodeType == NodeType.Element))
            {
                if (row.TagName == "TR" && row.GetAttribute("data-id") != null)
                {
                    string url = row.QuerySelector(":scope > .item-title-cell > a").GetAttribute("href");
                    if (url.EndsWith("coins")) continue;
                    if (url.Contains("/purchases/")) continue; // Should be picked up by bundles scrape
                    ids.Add(int.Parse(url.Substring(url.LastIndexOf("/") + 1)));
                }
            }

            return ids;
        }

        async Task<Tuple<List<Product>, string>> ParseProducts(string html)
        {
            List<Product> products = new List<Product>();
            using IDocument doc = await asContext.OpenNewAsync();
            string announcements = null;

            foreach (IElement nProduct in asContext.GetService<IHtmlParser>().ParseFragment(html, doc.Body).Where(x => x.NodeType == NodeType.Element))
            {
                if (nProduct.HasClassName("announcements"))
                {
                    announcements = nProduct.QuerySelector(".announcement").InnerHtml;
                    continue;
                }

                if (!nProduct.HasClassName("product")) throw new ParsingException("Non-product found at root level.", nProduct.OuterHtml);
                Product p = new Product();

                // Basic info
                var idAttr = nProduct.GetAttribute("data-id");
                // Gifted coins don't have IDs
                p.Id = idAttr == null ? -1 : int.Parse(idAttr);
                p.CoverUrl = nProduct.GetSingleByClassName("cover").GetAttribute("src");
                var nDetails = nProduct.GetSingleByClassName("details");
                if (p.Id == -1)
                {
                    // Coin handling
                    p.ProductName = nDetails.QuerySelector(":scope > h3").TextContent;
                    p.ProductInfoHtml = nDetails.GetSingleByClassName("product-type").InnerHtml;
                }
                else
                {
                    p.ProductName = nDetails.GetSingleByClassName("product-name").TextContent;
                    p.IsFavorite = nDetails.GetSingleOrDefaultByClassName("favorite-star")?.HasClassName("favorite") ?? false;
                    var nActions = nDetails.GetSingleOrDefaultByClassName("actions");
                    if (nActions != null)
                    {
                        var nProductMeta = nActions.GetSingleOrDefaultByClassName("product-meta");
                        if (nProductMeta != null && nProductMeta.InnerHtml != "\n")
                        {
                            string text = nProductMeta.TextContent;
                            if (text.Contains("Traded Out"))
                                p.IsTradedOut = true;
                            else if (text.Contains("Givenaway"))
                                p.IsGiveawayed = true;
                            else if (!text.Contains("Set for trade") && !text.Contains("Available in Giveaways"))
                                throw new ParsingException($"Product meta contains unknown text: {text}", nProductMeta.OuterHtml);
                        }
                        p.IsRevealed = nActions.GetSingleOrDefaultByClassName("reveal-product") == null;
                        if (!p.IsRevealed)
                        {
                            var giveawayButton = nActions.GetSingleOrDefaultByClassName("giveaway-product");
                            var tradeButton = nActions.GetSingleOrDefaultByClassName("trade-product");
                            if (giveawayButton != null)
                            {
                                p.IsSetForGiveaway = giveawayButton.HasClassName("active");
                                if (p.IsSetForGiveaway)
                                    p.GiveawayId = int.Parse(giveawayButton.GetAttribute("data-id"));
                                else
                                    p.GiveawayId = null;
                            }
                            else
                            {
                                p.IsSetForGiveaway = false;
                                p.GiveawayId = null;
                            }
                            if (tradeButton != null)
                            {
                                p.IsSetForTrade = tradeButton.HasClassName("active");
                                if (p.IsSetForTrade)
                                    p.TradeId = int.Parse(tradeButton.GetAttribute("data-id"));
                                else
                                    p.TradeId = null;
                            }
                            else
                            {
                                p.IsSetForTrade = false;
                                p.TradeId = null;
                            }
                        }
                    }
                    else
                    {
                        p.IsRevealed = true;
                    }
                    p.ProductInfoHtml = nDetails.GetSingleOrDefaultByClassName("product-info")?.InnerHtml;

                    if (p.IsRevealed)
                    {
                        var nDownloadsList = nDetails.QuerySelectorAll(":scope > .download-container");
                        IElement nDownloads = null;
                        if (nDownloadsList.Length > 1)
                        {
                            Console.WriteLine($"Product {p.ProductName} has multiple download containers!");
                        }
                        else if (nDownloadsList.Length == 1)
                        {
                            nDownloads = nDownloadsList[0];
                        }

                        if (nDownloads != null && nDownloads.InnerHtml != "\n" && nDownloads.InnerHtml.Length != 0)
                        {
                            // Keys
                            var nKeys = nDetails.QuerySelector("ul.keys");
                            if (nKeys != null)
                            {
                                foreach (var nKey in nKeys.QuerySelectorAll(":scope > .key"))
                                {
                                    Key k = new Key();
                                    k.Id = int.Parse(nKey.GetAttribute("data-id"));
                                    k.PlatformName = nKey.QuerySelector(":scope > strong").TextContent;
                                    k.IsThirdParty = nKey.InnerHtml.Contains("(3rd Party Key)");

                                    var nKeyMeta = nKey.GetSingleByClassName("key-meta");
                                    var nKeyUsed = nKeyMeta.QuerySelector(".usage");
                                    if (nKeyUsed != null)
                                    {
                                        k.IsUsed = nKeyUsed.HasAttribute("checked");
                                        k.IsTradedOut = false;
                                    }
                                    else
                                    {
                                        string text = nKeyMeta.TextContent;
                                        if (text.Contains("Traded out"))
                                            k.IsTradedOut = true;
                                        else if (text.Contains("Potentially not revealed"))
                                            k.IsPotentiallyNotRevealed = true;
                                        else if (text.Contains("Redeemed"))
                                            k.IsUsed = true;
                                        else if (text.Contains("Givenaway"))
                                            k.IsGiveawayed = true;
                                        else if (!text.Contains("Not revealed") && !text.Contains("Set for trade") && !text.Contains("Available in Giveaways"))
                                            throw new ParsingException($"Key meta contains unknown text: {text}", nKey.OuterHtml);
                                    }

                                    var nKeyUnrevealedGroup = nKey.GetSingleOrDefaultByClassName("unrevealed-group");
                                    if (nKeyUnrevealedGroup != null)
                                    {
                                        var nKeyUnrevealedButtons = nKeyUnrevealedGroup.GetSingleByClassName("input-group-btn");
                                        var giveawayButton = nKeyUnrevealedButtons.GetSingleOrDefaultByClassName("giveaway");
                                        var tradeButton = nKeyUnrevealedButtons.GetSingleOrDefaultByClassName("trades");
                                        if (giveawayButton != null)
                                        {
                                            k.IsSetForGiveaway = giveawayButton.HasClassName("active");
                                            if (k.IsSetForGiveaway)
                                                k.GiveawayId = int.Parse(giveawayButton.GetAttribute("data-id"));
                                            else
                                                k.GiveawayId = null;
                                        }
                                        else
                                        {
                                            k.IsSetForGiveaway = false;
                                            k.GiveawayId = null;
                                        }
                                        if (tradeButton != null)
                                        {
                                            k.IsSetForTrade = tradeButton.HasClassName("active");
                                            if (k.IsSetForTrade)
                                                k.TradeId = int.Parse(tradeButton.GetAttribute("data-id"));
                                            else
                                                k.TradeId = null;
                                        }
                                        else
                                        {
                                            k.IsSetForTrade = false;
                                            k.TradeId = null;
                                        }
                                    }
                                    else
                                    {
                                        k.IsRevealed = true;
                                        var nKeyRevealedGroup = nKey.GetSingleOrDefaultByClassName("revealed-group");
                                        k.Code = nKeyRevealedGroup.QuerySelector(".code").GetAttribute("value");
                                    }

                                    p.Keys.Add(k);
                                }
                            }

                            // Downloads
                            // Comics/other products
                            var nDownloadables = nDownloads.QuerySelectorAll(".row > .col-sm-3");
                            foreach (var nDownloadable in nDownloadables)
                            {
                                if (nDownloadable.InnerHtml == "\n" || nDownloadable.InnerHtml.Length == 0) continue;
                                var dp = new DownloadableProduct();
                                dp.Name = nDownloadable.QuerySelector(":scope > h3").TextContent;
                                dp.Files = ParseFiles(nDownloadable);
                                //if (dp.Files.Count == 0) System.Diagnostics.Debugger.Break();
                                p.Downloads.Add(dp);
                            }
                            // Games/music
                            nDownloadables = nDownloads.QuerySelectorAll(".row > .col-sm-2");
                            foreach (var nDownloadable in nDownloadables)
                            {
                                if (nDownloadable.InnerHtml == "\n" || nDownloadable.InnerHtml.Length == 0) continue;
                                var dp = new DownloadableProduct();
                                dp.Files = ParseFiles(nDownloadable);
                                //if (dp.Files.Count == 0) System.Diagnostics.Debugger.Break();
                                p.Downloads.Add(dp);
                            }

                            // Tracks
                            var nTracks = nDownloads.QuerySelector(".track-list");
                            if (nTracks != null)
                            {
                                foreach (var nTrack in nTracks.Children)
                                {
                                    var t = new Track();
                                    var nFav = nTrack.GetSingleByClassName("favorite-star");
                                    t.Id = int.Parse(nFav.GetAttribute("data-track-id"));
                                    t.IsFavorite = nDetails.GetSingleByClassName("favorite-star").HasClassName("favorite");
                                    t.Name = nTrack.QuerySelector(":scope > span").TextContent;
                                    p.Tracks.Add(t);
                                }
                            }
                        }
                    }
                }

                products.Add(p);
            }

            return new Tuple<List<Product>, string>(products, announcements);
        }

        List<DownloadFile> ParseFiles(IElement parent)
        {
            List<DownloadFile> files = new List<DownloadFile>();
            foreach (var link in parent.QuerySelectorAll(":scope > .btn-group > .dropdown-menu > li > a"))
            {
                if (link.TextContent == "Direct link")
                {
                    DownloadFile file = new DownloadFile();
                    file.PlatformName = link.GetAttribute("data-platform");
                    file.Url = link.GetAttribute("href");
                    files.Add(file);
                }
            }

            return files;
        }
    }
}
