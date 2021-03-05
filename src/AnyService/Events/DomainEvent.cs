namespace AnyService.Events
{
    public class DomainEvent: Event
    {
        public string PerformedByUserId { get; set; }
        public WorkContext WorkContext { get; set; }
    }
}