using LogProcessor.Common;
using LogProcessor.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LogProcessor.Console
{
    public class FakePersister : IPersistEventDetails
    {
        public Task Persist(LogEventDetails[] eventBatch)
        {
            Log.Logger.Debug($"{eventBatch.Length} events persisted");
            return Task.CompletedTask;
        }
    }
}
