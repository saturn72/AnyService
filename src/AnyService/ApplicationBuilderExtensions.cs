using AnyService.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace AnyService
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseAnyService(this IApplicationBuilder app)
        {
            app.UseMiddleware<AnyServiceWorkContextMiddleware>();
            app.UseMiddleware<AnyServicePermissionMiddleware>();

            return app;
        }
    }
}
