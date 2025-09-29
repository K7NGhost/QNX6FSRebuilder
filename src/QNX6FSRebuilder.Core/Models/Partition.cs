using System;
using System.Collections.Generic;
using System.Text;

namespace QNX6FSRebuilder.Core.Models
{
    public class Partition
    {
        public string Scheme { get; private set; }
        private object partition;  // Holds either MBRPartition or GPTPartition

        public Partition(string scheme, byte[] entryBytes)
        {
            Scheme = scheme.ToUpper();

            if (Scheme == "MBR")
            {
                partition = new MBRPartition(entryBytes);
            }
            else if (Scheme == "GPT")
            {
                partition = new GPTPartition(entryBytes);
            }
            else
            {
                throw new ArgumentException($"Unsupported partition scheme: {scheme}");
            }
        }

        public override string ToString()
        {
            return partition.ToString();
        }

        public ulong GetStartLba()
        {
            if (Scheme == "MBR")
            {
                return ((MBRPartition)partition).StartLba;
            }
            else if (Scheme == "GPT")
            {
                return ((GPTPartition)partition).FirstLba;
            }
            throw new InvalidOperationException("Unsupported partition scheme.");
        }

        public object GetPartitionType()
        {
            if (Scheme == "MBR")
            {
                return ((MBRPartition)partition).PartitionType;
            }
            else if (Scheme == "GPT")
            {
                return ((GPTPartition)partition).PartitionTypeGuid;
            }
            throw new InvalidOperationException("Unsupported partition scheme.");
        }

        public void SetStartLba(ulong value)
        {
            if (Scheme == "MBR")
            {
                ((MBRPartition)partition).SetStartLba(value);
            }
            else if (Scheme == "GPT")
            {
                ((GPTPartition)partition).SetStartLba(value);
            }
            else
            {
                throw new InvalidOperationException("Unsupported partition scheme.");
            }
        }
    }
}
