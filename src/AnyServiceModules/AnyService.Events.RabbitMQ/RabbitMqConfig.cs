namespace AnyService.Events.RabbitMQ
{
    public class RabbitMqConfig
    {
        public string IncomingExchange { get; set; }
        public string OutgoingExchange { get; set; }
        public string IncomingQueueName { get; set; }
        public int RetryCount { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
    }
}
