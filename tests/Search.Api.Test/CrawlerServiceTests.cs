using System.Net;
using SearchHub.Api.Configuration;
using SearchHub.Api.Services;

namespace Search.Api.Test;

public class CrawlerServiceTests
{
    [Fact]
    public async Task CrawlSiteAsync_OnlyFollowsSameDomainLinks()
    {
        var requestLog = new List<string>();

        var handler = new FakeHttpMessageHandler(requestLog);
        handler.Register("https://mysite.com/",
            "<html><body><a href='/about'>About</a><a href='https://evil.com/attack'>External</a><a href='https://mysite.com/contact'>Contact</a></body></html>");
        handler.Register("https://mysite.com/about",
            "<html><body><a href='https://anothersite.com/page'>Another site</a><p>About page</p></body></html>");
        handler.Register("https://mysite.com/contact",
            "<html><body><p>Contact page</p></body></html>");
        handler.Register("https://evil.com/attack",
            "<html><body><p>Should never be fetched</p></body></html>");
        handler.Register("https://anothersite.com/page",
            "<html><body><p>Should never be fetched</p></body></html>");

        var client = new HttpClient(handler);
        var crawler = new CrawlerService(client);

        var site = new SiteConfiguration { Id = 1, Name = "MySite", Url = "https://mysite.com/", FileName = "mysite.bin" };
        var pages = await crawler.CrawlSiteAsync(site);

        Assert.All(pages, p => Assert.StartsWith("https://mysite.com", p.Url));
        Assert.DoesNotContain("https://evil.com/attack", requestLog);
        Assert.DoesNotContain("https://anothersite.com/page", requestLog);
        Assert.Contains("https://mysite.com/", requestLog);
        Assert.Contains("https://mysite.com/about", requestLog);
        Assert.Contains("https://mysite.com/contact", requestLog);
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<string> _requestLog;
        private readonly Dictionary<string, string> _responses = [];

        public FakeHttpMessageHandler(List<string> requestLog)
        {
            _requestLog = requestLog;
        }

        public void Register(string url, string html)
        {
            _responses[url.TrimEnd('/')] = html;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requestLog.Add(request.RequestUri!.AbsoluteUri);

            var key = request.RequestUri.AbsoluteUri.TrimEnd('/');
            if (_responses.TryGetValue(key, out var html))
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html");
                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
