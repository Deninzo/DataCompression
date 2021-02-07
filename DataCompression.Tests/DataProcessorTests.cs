using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace DataCompression.Tests
{
    public class DataProcessorTests
    {
        private string _originalFile = "test.docx";
        private string _compressedFile = "compressed";
        private string _uncompressedFile = "uncompressed.docx";
        
        [Test]
        public void Compress_Working_Fine()
        {
            if (File.Exists(_compressedFile))
            {
                File.Delete(_compressedFile);
            }
            
            var processor = DataProcessorFactory.CreateDataProcessor("compress", new Mock<ILogger>().Object);
            processor.ProcessData(_originalFile, _compressedFile);
            Assert.Pass();
        }
        
        [Test]
        public void Decompress_Working_Fine()
        {
            if (File.Exists(_uncompressedFile))
            {
                File.Delete(_uncompressedFile);
            }

            var processor = DataProcessorFactory.CreateDataProcessor("decompress", new Mock<ILogger>().Object);
            processor.ProcessData(_compressedFile, _uncompressedFile);
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
