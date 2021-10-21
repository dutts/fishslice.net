using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace fishslice.Services
{
    public class Scraper : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IWebDriver _webDriver;

        public Scraper(ILogger logger, IRemoteWebDriverFactory remoteWebDriverFactory)
        {
            _logger = logger;
            _webDriver = remoteWebDriverFactory.GetRemoteWebDriver();
        }
        
        public async Task<UriScrapeResponse> Scrape(Guid requestId, UrlRequest urlRequest, CancellationToken cancellationToken)
        {
            var url = urlRequest.Url;

            _logger.LogInformation($"{requestId} : Received request, navigating to '{url}'");

            try
            {
                _webDriver.Navigate().GoToUrl(url);

                if (urlRequest.PreScrapeActions != null && urlRequest.PreScrapeActions.Count > 0)
                {
                    await PerformScrapeActions(urlRequest.PreScrapeActions, requestId, cancellationToken);
                }

                switch (urlRequest.ResourceType)
                {
                    case ResourceType.PageSource:
                        _logger.LogInformation($"{requestId} : Handling page source request for '{url}'");
                        var pageSource = _webDriver.PageSource;
                        return new UriScrapeResponse(requestId, ScrapeResult.Ok, pageSource);
                    case ResourceType.Screenshot:
                        _logger.LogInformation($"{requestId} : Handling screenshot request for '{url}'");
                        await Task.Delay(1000, cancellationToken);
                        _logger.LogInformation($"{requestId} : Begin screenshotting '{url}'");
                        var totalWidth =
                            (int)(long)((IJavaScriptExecutor)_webDriver).ExecuteScript("return document.body.offsetWidth");
                        var totalHeight =
                            (int)(long)((IJavaScriptExecutor)_webDriver).ExecuteScript(
                                "return document.body.parentNode.scrollHeight");
                        _webDriver.Manage().Window.Size = new System.Drawing.Size(totalWidth, totalHeight);
                        var screenshotString = ((ITakesScreenshot)_webDriver).GetScreenshot().AsBase64EncodedString;
                        _logger.LogInformation($"{requestId} : End screenshotting '{url}'");
                        return new UriScrapeResponse(requestId, ScrapeResult.Ok, screenshotString);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"{requestId} : Exception occurred in WebDriver, '{e}");
                return new UriScrapeResponse(requestId, ScrapeResult.Error, e.ToString());
            }
            return new UriScrapeResponse(requestId, ScrapeResult.Error, null);
        }

        private async Task PerformScrapeActions(List<PreScrapeAction> scrapeActions, Guid requestId, CancellationToken cancellationToken)
        {
            for(var i = 0; i < scrapeActions.Count; i++)
            {
                _logger.LogInformation($"{requestId} : Action {i + 1} (of {scrapeActions.Count})");
                var currentAction = scrapeActions[i];

              
                switch (scrapeActions[i].Type)
                {
                    case PreScrapeActionType.Sleep:
                        await HandleSleep((Sleep)currentAction, requestId, cancellationToken);
                        break;
                    case PreScrapeActionType.WaitForElement:
                        HandleWaitForElement((WaitForElement)currentAction, requestId, cancellationToken);
                        break;
                    case PreScrapeActionType.SetInputElement:
                        HandleSetInputElement((SetInputElement)currentAction, requestId, cancellationToken);
                        break;
                    case PreScrapeActionType.ClickButton:
                        HandleClickButton((ClickButton)currentAction, requestId, cancellationToken);
                        break;
                }
            }
            //throw new NotImplementedException();
        }

        private void HandleClickButton(ClickButton currentAction, Guid requestId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                $"{requestId} : Handling an ClickButton action");
            
            if (currentAction.WaitForMilliseconds > 0)
            {
                HandleWaitForElement(new WaitForElement(currentAction.SelectorXPath, currentAction.WaitForMilliseconds), requestId, cancellationToken);
            }
            
            var button = _webDriver.FindElement(By.XPath(currentAction.SelectorXPath));
            button.Click();
        }

        private void HandleSetInputElement(SetInputElement currentAction, Guid requestId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                $"{requestId} : Handling an SetInput action");

            if (currentAction.WaitForMilliseconds > 0)
            {
                HandleWaitForElement(new WaitForElement(currentAction.SelectorXPath, currentAction.WaitForMilliseconds), requestId, cancellationToken);
            }
            
            var inputElement = _webDriver.FindElement(By.XPath(currentAction.SelectorXPath));
            inputElement.SendKeys(currentAction.Value);
        }

        private void HandleWaitForElement(WaitForElement waitForElementAction, Guid requestId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                $"{requestId} : Handling a WaitFor action for {waitForElementAction.WaitForMilliseconds}ms");
            var wait = new WebDriverWait(_webDriver, TimeSpan.FromMilliseconds(waitForElementAction.WaitForMilliseconds));
            wait.Until(webDriver =>
            {
                if (cancellationToken.IsCancellationRequested) return true;
                try
                {
                    _logger.LogInformation(
                        $"{requestId} : Waiting for XPath '{waitForElementAction.SelectorXPath}'");
                    return webDriver.FindElement(By.XPath(waitForElementAction.SelectorXPath)).Displayed;
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
                catch (NoSuchElementException)
                {
                    return false;
                }
            });
        }

        private async Task HandleSleep(Sleep sleepAction, Guid requestId, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{requestId} : Delaying for {sleepAction.Milliseconds}ms");
            await Task.Delay(sleepAction.Milliseconds, cancellationToken);
        }
        
        public void Dispose()
        {
            _webDriver.Dispose();
        }
    }
}