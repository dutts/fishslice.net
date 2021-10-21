using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Transactions;
using fishslice.Converters;

namespace fishslice
{
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
        ClickButton
    }
    
    public abstract record PreScrapeAction(PreScrapeActionType Type);
    public record WaitForElement(string SelectorXPath, int WaitForMilliseconds = 0) : PreScrapeAction(PreScrapeActionType.WaitForElement);
    public record Sleep(int Milliseconds) : PreScrapeAction(PreScrapeActionType.Sleep);
    public record SetInputElement(string SelectorXPath, string Value, int WaitForMilliseconds = 0) : PreScrapeAction(PreScrapeActionType.SetInputElement);
    public record ClickButton(string SelectorXPath, int WaitForMilliseconds = 0) : PreScrapeAction(PreScrapeActionType.ClickButton);
    public record UrlRequest(Uri Url, ResourceType ResourceType, List<PreScrapeAction> PreScrapeActions);
    
    public enum ScrapeResult
    {
        Ok,
        Error
    }

    public record UriScrapeResponse(Guid RequestId, ScrapeResult ScrapeResult, string ResultString);
}
