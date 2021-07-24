using System;
using scrapy.Queue;

namespace scrapy
{
    public class RequestQueue : ConcurrentReferenceQueue<Uri>
    {
        public RequestQueue()
        {
        }
    }
}
