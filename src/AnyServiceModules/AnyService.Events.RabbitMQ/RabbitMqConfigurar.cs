using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using RabbitMQ.Client;

namespace AnyService.Events.RabbitMQ
{
    public class RabbitMqConfigurar
    {
        public void Configure(IServiceCollection services, IConfiguration configuration)
        {
            var rabbitMqConfig = new RabbitMqConfig();
            configuration.GetSection("rabbitMq").Bind(rabbitMqConfig);
            if (
                rabbitMqConfig.BrokerName.HasValue() ||
                rabbitMqConfig.QueueName.HasValue() ||
                rabbitMqConfig.HostName.HasValue() ||
                rabbitMqConfig.Port < 0 ||
                rabbitMqConfig.RetryCount <= 0)
                throw new ArgumentNullException(nameof(RabbitMqConfig));

            services.AddSingleton(rabbitMqConfig);

            var username = configuration["rabbitMq:username"];
            if (!username.HasValue())
                throw new ArgumentNullException(nameof(username));
            var password = configuration["rabbitMq:password"];
            if (!password.HasValue())
                throw new ArgumentNullException(nameof(password));
            services.AddSingleton<IConnectionFactory>(sp => new ConnectionFactory
            {
                HostName = rabbitMqConfig.HostName,
                Port = rabbitMqConfig.Port,
                DispatchConsumersAsync = true,
                UserName = username,
                Password = password,
            });
        }
    }
}
