using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using scrapy.Queue;

namespace scrapy.Services
{
    public class ScreenshotRequestQueue : ConcurrentReferenceQueue<UriRequestQueueItem> { }

    public class ScraperScreenshotService : BackgroundService
    {
        private readonly ScreenshotRequestQueue _requestScreenshotQueue;
        private readonly MemoryCache _scrapeResultCache;
        private readonly ILogger<ScraperService> _logger;

        public ScraperScreenshotService(ScreenshotRequestQueue requestScreenshotQueue, MemoryCache scrapeResultCache, ILogger<ScraperService> logger)
        {
            _requestScreenshotQueue = requestScreenshotQueue;
            _scrapeResultCache = scrapeResultCache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{GetType().Name} running at: {DateTimeOffset.UtcNow}");

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
                    if (_requestScreenshotQueue.TryDequeue(out UriRequestQueueItem uriRequest))
                    {
                        var uri = uriRequest.UriString;

                        _logger.LogInformation($"Dequeued screenshot request for '{uri}'");

                        try
                        {
                            driver.Navigate().GoToUrl(uri);

                            await Task.Delay(1000);

                            _logger.LogInformation($"Begin screenshotting '{uri}'");

                            var totalWidth = (int)(long)((IJavaScriptExecutor)driver).ExecuteScript("return document.body.offsetWidth");
                            var totalHeight = (int)(long)((IJavaScriptExecutor)driver).ExecuteScript("return document.body.parentNode.scrollHeight");

                            driver.Manage().Window.Size = new System.Drawing.Size(totalWidth, totalHeight);

                            var screenshotString = driver.GetScreenshot().AsBase64EncodedString;

                            _logger.LogInformation($"End screenshotting '{uri}'");

                            _scrapeResultCache.Set(new ScrapeResultCacheKey(uriRequest.RequestId, ResourceType.Screenshot), new UriScrapeResponse(uriRequest.RequestId, ScrapeResult.Ok, ResourceType.Screenshot, screenshotString));
                        }
                        catch (WebDriverException e)
                        {
                            _logger.LogError($"Exception occurred in WebDriver, '{e}");
                            _scrapeResultCache.Set(new ScrapeResultCacheKey(uriRequest.RequestId, ResourceType.Screenshot), new UriScrapeResponse(uriRequest.RequestId, ScrapeResult.Error, ResourceType.Screenshot, e.ToString()));
                            resetWebDriver = true;
                        }
                    }
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}
