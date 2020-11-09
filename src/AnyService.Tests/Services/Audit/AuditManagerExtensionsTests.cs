using AnyService.Services;
using AnyService.Services.Audit;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Tests.Services.Audit
{
    public class AuditManagerExtensionsTests
    {
        public class TestClass : IEntity
        {
            public string Id { get; set; }
        }
        [Fact]
        public async Task InsertCreateRecord()
        {
            var ah = new Mock<IAuditManager>();
            var t = new TestClass { Id = "a" };
            var wc = new WorkContext();
            
            await AuditManagerExtensions.InsertCreateRecord(ah.Object, t, wc);
            ah.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(x => x == typeof(TestClass)),
                It.Is<string>(i => i == t.Id),
                It.Is<string>(i => i == AuditRecordTypes.CREATE),
                It.Is<WorkContext>(w => w == wc), It.Is<TestClass>(x => x == t)),

                Times.Once);
        }

        [Fact]
        public async Task InsertReadRecord_SingleEntity()
        {
            var ah = new Mock<IAuditManager>();
            var read = new TestClass { Id = "b" };
            var wc = new WorkContext();

            await AuditManagerExtensions.InsertReadRecord(ah.Object, read, wc);
            ah.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(x => x == typeof(TestClass)),
                It.Is<string>(i => i == read.Id),
                It.Is<string>(i => i == AuditRecordTypes.READ),
               It.Is<WorkContext>(w => w == wc), It.Is<object>(x => x == read)),
                Times.Once);
        }
        [Fact]
        public async Task InsertReadRecord_Pagination()
        {
            var ah = new Mock<IAuditManager>();
            var page = new Pagination<TestClass>
            {
                Total = 123,
                Data = new[] { new TestClass { Id = "b" } }
            };
            var wc = new WorkContext();
            
            await AuditManagerExtensions.InsertReadRecord(ah.Object, page, wc);
            ah.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(x => x == typeof(TestClass)),
                It.Is<string>(i => i == null),
                It.Is<string>(i => i == AuditRecordTypes.READ),
               It.Is<WorkContext>(w => w == wc), It.Is<object>(x => x.GetPropertyValueByName<int>("total") == page.Total)),
                Times.Once);
        }

        [Fact]
        public async Task InsertUpdatedRecord()
        {
            var ah = new Mock<IAuditManager>();
            var after = new TestClass { Id = "a" };
            var before = new TestClass { Id = "b" };
            var wc = new WorkContext();

            await AuditManagerExtensions.InsertUpdatedRecord(ah.Object, after, before, wc);
            ah.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(x => x == typeof(TestClass)),
                It.Is<string>(i => i == after.Id),
                It.Is<string>(i => i == AuditRecordTypes.UPDATE),
                It.Is<WorkContext>(w => w == wc),
                It.Is<object>(x =>
                    x.GetPropertyValueByName<TestClass>("before") != null &&
                    x.GetPropertyValueByName<TestClass>("after") != null)),

                Times.Once);
        }
        [Fact]
        public async Task InsertDeletedRecord()
        {
            var ah = new Mock<IAuditManager>();
            var t = new TestClass { Id = "a" };
            var wc = new WorkContext();
            
            await AuditManagerExtensions.InsertDeletedRecord(ah.Object, t, wc);
            ah.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(x => x == typeof(TestClass)),
                It.Is<string>(i => i == t.Id),
                It.Is<string>(i => i == AuditRecordTypes.DELETE),
                It.Is<WorkContext>(w => w == wc), 
                It.Is<TestClass>(x => x == t)),
                Times.Once);
        }
    }
}
