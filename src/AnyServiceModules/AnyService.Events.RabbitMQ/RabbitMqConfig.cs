namespace AnyService.Events.RabbitMQ
{
    public class RabbitMqConfig
    {
        public string IncomingExchange { get; set; }
        public string IncomingExchangeType { get; set; } = "fanout";
        public string IncomingQueueName { get; set; }
        public string OutgoingExchange { get; set; }
        public string OutgoingExchangeType { get; set; } = "fanout";
        public int RetryCount { get; set; } = 5;
        public string HostName { get; set; }
        public int Port { get; set; } = 5672;
    }
}
