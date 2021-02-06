using AutoFixture;
using LogProcessor.Common;
using System;

namespace LogProcessor.Core.Tests
{
    internal static class LogEventGenerator
    {
        static Fixture fix = new Fixture();
        internal static LogEvent CreateStartedEvent()
        {
            var item = fix.Create<LogEvent>();
            item.state = EventStatus.STARTED;
            return item;
        }

        internal static LogEvent CreateFinishedEvent()
        {
            var item = fix.Create<LogEvent>();
            item.state = EventStatus.FINISHED;
            return item;
        }

        internal static LogEvent CreateStartedEvent(Guid id)
        {
            var item = CreateStartedEvent();
            item.id = id.ToString();
            return item;
        }
        internal static LogEvent CreateFinishedEvent(Guid id)
        {
            var item = CreateFinishedEvent();
            item.id = id.ToString();
            return item;
        }
    }
}
