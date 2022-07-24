using GroupeesDownload.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace GroupeesDownload
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int userId = 0;
            string cookie = "";
            string csrfToken = "";
            Client client = new Client(userId, cookie, csrfToken);
            Scraper scraper = new Scraper(client);

            JsonSerializerOptions serOpts = new JsonSerializerOptions { WriteIndented = true };
            //var bundles = await scraper.GetAllBundles();
            //string json = JsonSerializer.Serialize(bundles, serOpts);
            //File.WriteAllText("bundles.json", json);

            string json = File.ReadAllText("bundles.json");
            var bundles = JsonSerializer.Deserialize<List<Bundle>>(json, serOpts);
            json = File.ReadAllText("trades.json");
            var tradeProducts = JsonSerializer.Deserialize<List<Product>>(json, serOpts);

            AccountManagement accManage = new AccountManagement(client, scraper, bundles);
            var downloadsList = accManage.GenerateDownloadsList();
            foreach (var product in tradeProducts)
            {
                downloadsList.AddRange(accManage.GenerateDownloadsListForProduct(product));
            }
            File.WriteAllLines("downloads_list.txt", downloadsList);

            //json = JsonSerializer.Serialize(accManage.Bundles, serOpts);
            //File.WriteAllText("bundles.json", json);

            //var tradeProducts = await scraper.GetAllTradesCompletedProducts();
            //string json = JsonSerializer.Serialize(tradeProducts, serOpts);
            //File.WriteAllText("trades.json", json);
            ;
        }
    }
}
