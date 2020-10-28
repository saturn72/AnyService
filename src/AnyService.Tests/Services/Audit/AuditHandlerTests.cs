using AnyService.Events;
using AnyService.Services;
using AnyService.Services.Audit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Tests.Services.Audit
{
    public class AuditHandlerTests
    {
        public class EventDataObject : IEntity
        {
            public string Id { get; set; }
            public string Value { get; set; }
        }

        [Fact]
        public async Task CreatedHandler()
        {
            var logger = new Mock<ILogger<AuditHandler>>();
            var sp = MockServiceProvider(logger);
            var am = new Mock<IAuditManager>();
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);
            var h = new AuditHandler(sp.Object);

            var o = new EventDataObject
            {
                Id = "123",
                Value = "data"
            };

            var uId = "u-id";
            var wc = new WorkContext { CurrentUserId = uId };

            var ded = new DomainEvent
            {
                Data = o,
                PerformedByUserId = uId,
                WorkContext = wc
            };
            await h.CreateEventHandler(ded);
            am.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(t => t == typeof(EventDataObject)),
                It.Is<string>(i => i == o.Id),
                It.Is<string>(art => art == AuditRecordTypes.CREATE),
                It.Is<WorkContext>(w => w == wc),
                It.Is<object>(obj => obj == o)),
                Times.Once);
        }
        [Fact]
        public async Task ReadHandler()
        {
            var logger = new Mock<ILogger<AuditHandler>>();
            var sp = MockServiceProvider(logger);
            var am = new Mock<IAuditManager>();
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);
            var h = new AuditHandler(sp.Object);

            var o = new EventDataObject
            {
                Id = "123",
                Value = "data"
            };

            var uId = "u-id";
            var wc = new WorkContext { CurrentUserId = uId };

            var ded = new DomainEvent
            {
                Data = o,
                PerformedByUserId = uId,
                WorkContext = wc
            };
            await h.ReadEventHandler(ded);
            am.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(t => t == typeof(EventDataObject)),
                It.Is<string>(i => i == o.Id),
                It.Is<string>(art => art == AuditRecordTypes.READ),
                It.Is<WorkContext>(w => w == wc),
                It.Is<object>(obj => obj == o)),
                Times.Once);
        }
        [Fact]
        public async Task ReadHandler_Pagination()
        {
            var logger = new Mock<ILogger<AuditHandler>>();
            var sp = MockServiceProvider(logger);
            var am = new Mock<IAuditManager>();
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);
            var h = new AuditHandler(sp.Object);

            var o = new Pagination<string>
            {
                Data = new[] { "1", "2", "3" }
            };

            var uId = "u-id";
            var wc = new WorkContext { CurrentUserId = uId };

            var ded = new DomainEvent
            {
                Data = o,
                PerformedByUserId = uId,
                WorkContext = wc
            };
            await h.ReadEventHandler(ded);
            am.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(t => t == typeof(string)),
                It.Is<string>(i => i == null),
                It.Is<string>(art => art == AuditRecordTypes.READ),
                It.Is<WorkContext>(w => w == wc),
                It.Is<object>(obj => obj == o)),
                Times.Once);
        }
        [Fact]
        public async Task UpdateHandler_DomainEntity()
        {
            var logger = new Mock<ILogger<AuditHandler>>();
            var sp = MockServiceProvider(logger);
            var am = new Mock<IAuditManager>();
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);
            var h = new AuditHandler(sp.Object);

            var before = new EventDataObject
            {
                Id = "123",
                Value = "data"
            };

            var uId = "u-id";
            var wc = new WorkContext { CurrentUserId = uId };

            var ded = new DomainEvent
            {
                Data = before,
                PerformedByUserId = uId,
                WorkContext = wc
            };
            await h.UpdateEventHandler(ded);
            am.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(t => t == typeof(EventDataObject)),
                It.Is<string>(i => i == before.Id),
                It.Is<string>(art => art == AuditRecordTypes.UPDATE),
                It.Is<WorkContext>(w => w == wc),
                It.Is<object>(obj => obj == before)),
                Times.Once);
        }
        [Fact]
        public async Task UpdateHandler_AnonymousObject()
        {
            var logger = new Mock<ILogger<AuditHandler>>();
            var sp = MockServiceProvider(logger);
            var am = new Mock<IAuditManager>();
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);
            var h = new AuditHandler(sp.Object);

            var o = new
            {
                Id = "123",
                Value = "data"
            };

            var uId = "u-id";
            var wc = new WorkContext { CurrentUserId = uId };

            var ded = new DomainEvent
            {
                Data = o,
                PerformedByUserId = uId,
                WorkContext = wc
            };
            await h.UpdateEventHandler(ded);
            am.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(t => t == o.GetType()),
                It.Is<string>(i => i == null),
                It.Is<string>(art => art == AuditRecordTypes.UPDATE),
                It.Is<WorkContext>(w => w == wc),
                It.Is<object>(obj => obj == o)),
                Times.Once);
        }
        [Fact]
        public async Task UpdateHandler_EntityUpdatedEventData()
        {
            var logger = new Mock<ILogger<AuditHandler>>();
            var sp = MockServiceProvider(logger);
            var am = new Mock<IAuditManager>();
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);
            var h = new AuditHandler(sp.Object);

            EventDataObject before = new EventDataObject
            {
                Id = "123",
                Value = "data"
            },
            after = new EventDataObject
            {
                Id = "123",
                Value = "data2"
            };

            var uId = "u-id";
            var wc = new WorkContext { CurrentUserId = uId };

            var ded = new DomainEvent
            {
                Data = new EntityUpdatedDomainEvent(before, after),
                PerformedByUserId = uId,
                WorkContext = wc
            };
            await h.UpdateEventHandler(ded);
            am.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(t => t == typeof(EventDataObject)),
                It.Is<string>(i => i == before.Id),
                It.Is<string>(art => art == AuditRecordTypes.UPDATE),
                It.Is<WorkContext>(w => w == wc),
                It.Is<object>(obj => VerifyPayload(obj, before, after))),
                Times.Once);
        }

        private bool VerifyPayload(object obj, EventDataObject before, EventDataObject after)
        {
            var o = (obj as EntityUpdatedDomainEvent).Data as EntityUpdatedDomainEvent.EntityUpdatedEventData;
            return o.Before == before && o.After == after;
        }

        [Fact]
        public async Task DeleteHandler()
        {
            var logger = new Mock<ILogger<AuditHandler>>();
            var sp = MockServiceProvider(logger);
            var am = new Mock<IAuditManager>();
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);
            var h = new AuditHandler(sp.Object);

            var o = new EventDataObject
            {
                Id = "123",
                Value = "data"
            };

            var uId = "u-id";
            var wc = new WorkContext { CurrentUserId = uId };

            var ded = new DomainEvent
            {
                Data = o,
                PerformedByUserId = uId,
                WorkContext = wc
            };
            await h.DeleteEventHandler(ded);
            am.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(t => t == typeof(EventDataObject)),
                It.Is<string>(i => i == o.Id),
                It.Is<string>(art => art == AuditRecordTypes.DELETE),
                It.Is<WorkContext>(w => w == wc),
                It.Is<object>(obj => obj == o)),
                Times.Once);
        }
        private Mock<IServiceProvider> MockServiceProvider(Mock<ILogger<AuditHandler>> logger)
        {
            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(ILogger<AuditHandler>))).Returns(logger.Object);

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
