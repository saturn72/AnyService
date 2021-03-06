using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;

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
                rabbitMqConfig.RetryCount <= 0)
                throw new ArgumentNullException(nameof(RabbitMqConfig));

            services.AddSingleton(rabbitMqConfig);
        }
    }
}
