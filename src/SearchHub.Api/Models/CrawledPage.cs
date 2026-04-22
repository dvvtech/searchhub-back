namespace SearchHub.Api.Models;

public class CrawledPage
{
    public string Url { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public int SiteId { get; init; }
}
