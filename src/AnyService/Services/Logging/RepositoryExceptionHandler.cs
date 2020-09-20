using AnyService.Events;
using AnyService.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AnyService.Services.Logging
{
    public class RepositoryExceptionHandler
    {
        #region fields
        private readonly IServiceProvider _serviceProvider;
        #endregion
        #region ctor
        public RepositoryExceptionHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        #endregion
        public Func<DomainEventData, Task> InsertRecord => ded =>
        {
            var lr = ded.Data.GetPropertyValueByName<LogRecord>("logRecord");
            if (lr == null) return Task.CompletedTask;

            var logManager = _serviceProvider.GetService<ILogManager>();
            return logManager.InsertLogRecord(lr);
        };
    }
}
