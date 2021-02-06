using LiteDB;
using LogProcessor.Common;
using Serilog;
using System;
using System.Threading.Tasks;

namespace LogProcessor.DataAccess
{
    public class LiteDbPersistence : IDisposable, IPersistEventDetails
    {
        public LiteDbPersistence(Configuration configuration)
        {
            Initialize(configuration);
        }

        private void Initialize(Configuration configuration)
        {
            var dbFile =  $"{DateTime.UtcNow.ToString("HHmmss") }_{configuration.DbFileName}";
            db = new LiteDatabase($"FileName={dbFile}; Journal=False; Mode=Exclusive; InitialSize=50MB; Async=True");
            Log.Logger.Information("DbLite initialized with " + dbFile);
        }

        private bool disposedValue;
        private LiteDatabase db;
        private ILiteCollection<LogEventDetails> collection;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    db?.Dispose();
                }

                db = null;
                disposedValue = true;
            }
        }

        ~LiteDbPersistence()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public Task Persist(LogEventDetails[] eventBatch)
        {
            var collection = GetCollection();
            collection.InsertBulk(eventBatch);
            Log.Logger.Debug($"{eventBatch.Length} events written to {collection.Name}");
            return Task.CompletedTask;
        }

        private ILiteCollection<LogEventDetails> GetCollection()
        {
            if (this.collection == null)
                this.collection = db.GetCollection<LogEventDetails>();

            return collection;
        }
    }
}
