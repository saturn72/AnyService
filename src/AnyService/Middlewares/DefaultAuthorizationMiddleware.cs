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
    public sealed class DefaultAuthorizationMiddleware
    {
        private readonly ILogger<DefaultAuthorizationMiddleware> _logger;
        private readonly RequestDelegate _next;
        private static readonly IDictionary<string, Func<ClaimsPrincipal, bool>> AuthorizationWorkers
            = new Dictionary<string, Func<ClaimsPrincipal, bool>>(StringComparer.CurrentCultureIgnoreCase);
        private static readonly IReadOnlyDictionary<string, Func<AuthorizationInfo, AuthorizationNode>> HttpMethodToAuthorizationNode = new Dictionary<string, Func<AuthorizationInfo, AuthorizationNode>>(StringComparer.CurrentCultureIgnoreCase)
        {
            { "post", ai => ai.PostAuthorizationNode},
            { "get",ai =>ai.GetAuthorizationNode},
            { "put",ai => ai.PutAuthorizationNode},
            { "delete",ai =>  ai.DeleteAuthorizationNode},
        };
        private static readonly object lockObj = new object();
        public DefaultAuthorizationMiddleware(RequestDelegate next, ILogger<DefaultAuthorizationMiddleware> logger)
        {
            _logger = logger;
            _next = next;
        }
        public async Task InvokeAsync(HttpContext httpContext, WorkContext workContext)
        {
            _logger.LogDebug(LoggingEvents.Authorization, "Start middleware invokation");
            var entityConfig = workContext?.CurrentEntityConfigRecord;
            if (entityConfig?.ControllerSettings?.Authorization == null)
            {
                await _next(httpContext);
                return;
            }
            var currentHttpMethod = httpContext.Request.Method;
            var key = $"{entityConfig.ControllerSettings.Route}_{currentHttpMethod}";
            if (!AuthorizationWorkers.TryGetValue(key, out Func<ClaimsPrincipal, bool> worker))
            {
                var an = HttpMethodToAuthorizationNode[currentHttpMethod](entityConfig.ControllerSettings.Authorization);
                worker = cp => an.Roles.Any(r => cp.IsInRole(r));
                lock (lockObj)
                {
                    AuthorizationWorkers[key] = worker;
                }
            }

            if (!worker(httpContext.User))
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            await _next(httpContext);
            _logger.LogDebug(LoggingEvents.Authorization, "End middleware invokation");

        }
    }
}
