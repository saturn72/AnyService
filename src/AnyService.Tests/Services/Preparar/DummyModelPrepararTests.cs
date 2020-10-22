using AnyService.Services.Preparars;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Tests.Services.Preparar
{
    public class DummyModelPrepararTests
    {
        [Fact]
        public void AllReturns_TaskComple()
        {
            var t1 = new Mock<IEntity>();
            var t2 = new Mock<IEntity>();

            var l = new Mock<ILogger<DummyModelPreparar<IEntity>>>();

            var dmp = new DummyModelPreparar<IEntity>(l.Object);

            var res = dmp.PrepareForCreate(t1.Object);
            Verify(t1, res);
            res = dmp.PrepareForUpdate(t1.Object, t2.Object);
            Verify(t1, res);
            Verify(t2, res);
            
            res = dmp.PrepareForDelete(t1.Object);
            Verify(t1, res);
        }

        private void Verify(Mock<IEntity> d, Task t)
        {
            t.ShouldBe(Task.CompletedTask);
            d.VerifySet(s =>s.Id = It.IsAny<string>(), Times.Never());
            d.Reset();
        }

        public class TestClass : IEntity
        {
            public string Id { get; set; }
            public string Value { get; set; }

        }
    }
}