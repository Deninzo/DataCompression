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
    public class DataProcessor
    {
        private readonly ILogger _logger;
        private readonly Semaphore _semaphore;
        private readonly CompressedChunkStorage _storage;
        private CompressDataWriter _writer;
        private readonly ConcurrentQueue<Chunk> _chunksForProcess;
        private readonly WaitHandle[] _waitHandles;
        private int _chunkSize = 5 * 1024 * 1024;
        private readonly Action<Chunk> _actionToExecute;

        private bool _passThroughAllData;

        public DataProcessor(CompressionMode mode, int maxThreads, ILogger logger)
        {
            if (mode == CompressionMode.Compress)
                _actionToExecute = CompressChunk;
            else
                _actionToExecute = DecompressChunk;

            _logger = logger;
            _semaphore = new Semaphore(maxThreads, maxThreads);
            _storage = new CompressedChunkStorage(mode);
            _chunksForProcess = new ConcurrentQueue<Chunk>();
            _waitHandles = new WaitHandle[maxThreads + 1];
        }

        public void ProcessData(string inputFile, string outputFile)
        {
            _writer = new CompressDataWriter(_storage, outputFile, _logger);

            if (!File.Exists(inputFile))
                throw new ArgumentException("Original file does not exists");

            for (int i = 0; i < _waitHandles.Length; i++)
            {
                var handle = new EventWaitHandle(false, EventResetMode.ManualReset);
                _waitHandles[i] = handle;
                
                if(i == 0) continue;
                
                var thread = new Thread(ProcessAsync);
                thread.Start(handle);
            }
            
            using var originalFileStream = File.Open(inputFile, FileMode.Open);
            
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
            }

            _passThroughAllData = true;
        }

        // private void CompressAsync(string inputFile)
        // {
        //     var index = 1;
        //
        //     using var originalFileStream = File.OpenRead(inputFile);
        //     
        //     var buffer = new byte[_chunkSize];
        //     Thread lastThread = null;
        //     var countReadBytes = 0;
        //     
        //     while ((countReadBytes = originalFileStream.Read(buffer, 0, buffer.Length)) != 0)
        //     {
        //         lastThread = new Thread(CompressChunk);
        //         lastThread.Start(new Chunk
        //         {
        //             Index = index,
        //             Data = buffer.Take(countReadBytes).ToArray()
        //         });
        //         index++;
        //     }
        //
        //     lastThread?.Join();
        // }

        private void ProcessAsync(object param)
        {
            var waitHandle = (EventWaitHandle)param;
            
            while (!_passThroughAllData || _chunksForProcess.Count > 0)
            {
                Chunk chunk;
                while (!_chunksForProcess.TryDequeue(out chunk))
                {
                    
                }
                _actionToExecute(chunk);
            }

            waitHandle.Set();
        }
        
        // private void DecompressAsync(string inputFile)
        // {
        //     var index = 1;
        //
        //     using var originalFileStream = File.OpenRead(inputFile);
        //     
        //     var lengthBuffer = new byte[4];
        //     Thread lastThread = null;
        //
        //     while (originalFileStream.Read(lengthBuffer, 0, lengthBuffer.Length) != 0)
        //     {
        //         var length = BitConverter.ToInt32(lengthBuffer);
        //
        //         var buffer = new byte[length];
        //
        //         var countReadBytes = originalFileStream.Read(buffer, 0, buffer.Length);
        //
        //         lastThread = new Thread(DecompressChunk);
        //
        //         lastThread.Start(new Chunk
        //         {
        //             Data = buffer.Take(countReadBytes).ToArray(),
        //             Index = index
        //         });
        //
        //         index++;
        //     }
        //
        //     lastThread?.Join();
        // }
        
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

            var decompressedChunk = new byte[_chunkSize];
            var countReadBytes = compressedStream.Read(decompressedChunk);
            compressedStream.Flush();
            memoryStream.Flush();
            _storage.AddChunk(new Chunk {Index = chunk.Index, Data = decompressedChunk.Take(countReadBytes).ToArray()});
        }
    }
}
