using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace DataCompression
{
    public class CompressedChunkStorage : IDisposable
    {
        public ConcurrentDictionary<long, byte[]> WaitingChunks = new ConcurrentDictionary<long, byte[]>();
        
        private static readonly object SynchronizationObject = new object();

        private readonly CompressDataWriter _dataWriter;
        private long _lastAddedIndex;

        public CompressedChunkStorage(CompressionMode mode, string outputFile, ILogger logger)
        {
            _dataWriter = new CompressDataWriter(mode, outputFile, logger);
        }

        public CompressedChunkStorage(long lastAddedIndex, ConcurrentDictionary<long, byte[]> waitingChunks)
        {
            WaitingChunks = waitingChunks;
            _lastAddedIndex = lastAddedIndex;
        }

        public void AddChunk(Chunk chunk)
        {
            try
            {
                if (_lastAddedIndex == chunk.Index - 1)
                {
                    _dataWriter.WriteChunkToFile(chunk.Data);
                    
                    lock (SynchronizationObject)
                    {
                        _lastAddedIndex++;
                    }

                    if (WaitingChunks.TryGetValue(chunk.Index + 1, out var data))
                    {
                        WaitingChunks.Remove(chunk.Index + 1, out _);
                        AddChunk(new Chunk {Index = chunk.Index + 1, Data = data});
                    }
                }
                else
                {
                    while (!WaitingChunks.TryAdd(chunk.Index, chunk.Data))
                    {
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void Dispose()
        {
            _dataWriter?.Dispose();
        }
    }
}
