using System;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace DataCompression
{
    public static class DataProcessorFactory
    {
        public static DataProcessor CreateDataProcessor(string action, ILogger logger, string inputFile, string outputFile)
        {
            var countOfThreads = Environment.ProcessorCount;
            if (action == "compress") return new DataProcessor(CompressionMode.Compress, countOfThreads, logger, inputFile, outputFile);
            if (action == "decompress") return new DataProcessor(CompressionMode.Decompress, countOfThreads, logger, inputFile, outputFile);

            throw new ArgumentException("Got wrong action");
        }
    }
}
