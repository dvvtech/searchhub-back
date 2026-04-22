using ProtoBuf;
using SearchHub.Api.Configuration;
using SearchHub.Api.Models;

namespace SearchHub.Api.Services;

public interface IDataSerializer
{
    List<CrawledPage>? TryLoad(SiteConfiguration site);
    void Save(SiteConfiguration site, List<CrawledPage> pages);
}

public class ProtobufDataSerializer : IDataSerializer
{
    private readonly string _dataDirectory;
    private readonly ILogger<ProtobufDataSerializer> _logger;

    public ProtobufDataSerializer(ILogger<ProtobufDataSerializer> logger)
    {
        _logger = logger;
        _dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(_dataDirectory);
    }

    public List<CrawledPage>? TryLoad(SiteConfiguration site)
    {
        var filePath = Path.Combine(_dataDirectory, site.FileName);

        if (!File.Exists(filePath))
            return null;

        try
        {
            using var file = File.OpenRead(filePath);
            var pages = Serializer.Deserialize<List<CrawledPage>>(file);
            _logger.LogInformation("Loaded {Count} pages from cache for {SiteName}", pages.Count, site.Name);
            return pages;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load cache for {SiteName}, will re-crawl", site.Name);
            return null;
        }
    }

    public void Save(SiteConfiguration site, List<CrawledPage> pages)
    {
        var filePath = Path.Combine(_dataDirectory, site.FileName);

        try
        {
            using var file = File.Create(filePath);
            Serializer.Serialize(file, pages);
            _logger.LogInformation("Saved {Count} pages to cache for {SiteName}", pages.Count, site.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save cache for {SiteName}", site.Name);
        }
    }
}
