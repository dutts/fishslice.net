using System;

namespace scrapy
{
    public enum ResourceType
    {
        PageSource,
        Screenshot
    }

    public record UriRequest(string UriString, ResourceType ResourceType);

    public record UriRequestResponse(Guid RequestId);

    public record ScrapeResultCacheKey(Guid RequestId, ResourceType ResourceType);

    public record UriRequestQueueItem(Guid RequestId, string UriString);

    public enum ScrapeResult
    {
        Ok,
        Error
    }

    public record UriScrapeResponse(Guid RequestId, ScrapeResult ScrapeResult, ResourceType ResultType, string ResultString);
}
