using System;
using System.IO;
using System.Threading;
using fishslice.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Filters;

namespace fishslice.Controllers;

[ApiController]
[Route("[controller]")]
public class ScrapeController(ILogger<ScrapeController> logger) : ControllerBase
{
    /// <summary>
    ///     Requests a URL from the scraper
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

        using (logger.BeginScope("{RequestId} : Received request for '{RequestResourceType}' of {RequestUrl}'",
                   requestId, request.ResourceType, request.Url))
        {
            try
            {
                _ = request.Url.AbsoluteUri;
            }
            catch (Exception)
            {
                logger.LogError("Invalid uri string received");
                return BadRequest(
                    "Invalid uri string, needs to be a full absolute uri, e.g. 'http://www.google.com'");
            }

            var scraper = new Scraper(logger);
            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            var response = scraper.Scrape(requestId, request, cancellationToken);

            if (response.Result == null)
            {
                logger.LogInformation("Error, returning 204");
                return new NoContentResult();
            }

            logger.LogInformation("Scrape OK, returning 200");
            return Ok(response.Result);
        }
    }


    /// <summary>
    ///     Requests a screenshot from the scraper, returned as an image.png so displays in swagger UI
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

        using (logger.BeginScope("{RequestId} : Received request for '{RequestResourceType}' of {RequestUrl}'",
                   requestId, request.ResourceType, request.Url))
        {
            try
            {
                _ = request.Url.AbsoluteUri;
            }
            catch (Exception)
            {
                logger.LogError("Invalid uri string received ");
                return BadRequest(
                    "Invalid uri string, needs to be a full absolute uri, e.g. 'http://www.google.com'");
            }

            var scraper = new Scraper(logger);
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            var cancellationToken = cancellationTokenSource.Token;

            // Force the request to screenshot so we can just paste in what we're currently developing
            var response = scraper.Scrape(requestId, request with { ResourceType = ResourceType.Screenshot },
                cancellationToken);

            if (response.Result == null)
            {
                logger.LogInformation("Error, returning 204");
                return new NoContentResult();
            }

            logger.LogInformation("Scrape OK, returning image");
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
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_6_1) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.6 Safari/605.1.15",
            false,
            [
                new SetBrowserSize(1024, 768),
                new NavigateTo("https://duckduckgo.com"),
                new Sleep(1000),
                new SetInputElement("//*[@id=\"searchbox_input\"]", "Awesome people named Richard", 10000),
                new WaitForElement(
                    "//*[@id=\"searchbox_homepage\"]/div/div/div/button[contains(@aria-label, 'Search')]"),
                new ClickButton("//*[@id=\"searchbox_homepage\"]/div/div/div/button[contains(@aria-label, 'Search')]")
            ]);
    }
}