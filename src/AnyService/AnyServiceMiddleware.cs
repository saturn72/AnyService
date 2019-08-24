using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AnyService
{
    public class AnyServiceMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly KeyValuePair<string, Type> DefaultKeyValuePair = new KeyValuePair<string, Type>();
        private static readonly PathString AnyServicePath = new PathString("/" + Consts.AnyServiceControllerName);
        public AnyServiceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AnyServiceWorkContext workContext, AnyServiceRouteMapper routeMapper)
        {
            var request = context.Request;
            var path = request.Path;
            var stringToTypePair = routeMapper.Maps.FirstOrDefault(r => path.StartsWithSegments(r.Key, StringComparison.CurrentCultureIgnoreCase));
            if (!stringToTypePair.Equals(DefaultKeyValuePair))
            {
                workContext.CurrentType = stringToTypePair.Value;
                workContext.CurrentUserId = "some-user-id";
                request.Path = AnyServicePath.Add(path);
            }
            await _next(context);
        }
    }
}
