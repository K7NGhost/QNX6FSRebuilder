using Microsoft.Extensions.Logging;
using QNX6FSRebuilder.Core.Helpers;
using QNX6FSRebuilder.Core.Interfaces;
using QNX6FSRebuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace QNX6FSRebuilder.Core
{
    public class QNX6Parser
    {
        private readonly ILogger<QNX6Parser> _logger;
        private string filePath;
        private FileStream fStream;
        private bool shouldParseSecondSuperBlock = false;

        // Important variables
        private Dictionary<int, IINode> InodesMap;
        private Dictionary<int, IINode> LongNamesMap;
        private Dictionary<uint, List<DirEntry>> DirMap;
        private List<Models.File> Files;
        private List<LongFile> LongFiles;

        // To be used to determine the output partition
        private string extractionFolder = "";
        private string partitionString = "";
        private string partitionPath = "";
        private string loggingPath = "";

        private static readonly byte[] GPT_SIGNATURE = Encoding.ASCII.GetBytes("EFI PART");
        private const string QNX6_PARTITION_GUID = "CEF5A9AD-73BC-4601-89F3-CDEEEEE321A1";
        private const string FREEBSD_BOOT_GUID = "83BD6B9D-7F41-11DC-BE0B-001560B84F0F";
        private const int SUPERBLOCK_SIZE = 0x1000;
        private const int INODE_SIZE = 128;
        
        public QNX6Parser(ILogger<QNX6Parser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void SetupQNX6Parser(string filePath, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                return;
            extractionFolder = Path.Combine(outputPath, "extracted");
            loggingPath = Path.Combine(outputPath, "logs");
            fStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }

        public async Task ParseQNX6Async(string filePath, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                return;
            extractionFolder = Path.Combine(outputPath, "extracted");
            loggingPath = Path.Combine(outputPath, "logs");
            _logger.LogInformation($"The root folder will be: {extractionFolder}");
            fStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            List<Partition> partitions = GetAllPartitions();
            for (int i = 0; i < partitions.Count; i++)
            {
                _logger.LogInformation($"{partitions.ElementAt(i)}");
                partitionString = $"partition_{i + 1}";
                partitionPath = Path.Combine(extractionFolder, partitionString);
                _logger.LogInformation($"The partition path is: {partitionPath}");
                // Implement ParsePartition function here
                await ParsePartitionAsync(partitions.ElementAt(i));

            }
        }

        // Returns all the partitions of the image
        public List<Partition> GetAllPartitions()
        {

            var partitions = new List<Partition>();

            fStream.Seek(512, SeekOrigin.Begin);
            var gptSig = new byte[8];
            fStream.ReadExactly(gptSig, 0, 8);

            if (Helpers.Helpers.ByteArrayEquals(gptSig, GPT_SIGNATURE))
            {
                _logger.LogInformation("GPT Signature found, parsing partitions...");

                // Read GUID header
                fStream.Seek(512, SeekOrigin.Begin);
                byte[] headerData = new byte[512];
                fStream.Read(headerData, 0, 512);

                var guidHeader = new GUIDHeader(headerData);
                int numOfPartitions = (int)guidHeader.NumOfPartitions;
                _logger.LogInformation($"Number of partitions found: {numOfPartitions}");

                fStream.Seek(1024, SeekOrigin.Begin);

                for (int i = 0; i < numOfPartitions; i++)
                {
                    byte[] entryBytes = new byte[128];
                    fStream.Read(entryBytes, 0, 128);

                    if (!Helpers.Helpers.IsEmptyByteArray(entryBytes))
                    {
                        byte[] partitionType = new byte[16];
                        Array.Copy(entryBytes, 0, partitionType, 0, 16);

                        string guidStr = new Guid(partitionType).ToString().ToUpper();
                        _logger.LogInformation($"The partition type is: {guidStr}");

                        if (guidStr == "CEF5A9AD-73BC-4601-89F3-CDEEEEE321A1")
                        {
                            _logger.LogInformation($"[+] Supported QNX6 Partition Detected, Appending");
                            partitions.Add(new Partition("GPT", entryBytes));
                        }
                        else if (guidStr == "83BD6B9D-7F41-11DC-BE0B-001560B84F0F")
                        {
                            _logger.LogInformation($"[X] FreeBSD Boot partition Detected, Skipping...");
                        }
                        else
                        {
                            _logger.LogInformation("[X] Unknown Partition");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Empty array of bytes");
                    }
                }
            }
            else
            {
                _logger.LogInformation("[+] MBR Detected");
                fStream.Seek(446, SeekOrigin.Begin);

                for (int i = 0; i < 4; i++)
                {
                    byte[] entryBytes = new byte[16];
                    fStream.Read(entryBytes, 0, 16);

                    if (!Helpers.Helpers.IsEmptyByteArray(entryBytes))
                    {
                        byte partitionType = entryBytes[4];

                        if (partitionType == 0x05 || partitionType == 0x0F)
                        {
                            _logger.LogInformation("[+] (EBR) Extended Boot Record Detected, Processing...");
                            var partition = new Partition("MBR", entryBytes);
                            _logger.LogInformation($"The sector where the ebr starts is {partition.GetStartLba()}");

                            ulong baseLba = partition.GetStartLba();
                            var ebr = ExtendedBootRecord.FromDisk(fStream, baseLba, baseLba);
                            var logicalPartitions = ebr.GetAllLogicalPartitions();
                            partitions.AddRange(logicalPartitions);
                        }
                        else if (partitionType == 0xB1 || partitionType == 0xB2 || partitionType == 0xB3 || partitionType == 0xB4)
                        {
                            _logger.LogInformation("[+] Supported QNX6 Partition Detected, Appending...");
                            partitions.Add(new Partition("MBR", entryBytes));
                        }
                        else if (partitionType == 0x4D || partitionType == 0x4E || partitionType == 0x4F)
                        {
                            Console.WriteLine("[X] Unsupported QNX4 Partition Detected, Exiting...");
                            return new List<Partition>();
                        }

                    }
                }
            }
            return partitions;
        }

        // Parse each partition individually
        public async Task<Partition> ParsePartitionAsync(Partition partition)
        {
            ulong startSector = partition.GetStartLba();
            ulong offsetIntoPartition = 16;
            long targetSector = (long)(startSector + offsetIntoPartition);
            long superblockOffset = targetSector * 512;
            fStream.Seek((long)superblockOffset, SeekOrigin.Begin);

            byte[] superblockData = new byte[SUPERBLOCK_SIZE];
            int bytesRead = await fStream.ReadAsync(superblockData, 0, SUPERBLOCK_SIZE);
            _logger.LogInformation($"Byte Offset: {superblockOffset} (0x{superblockOffset:X})");
            Superblock superblock = new Superblock(superblockData);

            _logger.LogInformation($"{superblock}");
            _logger.LogInformation($"INodes: {superblock.RootNodeInode}");
            _logger.LogInformation($"bitmap: {superblock.RootNodeBitmap}");
            _logger.LogInformation($"longfilename: {superblock.RootNodeLongFilename}");

            long superblockEndOffset = superblockOffset + SUPERBLOCK_SIZE;
            
            if (superblock.Magic != 0x68191122)
            {
                _logger.LogInformation("Not a QNX6 Partition");
                return null;
            }

            // parse superblock Inodes
            var inodes = await ParseInodes(superblock, superblock.RootNodeInode, superblockEndOffset);
            _logger.LogInformation($"Num of iNodes: {inodes.Count}");

            InodesMap = Helpers.Helpers.BuildINodeMap(inodes);


            // parse superblock longInodes
            var longNames = await ParseInodes(superblock, superblock.RootNodeLongFilename, superblockEndOffset);
            LongNamesMap = Helpers.Helpers.BuildINodeMap(longNames);

            // Get the directories
            var (directories, longDirs) = ParseDirEntries(superblock, inodes.Cast<INode>(), superblockEndOffset);
            DirMap = Helpers.Helpers.BuildDirMap(directories);

            // Build File list
            Files = new List<Models.File>();
            foreach ( var dir in directories)
            {
                if (dir.InodeNumber == 0)
                    continue;
                try
                {
                    if (InodesMap.TryGetValue((int)dir.InodeNumber, out var inode) && inode != null)
                    {
                        var file = new Models.File(dir, (INode)inode, fStream, (int)superblock.BlockSize, superblockEndOffset);
                        Files.Add(file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{ex}");
                }
            }

            LongFiles = new List<LongFile>();
            foreach (var dir in longDirs)
            {
                if (dir.InodeNumber == 0)
                    continue;

                try
                {
                    if (InodesMap.TryGetValue((int)dir.InodeNumber, out var inode) &&
                        LongNamesMap.TryGetValue((int)dir.LongFileInumber, out var longInode) &&
                        inode != null && longInode != null)
                    {
                        var longFile = new LongFile(dir, (INode)inode, (LongNameINode)longInode, fStream, (int)superblock.BlockSize, superblockEndOffset);
                        if (longFile.FileName.Length < 510)
                            LongFiles.Add(longFile);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{ex}");
                }

                // combine regular + files
                var fileMap = Files.ToDictionary(f => (uint)f.FileId, f => (IFile)f);
                var longFileMap = LongFiles.ToDictionary(f => (uint)f.FileId, f => (IFile)f);

                // Merge both dictionaries
                var combinedMap = fileMap.Concat(longFileMap)
                             .GroupBy(kv => kv.Key)
                             .ToDictionary(g => g.Key, g => g.First().Value);

                var combinedList = Files.Cast<IFile>().Concat(LongFiles.Cast<IFile>()).ToList();

                // Construct the files
                ConstructFiles(extractionFolder, combinedList, combinedMap);

            }
            return partition;
        } 

        private void ConstructFiles(string extractionPath, List<IFile> files, Dictionary<uint, IFile> fileMap)
        {
            var paths = new Dictionary<uint, string>();
            
            // build all relative paths
            foreach (var f in files)
            {
                string path = Helpers.Helpers.BuildPaths(fileMap, f);
                _logger.LogInformation($"Built the path: {path}");
                if (!string.IsNullOrEmpty(path))
                    paths[(uint)f.FileId] = path;
            }

            // Construct all files
            foreach (var kvp in paths)
            {
                uint fileId = kvp.Key;
                string relativePath = kvp.Value;
                var file = fileMap[fileId];

                string fullPath = Path.Combine(extractionPath, relativePath);

                try
                {
                    if (file.FileType == "directory")
                    {
                        Directory.CreateDirectory(fullPath);
                        continue;
                    }

                    // Ensure parent directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

                    // Try to set file timestamps (accessed, modified)
                    try
                    {
                        var modified = DateTimeOffset.FromUnixTimeSeconds(file.ModifiedTime).UtcDateTime;
                        var accessed = DateTimeOffset.FromUnixTimeSeconds(file.AccessedTime).UtcDateTime;
                        System.IO.File.SetLastWriteTimeUtc(Path.GetDirectoryName(fullPath)!, modified);
                        System.IO.File.SetLastAccessTimeUtc(Path.GetDirectoryName(fullPath)!, accessed);
                    }
                    catch (Exception tsEx)
                    {
                    }

                    // Write file contents
                    try
                    {
                        using var outStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);

                        foreach (var dataBlock in file.FileData)
                        {
                            if (dataBlock == null || dataBlock.SequenceEqual(new byte[] { 0x00 }) || dataBlock.Length == 0)
                                continue;

                            // If dataBlock represents block pointers (e.g., 0 or 0xFFFFFFFF), skip
                            if (BitConverter.ToUInt32(dataBlock, 0) == 0xFFFFFFFF)
                                continue;

                            outStream.Write(dataBlock, 0, dataBlock.Length);
                        }
                    }
                    catch (Exception writeEx)
                    {
                        _logger.LogError($"{writeEx}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{ex}");
                }
            }
        }

        private (List<DirEntry> Directories, List<LongDirEntry> LongDirectories) ParseDirEntries(Superblock superblock, IEnumerable<INode> inodes, long offset)
        {
            var dirEntries = new List<DirEntry>();
            var longDirEntries = new List<LongDirEntry>();
            uint blockSize = superblock.BlockSize;

            foreach (var inode in inodes)
            {
                if ((inode.Mode & 0xF000) != 0x4000)
                    continue;

                var pointers = new List<uint>(inode.BlockPointerArray);
                int currentLevels = inode.Levels;

                if (currentLevels > 0)
                {
                    var nextPointers = new List<uint>();
                    foreach (var ptr in pointers)
                    {
                        if (ptr == 0 || ptr == 0xFFFFFFFF)
                            continue;

                        long blockOffset = ptr * blockSize + offset;
                        fStream.Seek(blockOffset, SeekOrigin.Begin);

                        byte[] blockData = new byte[blockSize];
                        int bytesRead = fStream.Read(blockData, 0, (int)blockSize);

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

                // Iterate through each block pointer
                foreach (var ptr in pointers)
                {
                    if (ptr == 0 || ptr == 0xFFFFFFFF)
                        continue;

                    long blockOffset = ptr * blockSize + offset;
                    fStream.Seek(blockOffset, SeekOrigin.Begin);

                    byte[] blockData = new byte[blockSize];
                    int bytesRead = fStream.Read(blockData, 0, (int)blockSize);

                    // Each directory entry = 32 bytes
                    for (int i = 0; i < bytesRead; i += 32)
                    {
                        if (i + 32 > bytesRead)
                            break;

                        byte[] chunk = new byte[32];
                        Array.Copy(blockData, i, chunk, 0, 32);

                        // Skip empty entries (all zeros)
                        if (chunk.All(b => b == 0x00))
                            continue;

                        var entry = new DirEntry(chunk);

                        if (entry.Name == "." || entry.Name == "..")
                            continue;

                        if (entry.NameLength > 27)
                        {
                            // Long directory entry
                            var longEntry = new LongDirEntry(chunk)
                            {
                                ParentInode = inode.Index
                            };

                            if (LongNamesMap.ContainsKey((int)longEntry.LongFileInumber) && longEntry.Size <= 255)
                                longDirEntries.Add(longEntry);

                            continue;
                        }

                        // Normal directory entry
                        entry.ParentInode = inode.Index;
                        dirEntries.Add(entry);
                    }
                }
            }
            return (dirEntries, longDirEntries);
        }


        private async Task<List<IINode>> ParseInodes(Superblock superBlock, RootNode rootNode, long offset)
        {
            _logger.LogInformation("[+] Parsing iNodes...");

            int inodeSize = 128;
            uint blockSize = superBlock.BlockSize;
            var root = rootNode;

            int currentLevel = root.Levels;
            var pointers = new List<uint>(root.PointerArray);

            while (currentLevel > 0)
            {
                var nextPointers = new List<uint>();

                foreach (var ptr in pointers)
                {
                    long blockOffset = ptr * blockSize + offset;
                    fStream.Seek(blockOffset, SeekOrigin.Begin);

                    byte[] blockData = new byte[blockSize];
                    int bytesRead = fStream.Read(blockData, 0, (int)blockSize);

                    for (int i = 0; i < bytesRead; i += 4)
                    {
                        if (i + 4 > bytesRead)
                            break;

                        uint p = BitConverter.ToUInt32(blockData, i);
                        nextPointers.Add(p);
                    }
                }

                pointers = nextPointers;
                currentLevel--;
            }

            var inodes = new List<IINode>();
            int inodeIndex = 0;

            if (ReferenceEquals(root, superBlock.RootNodeInode))
                inodeIndex = 1;
            else if (ReferenceEquals(root, superBlock.RootNodeLongFilename))
                inodeIndex = 0;

            _logger.LogInformation($"Amount of pointers in the array are {pointers.Count}");

            int counter = 0;

            foreach (var ptr in pointers)
            {
                long blockOffset = ptr * blockSize + offset;
                fStream.Seek(blockOffset, SeekOrigin.Begin);

                byte[] blockData = new byte[blockSize];
                int bytesRead = fStream.Read(blockData, 0, (int)blockSize);

                for (int i = 0; i < bytesRead; i += inodeSize)
                {
                    if (i + inodeSize > bytesRead)
                        break;

                    byte[] chunk = new byte[inodeSize];
                    Array.Copy(blockData, i, chunk, 0, inodeSize);

                    bool isEmpty = chunk.All(b => b == 0x00);
                    if (!isEmpty)
                    {
                        if (ReferenceEquals(root, superBlock.RootNodeLongFilename))
                        {
                            byte[] longData = new byte[512];
                            Array.Copy(blockData, i, longData, 0, Math.Min(512, bytesRead - i));
                            var longInode = new LongNameINode(longData)
                            {
                                Index = inodeIndex
                            };
                            inodes.Add(longInode);
                            inodeIndex++;
                            break;
                        }
                        else if (ReferenceEquals(root, superBlock.RootNodeInode))
                        {
                            var inodeObj = new INode(chunk)
                            {
                                Index = inodeIndex
                            };
                            if (inodeObj.Status != 1 && inodeObj.Status != 2 && inodeObj.Status != 3)
                            {
                                inodeIndex++;
                                continue;
                            }
                            inodes.Add(inodeObj);
                            inodeIndex++;
                        }
                        else
                        {
                            inodeIndex++;
                        }
                    }
                    else
                    {
                        inodeIndex++;
                    }
                }
            }

            _logger.LogInformation($"Amount of inodes indexed={inodeIndex} and amount of inodes in superblock={superBlock.NumOfInodes}");
            _logger.LogInformation($"Amount of pointers invalid are {counter}");

            return inodes;

        } 
    }
}
