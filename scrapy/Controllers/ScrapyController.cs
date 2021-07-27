using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using scrapy.Services;

namespace scrapy.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ScrapyController : ControllerBase
    {
        private readonly RequestQueue _requestQueue;
        private readonly ScreenshotRequestQueue _screenshotRequestQueue;
        private readonly MemoryCache _scrapeResultCache;
        private readonly ILogger<ScrapyController> _logger;

        public ScrapyController(RequestQueue requestQueue, ScreenshotRequestQueue screenshotRequestQueue, MemoryCache scrapeResultCache, ILogger<ScrapyController> logger)
        {
            _requestQueue = requestQueue;
            _screenshotRequestQueue = screenshotRequestQueue;
            _scrapeResultCache = scrapeResultCache;
            _logger = logger;
        }

        [HttpPost("/requestUri")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Post(string uriString)
        {
            _logger.LogInformation($"Handling POST request, received '{uriString}'");

            var requestId = Guid.NewGuid();

            _logger.LogInformation($"Enqueuing '{uriString}' with request id '{requestId}'");
            _requestQueue.Enqueue(new UriRequest(requestId, uriString));

            return Ok(new UriRequestResponse(requestId));
        }

        [HttpGet("/requestId")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Get(Guid requestId)
        {
            _logger.LogInformation($"Handling GET request, received request for '{requestId}'");

            if(_scrapeResultCache.TryGetValue($"{requestId}_SRC", out UriScrapeResponse response))
            {
                return Ok(response);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost("/requestScreenshotUri")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult PostScreenshot(string uriString)
        {
            _logger.LogInformation($"Handling POST request, received '{uriString}'");

            var requestId = Guid.NewGuid();

            _logger.LogInformation($"Enqueuing '{uriString}' with request id '{requestId}'");
            _screenshotRequestQueue.Enqueue(new UriRequest(requestId, uriString));

            return Ok(new UriRequestResponse(requestId));
        }

        [HttpGet("/requestScreenshotId")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetScreenshot(Guid requestId)
        {
            _logger.LogInformation($"Handling GET request, received request for '{requestId}'");

            if (_scrapeResultCache.TryGetValue($"{requestId}_SHT", out UriScrapeResponse response))
            {
                return Ok(response);
            }
            else
            {
                return NotFound();
            }
        }
    }
}
