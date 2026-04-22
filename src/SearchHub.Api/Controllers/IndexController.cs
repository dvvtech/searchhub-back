using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SearchHub.Api.Models;
using SearchHub.Api.Services;

namespace SearchHub.Api.Controllers;

[Route("index")]
[ApiController]
public class IndexController : ControllerBase
{
    private readonly IIndexingService _indexingService;

    public IndexController(IIndexingService indexingService)
    {
        _indexingService = indexingService;
    }

    [HttpPost("run/{id?}")]
    public async Task<IActionResult> Run(int? id)
    {
        if (_indexingService.IsRunning)
            return Conflict(new { message = "Indexing is already in progress" });

        if (id.HasValue)
        {
            _ = Task.Run(() => _indexingService.ReindexSiteAsync(id.Value));
            return Accepted(new { message = $"Reindexing site {id.Value} started" });
        }

        _ = Task.Run(() => _indexingService.ReindexAllAsync());
        return Accepted(new { message = "Indexing started" });
    }

    [HttpGet("status")]
    public ActionResult<IndexStatus> Status()
    {
        return Ok(_indexingService.GetStatus());
    }
}
