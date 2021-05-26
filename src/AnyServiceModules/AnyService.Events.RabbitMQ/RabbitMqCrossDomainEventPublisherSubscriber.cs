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

        private readonly IServiceProvider _services;
        private readonly ISubscriptionManager<IntegrationEvent> _subscriptionManager;
        private readonly RabbitMqConfig _config;
        private readonly ILogger<RabbitMqCrossDomainEventPublisherSubscriber> _logger;
        private readonly IRabbitMQPersistentConnection _publisherPersistentConnection;
        private readonly IModel _publisherChannel;
        private readonly IRabbitMQPersistentConnection _consumerPersistentConnection;
        private IModel _consumerChannel;

        public RabbitMqCrossDomainEventPublisherSubscriber(
            IRabbitMQPersistentConnection publisherPersistentConnection,
            IRabbitMQPersistentConnection consumerPersistentConnection,
            IServiceProvider services,
            ISubscriptionManager<IntegrationEvent> subscriptionManager,
            RabbitMqConfig config,
            ILogger<RabbitMqCrossDomainEventPublisherSubscriber> logger
            )
        {
            _publisherPersistentConnection = publisherPersistentConnection ?? throw new ArgumentNullException(nameof(publisherPersistentConnection));
            _consumerPersistentConnection = consumerPersistentConnection ?? throw new ArgumentNullException(nameof(consumerPersistentConnection));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _subscriptionManager = subscriptionManager;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _publisherChannel = _publisherPersistentConnection.CreateModel();
            _consumerChannel = CreateConsumerChannel();
        }

        private async Task OnHandlerRemoved(string @namespace, string eventKey)
        {
            TryConnect(_consumerPersistentConnection);
            _consumerChannel.QueueUnbind(
                queue: _config.IncomingQueue.Name,
                exchange: _config.IncomingExchange,
                routingKey: $"{@namespace}/{eventKey}");

            var allHandlers = await _subscriptionManager.GetAllHandlers();
            if (allHandlers.IsNullOrEmpty())
            {
                _config.IncomingQueue.Name = string.Empty;
                _consumerChannel.Close();
            }
        }
        public async Task Publish(IntegrationEvent @event)
        {
            TryConnect(_publisherPersistentConnection);
            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetryAsync(_config.RetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", @event.Id, $"{time.TotalSeconds:n1}", ex.Message);
                });

            _logger.LogDebug("Creating RabbitMQ channel to publish event: {namespace} {EventId} ({EventName})", @event.Namespace, @event.Id, @event.EventKey);
            _logger.LogDebug("Declaring RabbitMQ exchange to publish event: {EventId}", @event.Id);

            _publisherChannel.ExchangeDeclare(exchange: _config.OutgoingExchange, type: _config.OutgoingExchangeType);

            var message = @event.ToJsonString();
            var body = Encoding.UTF8.GetBytes(message);

            await policy.ExecuteAsync(() =>
            {
                var properties = _publisherChannel.CreateBasicProperties();
                properties.DeliveryMode = 2; // persistent
                _logger.LogDebug("Publishing event to RabbitMQ: {EventId}", @event.Id);
                return Task.Run(() => _publisherChannel.BasicPublish(
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
            TryConnect(_consumerPersistentConnection);
            _consumerChannel.QueueBind(
                queue: _config.IncomingQueue.Name,
                exchange: _config.IncomingExchange,
                routingKey: $"{@namespace}/{eventKey}");
            StartBasicConsume();
            return handlerId;
        }
        private void TryConnect(IRabbitMQPersistentConnection persistentConnection)
        {
            if (!persistentConnection.IsConnected)
                persistentConnection.TryConnect();
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
                    queue: _config.IncomingQueue.Name,
                    autoAck: _config.IncomingQueue.AutoAck,
                    consumer: consumer,
                    exclusive: _config.IncomingQueue.Exclusive,
                    arguments: _config.IncomingQueue.Arguments);
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
            TryConnect(_consumerPersistentConnection);
            _logger.LogTrace("Creating RabbitMQ consumer channel");
            _consumerChannel = _consumerPersistentConnection.CreateModel();
            _consumerChannel.ExchangeDeclare(exchange: _config.IncomingExchange,
                                    type: _config.IncomingExchangeType);
            _consumerChannel.QueueDeclare(queue: _config.IncomingQueue.Name ?? "",
                                 durable: _config.IncomingQueue.Durable,
                                 exclusive: _config.IncomingQueue.Exclusive,
                                 autoDelete: _config.IncomingQueue.AutoDelete,
                                 arguments: _config.IncomingQueue.Arguments);

            _consumerChannel.CallbackException += (sender, ea) =>
            {
                _logger.LogWarning(ea.Exception, "Recreating RabbitMQ consumer channel");

                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel();
                StartBasicConsume();
            };

            return _consumerChannel;
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
            using var scope = _services.CreateScope();
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
            //clear publisher
            _publisherChannel.Dispose();
            //clear subscriber
            _consumerChannel?.Dispose();
            _subscriptionManager.Clear();
        }
    }
}
