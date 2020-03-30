using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;

namespace AnyService.Middlewares
{
    public sealed class DefaultAuthorizationMiddleware
    {
        internal static bool ShouldUseMiddleware { get; set; }
        private readonly ILogger<DefaultAuthorizationMiddleware> _logger;
        private readonly RequestDelegate _next;
        private static readonly IDictionary<string, Func<ClaimsPrincipal, bool>> AuthorizationWorkers
            = new Dictionary<string, Func<ClaimsPrincipal, bool>>(StringComparer.CurrentCultureIgnoreCase);
        private static readonly IReadOnlyDictionary<string, Func<AuthorizationInfo, AuthorizationNode>> HttpMethodToAuthorizationNode = new Dictionary<string, Func<AuthorizationInfo, AuthorizationNode>>(StringComparer.CurrentCultureIgnoreCase)
        {
            { "post", ai => ai.PostAuthorizeNode},
            { "get",ai =>ai.GetAuthorizeNode},
            { "put",ai => ai.PutAuthorizeNode},
            { "delete",ai =>  ai.DeleteAuthorizeNode},
        };
        private static readonly object lockObj = new object();
        public DefaultAuthorizationMiddleware(RequestDelegate next, ILogger<DefaultAuthorizationMiddleware> logger)
        {
            _logger = logger;
            _next = next;
        }
        public async Task InvokeAsync(HttpContext httpContext, WorkContext workContext)
        {
            var entityConfig = workContext.CurrentEntityConfigRecord;
            if (entityConfig.Authorization == null)
            {
                await _next(httpContext);
                return;
            }
            var currentHttpMethod = httpContext.Request.Method;
            var key = $"{entityConfig.Route}_{currentHttpMethod}";
            if (!AuthorizationWorkers.TryGetValue(key, out Func<ClaimsPrincipal, bool> worker))
            {
                var an = HttpMethodToAuthorizationNode[currentHttpMethod](entityConfig.Authorization);
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
        }
    }
}