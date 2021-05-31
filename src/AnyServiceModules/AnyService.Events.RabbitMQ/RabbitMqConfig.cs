using System.Collections.Generic;
using System.ComponentModel;

namespace AnyService.Events.RabbitMQ
{
    public class RabbitMqConfig
    {
        [DefaultValue(5)]
        public int RetryCount { get; set; } = 5;
        public string HostName { get; set; }
        [DefaultValue(5672)]
        public int Port { get; set; } = 5672;
        public ChannelConfig Incoming { get; set; }
        public ExchangeConfig[] Outgoing { get; set; }
    }
    public class ChannelConfig
    {
        public ExchangeConfig[] Exchanges { get; set; }
        public QueueConfig[] Queues { get; set; }
    }
    public class ExchangeConfig
    {
        public IDictionary<string, object> Arguments { get; set; }
        public string Name { get; set; }
        [DefaultValue("direct")]
        public string Type { get; set; }
        [DefaultValue(false)]
        public bool AutoDelete { get; set; }
        [DefaultValue(false)]
        public bool Durable { get; set; }
    }
    public class QueueConfig
    {
        public IDictionary<string, object> Arguments { get; set; }
        [DefaultValue(false)]
        public bool AutoAck { get; set; }
        [DefaultValue(false)]
        public bool AutoDelete { get; set; }
        [DefaultValue(false)]
        public bool Durable { get; set; }
        [DefaultValue(false)]
        public bool Exclusive { get; set; }
        public string Name { get; set; }
        public string Exchange { get; set; }
    }
}
