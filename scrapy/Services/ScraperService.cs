using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace scrapy.Services
{
    public class ScraperService : BackgroundService
    {
        private readonly RequestQueue _requestQueue;
        private readonly MemoryCache _scrapeResultCache;
        private readonly ILogger<ScraperService> _logger;

        public ScraperService(RequestQueue requestQueue, MemoryCache scrapeResultCache, ILogger<ScraperService> logger)
        {
            _requestQueue = requestQueue;
            _scrapeResultCache = scrapeResultCache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            var resetWebDriver = false;

            while (!stoppingToken.IsCancellationRequested)
            {
                var options = new ChromeOptions();

                _logger.LogInformation("Creating WebDriver instance");
                options.AddArgument("start-maximized"); // https://stackoverflow.com/a/26283818/1689770
                options.AddArgument("enable-automation"); // https://stackoverflow.com/a/43840128/1689770
                options.AddArgument("--headless"); // only if you are ACTUALLY running headless
                options.AddArgument("--no-sandbox"); //https://stackoverflow.com/a/50725918/1689770
                options.AddArgument("--disable-infobars"); //https://stackoverflow.com/a/43840128/1689770
                options.AddArgument("--disable-dev-shm-usage"); //https://stackoverflow.com/a/50725918/1689770
                options.AddArgument("--disable-browser-side-navigation"); //https://stackoverflow.com/a/49123152/1689770
                options.AddArgument("--disable-gpu"); //https://stackoverflow.com/questions/51959986/how-to-solve-selenium-chromedriver-timed-out-receiving-message-from-renderer-exc

                using var driver = new RemoteWebDriver(new Uri("http://selenium.chrome:4444/wd/hub"), options);
                resetWebDriver = false;

                while (!resetWebDriver)
                {
                    if (_requestQueue.TryDequeue(out UriRequest uriRequest))
                    {
                        var uri = uriRequest.UriString;

                        _logger.LogInformation($"Dequeued '{uri}'");

                        try
                        {
                            driver.Navigate().GoToUrl(uri);

                            var pageSource = driver.PageSource;
                            _scrapeResultCache.Set(uriRequest.RequestId, new UriScrapeResponse(uriRequest.RequestId, ScrapeResult.Ok, pageSource));
                        }
                        catch (WebDriverException e)
                        {
                            _logger.LogError($"Exception occurred in WebDriver, '{e}");
                            _scrapeResultCache.Set(uriRequest.RequestId, new UriScrapeResponse(uriRequest.RequestId, ScrapeResult.Error, e.ToString()));
                            resetWebDriver = true;
                        }
                    }
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}
