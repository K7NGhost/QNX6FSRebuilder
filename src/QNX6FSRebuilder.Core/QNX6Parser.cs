using Microsoft.Extensions.Logging;
using QNX6FSRebuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace QNX6FSRebuilder.Core
{
    public class QNX6Parser
    {
        private readonly ILogger<QNX6Parser> _logger;
        private string filePath;
        private FileStream fStream;
        private bool shouldParseSecondSuperBlock = false;

        private static readonly byte[] GPT_SIGNATURE = Encoding.ASCII.GetBytes("EFI PART");
        private const string QNX6_PARTITION_GUID = "CEF5A9AD-73BC-4601-89F3-CDEEEEE321A1";
        private const string FREEBSD_BOOT_GUID = "83BD6B9D-7F41-11DC-BE0B-001560B84F0F";
        
        public QNX6Parser(ILogger<QNX6Parser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void ParseQNX6(string filePath, string outputPath)
        {
            fStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            GetAllPartitions();
        }

        public List<Partition> GetAllPartitions()
        {

            var partitions = new List<Partition>();

            fStream.Seek(512, SeekOrigin.Begin);
            var gptSig = new byte[8];
            fStream.ReadExactly(gptSig, 0, 8);

            if (ByteArrayEquals(gptSig, GPT_SIGNATURE))
            {
                _logger.LogInformation("GPT Signature found, parsing partitions...");

                // Read GUID header
                fStream.Seek(512, SeekOrigin.Begin);
                byte[] headerData = new byte[512];
                fStream.Read(headerData, 0, 512);

                var guidHeader = new GUIDHeader(headerData);
                int numOfPartitions = (int)guidHeader.NumOfPartitions;
                _logger.LogInformation($"Number of partitions found: {numOfPartitions}");

                fStream.Seek(1024, SeekOrigin.Begin);

                for (int i = 0; i < numOfPartitions; i++)
                {
                    byte[] entryBytes = new byte[128];
                    fStream.Read(entryBytes, 0, 128);

                    if (!IsEmptyByteArray(entryBytes))
                    {
                        byte[] partitionType = new byte[16];
                        Array.Copy(entryBytes, 0, partitionType, 0, 16);

                        string guidStr = new Guid(partitionType).ToString().ToUpper();
                        _logger.LogInformation($"The partition type is: {guidStr}");

                        if (guidStr == "CEF5A9AD-73BC-4601-89F3-CDEEEEE321A1")
                        {
                            _logger.LogInformation($"[+] Supported QNX6 Partition Detected, Appending");
                            partitions.Add(new Partition("GPT", entryBytes));
                        }
                        else if (guidStr == "83BD6B9D-7F41-11DC-BE0B-001560B84F0F")
                        {
                            _logger.LogInformation($"[X] FreeBSD Boot partition Detected, Skipping...");
                        }
                        else
                        {
                            _logger.LogInformation("[X] Unknown Partition");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Empty array of bytes");
                    }
                }
            }
            else
            {
                _logger.LogInformation("[+] MBR Detected");
                fStream.Seek(446, SeekOrigin.Begin);

                for (int i = 0; i < 4; i++)
                {
                    byte[] entryBytes = new byte[16];
                    fStream.Read(entryBytes, 0, 16);

                    if (!IsEmptyByteArray(entryBytes))
                    {
                        byte partitionType = entryBytes[4];

                        if (partitionType == 0x05 || partitionType == 0x0F)
                        {
                            _logger.LogInformation("[+] (EBR) Extended Boot Record Detected, Processing...");
                            var partition = new Partition("MBR", entryBytes);
                            _logger.LogInformation($"The sector where the ebr starts is {partition.GetStartLba()}");

                            ulong baseLba = partition.GetStartLba();
                            var ebr = ExtendedBootRecord.FromDisk(fStream, baseLba, baseLba);
                            var logicalPartitions = ebr.GetAllLogicalPartitions();
                            partitions.AddRange(logicalPartitions);
                        }
                        else if (partitionType == 0xB1 || partitionType == 0xB2 || partitionType == 0xB3 || partitionType == 0xB4)
                        {
                            _logger.LogInformation("[+] Supported QNX6 Partition Detected, Appending...");
                            partitions.Add(new Partition("MBR", entryBytes));
                        }
                        else if (partitionType == 0x4D || partitionType == 0x4E || partitionType == 0x4F)
                        {
                            Console.WriteLine("[X] Unsupported QNX4 Partition Detected, Exiting...");
                            return new List<Partition>();
                        }

                    }
                }
            }

            return partitions;

        }

        private static bool ByteArrayEquals(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length)
                return false;
            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i])
                    return false;
            }
            return true;
        }

        private static bool IsEmptyByteArray(byte[] array)
        {
            foreach (var b in array)
            {
                if (b != 0)
                    return false;
            }
            return true;
        }


    }
}
