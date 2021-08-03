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
            var log = new Mock<ILogger<AuditHandler>>();
            var am = new Mock<IAuditManager>();
            var h = new AuditHandler(am.Object, log.Object);

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
            var log = new Mock<ILogger<AuditHandler>>();
            var am = new Mock<IAuditManager>();
            var h = new AuditHandler(am.Object, log.Object);

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
            var log = new Mock<ILogger<AuditHandler>>();
            var am = new Mock<IAuditManager>();
            var h = new AuditHandler(am.Object, log.Object);
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
            var log = new Mock<ILogger<AuditHandler>>();
            var am = new Mock<IAuditManager>();
            var h = new AuditHandler(am.Object, log.Object);

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
            var log = new Mock<ILogger<AuditHandler>>();
            var am = new Mock<IAuditManager>();
            var h = new AuditHandler(am.Object, log.Object);

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
            var log = new Mock<ILogger<AuditHandler>>();
            var am = new Mock<IAuditManager>();
            var h = new AuditHandler(am.Object, log.Object);
            EventDataObject before = new()
            {
                Id = "123",
                Value = "data"
            },
            after = new()
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

        [Fact]
        public async Task DeleteHandler_SingleEntityDeleted()
        {
            var log = new Mock<ILogger<AuditHandler>>();
            var am = new Mock<IAuditManager>();
            var h = new AuditHandler(am.Object, log.Object);

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
            var log = new Mock<ILogger<AuditHandler>>();
            var am = new Mock<IAuditManager>();
            var h = new AuditHandler(am.Object, log.Object);

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
