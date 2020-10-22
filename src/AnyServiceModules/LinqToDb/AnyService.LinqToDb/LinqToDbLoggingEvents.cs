using Microsoft.Extensions.Logging;

namespace AnyService.LinqToDb
{
    public sealed class LinqToDbLoggingEvents
    {
        public static readonly EventId BulkInsert = new EventId(1, "bulk-insert");
        public static readonly EventId Insert = new EventId(2, "insert");
        public static readonly EventId BulkRead = new EventId(3, "bulk-read");
        public static readonly EventId Read = new EventId(4, "read");
        public static readonly EventId BulkUpdate = new EventId(5, "bulk-update");
        public static readonly EventId Update = new EventId(6, "update");
        public static readonly EventId BulkDelete = new EventId(7, "bulk-delete");
        public static readonly EventId Delete = new EventId(8, "delete");
    }
}
