using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task InvokeAsync(HttpContext context, WorkContext workContext)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            workContext.CurrentUserId = userId;

            var path = context.Request.Path;
            workContext.RequestPath = path;
            workContext.HttpMethod = context.Request.Method;

            var typeConfigRecord = GetRouteMap(path);
            if (typeConfigRecord != null && !typeConfigRecord.Equals(default))
            {
                workContext.CurrentTypeConfigRecord = typeConfigRecord;
                workContext.CurrentType = typeConfigRecord.Type;
            }
            await _next(context);
        }
        private TypeConfigRecord GetRouteMap(PathString path)
        {
            if (RouteMaps.TryGetValue(path, out TypeConfigRecord value))
                return value;

            value = TypeConfigRecordManager.TypeConfigRecords.FirstOrDefault(r => path.StartsWithSegments(r.RoutePrefix, StringComparison.CurrentCultureIgnoreCase));

            return (RouteMaps[path] = value);
        }
    }
}
