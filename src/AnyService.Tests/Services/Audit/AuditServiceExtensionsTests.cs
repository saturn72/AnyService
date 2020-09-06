﻿using AnyService.Services;
using AnyService.Services.Audit;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Tests.Services.Audit
{
    public class AuditServiceExtensionsTests
    {
        public class TestClass : IDomainModelBase
        {
            public string Id { get; set; }
        }
        [Fact]
        public async Task InsertCreateRecord()
        {
            var ah = new Mock<IAuditService>();
            var t = new TestClass { Id = "a" };
            await AuditServiceExtensions.InsertCreateRecord(ah.Object, t);
            ah.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(x => x == typeof(TestClass)),
                It.Is<string>(i => i == t.Id),
                It.Is<string>(i => i == AuditRecordTypes.CREATE),
                It.Is<TestClass>(x => x == t)),

                Times.Once);
        }

        [Fact]
        public async Task InsertReadRecord_SingleEntity()
        {
            var ah = new Mock<IAuditService>();
            var read = new TestClass { Id = "b" };
            await AuditServiceExtensions.InsertReadRecord(ah.Object, read);
            ah.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(x => x == typeof(TestClass)),
                It.Is<string>(i => i == read.Id),
                It.Is<string>(i => i == AuditRecordTypes.READ),
                It.Is<object>(x => x == read)),
                Times.Once);
        }
        [Fact]
        public async Task InsertReadRecord_Pagination()
        {
            var ah = new Mock<IAuditService>();
            var page = new Pagination<TestClass>
            {
                Data = new[] { new TestClass { Id = "b" } }
            };
            await AuditServiceExtensions.InsertReadRecord(ah.Object, page);
            ah.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(x => x == typeof(TestClass)),
                It.Is<string>(i => i == null),
                It.Is<string>(i => i == AuditRecordTypes.READ),
                It.Is<object>(x => x == page)),
                Times.Once);
        }

        [Fact]
        public async Task InsertUpdatedRecord()
        {
            var ah = new Mock<IAuditService>();
            var after = new TestClass { Id = "a" };
            var before = new TestClass { Id = "b" };
            await AuditServiceExtensions.InsertUpdatedRecord(ah.Object, after, before);
            ah.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(x => x == typeof(TestClass)),
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
            var ah = new Mock<IAuditService>();
            var t = new TestClass { Id = "a" };
            await AuditServiceExtensions.InsertDeletedRecord(ah.Object, t);
            ah.Verify(a => a.InsertAuditRecord(
                It.Is<Type>(x => x == typeof(TestClass)),
                It.Is<string>(i => i == t.Id),
                It.Is<string>(i => i == AuditRecordTypes.DELETE),
                It.Is<TestClass>(x => x == t)),

                Times.Once);
        }
    }
}
