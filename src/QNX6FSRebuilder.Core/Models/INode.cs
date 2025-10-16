using System;
using System.Collections.Generic;
using System.Text;

namespace QNX6FSRebuilder.Core.Models
{
    internal class INode
    {
        public int? Index { get; set; }
        public ulong Size { get; private set; }
        public uint Uid { get; private set; }
        public uint Gid { get; private set; }
        public uint Ftime { get; private set; }
        public uint Mtime { get; private set; }
        public uint Atime { get; private set; }
        public uint Ctime { get; private set; }
        public ushort Mode { get; private set; }
        public ushort ExtMode { get; private set; }
        public List<uint> BlockPointerArray { get; private set; }
        public byte Levels { get; private set; }
        public byte Status { get; private set; }

        public INode(byte[] data)
        {
            if (data.Length < 102)
                throw new ArgumentException("Not enough data to parse iNode");

            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms))
            {
                Size = br.ReadUInt64();    // <Q> 8 bytes
                Uid = br.ReadUInt32();     // <I>
                Gid = br.ReadUInt32();     // <I>
                Ftime = br.ReadUInt32();   // <I>
                Mtime = br.ReadUInt32();   // <I>
                Atime = br.ReadUInt32();   // <I>
                Ctime = br.ReadUInt32();   // <I>
                Mode = br.ReadUInt16();    // <H>
                ExtMode = br.ReadUInt16(); // <H>

                BlockPointerArray = new List<uint>(16);
                for (int i = 0; i < 16; i++)
                    BlockPointerArray.Add(br.ReadUInt32()); // <16I>

                Levels = br.ReadByte();    // <B>
                Status = br.ReadByte();    // <B>
            }
        }

        public string InterpretMode(int mode)
        {
            int fileType = mode & 0xF000;
            return fileType switch
            {
                0x4000 => "Directory",
                0x8000 => "Regular File",
                0xA000 => "Symbolic Link",
                0x2000 => "Character Device",
                0x6000 => "Block Device",
                0x1000 => "FIFO (Named Pipe)",
                0xC000 => "Socket",
                _ => "Unknown"
            };
        }

        public override string ToString()
        {
            string StatusToString(byte status) => status switch
            {
                0x1 => "Dir Entry",
                0x2 => "Deleted",
                0x3 => "Normal",
                _ => $"Unknown (0x{status:X2})"
            };

            string Fmt(uint ts)
            {
                try
                {
                    return DateTimeOffset.FromUnixTimeSeconds(ts).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                }
                catch
                {
                    return "Invalid Timestamp";
                }
            }

            return
    $@"<iNode>
  iNode_id: {Index}
  Size: {Size}
  UID: {Uid} | GID: {Gid}
  Times:
    Created:  {Fmt(Ctime)}
    Modified: {Fmt(Mtime)}
    Accessed: {Fmt(Atime)}
    File Time: {Fmt(Ftime)}
  Mode: 0x{Mode:X4} ({InterpretMode(Mode)})
  Levels: {Levels}
  Status: {StatusToString(Status)}
  Block Pointers: [{string.Join(", ", BlockPointerArray)}]";
        }
    }


}
