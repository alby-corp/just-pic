namespace JE2Sql
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text.Json.Serialization;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class Api : IDisposable
    {
        readonly HttpClient http;
        readonly PersistedCookieContainer cookies;

        public Api(Uri uri, string storage)
        {
            cookies = new PersistedCookieContainer(storage);
            
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = cookies.Container
            };

            http = new HttpClient(handler)
            {
                BaseAddress = uri
            };
        }

        public async Task<int?> TryGetMenuId(string name)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"/restaurants-{name}", UriKind.Relative))
            {
                Headers =
                {
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.100 Safari/537.36" }
                }
            };

            var page = await http.SendAsync(request);
            if (!page.IsSuccessStatusCode)
            {
                return null;
            }

            var regex = new Regex("JustEatData.MenuId = '(.+)';");
            var id = regex.Match(await page.Content.ReadAsStringAsync()).Groups[groupnum: 1].Value;

            return int.TryParse(id, out var result)
                ? (int?) result
                : null;
        }

        class Response
        {
            public JE.Menu Menu { get; set; }
        }

        public async Task<JE.Menu> GetMenu(int id)
        {
            var stream = await http.GetStreamAsync($"/menu/getproductsformenu?menuId={id}");

            var response = await JsonSerializer.ReadAsync<Response>(stream);

            return response.Menu;
        }
        
        public void Dispose()
        {
            cookies.Save();
            http.Dispose();
        }
    }
}