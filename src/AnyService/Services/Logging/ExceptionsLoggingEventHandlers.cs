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

        public Func<Event, IServiceProvider, Task> CreateEventHandler => LogErrorOnException;
        public Func<Event, IServiceProvider, Task> ReadEventHandler => LogErrorOnException;
        public Func<Event, IServiceProvider, Task> UpdateEventHandler => LogErrorOnException;
        public Func<Event, IServiceProvider, Task> DeleteEventHandler => LogErrorOnException;
        #region Utilties
        private Func<Event, IServiceProvider, Task> LogErrorOnException => (evt, services) =>
        {
            var logger = services.GetService<ILogger<ExceptionsLoggingEventHandlers>>();
            var lr = evt.Data.GetPropertyValueOrDefaultByName<LogRecord>("logRecord");
            var traceId = lr?.TraceId;
            if (!traceId.HasValue())
                return Task.CompletedTask;

            logger.LogError(GetEventId(), evt.Data.GetPropertyValueByName<Exception>("exception"), traceId);
            return Task.CompletedTask;
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