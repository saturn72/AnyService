using Microsoft.Extensions.Logging;

namespace AnyService.Services
{
    public sealed class LoggingEvents
    {
        public static readonly EventId BusinessLogicFlow = new EventId(1, "business-logic-flow");
        public static readonly EventId Audity = new EventId(2, "audity");
        public static readonly EventId Repository = new EventId(3, "repository");
        public static readonly EventId EventPublishing = new EventId(4, "event-publishing");
        public static readonly EventId Validation = new EventId(5, "validation");
        public static readonly EventId Controller = new EventId(6, "controller");
        public static readonly EventId Permission = new EventId(7, "permission");
        public static readonly EventId WorkContext = new EventId(7, "workcontext");
        public static readonly EventId Authorization = new EventId(8, "authorization");
    }
}