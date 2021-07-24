using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace scrapy.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ScrapyController : ControllerBase
    {
        private readonly RequestQueue _requestQueue;
        private readonly ILogger<ScrapyController> _logger;

        public ScrapyController(RequestQueue requestQueue, ILogger<ScrapyController> logger)
        {
            _requestQueue = requestQueue;
            _logger = logger;
        }

        [HttpPost("/request")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Post(Uri uri)
        {
            _logger.LogInformation($"Handling POST request, received '{uri}'");
            if (uri == null) return new BadRequestResult();

            _logger.LogInformation($"Enqueuing '{uri}'");
            _requestQueue.Enqueue(uri);

            return new AcceptedResult();
        }
    }
}
