using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using AnyService.Events;
using AnyService.Utilities;
using Microsoft.Extensions.Logging;
using AnyService.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System;
using AnyService.Logging;
using System.Linq;

namespace AnyService.Middlewares
{
    public class DefaultExceptionHandler : IExceptionHandler
    {
        private readonly IIdGenerator _idGenerator;
        private readonly ILogger<DefaultExceptionHandler> _logger;
        private readonly IEventBus _eventBus;
        private readonly IServiceProvider _serviceProvider;
        private const string ResponseJsonFormat = "{{\"exeptionId\":\"{0}\"}}";
        public DefaultExceptionHandler(IIdGenerator idGenerator,
        ILogger<DefaultExceptionHandler> logger, IEventBus eventBus,
        IServiceProvider serviceProvider)
        {
            _idGenerator = idGenerator;
            _logger = logger;
            _eventBus = eventBus;
            _serviceProvider = serviceProvider;
        }
        public async Task Handle(HttpContext context, object payload)
        {
            var wc = _serviceProvider.GetService<WorkContext>();
            _logger.LogDebug(LoggingEvents.UnexpectedException, $"UnexpectedException");
            var exId = _idGenerator.GetNext();
            HandleEventSourcing(context, wc, exId, payload.ToString());
            await HandleHttpResponseContent(context, exId);
        }
        private void HandleEventSourcing(HttpContext context, WorkContext workContext, object exId, string eventKey)
        {
            var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
            var ex = exceptionHandlerPathFeature?.Error;
            var httpRequest = context.Request;
            var record = BuildExceptionLogRecord(workContext, ex, exId.ToString(), httpRequest);

            _logger.LogDebug(LoggingEvents.EventPublishing, $"Publish event using {eventKey} key. Event Data: {record.ToJsonString()}");

            _eventBus.Publish(eventKey, new DomainEventData
            {
                Data = record,
                PerformedByUserId = workContext.CurrentUserId,
                WorkContext = workContext,
            });
        }
        private LogRecord BuildExceptionLogRecord(WorkContext workContext, Exception ex, string exceptionId, HttpRequest httpRequest)
        {
            var request = new
            {
                url = httpRequest.Host.Value,
                port = httpRequest.Host.Port,
                method = httpRequest.Method,
                path = httpRequest.Path,
                headers = httpRequest.Headers.Select(x => $"[{x.Key}:{x.Value}]").Aggregate((f, s) => $"{f}\n{s}"),
                query = httpRequest.QueryString.Value,
            };

            return new LogRecord
            {
                Level = LogRecordLevel.Error,
                ClientId = workContext.CurrentClientId,
                UserId = workContext.CurrentUserId,
                ExceptionId = exceptionId,
                ExceptionRuntimeType = ex?.ToString(),
                ExceptionRuntimeMessage = ExtractExceptionMessage(ex),
                Message = "Unexpected runtime exception was fired.\nStackTrace: " + ex?.StackTrace,
                IpAddress = workContext.IpAddress,
                RequestPath = request.path,
                RequestHeaders = request.headers,
                HttpMethod = request.method,
                Request = request.ToJsonString(),
                WorkContext = workContext.ToJsonString()
            };
        }
        private string ExtractExceptionMessage(Exception ex)
        {
            var tmp = ex;
            var exMsg = "";
            while (tmp != null)
            {
                exMsg += tmp.Message + "\n";
                tmp = tmp.InnerException;
            }
            return exMsg;
        }
        private async Task HandleHttpResponseContent(HttpContext context, object exceptionId)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            var responseBody = string.Format(ResponseJsonFormat, exceptionId);
            context.Response.ContentType = "text/json";
            await context.Response.WriteAsync(responseBody);
        }
    }
}