using AnyService.Events;
using Moq;
using System;
using Xunit;

namespace AnyService.Tests.Events
{
    public class EventBusExtensionsTests
    {
        [Fact]
        public void PublishCreated()
        {
            var key = "ek";
            var data = "data";
            var wc = new WorkContext
            {
                CurrentUserId = "userid",
            };
            var eb = new Mock<IEventBus>();
            EventBusExtensions.Publish(eb.Object, key, data, wc);
            eb.Verify(e => e.Publish(
                It.Is<string>(k => k == key),
                It.Is<DomainEvent>(ded =>
                    ded.Data.ToString() == data &&
                    ded.PerformedByUserId == wc.CurrentUserId &&
                    ded.WorkContext == wc)), Times.Once);
        }
        [Fact]
        public void PublishUpdated()
        {
            string key = "ek",
                before = "b",
                after = "a";

            var wc = new WorkContext
            {
                CurrentUserId = "userid",
            };
            var eb = new Mock<IEventBus>();
            EventBusExtensions.PublishUpdated(eb.Object, key, before, after, wc);
            eb.Verify(e => e.Publish(
                It.Is<string>(k => k == key),
                It.Is<DomainEvent>(ded =>
                    (ded.Data as EntityUpdatedDomainEvent<string>.EntityUpdatedEventData).Before == before &&
                    (ded.Data as EntityUpdatedDomainEvent<string>.EntityUpdatedEventData).After == after &&
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
            var eb = new Mock<IEventBus>();
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
