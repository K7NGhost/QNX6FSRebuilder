using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace QNX6FSRebuilder.Core.Models
{
    internal class File
    {

        private readonly DirEntry? _dirEntry;
        private readonly INode _inode;
        private readonly int _blockSize;
        private readonly FileStream _fStream;
        private readonly long _superblockEndOffset;

        public int? ParentId { get; private set; }
        public string FileName { get; private set; }
        public int? FileId { get; private set; }
        public ulong FileSize { get; private set; }
        public uint CreatedTime { get; private set; }
        public uint ModifiedTime { get; private set; }
        public uint AccessedTime { get; private set; }
        public List<byte[]> FileData { get; private set; }
        public ushort Mode { get; private set; }
        public string FileType { get; private set; }

        public File(DirEntry? directory, INode inode, FileStream fStream, int blockSize, long offset)
        {
            _dirEntry = directory;
            _inode = inode;
            _blockSize = blockSize;
            _fStream = fStream;
            _superblockEndOffset = offset;

            if (_dirEntry != null)
            {
                ParentId = _dirEntry.ParentInode;
                FileName = _dirEntry.Name;
            }
            else
            {
                ParentId = null;
                FileName = "deleted_" + _inode.Index;
            }

            FileId = _inode.Index;
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
            return $"<File id={FileId}, name='{FileName}', size={totalBytes} bytes, " +
                   $"created='{Fmt(CreatedTime)}', modified='{Fmt(ModifiedTime)}', accessed='{Fmt(AccessedTime)}'>";
        }
    }
}
