using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AnyService.Events;
using AnyService.Logging;
using Microsoft.Extensions.Logging;

namespace AnyService.Services.Logging
{
    public class ExceptionsLoggingEventHandlers
    {
        private static readonly object lockObj = new object();
        private static IDictionary<string, int> _eventIndexes = new Dictionary<string, int>();
        private readonly ILogger _logger;

        public ExceptionsLoggingEventHandlers(ILogger logger)
        {
            _logger = logger;
        }
        public Func<DomainEventData, Task> CreateEventHandler => LogErrorOnException;
        public Func<DomainEventData, Task> ReadEventHandler => LogErrorOnException;
        public Func<DomainEventData, Task> UpdateEventHandler => LogErrorOnException;
        public Func<DomainEventData, Task> DeleteEventHandler => LogErrorOnException;
        #region Utilties
        private Func<DomainEventData, Task> LogErrorOnException => ed =>
        {
            var lr = ed.Data.GetPropertyValueOrDefaultByName<LogRecord>("logRecord");
            var exceptionId = lr?.TraceId;
            if (!exceptionId.HasValue())
                return Task.CompletedTask;

            _logger.LogError(GetEventId(), ed.Data.GetPropertyValueByName<Exception>("exception"), exceptionId);
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