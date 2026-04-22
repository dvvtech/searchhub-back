namespace SearchHub.Api.Models;

public class SearchResponse
{
    public List<SearchResult> Results { get; init; } = [];
    public int Total { get; init; }
}
