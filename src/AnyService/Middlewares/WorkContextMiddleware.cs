﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AnyService.Http;
using AnyService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnyService.Middlewares
{
    public class WorkContextMiddleware
    {
        private const string MapKeyFormat = "{0}_{1}";
        private readonly ILogger<WorkContextMiddleware> _logger;
        private readonly Func<HttpContext, WorkContext, ILogger, Task<bool>> _onMissingUserIdOrClientIdHandler;
        private readonly RequestDelegate _next;
        protected readonly IReadOnlyDictionary<string, EndpointSettings> RouteEndpointSettingsMaps;
        protected readonly IReadOnlyDictionary<string, bool> ActivationMaps;

        public WorkContextMiddleware(
            RequestDelegate next,
            IEnumerable<EndpointSettings> endpointSettings,
            ILogger<WorkContextMiddleware> logger,
            Func<HttpContext, WorkContext, ILogger, Task<bool>> onMissingUserIdHandler = null
            )
        {
            _logger = logger;
            _next = next;
            RouteEndpointSettingsMaps = LoadRoutesEndpointSettingsMap(endpointSettings);
            ActivationMaps = ToActivationMaps(endpointSettings);
            _onMissingUserIdOrClientIdHandler = onMissingUserIdHandler ??= OnMissingUserIdWorkContextMiddlewareHandlers.DefaultOnMissingUserIdHandler;
        }

        public async Task InvokeAsync(HttpContext httpContext, WorkContext workContext)
        {
            _logger.LogInformation(LoggingEvents.WorkContext, $"Start {nameof(WorkContextMiddleware)} invokation");
            if (!await HttpContextToWorkContext(httpContext, workContext))
                return;

            var es = GetEndpointSettingsByRoute(httpContext.Request.Path);
            if (es != null)
            {
                workContext.CurrentEndpointSettings = es;
                workContext.RequestInfo = ToRequestInfo(httpContext, es);
                if (!MethodActive(workContext))
                {
                    httpContext.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    return;
                }
            }
            _logger.LogDebug(LoggingEvents.WorkContext, "Finish parsing current WorkContext");
            await _next(httpContext);
        }
        protected async Task<bool> HttpContextToWorkContext(HttpContext httpContext, WorkContext workContext)
        {
            _logger.LogDebug(LoggingEvents.WorkContext, $"Extract headers");
            workContext.SessionId = httpContext.Request.Headers[HttpHeaderNames.ClientSessionId];
            _logger.LogDebug(LoggingEvents.WorkContext, $"header: {HttpHeaderNames.ClientSessionId} with value {workContext.SessionId}");
            workContext.ReferenceId = httpContext.Request.Headers[HttpHeaderNames.ClientRequestReference];
            _logger.LogDebug(LoggingEvents.WorkContext, $"header: {HttpHeaderNames.ClientRequestReference} with value {workContext.ReferenceId}");

            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogDebug(LoggingEvents.WorkContext, $"UserId = {userId}");
            var clientId = httpContext.User.FindFirst("client_id")?.Value;
            _logger.LogDebug(LoggingEvents.WorkContext, $"ClientId = {clientId}");

            if (!clientId.HasValue() && !userId.HasValue() && !(await _onMissingUserIdOrClientIdHandler(httpContext, workContext, _logger)))
            {
                _logger.LogDebug(LoggingEvents.WorkContext, $"breaks middleware execution");
                return false;
            }
            workContext.CurrentUserId = userId;
            workContext.CurrentClientId = clientId;
            workContext.IpAddress = httpContext.Connection?.RemoteIpAddress?.ToString();
            return true;
        }

        private bool MethodActive(WorkContext workContext)
        {
            _logger.LogInformation(LoggingEvents.WorkContext,
                $"Validate HttpMethod is active for {nameof(EntityConfigRecord)}. {nameof(RequestInfo)}: {workContext.RequestInfo.Method}");

            var key = string.Format(MapKeyFormat, workContext.CurrentEndpointSettings.Name, workContext.RequestInfo.Method);
            return ActivationMaps[key];
        }
        private IReadOnlyDictionary<string, EndpointSettings> LoadRoutesEndpointSettingsMap(IEnumerable<EndpointSettings> endpointSettings)
        {
            var res = new Dictionary<string, EndpointSettings>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var es in endpointSettings)
                res[es.Route] = es;
            return res;
        }
        private IReadOnlyDictionary<string, bool> ToActivationMaps(IEnumerable<EndpointSettings> endpointSettings)
        {
            var res = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var es in endpointSettings)
            {
                var name = es.Name;
                res[string.Format(MapKeyFormat, name, "post")] = es.PostSettings.Active;
                res[string.Format(MapKeyFormat, name, "get")] = es.GetSettings.Active;
                res[string.Format(MapKeyFormat, name, "put")] = es.PutSettings.Active;
                res[string.Format(MapKeyFormat, name, "delete")] = es.DeleteSettings.Active;
            }
            return res;
        }
        private EndpointSettings GetEndpointSettingsByRoute(PathString path)
        {
            var esRoute = RouteEndpointSettingsMaps.FirstOrDefault(rm => path.StartsWithSegments(rm.Key));

            var res = (esRoute.Equals(default)) ? null : esRoute.Value;
            _logger.LogDebug(LoggingEvents.WorkContext,
                res != null ?
                    $"{nameof(EndpointSettings)} found: {res.Name}. using path: {path}" :
                    $"Entity is not found in anyservice's configured {nameof(RouteEndpointSettingsMaps)}. Path used: {path}. If this is not expected, please verify entity was configured when calling {nameof(ServiceCollectionExtensions.AddAnyService)} method.");
            return res;
        }
        private RequestInfo ToRequestInfo(HttpContext httpContext, EndpointSettings es)
        {
            var path = httpContext.Request.Path.ToString();
            _logger.LogDebug(LoggingEvents.WorkContext, $"Parse httpRequestInfo from route: {path}");
            var reqInfo = new RequestInfo
            {
                Path = path,
                Method = httpContext.Request.Method,
                RequesteeId = GetRequesteeId(es.Route, path),
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
