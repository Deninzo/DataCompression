using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;

namespace DataCompression
{
    public class DataProcessor : IDisposable
    {
        private readonly ILogger _logger;
        private readonly CompressedChunkStorage _storage;
        private readonly ConcurrentQueue<Chunk> _chunksForProcess;
        private readonly WaitHandle[] _waitHandles;
        private readonly int _chunkSize = 5 * 1024 * 1024;
        private readonly CompressionMode _compressionMode;
        private readonly string _inputFile;
        
        private bool _passThroughAllData;

        public DataProcessor(CompressionMode mode, int maxThreads, ILogger logger, string inputFile, string outputFile)
        {
            if (!File.Exists(inputFile))
                throw new ArgumentException("Original file does not exists");
            
            _compressionMode = mode;
            _logger = logger;
            _chunksForProcess = new ConcurrentQueue<Chunk>();
            _storage = new CompressedChunkStorage(_compressionMode, outputFile, _logger);
            _waitHandles = new WaitHandle[maxThreads];
            _inputFile = inputFile;
        }

        public void ProcessData()
        {
            for (int i = 0; i < _waitHandles.Length; i++)
            {
                var handle = new EventWaitHandle(true, EventResetMode.AutoReset);

                _waitHandles[i] = handle;

                var thread = new Thread(ProcessAsync);

                thread.Start(handle);
            }

            using var originalFileStream = File.Open(_inputFile, FileMode.Open);

            if (_compressionMode == CompressionMode.Compress)
                ReadDataForCompressing(originalFileStream);
            else
                ReadDataForDecompressing(originalFileStream);

            _passThroughAllData = true;

            WaitHandle.WaitAll(_waitHandles);
        }

        private void ReadDataForCompressing(FileStream originalFileStream)
        {
            var buffer = new byte[_chunkSize];
            var countReadBytes = 0;           
            var index = 1;

            while ((countReadBytes = originalFileStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                _chunksForProcess.Enqueue(new Chunk
                {
                    Index = index,
                    Data = buffer.Take(countReadBytes).ToArray()
                });
                index++;
                WaitHandle.WaitAny(_waitHandles);
            }
        }

        private void ReadDataForDecompressing(FileStream originalFileStream)
        {
            var index = 1;

            var lengthBuffer = new byte[4];
            
            while (originalFileStream.Read(lengthBuffer, 0, lengthBuffer.Length) != 0)
            {
                var length = BitConverter.ToInt32(lengthBuffer);
                
                var chunk = new Chunk
                {
                    Data = new byte[length],
                    Index = index
                };
                
                originalFileStream.Read(chunk.Data, 0, length);
                
                _chunksForProcess.Enqueue(chunk);
                index++;

                WaitHandle.WaitAny(_waitHandles);
            }
        }

        private void ProcessAsync(object param)
        {
            var waitHandle = (EventWaitHandle)param;
            
            while (!_passThroughAllData || _chunksForProcess.Count > 0)
            {
                Chunk chunk;
                while (!_chunksForProcess.TryDequeue(out chunk))
                {
                }
                if(_compressionMode == CompressionMode.Compress)
                    CompressChunk(chunk);
                else 
                    DecompressChunk(chunk);
                waitHandle.Set();
            }
        }

        public void CompressChunk(Chunk chunk)
        {
            using var memoryStream = new MemoryStream();
            using var compressedStream = new GZipStream(memoryStream, CompressionMode.Compress);
            compressedStream.Write(chunk.Data);
            compressedStream.Flush();
            memoryStream.Flush();
            var result = memoryStream.ToArray();

            _storage.AddChunk(new Chunk {Index = chunk.Index, Data = result});
        }

        private void DecompressChunk(Chunk chunk)
        {
            using var memoryStream = new MemoryStream(chunk.Data);
            using var compressedStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            
            var decompressedChunk = new Chunk
            {
                Index = chunk.Index, 
                Data = new byte[_chunkSize]
            };
            
            compressedStream.Read(decompressedChunk.Data);
            compressedStream.Flush();
            memoryStream.Flush();
            
            _storage.AddChunk(decompressedChunk);
        }

        public void Dispose()
        {
            _storage?.Dispose();
        }
    }
}
