using QNX6FSRebuilder.Core.Interfaces;
using QNX6FSRebuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QNX6FSRebuilder.Core.Helpers
{
    public static class Helpers
    {
        public static bool ByteArrayEquals(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length)
                return false;
            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i])
                    return false;
            }
            return true;
        }

        public static bool IsEmptyByteArray(byte[] array)
        {
            foreach (var b in array)
            {
                if (b != 0)
                    return false;
            }
            return true;
        }

        public static Dictionary<int, IINode> BuildINodeMap(IEnumerable<IINode> inodes)
        {
            return inodes.ToDictionary(i => i.Index, i => i);
        }

        public static Dictionary<uint, List<DirEntry>> BuildDirMap(IEnumerable<DirEntry> dirEntries)
        {
            var dirMap = new Dictionary<uint, List<DirEntry>>();

            foreach(var entry in dirEntries)
            {
                if (!dirMap.TryGetValue(entry.InodeNumber, out var list))
                {
                    list = new List<DirEntry>();
                    dirMap[entry.InodeNumber] = list;
                }

                list.Add(entry);
            }

            return dirMap;
        }

        internal static Dictionary<int, LongNameINode> BuildINodeMap(IEnumerable<LongNameINode> longNames)
        {
            return longNames.ToDictionary(i => i.Index, i => i);
        }

        public static string BuildPaths(Dictionary<uint, IFile> fileMap, IFile fileObj)
        {
            var parts = new List<string> { fileObj.FileName };
            var visited = new HashSet<uint>();

            if (!fileMap.TryGetValue((uint)fileObj.ParentId, out var current))
                return string.Join(Path.DirectorySeparatorChar, parts);

            while (current != null)
            {
                if (visited.Contains((uint)current.FileId))
                {
                    throw new InvalidOperationException("Circular reference detected");
                }

                visited.Add((uint)current.FileId);

                if (!ReferenceEquals(current, fileObj))
                {
                    parts.Insert(0, current.FileName);

                    // If parent ID is missing, we've reached the root
                    if (!fileMap.ContainsKey((uint)current.ParentId))
                        break;
                }

                fileMap.TryGetValue((uint)current.ParentId, out current);
            }

            return Path.Combine(parts.ToArray());
        }
    }
}
