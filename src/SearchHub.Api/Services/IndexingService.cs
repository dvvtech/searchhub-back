using SearchHub.Api.Configuration;
using SearchHub.Api.Models;

namespace SearchHub.Api.Services;

public interface IIndexingService
{
    Task ReindexAllAsync(CancellationToken ct = default);
    IndexStatus GetStatus();
    bool IsRunning { get; }
    event EventHandler<string>? SiteIndexed;
}

public class IndexingService : IIndexingService
{
    private readonly ICrawlerService _crawler;
    private readonly ILuceneIndexService _luceneIndex;
    private readonly SearchHubConfiguration _config;
    private readonly ILogger<IndexingService> _logger;
    private volatile bool _isRunning;
    private int _pagesIndexed;
    private string _currentSite = string.Empty;
    private readonly Lock _lock = new();

    public bool IsRunning => _isRunning;

    public event EventHandler<string>? SiteIndexed;

    public IndexingService(
        ICrawlerService crawler,
        ILuceneIndexService luceneIndex,
        SearchHubConfiguration config,
        ILogger<IndexingService> logger)
    {
        _crawler = crawler;
        _luceneIndex = luceneIndex;
        _config = config;
        _logger = logger;
    }

    public async Task ReindexAllAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_isRunning)
                return;
            _isRunning = true;
        }

        try
        {
            _luceneIndex.ClearIndex();
            _pagesIndexed = 0;

            foreach (var site in _config.Sites)
            {
                ct.ThrowIfCancellationRequested();
                _currentSite = site.Name;
                _logger.LogInformation("Indexing site: {SiteName} ({Url})", site.Name, site.Url);

                var pages = await _crawler.CrawlSiteAsync(site, ct);
                _luceneIndex.IndexPages(pages);

                Interlocked.Add(ref _pagesIndexed, pages.Count);
                _logger.LogInformation("Indexed {Count} pages from {SiteName}", pages.Count, site.Name);

                SiteIndexed?.Invoke(this, site.Name);
            }
        }
        finally
        {
            _currentSite = string.Empty;
            _isRunning = false;
        }
    }

    public IndexStatus GetStatus()
    {
        return new IndexStatus
        {
            IsRunning = _isRunning,
            PagesIndexed = _pagesIndexed,
            CurrentSite = _currentSite
        };
    }
}
