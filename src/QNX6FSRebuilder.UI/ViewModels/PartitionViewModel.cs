using QNX6FSRebuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace QNX6FSRebuilder.UI.ViewModels
{
    public class PartitionViewModel
    {
        public int Index { get; set; }
        public string DisplayName => $"Partition {Index + 1}";
        public Partition Partition { get; set; }
    }
}
