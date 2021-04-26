using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AnyService.Events.RabbitMQ
{

    public class RabbitMqCrossDomainEventPublisherSubscriber : ICrossDomainEventPublisher, ICrossDomainEventSubscriber, IDisposable
    {
        const int DefaultTasksTimeout = 60000;

        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISubscriptionManager<IntegrationEvent> _subscriptionManager;
        private readonly RabbitMqConfig _config;
        private readonly ILogger<RabbitMqCrossDomainEventPublisherSubscriber> _logger;
        private IModel _consumerChannel;

        public RabbitMqCrossDomainEventPublisherSubscriber(
            IRabbitMQPersistentConnection persistentConnection,
            IServiceProvider serviceProvider,
            ISubscriptionManager<IntegrationEvent> subscriptionManager,
            RabbitMqConfig config,
            ILogger<RabbitMqCrossDomainEventPublisherSubscriber> logger
            )
        {
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _subscriptionManager = subscriptionManager;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _consumerChannel = CreateConsumerChannel();
        }

        private async Task OnHandlerRemoved(string @namespace, string eventKey)
        {
            TryConnect();
            using var channel = _persistentConnection.CreateModel();
            channel.QueueUnbind(queue: _config.IncomingQueueName,
                exchange: _config.IncomingExchange,
                routingKey: $"{@namespace}/{eventKey}");

            var allHandlers = await _subscriptionManager.GetAllHandlers();
            if (allHandlers.IsNullOrEmpty())
            {
                _config.IncomingQueueName = string.Empty;
                _consumerChannel.Close();
            }
        }
        public async Task Publish(IntegrationEvent @event)
        {
            TryConnect();
            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetryAsync(_config.RetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", @event.Id, $"{time.TotalSeconds:n1}", ex.Message);
                });

            _logger.LogDebug("Creating RabbitMQ channel to publish event: {namespace} {EventId} ({EventName})", @event.Namespace, @event.Id, @event.EventKey);
            using var channel = _persistentConnection.CreateModel();
            _logger.LogDebug("Declaring RabbitMQ exchange to publish event: {EventId}", @event.Id);

            channel.ExchangeDeclare(exchange: _config.OutgoingExchange, type: _config.OutgoingExchangeType);

            var message = @event.ToJsonString();
            var body = Encoding.UTF8.GetBytes(message);

            await policy.ExecuteAsync(() =>
            {
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2; // persistent
                _logger.LogDebug("Publishing event to RabbitMQ: {EventId}", @event.Id);
                return Task.Run(() => channel.BasicPublish(
                    exchange: _config.OutgoingExchange,
                    routingKey: $"{@event.Namespace}/{@event.EventKey}",
                    mandatory: true,
                    basicProperties: properties,
                    body: body));
            });
        }
        public async Task<string> Subscribe(string @namespace, string eventKey, Func<IntegrationEvent, IServiceProvider, Task> handler, string alias)
        {
            _logger.LogDebug("Subscribing event handler for {EventKey} with {Name}", eventKey, alias);

            var handlerId = await _subscriptionManager.Subscribe(@namespace, eventKey, handler, alias);
            if (!handlerId.HasValue())
                throw new InvalidOperationException("Failed to subscribe handler");
            TryConnect();
            using var channel = _persistentConnection.CreateModel();
            channel.QueueBind(
                queue: _config.IncomingQueueName,
                exchange: _config.IncomingExchange,
                routingKey: $"{@namespace}/{eventKey}");
            StartBasicConsume();
            return handlerId;
        }
        private void TryConnect()
        {
            if (!_persistentConnection.IsConnected)
                _persistentConnection.TryConnect();
        }
        public async Task Unsubscribe(string handlerId)
        {
            _logger.LogInformation("Unsubscribing from event {handlerId}", handlerId);
            var h = await _subscriptionManager.GetByHandlerId(handlerId);
            await _subscriptionManager.Unsubscribe(handlerId);
            if (handlerId.HasValue())
                _ = OnHandlerRemoved(h.Namespace, h.EventKey);
        }
        private void StartBasicConsume()
        {
            _logger.LogTrace("Starting RabbitMQ basic consume");

            if (_consumerChannel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

                consumer.Received += Consumer_Received;

                _consumerChannel.BasicConsume(
                    queue: _config.IncomingQueueName,
                    autoAck: false,
                    consumer: consumer);
            }
            else
            {
                _logger.LogError("StartBasicConsume can't call on _consumerChannel == null");
            }
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            var keyArr = eventArgs.RoutingKey.Split("/");
            var @namespace = keyArr[0];
            var eventKey = keyArr[1];

            var message = Encoding.UTF8.GetString(eventArgs.Body.Span);
            try
            {
                if (message.ToLowerInvariant().Contains("throw-fake-exception"))
                {
                    throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
                }
                await ProcessEvent(@namespace, eventKey, message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "----- ERROR Processing message \"{Message}\"", message);
            }
            // Even on exception we take the message off the queue.
            // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
            // For more information see: https://www.rabbitmq.com/dlx.html
            _consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }

        private IModel CreateConsumerChannel()
        {
            TryConnect();
            _logger.LogTrace("Creating RabbitMQ consumer channel");
            var channel = _persistentConnection.CreateModel();
            channel.ExchangeDeclare(exchange: _config.IncomingExchange,
                                    type: _config.IncomingExchangeType);
            channel.QueueDeclare(queue: _config.IncomingQueueName ?? "",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            channel.CallbackException += (sender, ea) =>
            {
                _logger.LogWarning(ea.Exception, "Recreating RabbitMQ consumer channel");

                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel();
                StartBasicConsume();
            };

            return channel;
        }

        private async Task ProcessEvent(string @namespace, string eventKey, string message)
        {
            _logger.LogTrace("Processing RabbitMQ event: {EventKey}", eventKey);
            var handlerDatas = await _subscriptionManager.GetHandlers(@namespace, eventKey);
            if (handlerDatas.IsNullOrEmpty())
            {
                _logger.LogWarning("No subscription for RabbitMQ event: {EventName}", eventKey);
                return;
            }
            var tasks = new List<Task>();
            using var scope = _serviceProvider.CreateScope();
            foreach (var hd in handlerDatas)
            {
                var @event = message.ToObject<IntegrationEvent>();
                tasks.Add(hd.Handler(@event, scope.ServiceProvider));
                await Task.Yield();
            }
            Task.WaitAll(tasks.ToArray(), DefaultTasksTimeout);
        }
        public void Dispose()
        {
            _consumerChannel?.Dispose();
            _subscriptionManager.Clear();
        }
    }
}
