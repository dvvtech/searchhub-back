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

            var isExcluded = site.ExcludedPaths.Any(p => url.Equals(p, StringComparison.OrdinalIgnoreCase));

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

                if (!isExcluded)
                {
                    var (title, content) = ExtractPageData(doc, site.Id);

                    pages.Add(new CrawledPage
                    {
                        Url = url,
                        Title = title,
                        Content = content,
                        SiteId = site.Id
                    });
                }

                var links = doc.DocumentNode.SelectNodes("//a[@href]");
                if (links is not null)
                {
                    foreach (var link in links)
                    {
                        var href = link.GetAttributeValue("href", string.Empty);
                        if (string.IsNullOrWhiteSpace(href))
                            continue;

                        var pageUri = new Uri(url);
                        if (Uri.TryCreate(pageUri, href, out var resolvedUri)
                            && resolvedUri.Host == baseUri.Host
                            && (resolvedUri.Scheme == "http" || resolvedUri.Scheme == "https")
                            && !visited.Contains(resolvedUri.AbsoluteUri)
                            && IsHtmlPath(resolvedUri.AbsolutePath))
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

    private static readonly string[] _removeTags = ["nav", "header", "footer", "aside", "noscript", "script", "style", "a"];
    private static readonly string[] _removeIdOrClassPatterns = ["menu", "nav", "header", "footer", "sidebar", "banner", "breadcrumb"];

    private static (string title, string content) ExtractPageData(HtmlDocument doc, int siteId)
    {
        if (siteId is 1 or 2)
            return ExtractFromContentDiv(doc, siteId);

        var body = doc.DocumentNode.SelectSingleNode("//body");
        if (body is null)
            return (string.Empty, string.Empty);

        var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim() ?? string.Empty;
        return (System.Net.WebUtility.HtmlDecode(title), ExtractText(body));
    }

    private static (string title, string content) ExtractFromContentDiv(HtmlDocument doc, int siteId)
    {
        var contentDiv = doc.DocumentNode.SelectSingleNode("//div[@id='content']");
        if (contentDiv is null)
            return (string.Empty, string.Empty);

        var headingNode = siteId == 2
            ? contentDiv.SelectSingleNode(".//h2") ?? contentDiv.SelectSingleNode(".//h3")
            : contentDiv.SelectSingleNode(".//h1");

        var title = headingNode is not null ? CleanText(headingNode.InnerText) : string.Empty;
        headingNode?.Remove();

        return (title, ExtractText(contentDiv));
    }

    private static string ExtractText(HtmlNode root)
    {
        var toRemove = new List<HtmlNode>();

        foreach (var tag in _removeTags)
        {
            foreach (var node in root.SelectNodes($".//{tag}") ?? Enumerable.Empty<HtmlNode>())
                toRemove.Add(node);
        }

        foreach (var pattern in _removeIdOrClassPatterns)
        {
            var xpath = $".//*[contains(translate(@id,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'{pattern}') or contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'{pattern}')]";
            foreach (var node in root.SelectNodes(xpath) ?? Enumerable.Empty<HtmlNode>())
                toRemove.Add(node);
        }

        foreach (var node in toRemove.Distinct())
            node.Remove();

        return CleanText(root.InnerText);
    }

    private static string CleanText(string text)
    {
        var decoded = System.Net.WebUtility.HtmlDecode(text);
        return string.Join(' ', decoded.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool IsHtmlPath(string path)
    {
        var ext = Path.GetExtension(path.AsSpan());
        return ext.IsEmpty || ext.Equals(".html", StringComparison.OrdinalIgnoreCase) || ext.Equals(".htm", StringComparison.OrdinalIgnoreCase);
    }
}
