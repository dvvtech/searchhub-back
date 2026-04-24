using Microsoft.AspNetCore.Mvc;
using SearchHub.Api.Models;
using SearchHub.Api.Services;

namespace SearchHub.Api.Controllers;

[Route("search")]
[ApiController]
public class SearchController : ControllerBase
{
    private readonly ILuceneIndexService _luceneIndex;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ILuceneIndexService luceneIndex,
        ILogger<SearchController> logger)
    {
        _luceneIndex = luceneIndex;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<SearchResponse> Search(
        [FromQuery] string q,
        [FromQuery] int? siteId = null,
        [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Query parameter 'q' is required" });
        
        _logger.LogInformation("Received search request: q='{Query}', siteId={SiteId}, limit={Limit}", q, siteId, limit);

        var result = _luceneIndex.Search(q, siteId, limit);
        return Ok(result);
    }
}
