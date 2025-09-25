using QNX6FSRebuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace QNX6FSRebuilder.Core.Tests
{
    public class QNX6ParserTests
    {
        private const string TEST_QNX6_FILE_PATH = @"D:\qnx6_realImage.img";

        [Fact]
        public void GetAllPartitions_WithQNX6File_ReturnsPartitionsList()
        {
            if (!File.Exists(TEST_QNX6_FILE_PATH))
            {
                Assert.True(false, $"Test file not found at path: {TEST_QNX6_FILE_PATH}");
                return;
            }
            var parser = new QNX6Parser(TEST_QNX6_FILE_PATH, string.Empty);
            var partitions = parser.GetAllPartitions();

            Assert.NotNull(partitions);
            Assert.IsType<List<Partition>>(partitions);
        }
    }
}
