using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace DataCompression
{
    public class CompressDataWriter
    {
        private readonly ILogger _logger;
        private readonly CompressedChunkStorage _storage;
        private readonly string _outputFileName;
        
        private static readonly object Sync = new object();
        
        public CompressDataWriter(CompressedChunkStorage storage, string outputFileName, ILogger logger)
        {
            _logger = logger;
            _storage = storage;
            _storage.ChunkAdded += WriteChunkToFile;
            _outputFileName = outputFileName;
        }

        public void WriteChunkToFile(CompressionMode mode)
        {
            lock(Sync)
            {
                try
                {
                    using var writer = new FileStream(_outputFileName, FileMode.Append);
                    var chunk = _storage.GetChunk();

                    if (mode == CompressionMode.Compress)
                    {
                        var chunkLength = BitConverter.GetBytes(chunk.Length);

                        var dataWithLength = new byte[chunkLength.Length + chunk.Length];

                        chunkLength.CopyTo(dataWithLength, 0);
                        chunk.CopyTo(dataWithLength, chunkLength.Length);
                        writer.Write(dataWithLength);
                    }
                    else
                    {
                        writer.Write(chunk);
                    }

                    writer.Flush();
                    writer.Close();
                }
                catch(Exception e)
                {
                    _logger.LogError($"Error while write to output file: {e.Message}");
                }
            }
        }
    }
}
