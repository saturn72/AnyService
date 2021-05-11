using AnyService.Events;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Core.Tests.Events
{
    public class CrossDomainEventPublishManagerTests
    {
        [Fact]
        public async Task Publish_CallsAllPublishers()
        {
            var log = new Mock<ILogger<CrossDomainEventPublishManager>>();
            var publisher1 = new Mock<ICrossDomainEventPublisher>();
            var publisher2 = new Mock<ICrossDomainEventPublisher>();
            var pm = new CrossDomainEventPublishManager(
                new[] { publisher1.Object, publisher2.Object },
                log.Object);

            var evt = new IntegrationEvent("ns", "ek");
            await pm.PublishToAll(evt);
            publisher1.Verify(p => p.Publish(It.Is<IntegrationEvent>(e => e == evt)), Times.Once);
            publisher2.Verify(p => p.Publish(It.Is<IntegrationEvent>(e => e == evt)), Times.Once);
        }
    }
}
