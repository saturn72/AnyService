using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shouldly;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Events.RabbitMQ.Tests
{
    public class RabbitMqEventPublisherE2ETests : IDisposable
    {
        private IModel _consumerChannel;
        private string _incomingMessage;

        public void Dispose()
        {
            _consumerChannel?.Dispose();
        }

        [Fact]
        public async Task PublishToExchange()
        {
            var expData = "this is data";

            var config = new RabbitMqConfig
            {
                OutgoingExchange = "to-pump",
                IncomingExchange = "to-avosetgo-online",
                IncomingQueueName = "from-avosetgo-online",
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

            var ebLog = new Mock<ILogger<RabbitMqCrossDomainEventPublisherSubscriber>>();
            var eb = new RabbitMqCrossDomainEventPublisherSubscriber(pcon, sp, null, config, ebLog.Object);

            //register consumer to recieve message
            var connection = cf.CreateConnection();
            var consumer = RecieveIncomingMessage(connection, config);
            var ie = new IntegrationEvent("test/route")
            {
                Data = expData,
            };
            //publish event
            await eb.Publish("default", "event-key", ie);
            await Task.Delay(500);
            _incomingMessage.ShouldContain(ie.Id);
            _incomingMessage.ShouldContain(ie.Route);
            _incomingMessage.ShouldContain(expData);
        }

        private AsyncEventingBasicConsumer RecieveIncomingMessage(IConnection connection, RabbitMqConfig config)
        {
            _consumerChannel = connection.CreateModel();

            var queueName = _consumerChannel.QueueDeclare().QueueName;
            _consumerChannel.QueueBind(queue: queueName,
                              exchange: config.OutgoingExchange,
                              routingKey: "");


            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                _incomingMessage = Encoding.UTF8.GetString(body);
                return Task.CompletedTask;
            };
            _consumerChannel.BasicConsume(queue: queueName,
                                 autoAck: true,
                                 consumer: consumer);

            return consumer;
        }
    }
}