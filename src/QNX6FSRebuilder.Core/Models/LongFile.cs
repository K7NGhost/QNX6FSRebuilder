using QNX6FSRebuilder.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace QNX6FSRebuilder.Core.Models
{
    public class LongFile : IFile
    {
        private readonly LongDirEntry _dirEntry;
        private readonly INode _inode;
        private readonly LongNameINode _longInode;
        private readonly int _blockSize;
        private readonly FileStream _fStream;
        private readonly long _superblockEndOffset;

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

        public LongFile(LongDirEntry dirEntry, INode inode, LongNameINode longInode, FileStream fStream, int blockSize, long offset)
        {
            _dirEntry = dirEntry;
            _inode = inode;
            _longInode = longInode;
            _blockSize = blockSize;
            _fStream = fStream;
            _superblockEndOffset = offset;

            ParentId = _dirEntry.ParentInode;
            FileId = _inode.Index;
            FileName = _longInode.Name;
            FileSize = _inode.Size;
            CreatedTime = _inode.Ctime;
            ModifiedTime = _inode.Mtime;
            AccessedTime = _inode.Atime;
            FileData = GetDataFromInode(_inode);
            Mode = _inode.Mode;

            if (IsDirectory(_inode.Mode))
                FileType = "directory";
            else if (IsRegularFile(_inode.Mode))
                FileType = "file";
            else
                FileType = "Other";
        }

        private static string Fmt(uint ts)
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

        private static bool IsDirectory(ushort mode)
        {
            return (mode & 0xF000) == 0x4000;
        }

        private static bool IsRegularFile(ushort mode)
        {
            return (mode & 0xF000) == 0x8000;
        }

        private List<byte[]> GetDataFromInode(INode inode)
        {
            var fileBlocks = new List<byte[]>();
            var pointers = new List<uint>(inode.BlockPointerArray);
            int currentLevels = inode.Levels;

            // Handle indirect levels
            while (currentLevels > 0)
            {
                var nextPointers = new List<uint>();

                foreach (var ptr in pointers)
                {
                    if (ptr == 0 || ptr == 0xFFFFFFFF)
                        continue;

                    long blockOffset = ptr * _blockSize + _superblockEndOffset;
                    _fStream.Seek(blockOffset, SeekOrigin.Begin);

                    byte[] blockData = new byte[_blockSize];
                    int bytesRead = _fStream.Read(blockData, 0, _blockSize);

                    for (int i = 0; i < bytesRead; i += 4)
                    {
                        if (i + 4 > bytesRead)
                            break;

                        uint p = BitConverter.ToUInt32(blockData, i);
                        nextPointers.Add(p);
                    }
                }

                pointers = nextPointers;
                currentLevels--;
            }

            // Read actual data blocks
            foreach (var ptr in pointers)
            {
                if (ptr == 0 || ptr == 0xFFFFFFFF)
                    continue;

                long blockOffset = ptr * _blockSize + _superblockEndOffset;
                _fStream.Seek(blockOffset, SeekOrigin.Begin);

                byte[] dataBlock = new byte[_blockSize];
                int bytesRead = _fStream.Read(dataBlock, 0, _blockSize);

                if (bytesRead > 0)
                    fileBlocks.Add(dataBlock.Take(bytesRead).ToArray());
            }

            return fileBlocks;
        }

        public override string ToString()
        {
            long totalBytes = FileData.Sum(b => (long)b.Length);
            return $"<Long File parent id={ParentId}, id={FileId}, name='{FileName}', " +
                   $"size={totalBytes} bytes, created='{Fmt(CreatedTime)}', modified='{Fmt(ModifiedTime)}', accessed='{Fmt(AccessedTime)}'>";
        }
    }
}
