using System;
using System.Collections.Generic;
using System.Text;

namespace QNX6FSRebuilder.Core.Models
{
    public class GUIDHeader
    {
        public string Signature { get; private set; }
        public uint RevisionNumber { get; private set; }
        public uint HeaderSize { get; private set; }
        public uint Crc32 { get; private set; }
        public uint Reserved { get; private set; }
        public ulong CurrentLBA { get; private set; }
        public ulong BackupLBA { get; private set; }
        public ulong FirstUsableLBA { get; private set; }
        public ulong LastUsableLBA { get; private set; }
        public Guid DiskGUID { get; private set; }
        public ulong StartingLBA { get; private set; }
        public uint NumOfPartitions { get; private set; }
        public uint SizeOfPartition { get; private set; }
        public uint Crc32OfPartition { get; private set; }
        public byte[] Reserved2 { get; private set; }


        public GUIDHeader(byte[] binaryData)
        {
            using (var ms = new MemoryStream(binaryData))
            using (var reader = new BinaryReader(ms))
            {
                Signature = Encoding.ASCII.GetString(reader.ReadBytes(8)).TrimEnd('\0');
                RevisionNumber = reader.ReadUInt32();
                HeaderSize = reader.ReadUInt32();
                Crc32 = reader.ReadUInt32();
                Reserved = reader.ReadUInt32();
                CurrentLBA = reader.ReadUInt64();
                BackupLBA = reader.ReadUInt64();
                FirstUsableLBA = reader.ReadUInt64();
                LastUsableLBA = reader.ReadUInt64();

                byte[] guidBytes = reader.ReadBytes(16);
                DiskGUID = new Guid(guidBytes);

                StartingLBA = reader.ReadUInt64();
                NumOfPartitions = reader.ReadUInt32();
                SizeOfPartition = reader.ReadUInt32();
                Crc32OfPartition = reader.ReadUInt32();

                Reserved2 = reader.ReadBytes((int)(binaryData.Length - ms.Position));
            }
        }
    }
}
