using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Compression;

namespace DataCompression
{
    public class CompressedChunkStorage
    {
        public ConcurrentDictionary<long, byte[]> WaitingChunks = new ConcurrentDictionary<long, byte[]>();
        
        private static readonly object SynchronizationObject = new object();

        private readonly ConcurrentQueue<byte[]> _compressedChunks = new ConcurrentQueue<byte[]>();
        private readonly CompressionMode _mode;
        private long _lastAddedIndex;
        
        public delegate void ChunkAddedHandler(CompressionMode mode);
        public event ChunkAddedHandler ChunkAdded;

        public CompressedChunkStorage(CompressionMode mode)
        {
            _mode = mode;
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
                    _compressedChunks.Enqueue(chunk.Data);
                    lock (SynchronizationObject)
                    {
                        _lastAddedIndex++;
                    }
                    ChunkAdded?.Invoke(_mode);

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

        public byte[] GetChunk()
        {
            byte[] chunk;
            
            while (!_compressedChunks.TryDequeue(out chunk))
            {
                
            }

            return chunk;
        }
    }
}
