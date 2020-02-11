﻿using System;
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
        private static readonly IDictionary<string, EntityConfigRecord> RouteMaps = new Dictionary<string, EntityConfigRecord>();
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
                workContext.CurrentEntityConfigRecord = typeConfigRecord;
                workContext.RequestInfo = ToRequestInfo(httpContext, httpContext.Request.Method, typeConfigRecord);
            }
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
                RequesteeId = GetRequesteeId(),
                Parameters = httpContext.Request.Query.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value)).ToArray()
            };
            string GetRequesteeId() => path.Split("/", StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault(p => !p.StartsWith("__"));
        }
    }
}