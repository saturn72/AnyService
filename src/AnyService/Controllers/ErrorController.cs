using AnyService.Events;
using AnyService.Logging;
using AnyService.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace AnyService.Controllers
{
    [ApiController]
    [Route("__error")]
    public class ErrorController : ControllerBase
    {
        #region fields
        public static string ErrorEventKey { get; internal set; }
        private readonly WorkContext _workContext;
        private readonly IDomainEventBus _eventBus;
        private readonly AnyServiceConfig _config;
        private readonly ILogger<ErrorController> _logger;
        private static Func<IExceptionHandlerPathFeature, IActionResult> ProblemResolver;
        #endregion
        #region ctor
        public ErrorController(
            WorkContext workContext,
            IDomainEventBus eventBus,
            AnyServiceConfig config,
            IWebHostEnvironment env,
            ILogger<ErrorController> logger
            )
        {
            _workContext = workContext;
            _eventBus = eventBus;
            _config = config;
            _logger = logger;
            ProblemResolver ??= env.IsDevelopment() || config.OutputErrorOnNonDevelopementEnv ?
                ehpf => Problem(
                detail: ehpf.Error.StackTrace,
                instance: ehpf.Path,
                statusCode: StatusCodes.Status500InternalServerError,
                title: ehpf.Error.Message
                ) :
                ehpf => Problem();
        }
        #endregion
        public IActionResult Error()
        {
            var ehpf = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            PublishErrorDetails(ehpf);

            return ProblemResolver(ehpf);
        }

        private void PublishErrorDetails(IExceptionHandlerPathFeature ehpf)
        {
            var record = BuildLogRecord(_workContext, ehpf.Error, HttpContext.TraceIdentifier, HttpContext.Request, ehpf.Path);

            _logger.LogError(LoggingEvents.UnexpectedException, ehpf.Error, $"{nameof(HttpContext.TraceIdentifier)}: {HttpContext.TraceIdentifier}");
            _logger.LogInformation(LoggingEvents.UnexpectedException, $"Publish event using {ErrorEventKey} key. Event Data: {record.ToJsonString()}");
            _eventBus.Publish(ErrorEventKey, new DomainEvent
            {
                Data = new
                {
                    exception = ehpf.Error,
                    logRecord = record
                },
                PerformedByUserId = _workContext.CurrentUserId,
                WorkContext = _workContext,
            });

        }
        private LogRecord BuildLogRecord(WorkContext workContext, Exception ex, string traceId, HttpRequest httpRequest, string path)
        {
            var request = new
            {
                url = httpRequest.Host.Value,
                port = httpRequest.Host.Port,
                method = httpRequest.Method,
                path = path,
                headers = httpRequest.Headers.Select(x => $"[{x.Key}:{x.Value}]").Aggregate((f, s) => $"{f}\n{s}")
            };

            return new LogRecord
            {
                Level = LogRecordLevel.Error,
                ClientId = workContext.CurrentClientId,
                UserId = workContext.CurrentUserId,
                TraceId = traceId,
                ExceptionRuntimeType = ex?.ToString(),
                ExceptionRuntimeMessage = ExtractExceptionMessage(ex),
                Message = "Unexpected runtime exception was fired.\nStackTrace: " + ex?.StackTrace,
                IpAddress = workContext.IpAddress,
                RequestPath = request.path,
                RequestHeaders = request.headers,
                HttpMethod = request.method,
                Request = request.ToJsonString(),
                CreatedOnUtc = DateTime.UtcNow.ToIso8601(),
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
    }
}
