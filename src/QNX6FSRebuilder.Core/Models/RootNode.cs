using System;
using System.Collections.Generic;
using System.Text;

namespace QNX6FSRebuilder.Core.Models
{
    internal class RootNode
    {
        public ulong Size { get; private set; }
        public List<uint> PointerArray { get; set; }
        public byte Levels { get; private set; }
        public byte Mode { get; private set; }
        public byte[] Spare { get; private set; }
        public int? Index { get; set; }
        public RootNode(byte[] data)
        {
            if (data.Length < 74)
                throw new ArgumentException("Not enough data to parse RootNode");

            using (var ms = new MemoryStream(data))
            using (var br = new BinaryReader(ms))
            {
                Size = br.ReadUInt64();

                PointerArray = new List<uint>(16);
                for (int i = 0; i < 16; i++)
                {
                    PointerArray.Add(br.ReadUInt32());
                }

                Levels = br.ReadByte();
                Mode = br.ReadByte();
            }

            Spare = new byte[data.Length - 74];
            Array.Copy(data, 74, Spare, 0, Spare.Length);
        }

        public override string ToString()
        {
            string pointers = string.Join(", ", PointerArray);
            return $"<RootNode size={Size}, levels={Levels}, mode={Mode}, pointers=[{pointers}]>";
        }
    }
}
