using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using fishslice.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace fishslice.Services;

public class Scraper(ILogger logger)
{
    public async Task<UriScrapeResponse> Scrape(Guid requestId, UrlRequest urlRequest,
        CancellationToken cancellationToken)
    {
        var url = urlRequest.Url;

        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync();
            await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = urlRequest.UserAgent ??
                            "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_6_1) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.6 Safari/605.1.15"
            });

            var page = await context.NewPageAsync();

            if (urlRequest.CustomHeaders != null)
            {
                await page.SetExtraHTTPHeadersAsync(urlRequest.CustomHeaders);
            }

            logger.LogInformation("Navigating to url");
            await page.GotoAsync(url.AbsoluteUri);

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (urlRequest.ResourceType)
            {
                case ResourceType.PageSource:
                {
                    logger.LogInformation("Handling page source request");

                    if (urlRequest.PreScrapeActions is { Count: > 0 })
                    {
                        await PerformScrapeActions(browser, page, urlRequest.PreScrapeActions, cancellationToken);
                    }

                    var rawPageSource = await page.ContentAsync();

                    var pageSource = rawPageSource;

                    if (!urlRequest.PrettyPrintOutput)
                        return new UriScrapeResponse(requestId, ScrapeResult.Ok, rawPageSource);

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
                        logger.LogError(e, "Exception while trying to pretty print page source, reverting to raw");
                    }

                    return new UriScrapeResponse(requestId, ScrapeResult.Ok, pageSource);
                }
                case ResourceType.Screenshot:
                {
                    logger.LogInformation("Handling screenshot request");
                        
                    if (urlRequest.PreScrapeActions is { Count: > 0 })
                    {
                        await PerformScrapeActions(browser, page, urlRequest.PreScrapeActions, cancellationToken);
                    }
                        
                    await Task.Delay(1000, cancellationToken);
                    logger.LogInformation("Begin screenshotting");
                    var buffer = await page.ScreenshotAsync();
                    logger.LogInformation("End screenshotting");
                    return new UriScrapeResponse(requestId, ScrapeResult.Ok, Convert.ToBase64String(buffer));
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception occurred in WebDriver");
            return new UriScrapeResponse(requestId, ScrapeResult.Error, e.ToString());
        }

        return new UriScrapeResponse(requestId, ScrapeResult.Error, null);
    }

    private async Task PerformScrapeActions(IBrowser browser, IPage page, List<PreScrapeAction> scrapeActions,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < scrapeActions.Count; i++)
        {
            logger.LogInformation("Action {i + 1} (of {scrapeActions.Count})", i + 1, scrapeActions.Count);
            var currentAction = scrapeActions[i];

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (scrapeActions[i].Type)
            {
                case PreScrapeActionType.Sleep:
                    await HandleSleep((Sleep)currentAction, cancellationToken);
                    break;
                case PreScrapeActionType.WaitForElement:
                    await HandleWaitForElement(page, (WaitForElement)currentAction);
                    break;
                case PreScrapeActionType.SetInputElement:
                    await HandleSetInputElement(page, (SetInputElement)currentAction);
                    break;
                case PreScrapeActionType.ClickButton:
                    await HandleClickButton(page, (ClickButton)currentAction);
                    break;
                case PreScrapeActionType.SetBrowserSize:
                    await HandleSetBrowserSize(browser, (SetBrowserSize)currentAction);
                    break;
                case PreScrapeActionType.NavigateTo:
                    await HandleNavigateTo(page, (NavigateTo)currentAction);
                    break;
            }
        }
        //throw new NotImplementedException();
    }

    private async Task HandleClickButton(IPage page, ClickButton currentAction)
    {
        logger.LogInformation("Handling a ClickButton action");

        if (currentAction.WaitForMilliseconds > 0)
        {
            await HandleWaitForElement(page, new WaitForElement(currentAction.SelectorXPath.SanitiseXPath(),
                currentAction.WaitForMilliseconds));
        }

        await page.Locator(currentAction.SelectorXPath.SanitiseXPath()).ClickAsync();
    }

    private async Task HandleSetInputElement(IPage page, SetInputElement currentAction)
    {
        logger.LogInformation("Handling an SetInput action");

        if (currentAction.WaitForMilliseconds > 0)
        {
            await HandleWaitForElement(page, new WaitForElement(currentAction.SelectorXPath.SanitiseXPath(),
                currentAction.WaitForMilliseconds));
        }

        await page.Locator(currentAction.SelectorXPath.SanitiseXPath()).FillAsync(currentAction.Value);
    }

    private async Task HandleWaitForElement(IPage page, WaitForElement waitForElementAction)
    {
        logger.LogInformation("Handling a WaitFor action for {waitForElementAction.WaitForMilliseconds}ms",
            waitForElementAction.WaitForMilliseconds);
        await page.WaitForSelectorAsync(waitForElementAction.SelectorXPath.SanitiseXPath());
    }

    private async Task HandleSleep(Sleep sleepAction, CancellationToken cancellationToken)
    {
        logger.LogInformation("Delaying for {sleepAction.Milliseconds}ms", sleepAction.Milliseconds);
        await Task.Delay(sleepAction.Milliseconds, cancellationToken);
    }

    private async Task HandleSetBrowserSize(IBrowser browser, SetBrowserSize setBrowserSizeAction)
    {
        logger.LogInformation("Handling SetBrowserSize action, setting browser size to {Width} W x {Height} H",
            setBrowserSizeAction.Width, setBrowserSizeAction.Height);
        var options = new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize
            {
                Height = setBrowserSizeAction.Height,
                Width = setBrowserSizeAction.Width
            }
        };
        await browser.NewContextAsync(options);
    }

    private async Task HandleNavigateTo(IPage page, NavigateTo navigateToAction)
    {
        logger.LogInformation("Navigating to {navigateToActionUrl}", navigateToAction.Url);
        await page.GotoAsync(navigateToAction.Url);
    }
}