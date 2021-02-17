using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace DataCompression
{
    public class CompressDataWriter : IDisposable
    {
        private readonly ILogger _logger;
        private readonly FileStream _fileStream;
        private readonly CompressionMode _mode;
        
        private static readonly object Sync = new object();
        
        public CompressDataWriter(CompressionMode mode, string outputFileName, ILogger logger)
        {
            _mode = mode;
            _logger = logger;
            _fileStream = new FileStream(outputFileName, FileMode.Append);
        }

        public void WriteChunkToFile(byte[] chunk)
        {
            lock(Sync)
            {
                try
                {
                    if (_mode == CompressionMode.Compress)
                    {
                        var chunkLength = BitConverter.GetBytes(chunk.Length);

                        var dataWithLength = new byte[chunkLength.Length + chunk.Length];

                        chunkLength.CopyTo(dataWithLength, 0);
                        chunk.CopyTo(dataWithLength, chunkLength.Length);
                        _fileStream.Write(dataWithLength);
                    }
                    else
                    {
                        _fileStream.Write(chunk);
                    }

                    _fileStream.Flush();
                }
                catch(Exception e)
                {
                    _logger.LogError($"Error while write to output file: {e.Message}");
                }
            }
        }

        public void Dispose()
        {
            _fileStream?.Dispose();
        }
    }
}
