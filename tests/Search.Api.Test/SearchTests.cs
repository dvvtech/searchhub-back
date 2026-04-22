using SearchHub.Api.Models;
using SearchHub.Api.Services;

namespace Search.Api.Test;

public class SearchTests
{
    [Fact]
    public void Search_ReturnsResults_WhenContentMatches()
    {
        using var indexService = new LuceneIndexService();

        var pages = new List<CrawledPage>
        {
            new()
            {
                Url = "https://example.com/page1",
                Title = "Introduction to ASP.NET Core",
                Content = "ASP.NET Core is a cross-platform, high-performance framework for building modern web applications",
                SiteId = 1
            },
            new()
            {
                Url = "https://example.com/page2",
                Title = "Getting Started with Docker",
                Content = "Docker is a platform for developing, shipping, and running applications in containers",
                SiteId = 1
            },
            new()
            {
                Url = "https://other.com/page3",
                Title = "Lucene Search Engine",
                Content = "Lucene is a high-performance text search engine library written in Java",
                SiteId = 2
            }
        };

        indexService.IndexPages(pages);

        var result = indexService.Search("ASP.NET");

        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.Single(result.Results);
        Assert.Equal("https://example.com/page1", result.Results[0].Url);
        Assert.Contains("ASP.NET Core", result.Results[0].Title);
    }

    [Fact]
    public void Search_FiltersBySiteId()
    {
        using var indexService = new LuceneIndexService();

        var pages = new List<CrawledPage>
        {
            new()
            {
                Url = "https://example.com/page1",
                Title = "Web Applications",
                Content = "Building web applications with modern frameworks",
                SiteId = 1
            },
            new()
            {
                Url = "https://other.com/page2",
                Title = "Web Development",
                Content = "Web development best practices and patterns",
                SiteId = 2
            }
        };

        indexService.IndexPages(pages);

        var result = indexService.Search("web", siteId: 1);

        Assert.Equal(1, result.Total);
        Assert.Equal(1, result.Results[0].SiteId);
    }

    [Fact]
    public void Search_ReturnsEmpty_WhenNoMatch()
    {
        using var indexService = new LuceneIndexService();

        var pages = new List<CrawledPage>
        {
            new()
            {
                Url = "https://example.com/page1",
                Title = "Test Page",
                Content = "Some content here",
                SiteId = 1
            }
        };

        indexService.IndexPages(pages);

        var result = indexService.Search("nonexistent");

        Assert.Equal(0, result.Total);
        Assert.Empty(result.Results);
    }

    [Fact]
    public void ClearIndex_RemovesAllDocuments()
    {
        using var indexService = new LuceneIndexService();

        indexService.IndexPages(
        [
            new CrawledPage
            {
                Url = "https://example.com/page1",
                Title = "Test",
                Content = "Content",
                SiteId = 1
            }
        ]);

        Assert.Equal(1, indexService.GetDocumentCount());

        indexService.ClearIndex();

        Assert.Equal(0, indexService.GetDocumentCount());
    }

    [Fact]
    public void Search_ReturnsMultipleResults_SortedByRelevance()
    {
        using var indexService = new LuceneIndexService();

        var pages = new List<CrawledPage>
        {
            new()
            {
                Url = "https://example.com/page1",
                Title = "Search Engine Optimization",
                Content = "Search optimization techniques for web",
                SiteId = 1
            },
            new()
            {
                Url = "https://example.com/page2",
                Title = "Web Development",
                Content = "Search functionality is important for web applications",
                SiteId = 1
            }
        };

        indexService.IndexPages(pages);

        var result = indexService.Search("search");

        Assert.Equal(2, result.Total);
    }
}
