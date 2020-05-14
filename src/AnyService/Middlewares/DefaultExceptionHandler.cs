using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using AnyService.Events;
using AnyService.Utilities;
using Microsoft.Extensions.Logging;
using AnyService.Services;
using Microsoft.AspNetCore.Diagnostics;

namespace AnyService.Middlewares
{
    public class DefaultExceptionHandler : IExceptionHandler
    {
        private readonly IIdGenerator _idGenerator;
        private readonly ILogger<DefaultExceptionHandler> _logger;
        private readonly IEventBus _eventBus;
        private const string ResponseJsonFormat = "{{\"exeptionId\":\"{0}\"}}";
        public DefaultExceptionHandler(IIdGenerator idGenerator, ILogger<DefaultExceptionHandler> logger,
         IEventBus eventBus)
        {
            _idGenerator = idGenerator;
            _logger = logger;
            _eventBus = eventBus;
        }
        public async Task Handle(HttpContext context, WorkContext workContext, object payload)
        {
            _logger.LogDebug(LoggingEvents.UnexpectedException, $"UnexpectedException");
            var exId = _idGenerator.GetNext();
            HandleEventSourcing(context, workContext, exId, payload.ToString());
            await HandleHttpResponseContent(context, exId);
        }
        private void HandleEventSourcing(HttpContext context, WorkContext workContext, object exId, string eventKey)
        {
            var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
            var exceptionData = new
            {
                workContext,
                exceptionId = exId,
                exception = exceptionHandlerPathFeature?.Error,
                requestData = new
                {
                    Path = exceptionHandlerPathFeature?.Path,
                    Request = context.Request
                },
            };

            _logger.LogDebug(LoggingEvents.EventPublishing, $"Publish event using {eventKey} key");
            _eventBus.Publish(eventKey, new DomainEventData
            {
                Data = exceptionData,
                PerformedByUserId = workContext.CurrentUserId
            });
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