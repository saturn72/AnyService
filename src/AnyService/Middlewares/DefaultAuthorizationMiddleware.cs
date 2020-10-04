using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AnyService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AnyService.Middlewares
{
    public sealed class DefaultAuthorizationMiddleware
    {
        private readonly ILogger<DefaultAuthorizationMiddleware> _logger;
        private readonly RequestDelegate _next;
        private static readonly ConcurrentDictionary<string, Func<ClaimsPrincipal, bool>> AuthorizationWorkers
            = new ConcurrentDictionary<string, Func<ClaimsPrincipal, bool>>(StringComparer.CurrentCultureIgnoreCase);
        private static readonly IReadOnlyDictionary<string, Func<EndpointSettings, AuthorizeAttribute>> HttpMethodToAuthorizeAttribute = new Dictionary<string, Func<EndpointSettings, AuthorizeAttribute>>(StringComparer.CurrentCultureIgnoreCase)
        {
            { "post", es => es.PostSettings.Authorization},
            { "get",es =>es.GetSettings.Authorization },
            { "put",es => es.PutSettings.Authorization},
            { "delete",es =>  es.DeleteSettings.Authorization},
        };
        public DefaultAuthorizationMiddleware(RequestDelegate next, ILogger<DefaultAuthorizationMiddleware> logger)
        {
            _logger = logger;
            _next = next;
        }
        public async Task InvokeAsync(HttpContext httpContext, WorkContext workContext)
        {
            _logger.LogInformation(LoggingEvents.Authorization, $"Start {nameof(DefaultAuthorizationMiddleware)} invokation");
            var ecr = workContext?.CurrentEntityConfigRecord;

            if (ecr == null)
            {
                _logger.LogDebug(LoggingEvents.Authorization, $"No {nameof(EntityConfigRecord)} found in current {nameof(WorkContext)} - invokes {nameof(_next)}");

                await _next(httpContext);
                return;
            }

            var currentHttpMethod = httpContext.Request.Method;
            var key = $"{ecr.EndpointSettings.Route}_{currentHttpMethod}";
            if (!AuthorizationWorkers.TryGetValue(key, out Func<ClaimsPrincipal, bool> worker))
            {
                var aa = HttpMethodToAuthorizeAttribute[currentHttpMethod](ecr.EndpointSettings);
                worker = BuildAuthorizeLogic(aa);
                AuthorizationWorkers.TryAdd(key, worker);
            }

            if (!worker(httpContext.User))
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            await _next(httpContext);
            _logger.LogDebug(LoggingEvents.Authorization, "End middleware invokation");
        }

        private Func<ClaimsPrincipal, bool> BuildAuthorizeLogic(AuthorizeAttribute aa)
        {
            if (aa == null)
                return cp => true;

            if (aa.Policy.HasValue())
                throw new NotImplementedException();
            var roles = aa.Roles.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());

            return cp => roles.Any(r => cp.IsInRole(r));
        }
    }
}
