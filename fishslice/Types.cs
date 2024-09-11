using System;
using System.Collections.Generic;

namespace fishslice;

public enum ResourceType
{
    PageSource,
    Screenshot
}
    
public enum PreScrapeActionType
{
    WaitForElement,
    Sleep,
    SetInputElement,
    ClickButton,
    SetBrowserSize,
    NavigateTo
}
    
public abstract record PreScrapeAction(PreScrapeActionType Type);
public record WaitForElement(string SelectorXPath, int WaitForMilliseconds = 0) : PreScrapeAction(PreScrapeActionType.WaitForElement);
public record Sleep(int Milliseconds) : PreScrapeAction(PreScrapeActionType.Sleep);
public record SetInputElement(string SelectorXPath, string Value, int WaitForMilliseconds = 0) : PreScrapeAction(PreScrapeActionType.SetInputElement);
public record ClickButton(string SelectorXPath, int WaitForMilliseconds = 0) : PreScrapeAction(PreScrapeActionType.ClickButton);
public record SetBrowserSize(int Width, int Height) : PreScrapeAction(PreScrapeActionType.SetBrowserSize);
public record NavigateTo(string Url) : PreScrapeAction(PreScrapeActionType.NavigateTo);

public record UrlRequest(
    Uri Url,
    ResourceType ResourceType,
    string UserAgent,
    bool PrettyPrintOutput,
    List<PreScrapeAction> PreScrapeActions,
    List<KeyValuePair<string, string>> CustomHeaders = null);
    
public enum ScrapeResult
{
    Ok,
    Error
}

public record UriScrapeResponse(Guid RequestId, ScrapeResult ScrapeResult, string ResultString);