using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Middlewares
{
    public class OnMissingUserIdWorkContextMiddlewareHandlers
    {
        public static Task<bool> DefaultOnMissingUserIdHandler(HttpContext context, WorkContext workContext, ILogger logger)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            logger.LogDebug($"Missing userId and clientId - request could not be authenticated.");
            return Task.FromResult(false);
        }

        public static Func<HttpContext, WorkContext, ILogger, Task<bool>> PermittedPathsOnMissingUserIdHandler(IEnumerable<PathString> authNotRequiredPaths)
        {
            return (ctx, wc, l) =>
            {
                if (authNotRequiredPaths.Any(a => ctx.Request.Path.StartsWithSegments(a)))
                    return Task.FromResult(true);

                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                l.LogDebug($"Missing userId - user is unauthorized!");
                return Task.FromResult(false);
            };
        }
    }
}