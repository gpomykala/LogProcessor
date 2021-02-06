using LogProcessor.Common;
using LogProcessor.Core;
using LogProcessor.DataAccess;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace LogProcessor.Console
{
    class Program
    {
        private static Configuration configuration;

        static Program()
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

            configuration = BuildConfiguration();
        }

        public async static Task Main(string[] args)
        {
            if (args.Length != 1)
            {
                System.Console.WriteLine($"Usage: {Assembly.GetExecutingAssembly().GetName()} log_file_path");
                return;
            }
            var filePath = args[0];
            if (!File.Exists(filePath))
            {
                System.Console.WriteLine($"File not found at {filePath}");
                return;
            }
            using var liteDbPersister = new LiteDbPersistence(configuration);
            var streamProcessor = new StreamProcessor(liteDbPersister, configuration);
            using var fileStream = File.OpenRead(filePath);
            var watch = Stopwatch.StartNew();
            await streamProcessor.Process(fileStream);
            Log.Logger.Information($"{Path.GetFileName(filePath)} processed in {watch.Elapsed}");
        }

        private static Configuration BuildConfiguration()
        {
            IConfiguration configuration = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
              .Build();
            return new Configuration
            {
                InsertBatchSize = int.Parse(configuration[nameof(Configuration.InsertBatchSize)]),
                MaxDegreeOfParallelism = int.Parse(configuration[nameof(Configuration.MaxDegreeOfParallelism)]),
                DbFileName = configuration[nameof(Configuration.DbFileName)]
            };
        }
    }
}
