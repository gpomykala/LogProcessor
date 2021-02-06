using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LogProcessor.Common
{
    public interface IPersistEventDetails
    {
        Task Persist(LogEventDetails[] eventBatch);
    }
}
