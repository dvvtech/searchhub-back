using Lucene.Net.Analysis.Ru;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using SearchHub.Api.Models;

namespace SearchHub.Api.Services;

public interface ILuceneIndexService
{
    void IndexPages(List<CrawledPage> pages);
    SearchResponse Search(string query, int? siteId = null, int limit = 20);
    void ClearIndex();
    int GetDocumentCount();
}

public class LuceneIndexService : ILuceneIndexService, IDisposable
{
    private readonly RAMDirectory _directory;
    private readonly RussianAnalyzer _analyzer;
    private const LuceneVersion AppVersion = LuceneVersion.LUCENE_48;

    private const string FieldUrl = "url";
    private const string FieldTitle = "title";
    private const string FieldContent = "content";
    private const string FieldSiteId = "siteId";

    public LuceneIndexService()
    {
        _directory = new RAMDirectory();
        _analyzer = new RussianAnalyzer(AppVersion);
    }

    public void IndexPages(List<CrawledPage> pages)
    {
        var config = new IndexWriterConfig(AppVersion, _analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND
        };

        using var writer = new IndexWriter(_directory, config);

        foreach (var page in pages)
        {
            var doc = new Document
            {
                new StringField(FieldUrl, page.Url, Field.Store.YES),
                new TextField(FieldTitle, page.Title, Field.Store.YES),
                new TextField(FieldContent, page.Content, Field.Store.YES),
                new Int32Field(FieldSiteId, page.SiteId, Field.Store.YES)
            };

            writer.UpdateDocument(new Term(FieldUrl, page.Url), doc);
        }

        writer.Commit();
    }

    public SearchResponse Search(string queryText, int? siteId = null, int limit = 20)
    {
        using var reader = DirectoryReader.Open(_directory);
        var searcher = new IndexSearcher(reader);

        var booleanQuery = new BooleanQuery();

        var queryParser = new MultiFieldQueryParser(AppVersion, [FieldTitle, FieldContent], _analyzer);
        var parsed = queryParser.Parse(queryText);
        booleanQuery.Add(parsed, Occur.MUST);

        if (siteId.HasValue)
        {
            booleanQuery.Add(NumericRangeQuery.NewInt32Range(FieldSiteId, siteId.Value, siteId.Value, true, true), Occur.MUST);
        }

        var topDocs = searcher.Search(booleanQuery, limit);
        var results = new List<SearchResult>();

        foreach (var scoreDoc in topDocs.ScoreDocs)
        {
            var doc = searcher.Doc(scoreDoc.Doc);
            var content = doc.Get(FieldContent) ?? string.Empty;
            var snippet = BuildSnippet(content, queryText);

            results.Add(new SearchResult
            {
                Url = doc.Get(FieldUrl) ?? string.Empty,
                Title = doc.Get(FieldTitle) ?? string.Empty,
                Snippet = snippet,
                SiteId = int.TryParse(doc.Get(FieldSiteId), out var sid) ? sid : 0
            });
        }

        return new SearchResponse
        {
            Results = results,
            Total = topDocs.TotalHits
        };
    }

    public void ClearIndex()
    {
        var config = new IndexWriterConfig(AppVersion, _analyzer)
        {
            OpenMode = OpenMode.CREATE
        };
        using var writer = new IndexWriter(_directory, config);
        writer.DeleteAll();
        writer.Commit();
    }

    public int GetDocumentCount()
    {
        using var reader = DirectoryReader.Open(_directory);
        return reader.NumDocs;
    }

    private static string BuildSnippet(string content, string query)
    {
        if (content.Length <= 300)
            return content;

        var first300 = content[..300];
        var idx = first300.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
            return first300 + "...";

        var head = content[..150];
        var fullIdx = content.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (fullIdx < 0)
            return head + "...";

        var start = Math.Max(0, fullIdx - 75);
        var end = Math.Min(content.Length, fullIdx + query.Length + 75);
        var fragment = content[start..end];

        return head + "..." + fragment + "...";
    }

    public void Dispose()
    {
        _analyzer.Dispose();
        _directory.Dispose();
    }
}
