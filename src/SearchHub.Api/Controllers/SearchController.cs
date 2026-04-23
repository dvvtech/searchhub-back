using Microsoft.AspNetCore.Mvc;
using SearchHub.Api.Models;
using SearchHub.Api.Services;

namespace SearchHub.Api.Controllers;

[Route("search")]
[ApiController]
public class SearchController : ControllerBase
{
    private readonly ILuceneIndexService _luceneIndex;

    public SearchController(ILuceneIndexService luceneIndex)
    {
        _luceneIndex = luceneIndex;
    }

    [HttpGet]
    public ActionResult<SearchResponse> Search(
        [FromQuery] string q,
        [FromQuery] int? siteId = null,
        [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Query parameter 'q' is required" });

        var result = _luceneIndex.Search(q, siteId, limit);
        return Ok(result);
    }
}
