using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AnyService.Events;
using AnyService.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace AnyService.Services.Logging
{
    public class ExceptionsLoggingEventHandlers
    {
        private static readonly object lockObj = new object();
        private static IDictionary<string, int> _eventIndexes = new Dictionary<string, int>();
        private readonly IServiceProvider _serviceProvider;

        public ExceptionsLoggingEventHandlers(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public Func<DomainEvent, Task> CreateEventHandler => LogErrorOnException;
        public Func<DomainEvent, Task> ReadEventHandler => LogErrorOnException;
        public Func<DomainEvent, Task> UpdateEventHandler => LogErrorOnException;
        public Func<DomainEvent, Task> DeleteEventHandler => LogErrorOnException;
        #region Utilties
        private Func<DomainEvent, Task> LogErrorOnException => ed =>
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var logger = scope.ServiceProvider.GetService<ILogger<ExceptionsLoggingEventHandlers>>();
                var lr = ed.Data.GetPropertyValueOrDefaultByName<LogRecord>("logRecord");
                var traceId = lr?.TraceId;
                if (!traceId.HasValue())
                    return Task.CompletedTask;

                logger.LogError(GetEventId(), ed.Data.GetPropertyValueByName<Exception>("exception"), traceId);
                return Task.CompletedTask;
            }
        };
        private static EventId GetEventId()
        {
            var callerName = new StackTrace().GetFrame(0).GetMethod().Name;
            int eventIndex;
            lock (lockObj)
            {
                if (!_eventIndexes.TryGetValue(callerName, out eventIndex))
                {
                    eventIndex = _eventIndexes.Count + 1;
                    _eventIndexes[callerName] = eventIndex;
                }

            }
            return new EventId(eventIndex);
        }
        #endregion
    }
}