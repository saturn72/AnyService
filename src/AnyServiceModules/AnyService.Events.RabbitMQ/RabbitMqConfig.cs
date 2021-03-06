namespace AnyService.Events.RabbitMQ
{
    public class RabbitMqConfig
    {
        public string BrokerName { get; set; }
        public string QueueName { get; set; }
        public int RetryCount { get; set; }
    }
}
