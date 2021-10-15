using System.IO.Compression;
using Serilog.Sinks.File.Archive;

namespace fishslice.Logging
{
    public class SerilogHooks
    {
        public static ArchiveHooks LogArchiveHooks => new(CompressionLevel.Optimal);
    }
}