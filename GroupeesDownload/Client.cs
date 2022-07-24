using GroupeesDownload.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace GroupeesDownload
{
    class Client
    {
        const string ACCEPT_JAVASCRIPT = "text/javascript";
        const string ACCEPT_HTML = "text/html";
        const string BASE_ADDR = "https://groupees.com";
        const string BASE_ADDR_TRADES = "https://trades.groupees.com";

        HttpClient client;
        int userId;

        public Client(int userId, string cookie, string csrfToken)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer.Add(new System.Net.Cookie("_groupees_session", cookie, "/", "groupees.com"));
            client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("X-CSRF-Token", csrfToken);
            this.userId = userId;
        }

        public async Task<List<Bundle>> GetBundles(int page)
        {
            return await GetMoreEntries<Bundle>("bundles", page);
        }

        public async Task<List<T>> GetMoreEntries<T>(string kind, int page)
        {
            var builder = new UriBuilder($"{BASE_ADDR}/users/{userId}/more_entries");
            var queryBuilder = HttpUtility.ParseQueryString(builder.Query);
            queryBuilder["page"] = page.ToString();
            queryBuilder["kind"] = kind;
            builder.Query = queryBuilder.ToString();
            var json = await client.GetStringAsync(builder.Uri);
            var arr = JsonSerializer.Deserialize<string[]>(json);
            List<T> deserialzied = new List<T>();
            foreach (var ser in arr)
            {
                deserialzied.Add(JsonSerializer.Deserialize<T>(ser));
            }
            return deserialzied;
        }

        public async Task<string> GetTrades(int page)
        {
            var builder = new UriBuilder($"{BASE_ADDR_TRADES}/completed");
            var queryBuilder = HttpUtility.ParseQueryString(builder.Query);
            queryBuilder["page"] = page.ToString();
            builder.Query = queryBuilder.ToString();
            using (var resp = await GetResponseWithAccept(HttpMethod.Get, builder.Uri.ToString(), ACCEPT_JAVASCRIPT, true))
            {
                return await resp.Content.ReadAsStringAsync();
            }
        }

        public async Task<string> GetOrder(int id)
        {
            var builder = new UriBuilder($"{BASE_ADDR}/orders/{id}");
            var queryBuilder = HttpUtility.ParseQueryString(builder.Query);
            queryBuilder["user_id"] = userId.ToString();
            builder.Query = queryBuilder.ToString();

            using (var resp = await GetResponseWithAccept(HttpMethod.Get, builder.Uri.ToString(), ACCEPT_JAVASCRIPT))
            {
                return await resp.Content.ReadAsStringAsync();
            }
        }

        public async Task<string> GetProfileProductDetails(int id)
        {
            var builder = new UriBuilder($"{BASE_ADDR}/profile/products/{id}");
            var queryBuilder = HttpUtility.ParseQueryString(builder.Query);
            //queryBuilder["v"] = 1.ToString();
            builder.Query = queryBuilder.ToString();

            using (var resp = await GetResponseWithAccept(HttpMethod.Get, builder.Uri.ToString(), ACCEPT_JAVASCRIPT, true))
            {
                return await resp.Content.ReadAsStringAsync();
            }
        }

        public async Task<string> GetProfileProduct(int id)
        {
            using (var resp = await GetResponseWithAccept(HttpMethod.Get, $"{BASE_ADDR}/profile/products/{id}", ACCEPT_HTML))
            {
                return await resp.Content.ReadAsStringAsync();
            }
        }

        public async Task DeleteTradeableItem(int id)
        {
            await GetResponseWithAccept(HttpMethod.Delete, $"{BASE_ADDR}/tradeable_items/{id}", ACCEPT_JAVASCRIPT);
        }

        public async Task DeleteGiveawayItem(int id)
        {
            await GetResponseWithAccept(HttpMethod.Delete, $"{BASE_ADDR}/giveaway_items/{id}", ACCEPT_JAVASCRIPT);
        }

        public async Task RevealProduct(int id)
        {
            await GetResponseWithAccept(HttpMethod.Post, $"{BASE_ADDR}/user_products/{id}/reveal", ACCEPT_JAVASCRIPT);
        }

        public async Task RevealKey(int id)
        {
            await GetResponseWithAccept(HttpMethod.Post, $"{BASE_ADDR}/activation_codes/{id}/reveal", ACCEPT_JAVASCRIPT);
        }

        async Task<HttpResponseMessage> GetResponseWithAccept(HttpMethod verb, string requestUri, string type, bool addRequestedWith = false)
        {
            var req = new HttpRequestMessage(verb, requestUri);
            req.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(type));
            if (addRequestedWith) req.Headers.Add("X-Requested-With", "XMLHttpRequest");
            var resp = await client.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            return resp;
        }
    }
}
