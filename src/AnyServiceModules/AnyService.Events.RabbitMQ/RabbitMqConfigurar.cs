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
        public void Configure(IServiceCollection services, IConfiguration configuration, string sectionName = "rabbitMq")
        {
            var rabbitMqConfig = new RabbitMqConfig();
            configuration.GetSection(sectionName).Bind(rabbitMqConfig);

            if ((rabbitMqConfig.Incoming?.Exchanges).IsNullOrEmpty())
                throw new ArgumentNullException($"{nameof(RabbitMqConfig)} - Please specify incoming exchanges {nameof(RabbitMqConfig.Incoming)}");

            if (!rabbitMqConfig.HostName.HasValue())
                throw new ArgumentNullException($"{nameof(RabbitMqConfig)} - Please specify {nameof(RabbitMqConfig.HostName)}");

            if (rabbitMqConfig.Port < 0)
                throw new ArgumentNullException($"{nameof(RabbitMqConfig)} - Please specify {nameof(RabbitMqConfig.Port)} for host");

            if (rabbitMqConfig.RetryCount <= 0)
                throw new ArgumentNullException($"{nameof(RabbitMqConfig)} - Please specify {nameof(RabbitMqConfig.RetryCount)} for send message retry");

            services.AddSingleton(rabbitMqConfig);

            var username = configuration[sectionName + ":username"];
            var password = configuration[sectionName + ":password"];
            services.AddSingleton<IConnectionFactory>(sp =>
            {
                var cf = new ConnectionFactory
                {
                    HostName = rabbitMqConfig.HostName,
                    Port = rabbitMqConfig.Port,
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
                var cfg = sp.GetService<RabbitMqConfig>();
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
        }
    }
}
