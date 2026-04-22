using SearchHub.Api.Configuration;
using SearchHub.Api.Models;

namespace SearchHub.Api.Services;

public interface IIndexingService
{
    Task ReindexAllAsync(CancellationToken ct = default);
    Task ReindexSiteAsync(int siteId, CancellationToken ct = default);
    IndexStatus GetStatus();
    bool IsRunning { get; }
    event EventHandler<string>? SiteIndexed;
}

public class IndexingService : IIndexingService
{
    private readonly ICrawlerService _crawler;
    private readonly ILuceneIndexService _luceneIndex;
    private readonly IDataSerializer _dataSerializer;
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
        IDataSerializer dataSerializer,
        SearchHubConfiguration config,
        ILogger<IndexingService> logger)
    {
        _crawler = crawler;
        _luceneIndex = luceneIndex;
        _dataSerializer = dataSerializer;
        _config = config;
        _logger = logger;
    }

    public async Task ReindexAllAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_isRunning) return;
            _isRunning = true;
        }

        try
        {
            _luceneIndex.ClearIndex();
            _pagesIndexed = 0;

            foreach (var site in _config.Sites)
            {
                ct.ThrowIfCancellationRequested();
                await ProcessSiteAsync(site, forceCrawl: false, ct);
            }
        }
        finally
        {
            _currentSite = string.Empty;
            _isRunning = false;
        }
    }

    public async Task ReindexSiteAsync(int siteId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_isRunning) return;
            _isRunning = true;
        }

        try
        {
            var site = _config.Sites.FirstOrDefault(s => s.Id == siteId);
            if (site is null)
            {
                _logger.LogWarning("Site with id {SiteId} not found in configuration", siteId);
                return;
            }

            _pagesIndexed = 0;
            await ProcessSiteAsync(site, forceCrawl: true, ct);
        }
        finally
        {
            _currentSite = string.Empty;
            _isRunning = false;
        }
    }

    private async Task ProcessSiteAsync(SiteConfiguration site, bool forceCrawl, CancellationToken ct)
    {
        _currentSite = site.Name;
        _logger.LogInformation("Processing site: {SiteName} ({Url})", site.Name, site.Url);

        List<CrawledPage> pages;

        if (forceCrawl)
        {
            pages = await _crawler.CrawlSiteAsync(site, ct);
        }
        else
        {
            var cached = _dataSerializer.TryLoad(site);
            if (cached is not null)
            {
                pages = cached;
            }
            else
            {
                pages = await _crawler.CrawlSiteAsync(site, ct);
                _dataSerializer.Save(site, pages);
            }
        }

        if (forceCrawl)
        {
            _dataSerializer.Save(site, pages);
        }

        _luceneIndex.IndexPages(pages);

        Interlocked.Add(ref _pagesIndexed, pages.Count);
        _logger.LogInformation("Indexed {Count} pages from {SiteName}", pages.Count, site.Name);

        SiteIndexed?.Invoke(this, site.Name);
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
