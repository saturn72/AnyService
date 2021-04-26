using System.ComponentModel;

namespace AnyService.Events.RabbitMQ
{
    public class RabbitMqConfig
    {
        public string IncomingExchange { get; set; }
        [DefaultValue("direct")]
        public string IncomingExchangeType { get; set; } = "direct";
        public string IncomingQueueName { get; set; }
        public string OutgoingExchange { get; set; }
        [DefaultValue("direct")]
        public string OutgoingExchangeType { get; set; } = "direct";
        [DefaultValue(5)]
        public int RetryCount { get; set; } = 5;
        public string HostName { get; set; }
        [DefaultValue(5672)]
        public int Port { get; set; } = 5672;
    }
}
