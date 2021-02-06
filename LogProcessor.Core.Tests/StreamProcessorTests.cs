using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LogProcessor.Common;

namespace LogProcessor.Core.Tests
{
    internal class StreamProcessorBuilder
    {
        private IPersistEventDetails persistEventDetails;
        private Configuration configuration;

        internal StreamProcessorBuilder With(IPersistEventDetails persistEventDetails)
        {
            this.persistEventDetails = persistEventDetails;
            return this;
        }

        internal StreamProcessorBuilder With(Configuration configuration)
        {
            this.configuration = configuration;
            return this;
        }

        internal StreamProcessor Build()
        {
            return new StreamProcessor(
                persistEventDetails ?? Mock.Of<IPersistEventDetails>(),
                configuration ?? new Configuration()
                );
        }

        public static StreamProcessorBuilder New() => new StreamProcessorBuilder();
    }

    public class StreamProcessorTests
    {
        [Fact]
        public async Task Process_WhenFedWithEmptyStream_WhenShouldNotInsert()
        {
            var persistenceMock = new Mock<IPersistEventDetails>();
            var sut = StreamProcessorBuilder.New().With(persistenceMock.Object).Build();

            await sut.Process(new MemoryStream());

            persistenceMock.Verify(x => x.Persist(It.IsAny<LogEventDetails[]>()), Times.Never);
        }

        [Fact]
        public async Task Process_WhenFedWithInvalidJson_WhenShouldNotInsert()
        {
            var persistenceMock = new Mock<IPersistEventDetails>();
            var sut = StreamProcessorBuilder.New().With(persistenceMock.Object).Build();
            var input = new StreamBuilder().AppendText("invalid").Build();

            await sut.Process(input);

            persistenceMock.Verify(x => x.Persist(It.IsAny<LogEventDetails[]>()), Times.Never);
        }

        [Fact]
        public async Task Process_WhenFedWithNullStream_ThenShouldNotInsert()
        {
            var persistenceMock = new Mock<IPersistEventDetails>();
            var sut = StreamProcessorBuilder.New().With(persistenceMock.Object).Build();

            await sut.Process(null);

            persistenceMock.Verify(x => x.Persist(It.IsAny<LogEventDetails[]>()), Times.Never);
        }


        public static IEnumerable<object[]> NonMatchingEvents =>
            new List<object[]>
            {
                    new object[] { LogEventGenerator.CreateStartedEvent() },
                    new object[] { LogEventGenerator.CreateFinishedEvent() },
            };

        [Theory]
        [MemberData(nameof(NonMatchingEvents))]
        public async Task Process_WhenFedWithNonMatchingStartingEvent_ThenShouldNotInsert(LogEvent evt)
        {
            var persistenceMock = new Mock<IPersistEventDetails>();
            var sut = StreamProcessorBuilder.New().With(persistenceMock.Object).Build();
            var input = new StreamBuilder().AppendEvent(evt).Build();

            await sut.Process(null);

            persistenceMock.Verify(x => x.Persist(It.IsAny<LogEventDetails[]>()), Times.Never);
        }

        public static IEnumerable<object[]> MatchingEvents
        {
            get
            {
                var id = Guid.NewGuid();
                return new List<object[]>
                {
                        new object[] { LogEventGenerator.CreateStartedEvent(id), LogEventGenerator.CreateFinishedEvent(id) },
                        new object[] { LogEventGenerator.CreateFinishedEvent(id), LogEventGenerator.CreateStartedEvent(id) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MatchingEvents))]
        public async Task Process_WhenFedWithMatchingEvents_ThenShouldInsert(LogEvent one, LogEvent two)
        {
            var persistenceMock = new Mock<IPersistEventDetails>();
            var sut = StreamProcessorBuilder.New().With(persistenceMock.Object).Build();
            var input = new StreamBuilder().AppendEvent(one).AppendEvent(two).Build();

            await sut.Process(input);

            persistenceMock.Verify(x => x.Persist(It.Is<LogEventDetails[]>(
                x => x.Single().Id == one.id
                && Math.Abs(x.Single().Duration) == Math.Abs(one.timestamp - two.timestamp)
                )));
        }


        [Fact]
        public async Task Process_WhenFedWithMultipleEvents_ThenShouldInsertInBatches()
        {
            var persistenceMock = new Mock<IPersistEventDetails>();
            var configuration = new Configuration { InsertBatchSize = 3 };
            var sut = StreamProcessorBuilder.New()
                .With(persistenceMock.Object)
                .With(configuration)
                .Build();
            var streamBuilder = new StreamBuilder();
            var logEvents = CreateEventBatch().ToArray();
            foreach (var item in logEvents) streamBuilder.AppendEvent(item);
            var input = streamBuilder.Build();

            await sut.Process(input);

            persistenceMock.Verify(x => x.Persist(It.Is<LogEventDetails[]>(
                x => x.Length == configuration.InsertBatchSize
                )), Times.Exactly(logEvents.Count(x => x.state == EventStatus.FINISHED) / configuration.InsertBatchSize));


            persistenceMock.Verify(x => x.Persist(It.Is<LogEventDetails[]>(
                x => x.Length == logEvents.Count(x => x.state == EventStatus.FINISHED) % configuration.InsertBatchSize
                )), Times.Once);
        }

        private IEnumerable<LogEvent> CreateEventBatch()
        {
            for (var idx = 0; idx < 7; idx++)
            {
                var id = Guid.NewGuid();
                yield return LogEventGenerator.CreateStartedEvent(id);
                yield return LogEventGenerator.CreateFinishedEvent(id);
            }
        }
    }

}
