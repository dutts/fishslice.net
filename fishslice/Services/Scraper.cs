using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
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
                    {
                        _logger.LogInformation($"{requestId} : Handling page source request for '{url}'");
                        var rawPageSource = _webDriver.PageSource;
                        var pageSource = rawPageSource;
                        try
                        {
                            var htmlParser = new HtmlParser();
                            using var document = await htmlParser.ParseDocumentAsync(rawPageSource);
                            await using var sw = new StringWriter();
                            document.ToHtml(sw, new PrettyMarkupFormatter());
                            pageSource = sw.ToString();
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"{requestId} : Exception while trying to pretty print page source, reverting to raw");
                        }
                        return new UriScrapeResponse(requestId, ScrapeResult.Ok, pageSource);
                    }
                    case ResourceType.Screenshot:
                    {
                        _logger.LogInformation($"{requestId} : Handling screenshot request for '{url}'");
                        await Task.Delay(1000, cancellationToken);
                        _logger.LogInformation($"{requestId} : Begin screenshotting '{url}'");
                        var screenshotString = ((ITakesScreenshot)_webDriver).GetScreenshot().AsBase64EncodedString;
                        _logger.LogInformation($"{requestId} : End screenshotting '{url}'");
                        return new UriScrapeResponse(requestId, ScrapeResult.Ok, screenshotString);
                    }
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
                    case PreScrapeActionType.SetBrowserSize:
                        HandleSetBrowserSize((SetBrowserSize)currentAction, requestId, cancellationToken);
                        break;
                    case PreScrapeActionType.NavigateTo:
                        HandleNavigateTo((NavigateTo)currentAction, requestId, cancellationToken);
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
        
        private void HandleSetBrowserSize(SetBrowserSize currentAction, Guid requestId, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{requestId} : Handling SetBrowserSize action");

            SetBrowserWindowSize(requestId, currentAction.Width, currentAction.Height);
        }
        
        private void SetBrowserWindowSize(Guid requestId, int width, int height)
        {
            _logger.LogInformation($"{requestId} : Setting browser size to {width} W x {height} H");
            _webDriver.Manage().Window.Size = new System.Drawing.Size(width, height);
        }
        
        private void HandleNavigateTo(NavigateTo currentAction, Guid requestId, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{requestId} : Navigating to {currentAction.Url}");
            _webDriver.Navigate().GoToUrl(currentAction.Url);
        }
        
        public void Dispose()
        {
            _webDriver.Dispose();
        }
    }
}