using SearchHub.Api.Configuration;
using SearchHub.Api.Services;

namespace Search.Api.Test;

public class CrawlerDebugTests
{
    [Fact]
    public async Task CrawlRealSite_DebugTitleAndContent()
    {
        //var site = new SiteConfiguration
        //{
        //    Id = 1,
        //    Name = "Седьмой ключ",
        //    Url = "https://seventhkey.ru/news.html",
        //    FileName = "seveventhkey.bin",
        //    ExcludedPaths = new string[] {
        //            "https://seventhkey.ru/news",
        //            "https://seventhkey.ru/contacts.html"
        //        }
        //};
        var site = new SiteConfiguration
        {
            Id = 1,
            Name = "Яшель",
            Url = "https://yashel.center",
            FileName = "seveventhkey.bin",
            ExcludedPaths = new string[] {
                    "https://seventhkey.ru/news",
                    "https://seventhkey.ru/contacts.html"
                }
        };

        var httpClient = new HttpClient();
        var crawler = new CrawlerService(httpClient);
        var pages = await crawler.CrawlSiteAsync(site);

        foreach (var page in pages)
        {
            Console.WriteLine($"URL: {page.Url}");
            Console.WriteLine($"Title: {page.Title}");
            Console.WriteLine($"Content (first 200): {(page.Content.Length > 200 ? page.Content[..200] : page.Content)}");
            Console.WriteLine(new string('-', 80));
        }
    }
}
