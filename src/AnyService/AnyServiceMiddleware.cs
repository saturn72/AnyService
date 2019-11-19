using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AnyService
{
    public class AnyServiceMiddleware
    {
        private readonly RequestDelegate _next;
        public AnyServiceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, WorkContext workContext, RouteMapper routeMapper)
        {
            workContext.CurrentUserId = "some-user-id";

            var path = context.Request.Path;
            var stringToTypePair = routeMapper.Maps.FirstOrDefault(r => path.StartsWithSegments(r.Key, StringComparison.CurrentCultureIgnoreCase));
            if (!stringToTypePair.Equals(default))
            {
                workContext.CurrentType = stringToTypePair.Value;
            }
            await _next(context);
        }
    }
}
