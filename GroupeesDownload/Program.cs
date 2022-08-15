using GroupeesDownload.Models;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GroupeesDownload
{
    class Program
    {
        const string DEFAULT_BUNDLES_DB_NAME = "bundles.json";
        const string DEFAULT_TRADES_DB_NAME = "trades.json";
        const string DEFAULT_TPK_DB_NAME = "third_party_keys.json";

        static Client client;
        static Scraper scraper;
        static JsonSerializerOptions serOpts = new JsonSerializerOptions { WriteIndented = true };

        static async Task<int> Main(string[] args)
        {
            var userIdOption = new Option<int>("--user-id", "The user ID.");
            var cookieOption = new Option<string>("--cookie", "The value of the _groupees_session cookie.");
            var csrfTokenOption = new Option<string>("--csrf-token", "The CSRF token value.");
            var bundlesDbOption = new Option<FileInfo>("--bundles-db", () => new FileInfo(DEFAULT_BUNDLES_DB_NAME), "Path to bundle DB.");
            var tradesDbOption = new Option<FileInfo>("--trades-db", () => new FileInfo(DEFAULT_TRADES_DB_NAME), "Path to trades DB.");
            var tpkDbOption = new Option<FileInfo>("--tpk-db", () => new FileInfo(DEFAULT_TPK_DB_NAME), "Path to third party keys DB.");
            var allOption = new Option<bool>("--all", "Apply action to all items of this type.");
            var noCoversOption = new Option<bool>("--no-covers", "Do not include covers in list.");
            var outputOption = new Option<FileInfo>("--output", "Path to output list to.");
            var organizeOption = new Option<DirOrganizationType>("--organize", "Organize downloads into folders (for use with aria2).");
            var includeAllOption = new Option<bool>("--include-all", "Also include MP3s even if FLAC is available when downloading music.");
            var filterGamesOption = new Option<bool>("--filter-games", "Filter downloads to games.");
            var filterMusicOption = new Option<bool>("--filter-music", "Filter downloads to music.");
            var filterOthersOption = new Option<bool>("--filter-others", "Filter downloads to other products (e.g. comics).");
            var dedupeOption = new Option<bool>("--dedupe", "Deduplicate download links based on URL.");
            var forTpkOption = new Option<bool>("--for-tpk", "Operate on third party keys instead of bundles and trades.");
            var idsArgument = new Argument<int[]>("ids", "IDs of items to act upon.");

            var rootCommand = new RootCommand("Groupees Scraper");

            var dumpBundlesCommand = new Command("dump-bundles", "Dump all bundles.")
            {
                userIdOption,
                cookieOption,
                csrfTokenOption,
                bundlesDbOption,
            };
            rootCommand.AddCommand(dumpBundlesCommand);

            var dumpTradesCommand = new Command("dump-trades", "Dump all trade products.")
            {
                userIdOption,
                cookieOption,
                csrfTokenOption,
                tradesDbOption,
            };
            rootCommand.AddCommand(dumpTradesCommand);

            var unmarkTradesCommand = new Command("unmark-trades", "Remove products and keys from trades.")
            {
                userIdOption,
                cookieOption,
                csrfTokenOption,
                bundlesDbOption,
                tradesDbOption,
                tpkDbOption,
                allOption,
                forTpkOption,
                idsArgument,
            };
            rootCommand.AddCommand(unmarkTradesCommand);

            var unmarkGiveawaysCommand = new Command("unmark-giveaway", "Remove products and keys from giveaways.")
            {
                userIdOption,
                cookieOption,
                csrfTokenOption,
                bundlesDbOption,
                tradesDbOption,
                tpkDbOption,
                allOption,
                forTpkOption,
                idsArgument,
            };
            rootCommand.AddCommand(unmarkGiveawaysCommand);

            var revealProductsCommand = new Command("reveal-products", "Reveals products.")
            {
                userIdOption,
                cookieOption,
                csrfTokenOption,
                bundlesDbOption,
                tradesDbOption,
                tpkDbOption,
                allOption,
                forTpkOption,
                idsArgument,
            };
            rootCommand.Add(revealProductsCommand);

            var revealKeysCommand = new Command("reveal-keys", "Reveal keys.")
            {
                userIdOption,
                cookieOption,
                csrfTokenOption,
                bundlesDbOption,
                tradesDbOption,
                tpkDbOption,
                allOption,
                forTpkOption,
                idsArgument,
            };
            rootCommand.Add(revealKeysCommand);

            var generateLinksCommand = new Command("generate-links", "Generates download links list.")
            {
                bundlesDbOption,
                tradesDbOption,
                noCoversOption,
                outputOption,
                organizeOption,
                includeAllOption,
                filterMusicOption,
                filterGamesOption,
                filterOthersOption,
                dedupeOption,
            };
            rootCommand.Add(generateLinksCommand);

            var exportKeysCommand = new Command("export-keys", "Generates CSV list of keys.")
            {
                bundlesDbOption,
                tradesDbOption,
                tpkDbOption,
                outputOption,
            };
            rootCommand.Add(exportKeysCommand);

            var dumpThirdPartyKeysCommand = new Command("dump-third-party-keys", "Dumps all third party keys added by yourself.")
            {
                userIdOption,
                cookieOption,
                csrfTokenOption,
                bundlesDbOption,
                tradesDbOption,
                tpkDbOption,
            };
            rootCommand.Add(dumpThirdPartyKeysCommand);

            dumpBundlesCommand.SetHandler(async (userId, cookie, csrfToken, bundlesDb) =>
            {
                await InitClient(userId, cookie, csrfToken);
                var bundles = await scraper.GetAllBundles();
                SaveBundles(bundlesDb, bundles);
            }, userIdOption, cookieOption, csrfTokenOption, bundlesDbOption);

            dumpTradesCommand.SetHandler(async (userId, cookie, csrfToken, tradesDb) =>
            {
                await InitClient(userId, cookie, csrfToken);
                var tradeProducts = await scraper.GetAllTradesCompletedProducts();
                SaveProducts(tradesDb, tradeProducts);
            }, userIdOption, cookieOption, csrfTokenOption, tradesDbOption);

            unmarkTradesCommand.SetHandler(async (context) =>
            {
                var userId = context.ParseResult.GetValueForOption(userIdOption);
                var cookie = context.ParseResult.GetValueForOption(cookieOption);
                var csrfToken = context.ParseResult.GetValueForOption(csrfTokenOption);
                var bundlesDb = context.ParseResult.GetValueForOption(bundlesDbOption);
                var tradesDb = context.ParseResult.GetValueForOption(tradesDbOption);
                var tpkDb = context.ParseResult.GetValueForOption(tpkDbOption);
                var isAll = context.ParseResult.GetValueForOption(allOption);
                var forTpk = context.ParseResult.GetValueForOption(forTpkOption);
                var ids = context.ParseResult.GetValueForArgument(idsArgument);

                await InitClient(userId, cookie, csrfToken);

                var bundles = LoadBundles(bundlesDb);
                var tradeProducts = LoadProducts(tradesDb);
                var tpk = LoadProducts(tpkDb);
                AccountManagement accManage = new AccountManagement(client, scraper, bundles, tradeProducts, tpk);
                if (isAll)
                {
                    await accManage.UnsetTradeAllProducts(forTpk);
                }
                else
                {
                    await accManage.UnsetTradesById(ids);
                }
                if (accManage.Bundles != null) SaveBundles(bundlesDb, accManage.Bundles);
                if (accManage.TradeProducts != null) SaveProducts(tradesDb, accManage.TradeProducts);
                if (accManage.ThirdPartyKeys != null) SaveProducts(tpkDb, accManage.ThirdPartyKeys);
            });

            unmarkGiveawaysCommand.SetHandler(async (context) =>
            {
                var userId = context.ParseResult.GetValueForOption(userIdOption);
                var cookie = context.ParseResult.GetValueForOption(cookieOption);
                var csrfToken = context.ParseResult.GetValueForOption(csrfTokenOption);
                var bundlesDb = context.ParseResult.GetValueForOption(bundlesDbOption);
                var tradesDb = context.ParseResult.GetValueForOption(tradesDbOption);
                var tpkDb = context.ParseResult.GetValueForOption(tpkDbOption);
                var isAll = context.ParseResult.GetValueForOption(allOption);
                var forTpk = context.ParseResult.GetValueForOption(forTpkOption);
                var ids = context.ParseResult.GetValueForArgument(idsArgument);

                await InitClient(userId, cookie, csrfToken);

                var bundles = LoadBundles(bundlesDb);
                var tradeProducts = LoadProducts(tradesDb);
                var tpk = LoadProducts(tpkDb);
                AccountManagement accManage = new AccountManagement(client, scraper, bundles, tradeProducts, tpk);
                if (isAll)
                {
                    await accManage.UnsetGiveawayAllProducts(forTpk);
                }
                else
                {
                    await accManage.UnsetGiveawaysById(ids);
                }
                if (accManage.Bundles != null) SaveBundles(bundlesDb, accManage.Bundles);
                if (accManage.TradeProducts != null) SaveProducts(tradesDb, accManage.TradeProducts);
                if (accManage.ThirdPartyKeys != null) SaveProducts(tpkDb, accManage.ThirdPartyKeys);
            });

            revealProductsCommand.SetHandler(async (context) =>
            {
                var userId = context.ParseResult.GetValueForOption(userIdOption);
                var cookie = context.ParseResult.GetValueForOption(cookieOption);
                var csrfToken = context.ParseResult.GetValueForOption(csrfTokenOption);
                var bundlesDb = context.ParseResult.GetValueForOption(bundlesDbOption);
                var tradesDb = context.ParseResult.GetValueForOption(tradesDbOption);
                var tpkDb = context.ParseResult.GetValueForOption(tpkDbOption);
                var isAll = context.ParseResult.GetValueForOption(allOption);
                var forTpk = context.ParseResult.GetValueForOption(forTpkOption);
                var ids = context.ParseResult.GetValueForArgument(idsArgument);

                await InitClient(userId, cookie, csrfToken);

                var bundles = LoadBundles(bundlesDb);
                var tradeProducts = LoadProducts(tradesDb);
                var tpk = LoadProducts(tpkDb);
                AccountManagement accManage = new AccountManagement(client, scraper, bundles, tradeProducts, tpk);
                if (isAll)
                {
                    await accManage.RevealAllProducts(forTpk);
                }
                else
                {
                    await accManage.UnsetGiveawaysById(ids);
                }
                if (accManage.Bundles != null) SaveBundles(bundlesDb, accManage.Bundles);
                if (accManage.TradeProducts != null) SaveProducts(tradesDb, accManage.TradeProducts);
                if (accManage.ThirdPartyKeys != null) SaveProducts(tpkDb, accManage.ThirdPartyKeys);
            });

            revealKeysCommand.SetHandler(async (context) =>
            {
                var userId = context.ParseResult.GetValueForOption(userIdOption);
                var cookie = context.ParseResult.GetValueForOption(cookieOption);
                var csrfToken = context.ParseResult.GetValueForOption(csrfTokenOption);
                var bundlesDb = context.ParseResult.GetValueForOption(bundlesDbOption);
                var tradesDb = context.ParseResult.GetValueForOption(tradesDbOption);
                var tpkDb = context.ParseResult.GetValueForOption(tpkDbOption);
                var isAll = context.ParseResult.GetValueForOption(allOption);
                var forTpk = context.ParseResult.GetValueForOption(forTpkOption);
                var ids = context.ParseResult.GetValueForArgument(idsArgument);

                await InitClient(userId, cookie, csrfToken);

                var bundles = LoadBundles(bundlesDb);
                var tradeProducts = LoadProducts(tradesDb);
                var tpk = LoadProducts(tpkDb);
                AccountManagement accManage = new AccountManagement(client, scraper, bundles, tradeProducts, tpk);
                if (isAll)
                {
                    await accManage.RevealAllKeys(forTpk);
                }
                else
                {
                    await accManage.RevealKeysById(ids);
                }
                if (accManage.Bundles != null) SaveBundles(bundlesDb, accManage.Bundles);
                if (accManage.TradeProducts != null) SaveProducts(tradesDb, accManage.TradeProducts);
                if (accManage.ThirdPartyKeys != null) SaveProducts(tpkDb, accManage.ThirdPartyKeys);
            });

            generateLinksCommand.SetHandler(context =>
            {
                var bundlesDb = context.ParseResult.GetValueForOption(bundlesDbOption);
                var tradesDb = context.ParseResult.GetValueForOption(tradesDbOption);
                var noCovers = context.ParseResult.GetValueForOption(noCoversOption);
                var output = context.ParseResult.GetValueForOption(outputOption);
                var dirOrgType = context.ParseResult.GetValueForOption(organizeOption);
                var includeAll = context.ParseResult.GetValueForOption(includeAllOption);
                var filterGames = context.ParseResult.GetValueForOption(filterGamesOption);
                var filterMusic = context.ParseResult.GetValueForOption(filterMusicOption);
                var filterOthers = context.ParseResult.GetValueForOption(filterOthersOption);
                var dedupe = context.ParseResult.GetValueForOption(dedupeOption);

                var bundles = LoadBundles(bundlesDb);
                var tradeProducts = LoadProducts(tradesDb);
                AccountManagement accManage = new AccountManagement(client, scraper, bundles, tradeProducts, null);

                DownloadFilterTypes filter = DownloadFilterTypes.None;
                if (filterGames || filterMusic || filterOthers)
                {
                    if (filterGames) filter |= DownloadFilterTypes.Games;
                    if (filterMusic) filter |= DownloadFilterTypes.Music;
                    if (filterOthers) filter |= DownloadFilterTypes.Others;
                }
                else
                {
                    filter = DownloadFilterTypes.All;
                }
                if (includeAll) filter |= DownloadFilterTypes.MusicDownloadAll;

                var downloadsList = accManage.GenerateDownloadsList(!noCovers, dirOrgType, filter, dedupe);

                if (output == null) output = new FileInfo("downloads_list.txt");
                File.WriteAllLines(output.FullName, downloadsList);
            });

            exportKeysCommand.SetHandler((bundlesDb, tradesDb, tpkDb, output) =>
            {
                var bundles = LoadBundles(bundlesDb);
                var tradeProducts = LoadProducts(tradesDb);
                var tpk = LoadProducts(tpkDb);
                AccountManagement accManage = new AccountManagement(client, scraper, bundles, tradeProducts, tpk);

                var export = accManage.ExportAllKeys();

                if (output == null) output = new FileInfo("keys.csv");
                File.WriteAllLines(output.FullName, export);
            }, bundlesDbOption, tradesDbOption, tpkDbOption, outputOption);

            dumpThirdPartyKeysCommand.SetHandler(async (userId, cookie, csrfToken, bundlesDb, tradesDb, tpkDb) =>
            {
                await InitClient(userId, cookie, csrfToken);
                var bundles = LoadBundles(bundlesDb);
                var trades = LoadProducts(tradesDb);
                List<int> excludedIds = new List<int>();
                if (bundles != null) excludedIds.AddRange(bundles.SelectMany(x => x.Products).Select(x => x.Id));
                if (trades != null) excludedIds.AddRange(trades.Select(x => x.Id));
                var products = await scraper.GetAllThirdPartyKeys(excludedIds);
                SaveProducts(tpkDb, products);
            }, userIdOption, cookieOption, csrfTokenOption, bundlesDbOption, tradesDbOption, tpkDbOption);

            // ================================================================

            //var testCommand = new Command("test", "Debug functionality.")
            //{
            //    bundlesDbOption,
            //};
            //rootCommand.AddCommand(testCommand);

            //testCommand.SetHandler(bundlesDb =>
            //{
            //    var bundles = LoadBundles(bundlesDb);
            //    HashSet<string> platformNames = new HashSet<string>();
            //    foreach (var bundle in bundles)
            //    {
            //        foreach (var product in bundle.Products)
            //        {
            //            foreach (var downloadable in product.Downloads)
            //            {
            //                foreach (var file in downloadable.Files)
            //                {
            //                    platformNames.Add(file.PlatformName);
            //                }
            //            }
            //        }
            //    }

            //    foreach (var name in platformNames)
            //    {
            //        Console.WriteLine(name);
            //    }
            //}, bundlesDbOption);

            // ================================================================

            // Override default exception handling, see https://github.com/dotnet/command-line-api/issues/796#issuecomment-999734612
            var parser = new CommandLineBuilder(rootCommand).UseDefaults().UseExceptionHandler((e, context) =>
                {
                    if (e is AggregateException aggEx)
                    {
                        aggEx.Handle(inner =>
                        {
                            if (inner is ParsingException parseEx)
                            {
                                Console.WriteLine($"A parsing error occurred: {parseEx}");
                                Console.WriteLine();
                                Console.WriteLine("HTML causing the error:");
                                Console.WriteLine(parseEx.Html);
                            }
                            else
                            {
                                Console.WriteLine($"Something went wrong: {inner}");
                            }
                            return true;
                        });
                    }
                    else if (e is ParsingException parseEx)
                    {
                        Console.WriteLine($"A parsing error occurred: {parseEx}");
                        Console.WriteLine();
                        Console.WriteLine("HTML causing the error:");
                        Console.WriteLine(parseEx.Html);
                    }
                    else
                    {
                        Console.WriteLine($"Something went wrong: {e}");
                    }
                }, 1)
                .Build();

            return await parser.InvokeAsync(args);
        }

        static async Task InitClient(int userId, string cookie, string csrfToken)
        {
            if (string.IsNullOrEmpty(cookie)) throw new ArgumentException("No session cookie specified.", nameof(cookie));

            if (userId == 0 || string.IsNullOrEmpty(csrfToken))
            {
                client = new Client(cookie);
                scraper = new Scraper(client);
                await scraper.AutoSetTokens();
            }
            else
            {
                client = new Client(userId, cookie, csrfToken);
                scraper = new Scraper(client);
            }
        }

        static List<Bundle> LoadBundles(FileInfo bundlesDb)
        {
            string json = File.ReadAllText(bundlesDb.FullName);
            return JsonSerializer.Deserialize<List<Bundle>>(json, serOpts);
        }

        static void SaveBundles(FileInfo bundlesDb, List<Bundle> bundles)
        {
            string json = JsonSerializer.Serialize(bundles, serOpts);
            File.WriteAllText(bundlesDb.FullName, json);
        }

        static List<Product> LoadProducts(FileInfo db)
        {
            try
            {
                string json = File.ReadAllText(db.FullName);
                return JsonSerializer.Deserialize<List<Product>>(json, serOpts);
            }
            catch (FileNotFoundException)
            {
                if (db.Name != DEFAULT_TRADES_DB_NAME) throw;
                return null;
            }
        }

        static void SaveProducts(FileInfo db, List<Product> products)
        {
            string json = JsonSerializer.Serialize(products, serOpts);
            File.WriteAllText(db.FullName, json);
        }
    }
}
