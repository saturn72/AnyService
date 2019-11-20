using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AnyService
{
    public class AnyServiceMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly IDictionary<string, TypeConfigRecord> RouteMaps = new Dictionary<string, TypeConfigRecord>();
        public AnyServiceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, WorkContext workContext)
        {
            workContext.CurrentUserId = "some-user-id";

            var path = context.Request.Path;
            var typeConfigRecord = GetRouteMap(path);
            if (!typeConfigRecord.Equals(default))
            {
                workContext.CurrentType = typeConfigRecord.Type;
            }
            await _next(context);
        }
        private TypeConfigRecord GetRouteMap(PathString path)
        {
            if (RouteMaps.TryGetValue(path, out TypeConfigRecord value))
                return value;

            value = RouteMapper.TypeConfigRecords.FirstOrDefault(r => path.StartsWithSegments("/" + r.RoutePrefix, StringComparison.CurrentCultureIgnoreCase));

            return (RouteMaps[path] = value);
        }
    }
}
