using System;
using System.Collections.Generic;
using System.Text;

namespace LogProcessor.Common
{
    public class Configuration
    {
        public int InsertBatchSize { get; set; } = 5000;
        public int MaxDegreeOfParallelism { get; set; } = 1;
        public string DbFileName { get; set; }
    }
}
