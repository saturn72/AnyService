using System;
using AnyService.Events;
using AnyService.Logging;
using AnyService.Services.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AnyService.Tests.Services.Logging
{
    public class ExceptionsLoggingEventHandlersTests
    {
        [Fact]
        public void AllHandlersReturnsOnMissingInfo()
        {
            var logger = new Mock<ILogger<ExceptionsLoggingEventHandlers>>();
            var sp = MockServiceProvider(logger);
            var h = new ExceptionsLoggingEventHandlers(sp.Object);

            var e = new DomainEvent();
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
            var logger = new Mock<ILogger<ExceptionsLoggingEventHandlers>>();
            var sp = MockServiceProvider(logger);


            var h = new ExceptionsLoggingEventHandlers(sp.Object);

            var expEx = new Exception();
            var e = new DomainEvent
            {
                Data = new
                {
                    exception = expEx,
                    logRecord = new LogRecord { TraceId = "some-ex-id" },
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

        private Mock<IServiceProvider> MockServiceProvider(Mock<ILogger<ExceptionsLoggingEventHandlers>> logger)
        {
            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(ILogger<ExceptionsLoggingEventHandlers>))).Returns(logger.Object);
            var serviceScope = new Mock<IServiceScope>();
            serviceScope.Setup(x => x.ServiceProvider).Returns(sp.Object);

            var serviceScopeFactory = new Mock<IServiceScopeFactory>();
            serviceScopeFactory
                .Setup(x => x.CreateScope())
                .Returns(serviceScope.Object);

            sp.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                .Returns(serviceScopeFactory.Object);

            return sp;
        }
    }
}