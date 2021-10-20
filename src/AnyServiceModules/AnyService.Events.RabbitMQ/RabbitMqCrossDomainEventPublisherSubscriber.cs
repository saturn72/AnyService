﻿using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceProvider _services;
        private readonly ISubscriptionManager<IntegrationEvent> _subscriptionManager;
        private readonly RabbitMqOptions _config;
        private readonly ILogger<RabbitMqCrossDomainEventPublisherSubscriber> _logger;
        private readonly IRabbitMQPersistentConnection _publisherPersistentConnection;
        private readonly IRabbitMQPersistentConnection _consumerPersistentConnection;
        private IModel _publisherChannel;
        private IModel _consumerChannel;

        public RabbitMqCrossDomainEventPublisherSubscriber(
            IRabbitMQPersistentConnection publisherPersistentConnection,
            IRabbitMQPersistentConnection consumerPersistentConnection,
            IServiceProvider services,
            ISubscriptionManager<IntegrationEvent> subscriptionManager,
            RabbitMqOptions config,
            ILogger<RabbitMqCrossDomainEventPublisherSubscriber> logger
            )
        {
            _publisherPersistentConnection = publisherPersistentConnection ?? throw new ArgumentNullException(nameof(publisherPersistentConnection));
            _consumerPersistentConnection = consumerPersistentConnection ?? throw new ArgumentNullException(nameof(consumerPersistentConnection));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _subscriptionManager = subscriptionManager;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _publisherChannel = CreatePublisherChannel();
            _consumerChannel = CreateConsumerChannel();
        }

        private async Task OnHandlerRemoved(string @namespace, string eventKey)
        {
            _logger.LogDebug($"Remove handler for: {@namespace}, with {eventKey}");
            TryConnect(_consumerPersistentConnection);
            var routingKey = $"{@namespace}/{eventKey}";

            var qs = _config.Incoming.Queues;
            if (!qs.IsNullOrEmpty())
            {
                foreach (var q in qs)
                {
                    _logger.LogDebug($"Unbind queues {q.Name}, from exchange: {q.Exchange}, with routing key: {routingKey}");
                    _consumerChannel.QueueUnbind(
                        queue: q.Name,
                        exchange: q.Exchange,
                        routingKey: routingKey,
                        arguments: q.Arguments);
                }
            }
            var allHandlers = await _subscriptionManager.GetAllHandlers();
            if (allHandlers.IsNullOrEmpty())
                _consumerChannel.Close();
        }
        public async Task Publish(IntegrationEvent @event)
        {
            _logger.LogDebug("Publishing event: {namespace} {EventId} ({EventName}) {EventJson}", @event.Exchange, @event.Id, @event.RoutingKey, @event.ToJsonString());
            TryConnect(_publisherPersistentConnection);
            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetryAsync(_config.RetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", @event.Id, $"{time.TotalSeconds:n1}", ex.Message);
                });

            //_publisherChannel.ExchangeDeclarePassive(exchange: _config.OutgoingExchange);

            var message = @event.ToJsonString();
            var body = Encoding.UTF8.GetBytes(message);
            await policy.ExecuteAsync(() =>
            {
                var properties = _publisherChannel.CreateBasicProperties();
                if (@event.Expiration != default) //update expiration
                    properties.Expiration = @event.Expiration.ToString();
                properties.DeliveryMode = 2; // persistent
                _logger.LogDebug("Publishing event to RabbitMQ: {EventId}", @event.Id);
                return Task.Run(() =>
                    _publisherChannel.BasicPublish(
                                exchange: @event.Exchange,
                                routingKey: @event.RoutingKey ?? string.Empty,
                                mandatory: true,
                                basicProperties: properties,
                                body: body));
            });
        }
        public async Task<string> Subscribe(string exchange, string routingKey, Func<IntegrationEvent, IServiceProvider, Task> handlerSink, string alias)
        {
            _logger.LogDebug("Subscribing event handler for {EventKey} with {Name}", routingKey, alias);

            var handlerId = await _subscriptionManager.Subscribe(exchange, routingKey, handlerSink, alias);
            if (!handlerId.HasValue())
                throw new InvalidOperationException("Failed to subscribe handler");

            StartBasicConsume();
            return handlerId;
        }
        private void TryConnect(IRabbitMQPersistentConnection persistentConnection)
        {
            if (!persistentConnection.IsConnected)
            {
                _logger.LogDebug("Try reconnect...");
                persistentConnection.TryConnect();
            }
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
                var qs = _config.Incoming.Queues;
                if (!qs.IsNullOrEmpty())
                {
                    foreach (var q in qs)
                        _consumerChannel.BasicConsume(
                            queue: q.Name,
                            autoAck: q.AutoAck,
                            consumer: consumer,
                            exclusive: q.Exclusive,
                            arguments: q.Arguments);
                }
            }
            else
            {
                _logger.LogError($"{nameof(StartBasicConsume)} can't call on _consumerChannel == null");
            }
        }
        private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            _logger.LogDebug($"{nameof(Consumer_Received)}");
            var exchange = eventArgs.Exchange;
            var routingKey = eventArgs.RoutingKey;
            _logger.LogDebug($"{nameof(Consumer_Received)}: {nameof(eventArgs.Exchange)}: {exchange}, {nameof(eventArgs.RoutingKey)}: {routingKey}");

            var message = Encoding.UTF8.GetString(eventArgs.Body.Span);
            try
            {
                await ProcessEvent(exchange, routingKey, message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "----- ERROR Processing message \"{Message}\"", message);
            }
            // Even on exception we take the message off the queue.
            // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
            // For more information see: https://www.rabbitmq.com/dlx.html
            _consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            await Task.Yield();
        }
        private IModel CreatePublisherChannel()
        {
            TryConnect(_publisherPersistentConnection);
            _logger.LogTrace("Creating RabbitMQ publisher channel");
            var channel = _consumerPersistentConnection.CreateModel();

            channel.CallbackException += (sender, ea) =>
            {
                _logger.LogWarning(ea.Exception, "Recreating RabbitMQ publisher channel");

                channel.Dispose();
                _publisherChannel = CreatePublisherChannel();
            };

            return channel;
        }
        private IModel CreateConsumerChannel()
        {
            TryConnect(_consumerPersistentConnection);
            _logger.LogTrace("Creating RabbitMQ consumer channel");
            _consumerChannel = _consumerPersistentConnection.CreateModel();
            _logger.LogTrace("Consumer channel created successfully");

            foreach (var ex in _config.Incoming.Exchanges)
            {
                _logger.LogTrace("Declaring exchange {Exchange}", ex);
                _consumerChannel.ExchangeDeclare(
                    exchange: ex.Name,
                    type: ex.Type,
                    durable: ex.Durable,
                    autoDelete: ex.AutoDelete,
                    arguments: ex.Arguments);
                _logger.LogTrace($"Exchange declared: {nameof(ex.Name)} = {ex.Name}, {nameof(ex.Type)} = {ex.Type}, {nameof(ex.Durable)} = {ex.Durable}, {nameof(ex.AutoDelete)} = {ex.AutoDelete}, {nameof(ex.Arguments)} = {ex.Arguments?.ToJsonString() ?? ""}");

            }

            var qs = _config.Incoming.Queues;
            if (!qs.IsNullOrEmpty())
            {
                _logger.LogTrace("Declaring queues");
                foreach (var q in qs)
                {
                    _logger.LogTrace("Declaring queue: {Queue}", q.Name);

                    if (q.Arguments?.ContainsKey("x-message-ttl") == true)
                    {
                        var ttl = q.Arguments["x-message-ttl"].ToString();
                        q.Arguments["x-message-ttl"] = int.Parse(ttl);
                        _logger.LogTrace("Added: 'x-message-ttl' = {ttl} to queue's argumants", ttl);
                    }

                    _consumerChannel.QueueDeclare(queue: q.Name ?? "",
                                         durable: q.Durable,
                                         exclusive: q.Exclusive,
                                         autoDelete: q.AutoDelete,
                                         arguments: q.Arguments);
                    _logger.LogTrace($"Queue declared: {nameof(q.Name)} = {q.Name}, {nameof(q.Durable)} = {q.Durable}, {nameof(q.Exclusive)} = {q.Exclusive}, {nameof(q.AutoDelete)} = {q.AutoDelete}, {nameof(q.Arguments)} = {q.Arguments?.ToJsonString() ?? ""}");

                    q.RoutingKey ??= string.Empty;
                    _consumerChannel.QueueBind(
                        exchange: q.Exchange,
                        queue: q.Name,
                        routingKey: q.RoutingKey);
                    _logger.LogTrace("Queue {qName} was binded to Exchange {qExchange} with RoutingKey = {qRoutingKey}", q.Name, q.Exchange, q.RoutingKey);
                }
            }
            _consumerChannel.CallbackException += (sender, ea) =>
            {
                _logger.LogWarning(ea.Exception, "Recreating RabbitMQ consumer channel");

                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel();
                StartBasicConsume();
            };
            return _consumerChannel;
        }
        private async Task ProcessEvent(string exchange, string routingKey, string message)
        {
            _logger.LogTrace("Processing RabbitMQ event: {exchange}/{routingKey}", exchange, routingKey);
            var handlerDatas = await _subscriptionManager.GetHandlers(exchange, routingKey);
            if (handlerDatas.IsNullOrEmpty())
            {
                _logger.LogWarning("No subscription for RabbitMQ event: {exchange}/{routingKey}", exchange, routingKey);
                return;
            }
            await Task.Yield();
            using var scope = _services.CreateScope();
            foreach (var hd in handlerDatas)
            {
                var @event = message.ToObject<IntegrationEvent>();
                _ = hd.Handler(@event, scope.ServiceProvider);
            }
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
