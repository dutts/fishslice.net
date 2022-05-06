using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using fishslice.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Filters;

namespace fishslice.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ScrapeController : ControllerBase
    {
        private readonly ILogger<ScrapeController> _logger;
        private readonly IRemoteWebDriverFactory _remoteWebDriverFactory;

        public ScrapeController(ILogger<ScrapeController> logger, IRemoteWebDriverFactory remoteWebDriverFactory)
        {
            _logger = logger;
            _remoteWebDriverFactory = remoteWebDriverFactory;
        }

        /// <summary>
        /// Requests a URL from the scraper
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The Scrape response, as page source or screenshot</returns>
        /// <response code="201">Returns the scrape result</response>
        /// <response code="204">If the scraper was unable to acquire any content from the Url</response>
        /// <response code="400">Url is not absolute</response>
        [HttpPost("/requestUrl")]
        [SwaggerRequestExample(typeof(UrlRequest), typeof(UrlRequestExample))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public IActionResult Post(UrlRequest request)
        {
            var requestId = Guid.NewGuid();
            
            // Selenium insists on full absolute URLs so ensure we have been given one, as the json deserialiser doesn't spot this
            try
            {
                var _ = request.Url.AbsoluteUri;
            }
            catch (Exception)
            {
                _logger.LogError($"{requestId} : Invalid uri string received - '{request.Url}'");
                return BadRequest("Invalid uri string, needs to be a full absolute uri, e.g. 'http://www.google.com'");
            }

            _logger.LogInformation($"{requestId} : Received request for '{request.ResourceType}' of '{request.Url}'");
            
            using var scraper = new Scraper(_logger, _remoteWebDriverFactory);
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            var cancellationToken = cancellationTokenSource.Token;
            
            var response = scraper.Scrape(requestId, request, cancellationToken);

            if (response.Result == null)
            {
                _logger.LogInformation($"{requestId} : Error, returning 204");
                return new NoContentResult();
            }
            else
            {
                _logger.LogInformation($"{requestId} : Scrape OK, returning 200");
                return Ok(response.Result);
            }
        }
        
        
        /// <summary>
        /// Requests a screenshot from the scraper, returned as an image.png so displays in swagger UI
        /// </summary>
        [HttpPost("/requestScreenshotAsImage")]
        [SwaggerRequestExample(typeof(UrlRequest), typeof(UrlRequestExample))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("image/png")]
        public IActionResult PostRequestScreenshotAsImage(UrlRequest request)
        {
            var requestId = Guid.NewGuid();

            // Selenium insists on full absolute URLs so ensure we have been given one, as the json deserialiser doesn't spot this
            try
            {
                var _ = request.Url.AbsoluteUri;
            }
            catch (Exception)
            {
                _logger.LogError($"{requestId} : Invalid uri string received - '{request.Url}'");
                return BadRequest("Invalid uri string, needs to be a full absolute uri, e.g. 'http://www.google.com'");
            }

            _logger.LogInformation($"{requestId} : Received request for '{request.ResourceType}' of '{request.Url}'");

            using var scraper = new Scraper(_logger, _remoteWebDriverFactory);
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            var cancellationToken = cancellationTokenSource.Token;

            // Force the request to screenshot so we can just paste in what we're currently developing
            var response = scraper.Scrape(requestId, request with { ResourceType = ResourceType.Screenshot }, cancellationToken);

            if (response.Result == null)
            {
                _logger.LogInformation($"{requestId} : Error, returning 204");
                return new NoContentResult();
            }
            else
            {
                _logger.LogInformation($"{requestId} : Scrape OK, returning image");
                var tmpFile = Path.GetTempFileName();
                System.IO.File.WriteAllBytes(tmpFile, Convert.FromBase64String(response.Result.ResultString));
                return PhysicalFile(tmpFile, "image/png");
            }
        }
    }

    public class UrlRequestExample : IExamplesProvider<UrlRequest>
    {
        public UrlRequest GetExamples()
        {
            return new UrlRequest(new Uri("https://duckduckgo.com"), ResourceType.PageSource,
                new List<PreScrapeAction>()
                {
                    new Sleep(1000),
                    new SetInputElement("//*[@id=\"search_form_input_homepage\"]", "Awesome people named Richard", 10000),
                    new WaitForElement("//*[@id=\"search_button_homepage\"]"),
                    new ClickButton("//*[@id=\"search_button_homepage\"]")
                });
        }
    }
}