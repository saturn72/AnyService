using AnyService.Services.Audit;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Tests.Services.Audit
{
    public class AuditHelperExtensionsTests
    {
        public class TestClass : IDomainModelBase
        {
            public string Id { get; set; }
        }
        [Fact]
        public async Task InsertCreateRecord()
        {
            var ah = new Mock<IAuditHelper>();
            var t = new TestClass { Id = "a" };
            await AuditHelperExtensions.InsertCreateRecord(ah.Object, t);
            ah.Verify(a => a.InsertAuditRecord(
                It.Is<string>(x => x == typeof(TestClass).FullName),
                It.Is<string>(i => i == t.Id),
                It.Is<string>(i => i == AuditRecordTypes.CREATE),
                It.Is<TestClass>(x => x == t)),

                Times.Once);
        }
        [Fact]
        public async Task InsertUpdatedRecord()
        {
            var ah = new Mock<IAuditHelper>();
            var after = new TestClass { Id = "a" };
            var before = new TestClass { Id = "b" };
            await AuditHelperExtensions.InsertUpdatedRecord(ah.Object, after, before);
            ah.Verify(a => a.InsertAuditRecord(
                It.Is<string>(x => x == typeof(TestClass).FullName),
                It.Is<string>(i => i == after.Id),
                It.Is<string>(i => i == AuditRecordTypes.UPDATE),
                It.Is<object>(x =>
                    x.GetPropertyValueByName<TestClass>("before") == before &&
                    x.GetPropertyValueByName<TestClass>("after") == after)),

                Times.Once);
        }
        [Fact]
        public async Task InsertDeletedRecord()
        {
            var ah = new Mock<IAuditHelper>();
            var t = new TestClass { Id = "a" };
            await AuditHelperExtensions.InsertDeletedRecord(ah.Object, t);
            ah.Verify(a => a.InsertAuditRecord(
                It.Is<string>(x => x == typeof(TestClass).FullName),
                It.Is<string>(i => i == t.Id),
                It.Is<string>(i => i == AuditRecordTypes.DELETE),
                It.Is<TestClass>(x => x == t)),

                Times.Once);
        }
    }
}
