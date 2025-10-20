using System;
using System.Collections.Generic;
using System.Text;

namespace QNX6FSRebuilder.Core.Models
{
    public class MBRPartition
    {
        public byte BootIndicator { get; private set; }
        public byte[] StartCHS { get; private set; }
        public byte PartitionType { get; private set; }
        public byte[] EndCHS { get; private set; }
        public ulong StartLba { get; set; }
        public uint NumSectors { get; private set; }

        public MBRPartition(byte[] entryBytes)
        {
            using (var ms = new MemoryStream(entryBytes))
            using (var reader = new BinaryReader(ms))
            {
                BootIndicator = reader.ReadByte();
                StartCHS = reader.ReadBytes(3);
                PartitionType = reader.ReadByte();
                EndCHS = reader.ReadBytes(3);
                StartLba = reader.ReadUInt32();
                NumSectors = reader.ReadUInt32();
            }
        }

        public override string ToString()
        {
            return $"<MBRPartition type=0x{PartitionType:X2} start_lba={StartLba} sectors={NumSectors}>";
        }

        public void SetStartLba(ulong value)
        {
            StartLba = value;
        }
    }
}
