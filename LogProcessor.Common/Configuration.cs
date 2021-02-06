using System;
using System.Collections.Generic;
using System.Text;

namespace LogProcessor.Common
{
    public class Configuration
    {
        public int InsertBatchSize { get; set; }
        public int MaxDegreeOfParallelism { get; set; }
        public string DbFileName { get; set; }
    }
}
