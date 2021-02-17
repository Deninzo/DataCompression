using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace DataCompression
{
    class Program
    {
        private static ILogger _logger;
        
        static int Main(string[] args)
        {
            _logger = LoggerFactory.Create(builder => builder.AddNLog())
                .CreateLogger<Program>();

            if (args.Length != 3)
            {
                _logger.LogCritical("Got wrong input params");
                return 1;
            }

            var action = args[0];
            var inputFileName = args[1];
            var outputFileName = args[2];
            
            try
            {
                using var compressor = DataProcessorFactory.CreateDataProcessor(action, _logger, inputFileName, outputFileName);
                compressor.ProcessData();
            }
            catch (Exception e)
            {
                _logger.LogError($"Got error during processing {e.Message}.Stack trace: {e.StackTrace}");
                return 1;
            }

            return 0;
        }
    }
}
