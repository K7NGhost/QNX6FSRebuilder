using System;
using System.Collections.Generic;
using System.Text;

namespace QNX6FSRebuilder.Core.Models
{
    public class DirEntry
    {
        public int? ParentInode { get; set; }
        public uint InodeNumber { get; private set; }
        public byte NameLength { get; private set; }
        public string Name { get; private set; }

        public DirEntry(byte[] data)
        {
            if (data.Length < 5)
                throw new ArgumentException("Not enough data to parse DirEntry");

            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms))
            {
                InodeNumber = br.ReadUInt32(); // <I> = 4 bytes
                NameLength = br.ReadByte();    // <B> = 1 byte

                if (data.Length < 5 + NameLength)
                    throw new ArgumentException("Data too short for specified name length.");

                byte[] nameBytes = br.ReadBytes(NameLength);
                Name = Encoding.UTF8.GetString(nameBytes);
            }
        }

        public override string ToString()
        {
            return $"<dir_entry parent_inode={ParentInode} inode_id={InodeNumber}, length='{NameLength}', name='{Name}'>";
        }
    }
}
