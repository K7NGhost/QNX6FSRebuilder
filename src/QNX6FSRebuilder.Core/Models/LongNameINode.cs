using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace QNX6FSRebuilder.Core.Models
{
    internal class LongNameINode
    {
        public int? Index { get; set; }
        public ushort NameLength { get; private set; }
        public string Name { get; private set; }

        public LongNameINode(byte[] data)
        {
            if (data.Length < 2)
                throw new ArgumentException("Not enough data to parse LongNameiNode");

            // Read first 2 bytes as unsigned short (little endian)
            NameLength = BitConverter.ToUInt16(data, 0);

            // Ensure we don’t exceed available data
            int maxLength = Math.Min(data.Length - 2, NameLength);
            byte[] nameBytes = new byte[maxLength];
            Array.Copy(data, 2, nameBytes, 0, maxLength);

            // Decode UTF-8 and sanitize filename
            string decoded = Encoding.UTF8.GetString(nameBytes);
            Name = SanitizeFilename(decoded);
        }

        private string SanitizeFilename(string name)
        {
            // Remove null characters
            name = name.Replace("\x00", "");

            // Replace Windows-invalid characters
            name = Regex.Replace(name, @"[<>:""/\\|?*\x00-\x1F]", "_");

            // Replace other nonstandard characters
            name = Regex.Replace(name, @"[^\w\s.\-()]", "_");

            // Trim trailing dots, underscores, or spaces
            name = name.Trim(' ', '.', '_');

            // Truncate very long filenames
            if (name.Length > 100)
                name = name.Substring(0, 100) + $"_{Index}";

            // Handle unnamed or placeholder names
            if (string.IsNullOrWhiteSpace(name) || Regex.IsMatch(name, @"^_+$"))
                name = $"unnamed_{Index}";

            return name;
        }

        public override string ToString()
        {
            return $"<LongNameiNode name_length={NameLength} file_name='{Name}'>";
        }
    }
}
