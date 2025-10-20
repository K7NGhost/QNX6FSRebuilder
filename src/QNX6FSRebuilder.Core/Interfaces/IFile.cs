using System;
using System.Collections.Generic;
using System.Text;

namespace QNX6FSRebuilder.Core.Interfaces
{
    public interface IFile
    {
        public int? ParentId { get; set; }
        public int FileId { get; set; }
        public string FileName { get; set; }
        public ulong FileSize { get; set; }
        public uint CreatedTime { get; set; }
        public uint ModifiedTime { get; set; }
        public uint AccessedTime { get; set; }
        public List<byte[]> FileData { get; set; }
        public ushort Mode { get; set; }
        public string FileType { get; set; }

    }
}
