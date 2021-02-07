using System.Collections.Concurrent;
using System.Collections.Generic;
using NUnit.Framework;

namespace DataCompression.Tests
{
    public class DataStorageTests
    {
        [Test]
        [TestCaseSource(nameof(SourceForCheckFlushBuffer))]
        public void AddChunk_Works_Correct(long lastAddedIndex,int bufferLength, Chunk chunk, ConcurrentDictionary<long, byte[]> buffer)
        {
            var storage = new CompressedChunkStorage(lastAddedIndex, buffer);
            storage.AddChunk(chunk);
            
            Assert.IsTrue(storage.WaitingChunks.Count == bufferLength);
        }

        private static IEnumerable<object[]> SourceForCheckFlushBuffer()
        {
            return new[]
            {
                new object[]
                {
                    1, 0, new Chunk {Index = 2, Data = new byte[] {1}}, new ConcurrentDictionary<long, byte[]>
                    {
                        [3] = new byte[1],
                        [4] = new byte[1],
                    }
                },
                new object[]
                {
                    1, 0, new Chunk {Index = 2, Data = new byte[] {1}}, new ConcurrentDictionary<long, byte[]>()
                }, 
                new object[]
                {
                    0, 0, new Chunk {Index = 1, Data = new byte[] {1}}, new ConcurrentDictionary<long, byte[]>()
                }, 
                new object[]
                {
                    0, 1, new Chunk {Index = 2, Data = new byte[] {1}}, new ConcurrentDictionary<long, byte[]>()
                }, 
            };
        }
    }
}
