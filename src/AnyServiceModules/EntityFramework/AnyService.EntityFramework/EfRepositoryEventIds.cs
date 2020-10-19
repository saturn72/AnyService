using Microsoft.Extensions.Logging;

namespace AnyService.EntityFramework
{
    public class EfRepositoryEventIds
    {
        public static EventId Create = new EventId(101, "create");
        public static EventId Read = new EventId(102, "read");
        public static EventId Update = new EventId(103, "update");
        public static EventId Delete = new EventId(104, "delete");

        public static EventId EfRepositoryBridge = new EventId(105, "bridge");
    }
}
