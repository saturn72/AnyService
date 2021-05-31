using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
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

        [Theory]
        [MemberData(nameof(PublishToExchange_DATA))]
        public async Task PublishToExchange(Func<RabbitMqConfig> func)
        {
            var expData = "this is data";

            var config = func();
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
            var eb = new RabbitMqCrossDomainEventPublisherSubscriber(pcon, pcon, sp, null, config, ebLog.Object);

            //register consumer to recieve message
            var connection = cf.CreateConnection();
            var consumer = RecieveIncomingMessage(connection, config);
            var ie = new IntegrationEvent("test", "route")
            {
                Data = expData,
            };
            //publish event
            await eb.Publish(ie);
            await Task.Delay(500);
            _incomingMessage.ShouldContain(ie.Id);
            _incomingMessage.ShouldContain(ie.Namespace);
            _incomingMessage.ShouldContain(expData);
        }

        private AsyncEventingBasicConsumer RecieveIncomingMessage(IConnection connection, RabbitMqConfig config)
        {
            _consumerChannel = connection.CreateModel();

            _consumerChannel.ExchangeDeclare(config.Outgoing[0].Name, "fanout");
            var queueName = _consumerChannel.QueueDeclare().QueueName;
            _consumerChannel.QueueBind(queue: queueName,
                              exchange: config.Outgoing[0].Name,
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
        public static IEnumerable<object[]> PublishToExchange_DATA => new[]
      {
            new object[]
            {
                new Func<RabbitMqConfig>(() => new RabbitMqConfig
                {
                    Outgoing = new[] { new ExchangeConfig { Name = "out-test-ex" } },
                    Incoming = new ChannelConfig
                    {
                        Exchanges = new[] { new ExchangeConfig { Name = "in-test-ex", Type = "fanout", }, },
                        Queues = new[]
                        {
                            new QueueConfig
                            {
                                Name = "in-test-queue",
                                Exchange = "in-test-ex"
                            },
                        },
                    },
                    RetryCount = 5,
                    HostName = "qcorerabbit.westeurope.cloudapp.azure.com",
                    Port = 5672,
                })
            },
            new object[]
            {
                new Func<RabbitMqConfig>(() =>
                {
                    var json = @"{
                        ""outgoing"":[{ ""name"":""out-test-ex"" }],
                        ""incoming"":{
                            ""exchanges"":[{ ""name"":""in-test-ex"", ""type"":""fanout""}],
                            ""queues"":[{""name"":""in-test-queue"", ""exchange"":""in-test-ex""}]
                        },
                        ""retryCount"":5,
                        ""hostName"": ""qcorerabbit.westeurope.cloudapp.azure.com"",
                        ""port"":5672
                        }";
                    return JsonSerializer.Deserialize<RabbitMqConfig>(json);
                })
            }
        };

    }
}