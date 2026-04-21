using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SearchHub.Api.Controllers
{
    [Route("search")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        // GET /search?q=текст&siteId=1&limit=20
        //[HttpGet]
        //public async Task<SearchResponse> Search(
        //    [FromQuery] string q,
        //    [FromQuery] int? siteId = null,      // поиск по конкретному сайту
        //    [FromQuery] int limit = 20)
        //{
        //    // ...
        //}
    }
}
