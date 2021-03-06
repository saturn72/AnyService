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

    public class RabbitMqCrossDomainEventBus : ICrossDomainEventBus, IDisposable
    {
        const int DefaultTasksTimeout = 60000;

        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISubscriptionManager<IntegrationEvent> _subscriptionManager;
        private readonly RabbitMqConfig _config;
        private readonly ILogger<RabbitMqCrossDomainEventBus> _logger;
        private IModel _consumerChannel;

        public RabbitMqCrossDomainEventBus(
            IRabbitMQPersistentConnection persistentConnection,
            IServiceProvider serviceProvider,
            ISubscriptionManager<IntegrationEvent> subscriptionManager,
            RabbitMqConfig config,
            ILogger<RabbitMqCrossDomainEventBus> logger,
            int retryCount = 5
            )
        {
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _consumerChannel = CreateConsumerChannel();
        }

        private async Task OnHandlerRemoved(string handlerId)
        {
            TryConnect();
            using var channel = _persistentConnection.CreateModel();
            channel.QueueUnbind(queue: _config.QueueName,
                exchange: _config.BrokerName,
                routingKey: handlerId);

            var allHandlers = await _subscriptionManager.GetAllHandlers();
            if (allHandlers.IsNullOrEmpty())
            {
                _config.QueueName = string.Empty;
                _consumerChannel.Close();
            }
        }
        public async Task Publish(string eventKey, IntegrationEvent @event)
        {
            var handlersData = await _subscriptionManager.GetHandlers(eventKey);
            if (handlersData.IsNullOrEmpty())
                return;

            TryConnect();
            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_config.RetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", @event.Id, $"{time.TotalSeconds:n1}", ex.Message);
                });

            _logger.LogDebug("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", @event.Id, eventKey);

            using var channel = _persistentConnection.CreateModel();
            _logger.LogDebug("Declaring RabbitMQ exchange to publish event: {EventId}", @event.Id);

            channel.ExchangeDeclare(exchange: _config.BrokerName, type: "direct");

            var message = @event.ToJsonString();
            var body = Encoding.UTF8.GetBytes(message);

            policy.Execute(() =>
            {
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2; // persistent
                _logger.LogDebug("Publishing event to RabbitMQ: {EventId}", @event.Id);
                channel.BasicPublish(
                    exchange: _config.BrokerName,
                    routingKey: eventKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
            });
        }
        public async Task<string> Subscribe(string eventKey, Func<IntegrationEvent, IServiceProvider, Task> handler, string name)
        {
            _logger.LogDebug("Subscribing event handler for {EventKey} with {Name}", eventKey, name);

            var handlerId = await _subscriptionManager.Subscribe(eventKey, handler, name);
            if (handlerId.HasValue())
            {
                TryConnect();
                using var channel = _persistentConnection.CreateModel();
                channel.QueueBind(queue: _config.QueueName,
                                  exchange: _config.BrokerName,
                                  routingKey: eventKey);
                StartBasicConsume();
            }
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
            await _subscriptionManager.Unsubscribe(handlerId);
            if (handlerId.HasValue())
                _ = OnHandlerRemoved(handlerId);
        }


        private void StartBasicConsume()
        {
            _logger.LogTrace("Starting RabbitMQ basic consume");

            if (_consumerChannel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

                consumer.Received += Consumer_Received;

                _consumerChannel.BasicConsume(
                    queue: _config.QueueName,
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
            var eventName = eventArgs.RoutingKey;
            var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

            try
            {
                if (message.ToLowerInvariant().Contains("throw-fake-exception"))
                {
                    throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
                }

                await ProcessEvent(eventName, message);
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

            channel.ExchangeDeclare(exchange: _config.BrokerName,
                                    type: "direct");

            channel.QueueDeclare(queue: _config.QueueName,
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

        private async Task ProcessEvent(string eventKey, string message)
        {
            _logger.LogTrace("Processing RabbitMQ event: {EventKey}", eventKey);
            var handlerDatas = await _subscriptionManager.GetHandlers(eventKey);
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
            if (_consumerChannel != null)
                _consumerChannel.Dispose();
            _subscriptionManager.Clear();
        }
    }
}
