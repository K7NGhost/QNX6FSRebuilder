using QNX6FSRebuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace QNX6FSRebuilder.Core
{
    public class QNX6Parser
    {
        private string filePath;
        private FileStream fStream;
        private bool shouldParseSecondSuperBlock = false;

        private static readonly byte[] GPT_SIGNATURE = Encoding.ASCII.GetBytes("EFI PART");
        private const string QNX6_PARTITION_GUID = "CEF5A9AD-73BC-4601-89F3-CDEEEEE321A1";
        private const string FREEBSD_BOOT_GUID = "83BD6B9D-7F41-11DC-BE0B-001560B84F0F";
        
        public QNX6Parser(string filePath, string outputPath)
        {
            fStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }

        public void ParseQNX6()
        {

        }

        public List<Partition> GetAllPartitions()
        {

            var partitions = new List<Partition>();

            fStream.Seek(512, SeekOrigin.Begin);
            var gptSig = new byte[8];
            fStream.Read(gptSig, 0, 8);

            if (ByteArrayEquals(gptSig, GPT_SIGNATURE))
            {
                Console.WriteLine("GPT Signature found, parsing partitions...");
            }
            else
            {
                Console.WriteLine("MBR Detected");
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
