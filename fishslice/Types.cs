using System;

namespace fishslice
{
    public enum ResourceType
    {
        PageSource,
        Screenshot
    }

    public enum WaitForType
    {
        ClassName,
        Id,
        Milliseconds
    }

    public record WaitFor(WaitForType WaitForType, string Value);

    public record UriRequest(string UriString, ResourceType ResourceType, WaitFor WaitFor = null);

    public record UriRequestResponse(Guid RequestId);

    public record ScrapeResultCacheKey(Guid RequestId, ResourceType ResourceType);

    public record UriRequestQueueItem(Guid RequestId, string UriString, WaitFor WaitFor);

    public enum ScrapeResult
    {
        Ok,
        Error
    }

    public record UriScrapeResponse(Guid RequestId, ScrapeResult ScrapeResult, ResourceType ResultType, string ResultString);
}
