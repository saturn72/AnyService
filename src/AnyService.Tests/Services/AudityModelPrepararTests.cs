using System.Threading.Tasks;
using AnyService.Audity;
using AnyService.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AnyService.Tests.Services
{

    public class AudityModelPrepararTests
    {
        [Fact]
        public async Task PrepareForCreate_NotVallingAuditHelper()
        {
            var ah = new Mock<AuditHelper>(null);
            var wc = new WorkContext { CurrentUserId = "userId" };
            var l = new Mock<ILogger<AudityModelPreparar<TestModel>>>();

            var m = new TestModel();
            var pm = new AudityModelPreparar<TestModel>(ah.Object, wc, l.Object);
            await pm.PrepareForCreate(m);
            ah.Verify(a => a.PrepareForCreate(It.IsAny<ICreatableAudit>(), It.IsAny<string>()), Times.Never);
        }
        [Fact]
        public async Task PrepareForCreate_CallAuditHelper()
        {
            var ah = new Mock<AuditHelper>(null);
            var wc = new WorkContext { CurrentUserId = "userId" };
            var l = new Mock<ILogger<AudityModelPreparar<AuditableTestModel>>>();

            var m = new AuditableTestModel();
            var pm = new AudityModelPreparar<AuditableTestModel>(ah.Object, wc, l.Object);
            await pm.PrepareForCreate(m);
            ah.Verify(a => a.PrepareForCreate(It.Is<ICreatableAudit>(c => c == m), It.Is<string>(s => s == wc.CurrentUserId)), Times.Once);
        }

        [Fact]
        public async Task PrepareForUpdate_NotVallingAuditHelper()
        {
            var ah = new Mock<AuditHelper>(null);
            var wc = new WorkContext { CurrentUserId = "userId" };
            var l = new Mock<ILogger<AudityModelPreparar<TestModel>>>();

            var before = new TestModel();
            var after = new TestModel();

            var pm = new AudityModelPreparar<TestModel>(ah.Object, wc, l.Object);
            await pm.PrepareForUpdate(before, after);
            ah.Verify(a => a.PrepareForUpdate(It.Is<IUpdatableAudit>(c => c == before), It.Is<IUpdatableAudit>(c => c == before), It.IsAny<string>()), Times.Never);
        }
        [Fact]
        public async Task PrepareForUpdate_CallAuditHelper()
        {
            var ah = new Mock<AuditHelper>(null);
            var wc = new WorkContext { CurrentUserId = "userId" };
            var l = new Mock<ILogger<AudityModelPreparar<AuditableTestModel>>>();

            var before = new AuditableTestModel();
            var after = new AuditableTestModel();

            var pm = new AudityModelPreparar<AuditableTestModel>(ah.Object, wc, l.Object);
            await pm.PrepareForUpdate(before, after);
            ah.Verify(a => a.PrepareForUpdate(It.Is<IUpdatableAudit>(c => c == before), It.Is<IUpdatableAudit>(c => c == after), It.Is<string>(s => s == wc.CurrentUserId)), Times.Once);
        }
        [Fact]
        public async Task PrepareForDelete_NotVallingAuditHelper()
        {
            var ah = new Mock<AuditHelper>(null);
            var wc = new WorkContext { CurrentUserId = "userId" };
            var l = new Mock<ILogger<AudityModelPreparar<TestModel>>>();

            var m = new TestModel();
            var pm = new AudityModelPreparar<TestModel>(ah.Object, wc, l.Object);
            await pm.PrepareForCreate(m);
            ah.Verify(a => a.PrepareForDelete(It.IsAny<IDeletableAudit>(), It.IsAny<string>()), Times.Never);
        }
        [Fact]
        public async Task PrepareForDelete_CallAuditHelper()
        {
            var ah = new Mock<AuditHelper>(null);
            var wc = new WorkContext { CurrentUserId = "userId" };
            var l = new Mock<ILogger<AudityModelPreparar<AuditableTestModel>>>();

            var m = new AuditableTestModel();
            var pm = new AudityModelPreparar<AuditableTestModel>(ah.Object, wc, l.Object);
            await pm.PrepareForDelete(m);
            ah.Verify(a => a.PrepareForDelete(It.Is<IDeletableAudit>(c => c == m), It.Is<string>(s => s == wc.CurrentUserId)), Times.Once);
        }
    }
}
