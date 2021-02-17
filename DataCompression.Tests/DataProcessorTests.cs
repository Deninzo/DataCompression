using System.IO;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DataCompression.Tests
{
    public class DataProcessorTests
    {
        private string _originalFile = @"bigfile";
        private string _compressedFile = "compressed";
        private string _uncompressedFile = "uncompressed";
        
        [Test]
        public void Compress_Working_Fine()
        {
            if (File.Exists(_compressedFile))
            {
                File.Delete(_compressedFile);
            }
            
            using var processor = DataProcessorFactory.CreateDataProcessor("compress", new Mock<ILogger>().Object, _originalFile, _compressedFile);
            processor.ProcessData();
            Assert.Pass();
        }
        
        [Test]
        public void Decompress_Working_Fine()
        {
            if (File.Exists(_uncompressedFile))
            {
                File.Delete(_uncompressedFile);
            }

            using var processor = DataProcessorFactory.CreateDataProcessor("decompress", new Mock<ILogger>().Object, _compressedFile, _uncompressedFile);
            processor.ProcessData();
            Assert.Pass();
        }
        
        [Test]
        public void OriginalFile_Equal_Uncompressed()
        {
            var original = File.OpenRead(_originalFile);

            var uncompressed = File.OpenRead(_uncompressedFile);
            
            if(original.Length != uncompressed.Length) Assert.Fail();

            for (int i = 0; i < 10000; i++)
            {
                if(original.ReadByte() != uncompressed.ReadByte())
                    Assert.Fail();
            }
            
            Assert.Pass();
        }
    }
}
