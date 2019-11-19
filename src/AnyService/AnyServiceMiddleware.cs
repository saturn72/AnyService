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

        public async Task InvokeAsync(HttpContext context, WorkContext workContext, RouteMapper routeMapper)
        {
            workContext.CurrentUserId = "some-user-id";

            var path = context.Request.Path;
            var typeConfigRecord = GetRouteMap(path, routeMapper);
            if (!typeConfigRecord.Equals(default))
            {
                workContext.CurrentType = typeConfigRecord.Type;

                if (!path.StartsWithSegments(typeConfigRecord.ControllerRoute, StringComparison.InvariantCultureIgnoreCase))
                    context.Request.Path = path.Value.Replace(typeConfigRecord.RoutePrefix, typeConfigRecord.ControllerRoute, StringComparison.InvariantCultureIgnoreCase);
            }
            await _next(context);
        }
        private TypeConfigRecord GetRouteMap(PathString path, RouteMapper routeMapper)
        {
            if (RouteMaps.TryGetValue(path, out TypeConfigRecord value))
                return value;
            value = routeMapper.Maps.FirstOrDefault(r => path.StartsWithSegments(r.RoutePrefix, StringComparison.CurrentCultureIgnoreCase));

            return (RouteMaps[path] = value);
        }
    }
}
