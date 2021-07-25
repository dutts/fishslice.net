using System;

namespace scrapy
{
    public record UriRequestResponse(Guid RequestId);

    public record UriRequest(Guid RequestId, string UriString);

    public enum ScrapeResult
    {
        Ok,
        Error
    }

    public record UriScrapeResponse(Guid RequestId, ScrapeResult ScrapeResult, string Result);
}
