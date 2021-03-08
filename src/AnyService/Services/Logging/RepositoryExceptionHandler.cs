using AnyService.Events;
using AnyService.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AnyService.Services.Logging
{
    public class RepositoryExceptionHandler
    {
        public Func<DomainEvent, IServiceProvider, Task> InsertRecord => (evt, services) =>
        {
            var lr = evt.Data.GetPropertyValueByName<LogRecord>("logRecord");
            if (lr == null) return Task.CompletedTask;

            return services.GetService<ILogRecordManager>().InsertLogRecord(lr);
        };
    }
}
