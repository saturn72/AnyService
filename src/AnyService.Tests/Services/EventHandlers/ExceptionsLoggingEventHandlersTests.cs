using System;
using AnyService.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AnyService.Tests.Services.EventHandlers
{
    public class ExceptionsLoggingEventHandlersTests
    {
        [Fact]
        public void AllHandlersReturnsOnNonException()
        {
            var logger = new Mock<ILogger>();
            var h = new ExceptionsLoggingEventHandlers(logger.Object);

            var e = new DomainEventData();
            h.CreateEventHandler(e);
            h.ReadEventHandler(e);
            h.UpdateEventHandler(e);
            h.DeleteEventHandler(e);
            logger.Verify(l => l.Log(
                    It.Is<LogLevel>(level => level == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.IsAny<object>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<object, Exception, string>>()),
                Times.Never);
        }

        [Fact]
        public void AllHandlersLogErrorOnException()
        {
            var logger = new Mock<ILogger>();
            var h = new ExceptionsLoggingEventHandlers(logger.Object);

            var expEx = new Exception();
            var e = new DomainEventData
            {
                Data = new
                {
                    exceptionId = "some-ex-id",
                    exception = expEx,
                }
            };
            h.CreateEventHandler(e);
            VerifyLogger();
            h.ReadEventHandler(e);
            VerifyLogger();
            h.UpdateEventHandler(e);
            VerifyLogger();
            h.DeleteEventHandler(e);
            VerifyLogger();

            void VerifyLogger()
            {
                logger.Verify(l => l.Log(
                        It.Is<LogLevel>(level => level == LogLevel.Error),
                        It.IsAny<EventId>(),
                        It.IsAny<It.IsAnyType>(),
                        It.IsAny<Exception>(),
                         (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                    Times.Once);
                logger.Reset();
            }
        }
    }
}