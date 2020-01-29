using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AnyService.Middlewares
{
    public class WorkContextMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly IDictionary<string, TypeConfigRecord> RouteMaps = new Dictionary<string, TypeConfigRecord>();
        public WorkContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, WorkContext workContext)
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!userId.HasValue())
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            workContext.CurrentUserId = userId;

            var typeConfigRecord = GetRouteMap(httpContext.Request.Path);
            if (typeConfigRecord != null && !typeConfigRecord.Equals(default))
            {
                workContext.CurrentTypeConfigRecord = typeConfigRecord;
                workContext.CurrentType = typeConfigRecord.Type;
                workContext.RequestInfo = ToRequestInfo(httpContext, httpContext.Request.Method, typeConfigRecord);
            }
            await _next(httpContext);
        }
        private static TypeConfigRecord GetRouteMap(PathString path)
        {
            if (RouteMaps.TryGetValue(path, out TypeConfigRecord value))
                return value;

            value = TypeConfigRecordManager.TypeConfigRecords.FirstOrDefault(r => path.StartsWithSegments(r.RoutePrefix, StringComparison.CurrentCultureIgnoreCase));

            return (RouteMaps[path] = value);
        }
        private static RequestInfo ToRequestInfo(HttpContext httpContext, string httpMethod, TypeConfigRecord typeConfigRecord)
        {
            var uric = httpContext.Request.Path.ToUriComponent();
            var path = httpContext.Request.Path.ToString();
            return new RequestInfo
            {
                Path = path,
                Method = httpMethod,
                RequesteeId = GetRequesteeId(),
                Parameters = httpContext.Request.Query.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value)).ToArray()
            };
            string GetRequesteeId()
            {
                var resource = typeConfigRecord.RoutePrefix;
                var idx = path.LastIndexOf(resource, 0, StringComparison.InvariantCultureIgnoreCase) + resource.Length + 1;
                var requesteeId = path.Substring(idx);
                return requesteeId.StartsWith("/") ? requesteeId.Substring(1) : requesteeId;
            }
        }
    }
}
