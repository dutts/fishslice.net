using System;
using scrapy.Queue;

namespace scrapy
{
    public class RequestQueue : ConcurrentReferenceQueue<UriRequest>
    {
        public RequestQueue()
        {
        }
    }
}
