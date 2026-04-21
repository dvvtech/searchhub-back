using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SearchHub.Api.Controllers
{
    [Route("index")]
    [ApiController]
    public class IndexController : ControllerBase
    {
        // POST /api/index/run - ручной запуск фоновой индексации всех сайтов
        // GET /api/index/status - статус текущей индексации
    }
}
