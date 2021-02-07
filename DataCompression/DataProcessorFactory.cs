using System;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace DataCompression
{
    public static class DataProcessorFactory
    {
        public static DataProcessor CreateDataProcessor(string action, ILogger logger)
        {
            var countOfProcessor = Environment.ProcessorCount + 4;
            if (action == "compress") return new DataProcessor(CompressionMode.Compress, countOfProcessor, logger);
            if (action == "decompress") return new DataProcessor(CompressionMode.Decompress, countOfProcessor, logger);

            throw new ArgumentException("Got wrong action");
        }
    }
}
