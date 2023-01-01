using AnyService.Events;

namespace AnyService.Tests.Events
{
    public class EventBusExtensionsTests
    {
        public class TestClass : IEntity
        {
            public string Id { get; set; }
            public string Value { get; set; }
        }
        [Fact]
        public void PublishCreated()
        {
            var key = "ek";
            var data = new TestClass
            {
                Id = "123",
                Value = "data"
            };
            var wc = new WorkContext
            {
                CurrentUserId = "userid",
            };
            var eb = new Mock<IDomainEventBus>();
            EventBusExtensions.Publish(eb.Object, key, data, wc);
            eb.Verify(e => e.Publish(
                It.Is<string>(k => k == key),
                It.Is<DomainEvent>(ded =>
                    ded.Data == data &&
                    ded.PerformedByUserId == wc.CurrentUserId &&
                    ded.WorkContext == wc)), Times.Once);
        }
        [Fact]
        public void PublishUpdated()
        {
            var key = "ek";
            TestClass before = new TestClass
            {
                Id = "123",
                Value = "b"
            },
            after = new TestClass
            {
                Id = "123",
                Value = "a",
            };

            var wc = new WorkContext
            {
                CurrentUserId = "userid",
            };
            var eb = new Mock<IDomainEventBus>();
            EventBusExtensions.PublishUpdated(eb.Object, key, before, after, wc);
            eb.Verify(e => e.Publish(
                It.Is<string>(k => k == key),
                It.Is<DomainEvent>(ded =>
                    (ded.Data as EntityUpdatedDomainEvent.EntityUpdatedEventData).Before == before &&
                    (ded.Data as EntityUpdatedDomainEvent.EntityUpdatedEventData).After == after &&
                    ded.PerformedByUserId == wc.CurrentUserId &&
                    ded.WorkContext == wc)), Times.Once);
        }
        [Fact]
        public void PublishException()
        {
            var key = "ek";
            var ex = new Exception();
            var data = "this is data";
            var wc = new WorkContext
            {
                CurrentUserId = "userid",
            };
            var eb = new Mock<IDomainEventBus>();
            EventBusExtensions.PublishException(eb.Object, key, ex, data, wc);
            eb.Verify(e => e.Publish(
                It.Is<string>(k => k == key),
                It.Is<DomainEvent>(ded =>
                    (ded.Data as DomainExceptionEvent.DomainExceptionEventData).Data.ToString() == data &&
                    (ded.Data as DomainExceptionEvent.DomainExceptionEventData).Exception == ex &&
                    ded.PerformedByUserId == wc.CurrentUserId &&
                    ded.WorkContext == wc)), Times.Once);
        }
    }
}
