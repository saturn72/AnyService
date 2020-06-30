using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AnyService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AnyService.Middlewares
{
    public class WorkContextMiddleware
    {
        private readonly ILogger<WorkContextMiddleware> _logger;
        private readonly RequestDelegate _next;
        protected readonly IReadOnlyDictionary<string, EntityConfigRecord> RouteMaps;
        private readonly Func<HttpContext, WorkContext, ILogger, Task<bool>> _onMissingUserIdHandler;

        public WorkContextMiddleware(
            RequestDelegate next,
            ILogger<WorkContextMiddleware> logger,
            IEnumerable<EntityConfigRecord> entityConfigRecords,
            Func<HttpContext, WorkContext, ILogger, Task<bool>> onMissingUserIdHandler = null
            )
        {
            _logger = logger;
            _next = next;
            RouteMaps = LoadRoutes(entityConfigRecords);
            _onMissingUserIdHandler = onMissingUserIdHandler ??= OnMissingUserIdWorkContextMiddlewareHandlers.DefaultOnMissingUserIdHandler;
        }

        public async Task InvokeAsync(HttpContext httpContext, WorkContext workContext)
        {
            _logger.LogDebug(LoggingEvents.WorkContext, "Start WorkContextMiddleware invokation");
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogDebug(LoggingEvents.WorkContext, $"UserId = {userId}");
            var clientId = httpContext.User.FindFirst("client_id")?.Value;
            _logger.LogDebug(LoggingEvents.WorkContext, $"ClientId = { clientId}");

            if (!clientId.HasValue() && !userId.HasValue() && !(await _onMissingUserIdHandler(httpContext, workContext, _logger)))
                return;

            workContext.CurrentUserId = userId;
            workContext.CurrentClientId = clientId;

            var ecr = GetEntityconfigRecordByRoute(httpContext.Request.Path);
            if (ecr != null && !ecr.Equals(default))
            {
                workContext.CurrentEntityConfigRecord = ecr;
                workContext.RequestInfo = ToRequestInfo(httpContext, ecr);
            }
            _logger.LogDebug(LoggingEvents.WorkContext, "Finish parsing current WorkContext");
            await _next(httpContext);
        }
        private IReadOnlyDictionary<string, EntityConfigRecord> LoadRoutes(IEnumerable<EntityConfigRecord> entityConfigRecords)
        {
            var res = new Dictionary<string, EntityConfigRecord>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var ecr in entityConfigRecords)
                res[ecr.Route] = ecr;
            return res;
        }
        private EntityConfigRecord GetEntityconfigRecordByRoute(PathString path)
        {
            var e = RouteMaps.FirstOrDefault(rm => path.StartsWithSegments(rm.Key));
            var res = (e.Equals(default)) ? null : e.Value;
            _logger.LogDebug(LoggingEvents.WorkContext, 
                res != null ? 
                    $"Entity found: {res.Type.Name}. using path: {path}" : 
                    $"Entity was not found in anyservice entities collection. using path: {path}");
            return res;
        }
        private RequestInfo ToRequestInfo(HttpContext httpContext, EntityConfigRecord typeConfigRecord)
        {
            var path = httpContext.Request.Path.ToString();
            _logger.LogDebug(LoggingEvents.WorkContext, $"Parse httpRequestInfo from route: {path}");
            var reqInfo = new RequestInfo
            {
                Path = path,
                Method = httpContext.Request.Method,
                RequesteeId = GetRequesteeId(typeConfigRecord.Route, path),
                Parameters = httpContext.Request.Query?.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value)).ToArray()
            };
            _logger.LogDebug(LoggingEvents.WorkContext, $"Parsed requestInfo: {reqInfo.ToJsonString()}");
            return reqInfo;
        }
        private string GetRequesteeId(string route, string path)
        {
            _logger.LogDebug(LoggingEvents.WorkContext, $"Extract {nameof(RequestInfo.RequesteeId)} from path: {path}");
            var idx = path.LastIndexOf(route, 0, StringComparison.InvariantCultureIgnoreCase) + route.Length + 1;
            var requesteeId = path.Substring(idx);
            while (requesteeId.StartsWith("/"))
                requesteeId = requesteeId.Substring(1);

            while (requesteeId.StartsWith(Consts.ReservedPrefix))
            {
                idx = requesteeId.IndexOf("/");
                if (idx < 0)
                    return null;
                requesteeId = requesteeId.Substring(idx + 1);

                while (requesteeId.StartsWith("/"))
                    requesteeId = requesteeId.Substring(1);
            }
            _logger.LogDebug(LoggingEvents.WorkContext, $"Extracted {nameof(RequestInfo.RequesteeId)} value = {requesteeId}");
            return requesteeId;
        }
    }
}
