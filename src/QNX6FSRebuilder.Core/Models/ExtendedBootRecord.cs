using System;
using System.Collections.Generic;
using System.Text;

namespace QNX6FSRebuilder.Core.Models
{
    internal class ExtendedBootRecord
    {
        public byte[] Unused { get; private set; }
        public byte[] FirstEntry { get; private set; }
        public byte[] SecondEntry { get; private set; }
        public byte[] UnusedEntry1 { get; private set; }
        public byte[] UnusedEntry2 { get; private set; }
        public byte[] Signature { get; private set; }

        public Partition LogicalPartition { get; private set; }
        public Partition SecondPartition { get; private set; }

        public ulong AbsoluteLba { get; private set; }
        public ExtendedBootRecord NextEbr { get; private set; }

        public ExtendedBootRecord(byte[] entryBytes, ulong currentLba, ulong baseLba)
        {
            using (var ms = new MemoryStream(entryBytes))
            using (var reader = new BinaryReader(ms))
            {
                // struct.unpack("<446s16s16s16s16s2s", entry_bytes)
                Unused = reader.ReadBytes(446);
                FirstEntry = reader.ReadBytes(16);
                SecondEntry = reader.ReadBytes(16);
                UnusedEntry1 = reader.ReadBytes(16);
                UnusedEntry2 = reader.ReadBytes(16);
                Signature = reader.ReadBytes(2);
            }

            LogicalPartition = new Partition("MBR", FirstEntry);
            SecondPartition = new Partition("MBR", SecondEntry);

            AbsoluteLba = LogicalPartition.GetStartLba() + currentLba;
            Console.WriteLine($"current lba of first entry {LogicalPartition.GetStartLba()} and current lba {currentLba}");

            LogicalPartition.SetStartLba(AbsoluteLba);
            NextEbr = null;
        }

        public static ExtendedBootRecord FromDisk(Stream fStream, ulong baseLba, ulong currentLba)
        {
            fStream.Seek((long)(currentLba * 512), SeekOrigin.Begin);
            byte[] entryBytes = new byte[512];
            fStream.Read(entryBytes, 0, 512);

            var ebr = new ExtendedBootRecord(entryBytes, currentLba, baseLba);

            ulong nextStart = ebr.SecondPartition.GetStartLba();
            if (nextStart != 0)
            {
                ulong nextEbrLba = baseLba + nextStart;
                ebr.NextEbr = FromDisk(fStream, baseLba, nextEbrLba);
            }

            return ebr;
        }

        public List<Partition> GetAllLogicalPartitions()
        {
            var partitions = new List<Partition> { LogicalPartition };

            if (NextEbr != null)
            {
                partitions.AddRange(NextEbr.GetAllLogicalPartitions());
            }

            return partitions;
        }
    }
}
