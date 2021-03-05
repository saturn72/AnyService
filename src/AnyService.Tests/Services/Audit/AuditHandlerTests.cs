using AnyService.Audity;
using AnyService.Events;
using AnyService.Services;
using AnyService.Services.Audit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Tests.Services.Audit
{
    public class AuditHandlerTests
    {
        public AuditHandlerTests()
        {
            var ecrs = new[]
            {
                new EntityConfigRecord
                {
                    Type = typeof(EventDataObject),
                }
            };
            AuditManagerExtensions.AddEntityConfigRecords(ecrs);
        }
        public class EventDataObject : IEntity
        {
            public string Id { get; set; }
            public string Value { get; set; }
        }
        [Fact]
        public async Task CreatedHandler_SingleEntityCreated()
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
            await h.CreateEventHandler(ded, null);

            am.Verify(a => a.Insert(
                It.Is<IEnumerable<AuditRecord>>(ars =>
                    ars.Count() == 1 &&
                    ars.ElementAt(0).EntityId == o.Id)),
                Times.Once);
        }
        [Fact]
        public async Task CreatedHandler_BulkCreated()
        {
            var logger = new Mock<ILogger<AuditHandler>>();
            var sp = MockServiceProvider(logger);
            var am = new Mock<IAuditManager>();
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);
            var h = new AuditHandler(sp.Object);

            var o0 = new EventDataObject
            {
                Id = "000",
                Value = "data"
            };
            var o1 = new EventDataObject
            {
                Id = "111",
                Value = "data"
            };

            var uId = "u-id";
            var wc = new WorkContext { CurrentUserId = uId };

            var ded = new DomainEvent
            {
                Data = new[] { o0, o1 },
                PerformedByUserId = uId,
                WorkContext = wc
            };
            await h.CreateEventHandler(ded, null);

            am.Verify(a => a.Insert(
                It.Is<IEnumerable<AuditRecord>>(ars =>
                    ars.Count() == 2 &&
                    ars.ElementAt(0).EntityId == o0.Id &&
                    ars.ElementAt(0).AuditRecordType == AuditRecordTypes.CREATE &&
                    ars.ElementAt(1).EntityId == o1.Id &&
                    ars.ElementAt(1).AuditRecordType == AuditRecordTypes.CREATE
                    )),
                Times.Once);
        }
        [Fact]
        public async Task ReadHandler_Single()
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
            await h.ReadEventHandler(ded, null);
            am.Verify(a => a.Insert(
               It.Is<IEnumerable<AuditRecord>>(ars =>
                   ars.Count() == 1 &&
                   ars.ElementAt(0).EntityId == o.Id &&
                   ars.ElementAt(0).AuditRecordType == AuditRecordTypes.READ)),
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

            var o0 = new EventDataObject
            {
                Id = "123",
                Value = "data"
            };
            var o1 = new EventDataObject
            {
                Id = "123",
                Value = "data"
            };
            var page = new Pagination<EventDataObject>
            {
                Data = new[] { o0, o1 }
            };

            var uId = "u-id";
            var wc = new WorkContext { CurrentUserId = uId };

            var ded = new DomainEvent
            {
                Data = page,
                PerformedByUserId = uId,
                WorkContext = wc
            };
            await h.ReadEventHandler(ded, null);
            am.Verify(a => a.Insert(
               It.Is<IEnumerable<AuditRecord>>(ars =>
                   ars.Count() == 2 &&
                   ars.ElementAt(0).EntityId == o0.Id &&
                   ars.ElementAt(0).AuditRecordType == AuditRecordTypes.READ &&
                   ars.ElementAt(1).EntityId == o1.Id &&
                   ars.ElementAt(1).AuditRecordType == AuditRecordTypes.READ
                   )),
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
            var after = new EventDataObject
            {
                Id = "123",
                Value = "data_2"
            };

            var uId = "u-id";
            var wc = new WorkContext { CurrentUserId = uId };

            var ded = new DomainEvent
            {
                Data = new EntityUpdatedDomainEvent(before, after),
                PerformedByUserId = uId,
                WorkContext = wc
            };
            await h.UpdateEventHandler(ded, null);
            am.Verify(a => a.Insert(
               It.Is<IEnumerable<AuditRecord>>(ars =>
                   ars.Count() == 1 &&
                   ars.ElementAt(0).EntityId == before.Id &&
                   ars.ElementAt(0).AuditRecordType == AuditRecordTypes.UPDATE
                   )),
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
            await h.UpdateEventHandler(ded, null);
            am.Verify(a => a.Insert(
               It.Is<IEnumerable<AuditRecord>>(ars =>
                   ars.Count() == 1 &&
                   ars.ElementAt(0).EntityId == before.Id &&
                   ars.ElementAt(0).AuditRecordType == AuditRecordTypes.UPDATE
                   )),
               Times.Once);
        }

        private bool VerifyPayload(object obj, EventDataObject before, EventDataObject after)
        {
            var o = (obj as EntityUpdatedDomainEvent).Data as EntityUpdatedDomainEvent.EntityUpdatedEventData;
            return o.Before == before && o.After == after;
        }
        [Fact]
        public async Task DeleteHandler_SingleEntityDeleted()
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
            await h.DeleteEventHandler(ded, null);

            am.Verify(a => a.Insert(
                It.Is<IEnumerable<AuditRecord>>(ars =>
                    ars.Count() == 1 &&
                    ars.ElementAt(0).EntityId == o.Id &&
                     ars.ElementAt(0).AuditRecordType == AuditRecordTypes.DELETE)),
                Times.Once);
        }
        [Fact]
        public async Task DeletedHandler_BulkDeleted()
        {
            var logger = new Mock<ILogger<AuditHandler>>();
            var sp = MockServiceProvider(logger);
            var am = new Mock<IAuditManager>();
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);
            var h = new AuditHandler(sp.Object);

            var o0 = new EventDataObject
            {
                Id = "000",
                Value = "data"
            };
            var o1 = new EventDataObject
            {
                Id = "111",
                Value = "data"
            };

            var uId = "u-id";
            var wc = new WorkContext { CurrentUserId = uId };

            var ded = new DomainEvent
            {
                Data = new[] { o0, o1 },
                PerformedByUserId = uId,
                WorkContext = wc
            };
            await h.DeleteEventHandler(ded, null);

            am.Verify(a => a.Insert(
                It.Is<IEnumerable<AuditRecord>>(ars =>
                    ars.Count() == 2 &&
                    ars.ElementAt(0).EntityId == o0.Id &&
                     ars.ElementAt(0).AuditRecordType == AuditRecordTypes.DELETE &&
                    ars.ElementAt(1).EntityId == o1.Id &&
                    ars.ElementAt(1).AuditRecordType == AuditRecordTypes.DELETE
                    )),
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
