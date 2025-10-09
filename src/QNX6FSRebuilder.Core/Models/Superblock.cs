using System;
using System.Collections.Generic;
using System.Text;

namespace QNX6FSRebuilder.Core.Models
{
    internal class Superblock
    {
        public uint Magic { get; }
        public uint Checksum { get; }
        public ulong Serial { get; }
        public uint CTime { get; }
        public uint ATime { get; }
        public uint Flags { get; }
        public ushort Version1 { get; }
        public ushort Version2 { get; }
        public Guid VolumeId { get; }
        public uint BlockSize { get; }
        public uint NumOfInodes { get; }
        public uint FreeInodes { get; }
        public uint NumOfBlocks { get; }
        public uint FreeBlocks { get; }
        public uint AllocGroups { get; }

        public RootNode RootNodeInode { get; }
        public RootNode RootNodeBitmap { get; }
        public RootNode RootNodeLongFilename { get; }

        public Superblock(byte[] data)
        {
            if (data == null || data.Length < 312) {
                throw new ArgumentException("SuperBlock data is too short");
            }

            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                Magic = reader.ReadUInt32();
                Checksum = reader.ReadUInt32();
                Serial = reader.ReadUInt64();
                CTime = reader.ReadUInt32();
                ATime = reader.ReadUInt32();
                Flags = reader.ReadUInt32();
                Version1 = reader.ReadUInt16();
                Version2 = reader.ReadUInt16();

                byte[] volumeIdRaw = reader.ReadBytes(16);
                VolumeId = new Guid(volumeIdRaw);

                BlockSize = reader.ReadUInt32();
                NumOfInodes = reader.ReadUInt32();
                FreeInodes = reader.ReadUInt32();
                NumOfBlocks = reader.ReadUInt32();
                FreeBlocks = reader.ReadUInt32();
                AllocGroups = reader.ReadUInt32();

                // Parse RootNodes
                RootNodeInode = new RootNode(data.Skip(72).Take(80).ToArray());
                RootNodeBitmap = new RootNode(data.Skip(152).Take(80).ToArray());
                RootNodeLongFilename = new RootNode(data.Skip(232).Take(80).ToArray());
            }
        }

        public override string ToString()
        {
            return $"<SuperBlock magic=0x{Magic:X}, volumeid={VolumeId}, serial={Serial}, " +
                   $"block_size={BlockSize}, inodes={NumOfInodes}, blocks={NumOfBlocks}>";
        }
    }
}
