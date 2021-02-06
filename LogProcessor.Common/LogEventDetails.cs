using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LogProcessor.Common
{
    [DebuggerDisplay("{Id} {Duration}")]
    public class LogEventDetails
    {
        public LogEventDetails(string id, long duration, string type, string host)
        {
            Id = id;
            Duration = duration;
            Type = type;
            Host = host;
        }

        public string Id;
        public long Duration;
        public string Type;
        public string Host;
        public bool Alert => Duration > 4;
    }
}
