namespace SearchHub.Api.Models;

public class SearchResult
{
    public string Url { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Snippet { get; init; } = string.Empty;
    public int SiteId { get; init; }
}
