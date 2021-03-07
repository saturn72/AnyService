using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Events.RabbitMQ.Tests
{
    public class RabbitMqE2ETests
    {
        [Fact]
        public async Task PublishToExchange()
        {
            var config = new RabbitMqConfig
            {
                BrokerName = "qcore-broker",
                QueueName = "from-avosetgo-online",
                RetryCount = 5,
                HostName = "localhost",
                Port = 5672,
            };
            var cf = new ConnectionFactory
            {
                HostName = config.HostName,
                Port = config.Port,
                DispatchConsumersAsync = true,
                UserName = "avosetgo-online",
                Password = "!qaz2wsX",
            };
            var pconLogger = new Mock<ILogger<DefaultRabbitMQPersistentConnection>>();
            var pcon = new DefaultRabbitMQPersistentConnection(cf, config, pconLogger.Object);
            var services = new ServiceCollection();
            var sp = services.BuildServiceProvider();

            var ebLog = new Mock<ILogger<RabbitMqCrossDomainEventBus>>();
            var eb = new RabbitMqCrossDomainEventBus(pcon, sp, null, config, ebLog.Object);

            var ie = new IntegrationEvent
            {
                Data = "this is data",
            };
            await eb.Publish("default", "event-key", ie);
        }
    }
}
