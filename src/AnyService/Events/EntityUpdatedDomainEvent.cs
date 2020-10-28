namespace AnyService.Events
{
    public sealed class EntityUpdatedDomainEvent : DomainEvent
    {
        public EntityUpdatedDomainEvent(IEntity before, IEntity after)
        {
            Data = new EntityUpdatedEventData
            {
                Before = before,
                After = after
            };
        }

        public class EntityUpdatedEventData
        {
            public IEntity Before { get; set; }
            public IEntity After { get; set; }
        }
    }
}