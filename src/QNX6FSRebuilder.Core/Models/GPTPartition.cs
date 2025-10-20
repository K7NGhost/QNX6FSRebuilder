using System;
using System.Collections.Generic;
using System.Text;

namespace QNX6FSRebuilder.Core.Models
{
    public class GPTPartition
    {
        public Guid PartitionTypeGuid { get; private set; }
        public Guid UniquePartitionGuid { get; private set; }
        public ulong FirstLba { get; set; }
        public ulong LastLba { get; private set; }
        public ulong Attributes { get; private set; }
        public string Name { get; private set; }

        public GPTPartition(byte[] entryBytes)
        {
            using (var ms = new MemoryStream(entryBytes))
            using (var reader = new BinaryReader(ms))
            {
                byte[] typeGuidBytes = reader.ReadBytes(16);
                byte[] uniqueGuidBytes = reader.ReadBytes(16);

                PartitionTypeGuid = new Guid(typeGuidBytes);
                UniquePartitionGuid = new Guid(uniqueGuidBytes);

                FirstLba = reader.ReadUInt64();
                LastLba = reader.ReadUInt64();
                Attributes = reader.ReadUInt64();

                byte[] nameRaw = reader.ReadBytes(72);
                Name = Encoding.Unicode.GetString(nameRaw).TrimEnd('\0');
            }
        }

        public override string ToString()
        {
            return $"<GPTPartition name='{Name}' start_lba={FirstLba} end_lba={LastLba}>";
        }

        public void SetStartLba(ulong value)
        {
            FirstLba = value;
        }
    }
}
