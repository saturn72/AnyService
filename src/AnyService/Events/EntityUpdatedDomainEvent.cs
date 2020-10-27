namespace AnyService.Events
{
    public sealed class EntityUpdatedDomainEvent<T> : DomainEvent
    {
        public EntityUpdatedDomainEvent(T before, T after)
        {
            Data = new EntityUpdatedEventData
            {
                Before = before,
                After = after
            };
        }

        public class EntityUpdatedEventData
        {
            public T Before { get; set; }
            public T After { get; set; }
        }
    }
}