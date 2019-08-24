namespace AnyService.Services
{
    public sealed class EventKeyRecord
    {
        public EventKeyRecord(string createEventKey, string readEventKey, string updateEventKey, string deleteEventKey)
        {
            Create = createEventKey;
            Read = readEventKey;
            Update = updateEventKey;
            Delete = deleteEventKey;
        }

        public string Create { get; }
        public string Read { get; }
        public string Update { get; }
        public string Delete { get; }
    }
}