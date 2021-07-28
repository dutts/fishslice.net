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
        public IActionResult Post(UriRequest request)
        {
            _logger.LogInformation($"Handling POST request, received '{request.UriString}'");

            var requestId = Guid.NewGuid();

            _logger.LogInformation($"Enqueuing request for '{request.ResourceType}' of '{request.UriString}' with request id '{requestId}'");

            switch (request.ResourceType)
            {
                case ResourceType.PageSource:
                    _requestQueue.Enqueue(new UriRequestQueueItem(requestId, request.UriString));
                    break;
                case ResourceType.Screenshot:
                    _screenshotRequestQueue.Enqueue(new UriRequestQueueItem(requestId, request.UriString));
                    break;
            }
            
            return Ok(new UriRequestResponse(requestId));
        }

        [HttpGet("/requestId")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Get(Guid requestId, ResourceType resourceType)
        {
            _logger.LogInformation($"Handling GET request, received request for '{resourceType}' with request id '{requestId}'");

            var lookup = new ScrapeResultCacheKey(requestId, resourceType);
            if (_scrapeResultCache.TryGetValue(lookup, out UriScrapeResponse response))
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
