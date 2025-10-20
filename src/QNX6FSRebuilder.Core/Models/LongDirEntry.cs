using System;
using System.Collections.Generic;
using System.Text;

namespace QNX6FSRebuilder.Core.Models
{
    public class LongDirEntry
    {
        public int? ParentInode { get; set; }
        public uint InodeNumber { get; private set; }
        public uint Size { get; private set; }
        public uint LongFileInumber { get; private set; }
        public byte Checksum { get; private set; }

        public LongDirEntry(byte[] data)
        {
            if (data.Length < 13)
                throw new ArgumentException("LongDirEntry data must be at least 13 bytes");

            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms))
            {
                InodeNumber = br.ReadUInt32();       // <I>
                Size = br.ReadUInt32();              // <I>
                LongFileInumber = br.ReadUInt32();   // <I>
                Checksum = br.ReadByte();            // <B>
            }
        }

        public override string ToString()
        {
            return $"<LongDirEntry inode_number={InodeNumber}, size={Size}, " +
                   $"long_file_inumber={LongFileInumber}, checksum=0x{Checksum:X2}>";
        }
    }
}
