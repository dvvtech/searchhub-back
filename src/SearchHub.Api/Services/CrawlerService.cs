using SearchHub.Api.Configuration;
using SearchHub.Api.Models;
using HtmlAgilityPack;

namespace SearchHub.Api.Services;

public interface ICrawlerService
{
    Task<List<CrawledPage>> CrawlSiteAsync(SiteConfiguration site, CancellationToken ct = default);
}

public class CrawlerService : ICrawlerService
{
    private readonly HttpClient _httpClient;
    private const int MaxPages = 500;

    public CrawlerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<CrawledPage>> CrawlSiteAsync(SiteConfiguration site, CancellationToken ct = default)
    {
        var baseUri = new Uri(site.Url);
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        var pages = new List<CrawledPage>();

        queue.Enqueue(site.Url);

        while (queue.Count > 0 && pages.Count < MaxPages)
        {
            ct.ThrowIfCancellationRequested();

            var url = queue.Dequeue();

            if (!visited.Add(url))
                continue;

            try
            {
                var response = await _httpClient.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode)
                    continue;

                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (contentType is null || !contentType.Contains("html"))
                    continue;

                var html = await response.Content.ReadAsStringAsync(ct);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim() ?? url;
                var content = ExtractText(doc);

                pages.Add(new CrawledPage
                {
                    Url = url,
                    Title = System.Net.WebUtility.HtmlDecode(title),
                    Content = content,
                    SiteId = site.Id
                });

                var links = doc.DocumentNode.SelectNodes("//a[@href]");
                if (links is not null)
                {
                    foreach (var link in links)
                    {
                        var href = link.GetAttributeValue("href", string.Empty);
                        if (string.IsNullOrWhiteSpace(href))
                            continue;

                        if (Uri.TryCreate(baseUri, href, out var resolvedUri)
                            && resolvedUri.Host == baseUri.Host
                            && (resolvedUri.Scheme == "http" || resolvedUri.Scheme == "https")
                            && !visited.Contains(resolvedUri.AbsoluteUri))
                        {
                            queue.Enqueue(resolvedUri.AbsoluteUri);
                        }
                    }
                }
            }
            catch
            {
                // skip unreachable pages
            }
        }

        return pages;
    }

    private static string ExtractText(HtmlDocument doc)
    {
        var body = doc.DocumentNode.SelectSingleNode("//body");
        if (body is null)
            return string.Empty;

        var text = body.InnerText;
        var decoded = System.Net.WebUtility.HtmlDecode(text);
        var cleaned = string.Join(' ', decoded.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
        return cleaned;
    }
}
