using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using fishslice.Services;

namespace fishslice.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ScrapeController : ControllerBase
    {
        private readonly RequestQueue _requestQueue;
        private readonly MemoryCache _scrapeResultCache;
        private readonly ILogger<ScrapeController> _logger;

        public ScrapeController(RequestQueue requestQueue, MemoryCache scrapeResultCache, ILogger<ScrapeController> logger)
        {
            _requestQueue = requestQueue;
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

            _requestQueue.Enqueue(new UriRequestQueueItem(requestId, request.ResourceType, request.UriString, request.WaitFor));
            
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
