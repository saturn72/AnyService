using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using Shouldly;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Events.RabbitMQ.Tests
{
    public class RabbitMqEventSubscriberE2ETests
    {
        [Fact]
        public async Task ConsumeIncomingMessage()
        {

            var expData = "this is data";

            var config = new RabbitMqConfig
            {
                OutgoingExchange = "to-pump",
                IncomingExchange = "to-avosetgo-online",
                IncomingQueueName = "to-avosetgo-online-queue",
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

            var smLog = new Mock<ILogger<DefaultSubscriptionManager<IntegrationEvent>>>();
            var sm = new DefaultSubscriptionManager<IntegrationEvent>(smLog.Object);

            var ebLog = new Mock<ILogger<RabbitMqCrossDomainEventPublisherSubscriber>>();
            var eb = new RabbitMqCrossDomainEventPublisherSubscriber(pcon, sp, sm, config, ebLog.Object);

            var i = "init-data";
            var f = new Func<IntegrationEvent, IServiceProvider, Task>((evt, services) =>
            {
                i = expData;
                return Task.CompletedTask;
            });
            var ns = "test-ns";
            var ek = "test-key";
            await eb.Subscribe(ns, ek, f, "f-name");
            var evt = new IntegrationEvent(ns + "/" + ek) { Data = expData };
            PublishEvent(evt, config, pcon);
            await Task.Delay(500);
            i.ShouldBe(expData);
        }

        private void PublishEvent(IntegrationEvent evt, RabbitMqConfig config, IRabbitMQPersistentConnection persistentConnection)
        {
            using var channel = persistentConnection.CreateModel();
            var message = evt.ToJsonString();
            var body = Encoding.UTF8.GetBytes(message);

            var properties = channel.CreateBasicProperties();
            properties.DeliveryMode = 2; // persistent
            channel.BasicPublish(
                exchange: config.IncomingExchange,
                routingKey: evt.Route,
                mandatory: true,
                basicProperties: properties,
                body: body);
        }
    }
}