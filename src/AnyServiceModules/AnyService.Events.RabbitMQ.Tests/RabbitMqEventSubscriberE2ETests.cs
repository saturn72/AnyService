using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Events.RabbitMQ.Tests
{
    public class RabbitMqEventSubscriberE2ETests
    {
        [Theory]
        [MemberData(nameof(ConsumeIncomingMessage_DATA))]
        public async Task ConsumeIncomingMessage(Func<RabbitMqConfig> func)
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
            var pCon = new DefaultRabbitMQPersistentConnection(cf, config, pconLogger.Object);
            var services = new ServiceCollection();
            var sp = services.BuildServiceProvider();

            var smLog = new Mock<ILogger<DefaultSubscriptionManager<IntegrationEvent>>>();
            var sm = new DefaultSubscriptionManager<IntegrationEvent>(smLog.Object);

            var ebLog = new Mock<ILogger<RabbitMqCrossDomainEventPublisherSubscriber>>();
            var eb = new RabbitMqCrossDomainEventPublisherSubscriber(pCon, pCon, sp, sm, config, ebLog.Object);

            var i = "init-data";
            var f = new Func<IntegrationEvent, IServiceProvider, Task>((evt, services) =>
            {
                i = expData;
                return Task.CompletedTask;
            });
            var ns = "test-ns";
            var ek = "test-key";
            await eb.Subscribe(ns, ek, f, "f-name");
            var evt = new IntegrationEvent(ns, ek) { Data = expData };
            await eb.Publish(evt);
            //PublishEvent(evt, config, pcon);
            await Task.Delay(500);
            i.ShouldBe(expData);
        }
        public static IEnumerable<object[]> ConsumeIncomingMessage_DATA => new[]
        {
            new object[]
            {
                new Func<RabbitMqConfig>(() => new RabbitMqConfig
                {
                    Outgoing = new[] { new ExchangeConfig { Name = "in-test-ex" } },
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
                    ""outgoing"":[{ ""name"":""in-test-ex"" }],
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