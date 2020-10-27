using AnyService.Events;
using AnyService.Services;
using AnyService.Services.Audit;
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
            var sp = new Mock<IServiceProvider>();
            var logger = new Mock<ILogger<AuditHandler>>();
            sp.Setup(s => s.GetService(typeof(ILogger<AuditHandler>))).Returns(logger.Object);
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
                It.Is<object>(obj => obj == o)),
                Times.Once);
        }
        [Fact]
        public async Task ReadHandler()
        {
            var sp = new Mock<IServiceProvider>();
            var logger = new Mock<ILogger<AuditHandler>>();
            sp.Setup(s => s.GetService(typeof(ILogger<AuditHandler>))).Returns(logger.Object);
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
                It.Is<object>(obj => obj == o)),
                Times.Once);
        }
        [Fact]
        public async Task ReadHandler_Pagination()
        {
            var sp = new Mock<IServiceProvider>();
            var logger = new Mock<ILogger<AuditHandler>>();
            sp.Setup(s => s.GetService(typeof(ILogger<AuditHandler>))).Returns(logger.Object);
            var am = new Mock<IAuditManager>();
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);
            var h = new AuditHandler(sp.Object);

            var o = new Pagination<string>
            {
                Data = new[] {"1", "2", "3"}
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
                It.Is<object>(obj => obj == o)),
                Times.Once);
        }
        [Fact]
        public async Task UpdateHandler_DomainEntity()
        {
            var sp = new Mock<IServiceProvider>();
            var logger = new Mock<ILogger<AuditHandler>>();
            sp.Setup(s => s.GetService(typeof(ILogger<AuditHandler>))).Returns(logger.Object);
            var am = new Mock<IAuditManager>();
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);
            var h = new AuditHandler(sp.Object);

            var  before = new EventDataObject
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
                It.Is<object>(obj => obj == before)),
                Times.Once);
        }
        [Fact]
        public async Task UpdateHandler_AnonymousObject()
        {
            var sp = new Mock<IServiceProvider>();
            var logger = new Mock<ILogger<AuditHandler>>();
            sp.Setup(s => s.GetService(typeof(ILogger<AuditHandler>))).Returns(logger.Object);
            var am = new Mock<IAuditManager>();
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);
            var h = new AuditHandler(sp.Object);

            var before = new
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
                It.Is<Type>(t => t == typeof(object)),
                It.Is<string>(i => i == before.Id),
                It.Is<string>(art => art == AuditRecordTypes.UPDATE),
                It.Is<object>(obj => obj == before)),
                Times.Once);
        }
        [Fact]
        public async Task UpdateHandler_EntityUpdatedEventData()
        {
            var sp = new Mock<IServiceProvider>();
            var logger = new Mock<ILogger<AuditHandler>>();
            sp.Setup(s => s.GetService(typeof(ILogger<AuditHandler>))).Returns(logger.Object);
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
                Data = before,
                PerformedByUserId = uId,
                WorkContext = wc
            };
            await h.UpdateEventHandler(ded);
            am.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(t => t == typeof(EventDataObject)),
                It.Is<string>(i => i == before.Id),
                It.Is<string>(art => art == AuditRecordTypes.UPDATE),
                It.Is<object>(obj =>
                    (obj as EntityUpdatedDomainEvent<EventDataObject>.EntityUpdatedEventData).Before == before &&
                    (obj as EntityUpdatedDomainEvent<EventDataObject>.EntityUpdatedEventData).After == after)),
                Times.Once);
        }
        [Fact]
        public async Task DeleteHandler()
        {
            var sp = new Mock<IServiceProvider>();
            var logger = new Mock<ILogger<AuditHandler>>();
            sp.Setup(s => s.GetService(typeof(ILogger<AuditHandler>))).Returns(logger.Object);
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
                It.Is<object>(obj => obj == o)),
                Times.Once);
        }
    }
}
