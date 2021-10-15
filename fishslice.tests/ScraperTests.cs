using System;
using System.Collections.Generic;
using System.Threading;
using FakeItEasy;
using NUnit.Framework;
using fishslice.Services;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

namespace fishslice.tests
{
    public class ScraperTests
    {
        [Test]
        public void WebDriverExceptionResultInErrorResponse()
        {
            var webDriverFactory = A.Fake<IRemoteWebDriverFactory>();
            var webDriver = A.Fake<IWebDriver>();
            A.CallTo(() => webDriverFactory.GetRemoteWebDriver()).Returns(webDriver);
            A.CallTo(() => webDriver.Navigate()).Throws(new WebDriverException());

            var requestId = Guid.NewGuid();
            
            var scraper = new Scraper(A.Fake<ILogger<Scraper>>(), webDriverFactory);

            Assert.DoesNotThrow(() =>
            {
               var response = scraper.Scrape(requestId,  new UrlRequest(new Uri("http://www.google.com"), ResourceType.PageSource, new List<PreScrapeAction>()), new CancellationToken()).Result;
               Assert.AreEqual(requestId, response.RequestId);
               Assert.AreEqual(ScrapeResult.Error, response.ScrapeResult);
            });
        }
        
        [Test]
        public void WebDriverTimeoutExceptionResultInErrorResponse()
        {
            var webDriverFactory = A.Fake<IRemoteWebDriverFactory>();
            var webDriver = A.Fake<IWebDriver>();
            A.CallTo(() => webDriverFactory.GetRemoteWebDriver()).Returns(webDriver);
            A.CallTo(() => webDriver.FindElement(A<By>._)).Throws(new WebDriverTimeoutException());

            var requestId = Guid.NewGuid();
            
            var scraper = new Scraper(A.Fake<ILogger<Scraper>>(), webDriverFactory);

            Assert.DoesNotThrow(() =>
            {
                var response = scraper.Scrape(requestId,
                    new UrlRequest(new Uri("http://www.google.com"), ResourceType.PageSource,
                        new List<PreScrapeAction>() { new WaitForElement("//foo") }), new CancellationToken()).Result;
                Assert.AreEqual(requestId, response.RequestId);
                Assert.AreEqual(ScrapeResult.Error, response.ScrapeResult);
            });
        }
    }
}