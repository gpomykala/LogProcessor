using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;
using System.Linq;
using Serilog;
using System.Text.Json;
using LogProcessor.Common;

namespace LogProcessor.Core
{
    public class StreamProcessor
    {

        private readonly ConcurrentDictionary<string, LogEvent> startedEvents = new ConcurrentDictionary<string, LogEvent>();
        private readonly ConcurrentDictionary<string, LogEvent> finishedEvents = new ConcurrentDictionary<string, LogEvent>();
        private readonly IPersistEventDetails eventPersistence;
        private readonly Configuration configuration;

        public StreamProcessor(IPersistEventDetails eventPersistence, Configuration configuration)
        {
            this.eventPersistence = eventPersistence;
            this.configuration = configuration;
        }

        public async Task Process(Stream inputStream)
        {
            if (inputStream == null) return;

            var pipeline = BuildPipeline();
            var pipelineEntry = pipeline.Item1;
            var pipelineEnd = pipeline.Item2;
            using var textReader = new StreamReader(inputStream);
            string currentLine = null;
            while ((currentLine = await textReader.ReadLineAsync()) != null)
            {
                pipelineEntry.Post(currentLine);
            }
            pipelineEntry.Complete();

            await pipelineEnd.Completion;
        }

        private (ITargetBlock<string>, IDataflowBlock) BuildPipeline()
        {
            var dataFlowOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = configuration.MaxDegreeOfParallelism
            };
            TransformBlock<string, LogEvent> parseJson = new TransformBlock<string, LogEvent>(input =>
            {
                return ParseLogEvent(input);
            }, dataFlowOptions);

            var matchEvents = new TransformBlock<LogEvent, LogEventDetails>(input =>
            {
                return ProcessLogEvent(input);
            }, dataFlowOptions);
            var batchEventDetails = new BatchBlock<LogEventDetails>(configuration.InsertBatchSize);
            var insertEventDetails = new ActionBlock<LogEventDetails[]>(async chunk =>
            {
                await eventPersistence.Persist(chunk);
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = configuration.MaxDegreeOfParallelism * 4 });
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            parseJson.LinkTo(matchEvents, linkOptions, input => input != null);
            // drain null values
            parseJson.LinkTo(DataflowBlock.NullTarget<LogEvent>(), linkOptions);
            matchEvents.LinkTo(batchEventDetails, linkOptions, input => input != null);
            // drain null values
            matchEvents.LinkTo(DataflowBlock.NullTarget<LogEventDetails>(), linkOptions);
            batchEventDetails.LinkTo(insertEventDetails, linkOptions);
            // When the batch block completes, set the action block also to complete.
            batchEventDetails.Completion.ContinueWith(delegate { insertEventDetails.Complete(); });
            return (parseJson, insertEventDetails);
        }

        private LogEventDetails ProcessLogEvent(LogEvent input)
        {
            var matchingEvent = GetMatchingEvent(input);
            if (matchingEvent != null)
            {
                return CreateEventDetails(input, matchingEvent);
            }
            CacheEvent(input);
            return null;
        }

        private static LogEvent ParseLogEvent(string input)
        {
            try
            {
                return ObjectSerializer.Deserialize<LogEvent>(input);
            }
            catch (JsonException ex)
            {
                Log.Logger.Error($"Unable to parse {input} as {typeof(LogEvent).Name}", ex);
                return null;
            }
        }

        private LogEventDetails CreateEventDetails(params LogEvent[] input)
        {
            var startEvent = input.Single(x => x.state == EventStatus.STARTED);
            var endEvent = input.Single(x => x.state == EventStatus.FINISHED);
            var duration = endEvent.timestamp - startEvent.timestamp;
            var eventDetails = new LogEventDetails(startEvent.id, duration, startEvent.type ?? endEvent.type, startEvent.host ?? endEvent.host);
            if (duration < 0)
            {
                Log.Logger.Warning($"Invalid duration ({duration}) on {startEvent.id}. Started at {startEvent.timestamp}, finished at {endEvent.timestamp}");
            }
            return eventDetails;
        }

        private void CacheEvent(LogEvent input)
        {
            switch (input.state)
            {
                case EventStatus.FINISHED:
                    finishedEvents.TryAdd(input.id, input);
                    return;
                case EventStatus.STARTED:
                    startedEvents.TryAdd(input.id, input);
                    return;
                default:
                    return;
            }
        }

        private LogEvent GetMatchingEvent(LogEvent input)
        {
            switch (input.state)
            {
                case EventStatus.FINISHED:
                    startedEvents.TryRemove(input.id, out var startedEvent);
                    return startedEvent;
                case EventStatus.STARTED:
                    finishedEvents.TryRemove(input.id, out var finishedEvent);
                    return finishedEvent;
                default:
                    return null;
            }
        }
    }
}
