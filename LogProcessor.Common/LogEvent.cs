using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LogProcessor.Common
{
    public enum EventStatus
    {
        None,
        STARTED,
        FINISHED
    }
    [DebuggerDisplay("{id} {state}")]
    public class LogEvent : IEquatable<LogEvent>
    {
        public string id { get; set; }
        public EventStatus state { get; set; }
        public string type { get; set; }
        public string host { get; set; }
        public long timestamp { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as LogEvent);
        }

        public bool Equals(LogEvent other)
        {
            return other != null &&
                   id == other.id &&
                   state == other.state;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(id, state);
        }
    }
}
