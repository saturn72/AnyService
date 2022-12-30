using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using RabbitMQ.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace AnyService.Events.RabbitMQ
{
    public class RabbitMqConfigurar
    {
        public RabbitMqOptions Configure(
            IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "rabbitMq")
        {
            var rabbitMqOptions = new RabbitMqOptions();
            configuration.GetSection(sectionName)
                .Bind(rabbitMqOptions);

            if ((rabbitMqOptions.Incoming?.Exchanges).IsNullOrEmpty() && rabbitMqOptions.Outgoing?.Length == 0)
                throw new ArgumentNullException($"{nameof(rabbitMqOptions)} - Please specify {nameof(RabbitMqOptions.Incoming)} and/or {nameof(RabbitMqOptions.Outgoing)} exchanges");

            if (!rabbitMqOptions.HostName.HasValue())
                throw new ArgumentNullException($"{nameof(rabbitMqOptions)} - Please specify {nameof(rabbitMqOptions.HostName)}");

            if (rabbitMqOptions.Port < 0)
                throw new ArgumentNullException($"{nameof(rabbitMqOptions)} - Please specify {nameof(rabbitMqOptions.Port)} for host");

            if (rabbitMqOptions.RetryCount <= 0)
                throw new ArgumentNullException($"{nameof(rabbitMqOptions)} - Please specify {nameof(rabbitMqOptions.RetryCount)} for send message retry");

            services.AddSingleton(rabbitMqOptions);

            var username = configuration[sectionName + ":username"];
            var password = configuration[sectionName + ":password"];
            services.AddSingleton<IConnectionFactory>(sp =>
            {
                var cf = new ConnectionFactory
                {
                    HostName = rabbitMqOptions.HostName,
                    Port = rabbitMqOptions.Port,
                    DispatchConsumersAsync = true,
                };
                if (username.HasValue())
                    cf.UserName = username;
                if (password.HasValue())
                    cf.Password = password;
                return cf;
            });
            services.TryAddSingleton<ICrossDomainEventPublishManager, CrossDomainEventPublishManager>();
            services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
            {
                var cfg = sp.GetService<RabbitMqOptions>();
                var lf = sp.GetService<ILoggerFactory>();
                return new DefaultRabbitMQPersistentConnection(
                    sp.GetService<IConnectionFactory>(),
                    lf.CreateLogger<DefaultRabbitMQPersistentConnection>(),
                    cfg.Endpoints,
                    cfg.RetryCount);
            });
            services.AddSingleton<ICrossDomainEventPublisher, RabbitMqCrossDomainEventPublisherSubscriber>();
            services.AddSingleton<ICrossDomainEventSubscriber, RabbitMqCrossDomainEventPublisherSubscriber>();
            services.TryAddSingleton<ISubscriptionManager<IntegrationEvent>, DefaultSubscriptionManager<IntegrationEvent>>();

            return rabbitMqOptions;
        }
    }
}
