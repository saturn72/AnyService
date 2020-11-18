using AnyService.Audity;
using AnyService.Events;
using AnyService.Services;
using AnyService.Services.Audit;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Tests.Services.Audit
{
    public class AuditManagerTests
    {
        #region InsertRecord

        public class TestClass : IEntity, IFullAudit
        {
            public string Id { get; set; }
        }
        [Theory]
        [InlineData(AuditRecordTypes.CREATE)]
        [InlineData(AuditRecordTypes.READ)]
        [InlineData(AuditRecordTypes.UPDATE)]
        [InlineData(AuditRecordTypes.DELETE)]
        public async Task DoesNotCreateRecord(string art)
        {
            var aSettings = new AuditSettings
            {
                AuditRules = new AuditRules()
            };
            var ecrs = new[]
            {
                new EntityConfigRecord{Type = typeof(AuditRecord), EventKeys = new EventKeyRecord("c", "r", "u", "d")}
            };
            var am = new AuditManager(null, aSettings, ecrs, null, null);
            var res = await am.InsertAuditRecord(null, null, art, null, null);
            res.ShouldBeNull();
        }
        [Fact]
        public async Task CreatesNewRecord()
        {
            var aSettings = new AuditSettings
            {
                AuditRules = new AuditRules
                {
                    AuditCreate = true
                }
            };
            string eId = "entity-id",
                createdKey = "c";
            var data = new TestClass();
            var dbData = new AuditRecord();
            var wc = new WorkContext
            {

            };
            var ecrs = new[]
            {
                new EntityConfigRecord{Type = typeof(AuditRecord), EntityKey = eId, EventKeys = new EventKeyRecord(createdKey, "r", "u", "d")},
                new EntityConfigRecord{Type = typeof(TestClass), Name = "name", EntityKey = "test-class", EventKeys = new EventKeyRecord("ccc", "r", "u", "d")}
            };
            var repo = new Mock<IRepository<AuditRecord>>();
            repo.Setup(r => r.Insert(It.IsAny<AuditRecord>())).ReturnsAsync(dbData);
            var eb = new Mock<IEventBus>();
            var logger = new Mock<ILogger<AuditManager>>();

            var am = new AuditManager(repo.Object, aSettings, ecrs, eb.Object, logger.Object);
            var res = await am.InsertAuditRecord(typeof(TestClass), eId, AuditRecordTypes.CREATE, wc, data);
            res.ShouldNotBeNull();
            eb.Verify(e => e.Publish(It.Is<string>(s => s == createdKey), It.Is<DomainEvent>(ded => ded.Data == dbData)), Times.Once);
        }
        #endregion
        #region Get All
        [Fact]
        public async Task GetAll_ReturnsErrorOn_RepositoryException()
        {
            var repo = new Mock<IRepository<AuditRecord>>();
            repo.Setup(x => x.GetAll(It.IsAny<Pagination<AuditRecord>>()))
                .ThrowsAsync(new Exception());
            var aConfig = new AuditSettings();
            var ecrs = new[]
            {
                new EntityConfigRecord{Type = typeof(AuditRecord), EventKeys = new EventKeyRecord("c", "r", "u", "d")}
            };
            var logger = new Mock<ILogger<AuditManager>>();
            var aSrv = new AuditManager(repo.Object, aConfig, ecrs, null, logger.Object);
            var srvRes = await aSrv.GetAll(new AuditPagination());
            srvRes.Result.ShouldBe(ServiceResult.Error);
        }
        [Fact]
        public async Task GetAll_ReturnsEmptyArray_OnRepositoryNull()
        {
            var repo = new Mock<IRepository<AuditRecord>>();
            repo.Setup(x => x.GetAll(It.IsAny<Pagination<AuditRecord>>()))
                .ReturnsAsync(null as IEnumerable<AuditRecord>);
            var aConfig = new AuditSettings();
            var ecrs = new[]
            {
                new EntityConfigRecord{Type = typeof(AuditRecord), EventKeys = new EventKeyRecord("c", "r", "u", "d")}
            };
            var logger = new Mock<ILogger<AuditManager>>();
            var aSrv = new AuditManager(repo.Object, aConfig, ecrs, null, logger.Object);
            var srvRes = await aSrv.GetAll(new AuditPagination());
            srvRes.Result.ShouldBe(ServiceResult.Ok);
            srvRes.Payload.Data.ShouldBeEmpty();
        }
        [Fact]
        public async Task GetAll_ReturnsRepositoryData()
        {
            var repo = new Mock<IRepository<AuditRecord>>();
            var repoData = new[]
            {
                new AuditRecord { Id = "a" },
                new AuditRecord { Id = "b" },
                new AuditRecord { Id = "c" },
            };
            repo.Setup(x => x.GetAll(It.IsAny<Pagination<AuditRecord>>())).ReturnsAsync(repoData);
            var aConfig = new AuditSettings();
            var ecrs = new[]
            {
                new EntityConfigRecord{Type = typeof(AuditRecord), EventKeys = new EventKeyRecord("c", "r", "u", "d")}
            };
            var logger = new Mock<ILogger<AuditManager>>();
            var aSrv = new AuditManager(repo.Object, aConfig, ecrs, null, logger.Object);
            var srvRes = await aSrv.GetAll(new AuditPagination());
            srvRes.Result.ShouldBe(ServiceResult.Ok);
            srvRes.Payload.Data.ShouldBe(repoData);
        }
        #endregion
        #region Query builder
        private const string
            Entity1 = "e1",
            Entity2 = "e2",
            Entity3 = "e3",
            Create = "c",
            Read = "r",
            Update = "u",
            Delete = "d",
            Name1 = "n1",
            Name2 = "n2",
            Name3 = "n3",
            Client1 = "c1",
            User1 = "u1";

        [Theory]
        [MemberData(nameof(BuildAuditPaginationQuery_DATA))]
        public void BuildAuditPaginationQuery(AuditPagination p, int[] selectedIndexes)
        {
            var a = new TestAuditManager();
            var q = a.QueryBuilder(p);

            var res = _records.Where(q).ToArray();
            res.Count().ShouldBe(selectedIndexes.Count());

            for (int i = 0; i < selectedIndexes.Length; i++)
                res.ShouldContain(x => x == _records.ElementAt(selectedIndexes[i]));
        }
        public static IEnumerable<object[]> BuildAuditPaginationQuery_DATA = new[]
        {
            //Ids
            new object[]{ new AuditPagination{EntityIds = new []{ Entity1 },}, new[]{0, 2, 4, 7 }},

            //record types
            new object[]{ new AuditPagination{AuditRecordTypes = new[]{Create },}, new[]{0,1,6 }},
            new object[]{ new AuditPagination{
                AuditRecordTypes = new[]{Create, Read },}, new[]{0,1,2,6 }},
            
            //id + record types
            new object[]
            {
                new AuditPagination{
                    EntityIds = new[] { Entity1},
                    AuditRecordTypes = new[]{ Read },
                },
                new[]{ 2 }},

            //names
             new object[]{new AuditPagination{EntityNames= new[] { Name1, Name2 } },new[]{ 0, 2, 4, 5, 6 }},

             //names + recrd types
             new object[]{new AuditPagination{
                 AuditRecordTypes = new[]{Create},
                 EntityNames= new[] { Name1, Name2 } },new[]{ 0, 6 }},

             //clientIds
             new object[]{new AuditPagination{ClientIds= new[] { Client1 } },new[]{ 0, 2, 6 }},

             //clientIds + names
             new object[]{ new AuditPagination { ClientIds = new[] { Client1 }, EntityNames = new[]{Name1 } },new[]{ 0, 2 }},

             //user Ids
             new object[]{new AuditPagination{UserIds= new[] { User1} },new[]{ 3, 5, 7 }},

             //user Ids + record type
             new object[]{new AuditPagination
             {
                 UserIds= new[] { User1},
                 AuditRecordTypes = new[]{Delete}
             },new[]{ 5,}},

              //client Ids
             new object[]{new AuditPagination{ ClientIds = new[] { Client1} },new[]{0, 2, 6 }},

             //clientIds + names
             new object[]{new AuditPagination
             {
                 ClientIds= new[] { Client1},
                 EntityNames = new[]{Name2}
             },new[]{ 6,}},

             //from
             new object[]{new AuditPagination{ FromUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3))},new[]{7, 8 }},

             // from + record type
             new object[]
             {
                 new AuditPagination{
                     FromUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3)),
                     AuditRecordTypes = new[]{Delete }
                 },new[]{8 }},

             //to
             new object[]{new AuditPagination{ ToUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3))}
             ,new[]{0, 1, 2, 3, 4, 5, 6,}},

             // from + to
             new object[]
             {
                 new AuditPagination{
                     ToUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3)),
                     FromUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(10)),
                 },new[]{1 }},
        };
        private readonly IEnumerable<AuditRecord> _records = new[]
        {
            new AuditRecord {Id = "a", EntityId = Entity1,  AuditRecordType = Create, EntityName = Name1, ClientId = Client1,
                CreatedOnUtc = DateTime.MinValue.ToIso8601()},

            new AuditRecord {Id = "b", EntityId = Entity2,  AuditRecordType = Create, EntityName = Name3,
                CreatedOnUtc = DateTime.UtcNow.Subtract(TimeSpan.FromDays(5)).ToIso8601()},

            new AuditRecord {Id = "c", EntityId = Entity1,  AuditRecordType = Read,  EntityName = Name1, ClientId = Client1,
                CreatedOnUtc = DateTime.MinValue.ToIso8601()},

            new AuditRecord {Id = "d", EntityId = Entity3,  AuditRecordType = Update,  EntityName = Name3, UserId = User1,
                CreatedOnUtc = DateTime.MinValue.ToIso8601()},

            new AuditRecord {Id = "e", EntityId = Entity1,  AuditRecordType = Delete,  EntityName = Name2,
                CreatedOnUtc = DateTime.MinValue.ToIso8601()},

            new AuditRecord {Id = "f", EntityId = Entity2,  AuditRecordType = Delete,  EntityName = Name1, UserId = User1,
                CreatedOnUtc = DateTime.MinValue.ToIso8601()},

            new AuditRecord {Id = "g", EntityId = Entity3,  AuditRecordType = Create,  EntityName = Name2, ClientId = Client1,
                CreatedOnUtc = DateTime.MinValue.ToIso8601()},

            new AuditRecord {Id = "h", EntityId = Entity1,  AuditRecordType = Update,  EntityName = Name3, UserId = User1,
                CreatedOnUtc= DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)).ToIso8601()},
            new AuditRecord {Id = "i", EntityId = Entity3,  AuditRecordType = Delete,  EntityName = Name3,
                CreatedOnUtc= DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)).ToIso8601()},
        };

        public class TestAuditManager : AuditManager
        {
            static IEnumerable<EntityConfigRecord> ecrs = new[]
            {
                new EntityConfigRecord{Type = typeof(AuditRecord), EventKeys = new EventKeyRecord("c", "r", "u", "d")}
            };
            public TestAuditManager() : base(null, null, ecrs, null, null)
            {
            }
            public Func<AuditRecord, bool> QueryBuilder(AuditPagination pagination) => BuildAuditPaginationQuery(pagination);
        }
        #endregion
    }
}
