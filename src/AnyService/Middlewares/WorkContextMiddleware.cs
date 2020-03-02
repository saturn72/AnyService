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
        private static readonly IDictionary<string, EntityConfigRecord> RouteMaps = new Dictionary<string, EntityConfigRecord>();
        public WorkContextMiddleware(RequestDelegate next, ILogger<WorkContextMiddleware> logger)
        {
            _logger = logger;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, WorkContext workContext)
        {
            _logger.LogDebug(LoggingEvents.WorkContext, "Start WorkContextMiddleware invokation");
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!userId.HasValue())
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                _logger.LogDebug($"Missing userId - user is unauthorized!");
                return;
            }
            workContext.CurrentUserId = userId;

            var typeConfigRecord = GetRouteMap(httpContext.Request.Path);
            if (typeConfigRecord != null && !typeConfigRecord.Equals(default))
            {
                workContext.CurrentEntityConfigRecord = typeConfigRecord;
                workContext.RequestInfo = ToRequestInfo(httpContext, httpContext.Request.Method, typeConfigRecord);
            }
            _logger.LogDebug(LoggingEvents.WorkContext, "Finish parsing current WorkContext");
            await _next(httpContext);
        }
        private static EntityConfigRecord GetRouteMap(PathString path)
        {
            if (RouteMaps.TryGetValue(path, out EntityConfigRecord value))
                return value;

            value = EntityConfigRecordManager.EntityConfigRecords.FirstOrDefault(r => path.StartsWithSegments(r.Route, StringComparison.CurrentCultureIgnoreCase));
            return (RouteMaps[path] = value);
        }
        private static RequestInfo ToRequestInfo(HttpContext httpContext, string httpMethod, EntityConfigRecord typeConfigRecord)
        {
            var uric = httpContext.Request.Path.ToUriComponent();
            var path = httpContext.Request.Path.ToString();
            return new RequestInfo
            {
                Path = path,
                Method = httpMethod,
                RequesteeId = GetRequesteeId(typeConfigRecord.Route, path),
                Parameters = httpContext.Request.Query.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value)).ToArray()
            };
        }
        private static string GetRequesteeId(string route, string path)
        {
            var idx = path.LastIndexOf(route, 0, StringComparison.InvariantCultureIgnoreCase) + route.Length + 1;
            var requesteeId = path.Substring(idx);
            while (requesteeId.StartsWith("/"))
                requesteeId = requesteeId.Substring(1);

            if (requesteeId.StartsWith(Consts.ReservedPrefix))
            {
                idx = requesteeId.IndexOf("/");
                requesteeId = requesteeId.Substring(idx + 1);

                while (requesteeId.StartsWith("/"))
                    requesteeId = requesteeId.Substring(1);
            }

            return requesteeId;
        }
    }
}
