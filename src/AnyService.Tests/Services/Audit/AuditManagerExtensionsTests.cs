using AnyService.Audity;
using AnyService.Services.Audit;

namespace AnyService.Tests.Services.Audit
{
    public class AuditManagerExtensionsTests
    {
        public class TestClass : IEntity
        {
            public string Id { get; set; }
        }
        public AuditManagerExtensionsTests()
        {
            var ecrs = new[]
            {
                new EntityConfigRecord
                {
                    Type = typeof(TestClass),
                }
            };
            AuditManagerExtensions.AddEntityConfigRecords(ecrs);
        }
        [Fact]
        public async Task InsertCreateRecord()
        {
            var ah = new Mock<IAuditManager>();
            var t = new TestClass { Id = "a" };
            var wc = new WorkContext();
            var ctx = "ctx";

            await AuditManagerExtensions.InsertCreateRecords(ah.Object, new[] { t }, wc, ctx);
            ah.Verify(a => a.Insert(It.Is<IEnumerable<AuditRecord>>(ars =>
               ars.Count() == 1 &&
               ars.ElementAt(0).AuditRecordType == AuditRecordTypes.CREATE &&
               ars.ElementAt(0).EntityId == t.Id &&
               ars.ElementAt(0).Context == ctx.ToJsonString())),
                Times.Once);
        }

        [Fact]
        public async Task InsertReadRecord_SingleEntity()
        {
            var ah = new Mock<IAuditManager>();
            var t = new TestClass { Id = "b" };
            var wc = new WorkContext();
            var ctx = "ctx";

            await AuditManagerExtensions.InsertReadRecords(ah.Object, new[] { t }, wc, ctx);
            ah.Verify(a => a.Insert(It.Is<IEnumerable<AuditRecord>>(ars =>
               ars.Count() == 1 &&
               ars.ElementAt(0).AuditRecordType == AuditRecordTypes.READ &&
               ars.ElementAt(0).EntityId == t.Id &&
               ars.ElementAt(0).Context == ctx.ToJsonString())),
                Times.Once);
        }
        [Fact]
        public async Task InsertUpdatedRecord()
        {
            var ah = new Mock<IAuditManager>();
            var after = new TestClass { Id = "a" };
            var before = new TestClass { Id = "b" };
            var wc = new WorkContext();
            var ctx = "ctx";

            await AuditManagerExtensions.InsertUpdatedRecord(ah.Object, after, before, wc, ctx);
            ah.Verify(a => a.Insert(It.Is<IEnumerable<AuditRecord>>(ars =>
               ars.Count() == 1 &&
               ars.ElementAt(0).AuditRecordType == AuditRecordTypes.UPDATE &&
               ars.ElementAt(0).EntityId == after.Id &&
               ars.ElementAt(0).Context == ctx.ToJsonString())),
                Times.Once);
        }
        [Fact]
        public async Task InsertDeletedRecord()
        {
            var ah = new Mock<IAuditManager>();
            var t = new TestClass { Id = "a" };
            var wc = new WorkContext();
            var ctx = "ctx";

            await AuditManagerExtensions.InsertDeletedRecord(ah.Object, new[] { t }, wc, ctx);
            ah.Verify(a =>
            a.Insert(It.Is<IEnumerable<AuditRecord>>(ars =>
               ars.Count() == 1 &&
               ars.ElementAt(0).AuditRecordType == AuditRecordTypes.DELETE &&
               ars.ElementAt(0).Context == ctx.ToJsonString())),
               Times.Once);
        }
    }
}
